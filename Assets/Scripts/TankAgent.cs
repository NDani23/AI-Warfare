using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using System.Linq;
using UnityEngine.Events;

public enum Team
{
    Red = 0,
    Yellow = 1
}

public class TankAgent : Agent
{
    [SerializeField] private TankController tankController;
    [SerializeField] private InputController inputController;
    [SerializeField] private Rigidbody tankRB;
    [SerializeField] private Transform tankCannon;
    [SerializeField] private int memberID;
    [SerializeField] private Transform DeadTankPrefab;

    private EnvController envController;

    BehaviorParameters m_BehaviorParameters;
    RayPerceptionSensorComponent3D aimSensor = null;
    BufferSensorComponent detectedEnemiesSensor = null;

    private float health = 100;

    private float RegenHealthCooldown = 0;

    public bool inCT = false;

    public Team team { get; set; }

    public UnityEvent DiedEvent;
    public UnityEvent RespawnEvent;

    public override void Initialize()
    {
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        envController = GetComponentInParent<EnvController>();

        if (m_BehaviorParameters.TeamId == (int)Team.Red)
        {
            team = Team.Red;
        }
        else if (m_BehaviorParameters.TeamId == (int)Team.Yellow)
        {
            team = Team.Yellow;
        }

        var c = GetComponentsInChildren<RayPerceptionSensorComponent3D>();
        for (int i = 0; i < c.Length; i++)
        {
            if (c[i].SensorName == "AimSensor")
            {
                aimSensor = c[i];
                break;
            }
                
        }
        detectedEnemiesSensor = GetComponent<BufferSensorComponent>();
    }

    public override void OnEpisodeBegin()
    {
        ResetAgent();
    }

    public override void CollectObservations(VectorSensor sensor)
    {

        sensor.AddObservation(Vector3.Dot(transform.forward, tankRB.velocity) / 30.0f);
        sensor.AddObservation(tankController.coolDownTime / 3.0f);
        sensor.AddObservation(health / 100.0f);
        sensor.AddObservation(transform.InverseTransformVector(tankCannon.forward));
        sensor.AddObservation(transform.InverseTransformVector(Vector3.Normalize(envController.GetCTPosition() - transform.localPosition)));
        sensor.AddObservation(Vector3.Distance(envController.GetCTPosition(), transform.localPosition) / 700.0f);

        float stateNum = team == Team.Red ? -1 * envController.getStateNum() : envController.getStateNum();


        CTState currentState = envController.ctState;
        int stateObs = 0;
        switch (currentState)
        {
            case CTState.Red:
                stateObs = team == Team.Red ? 1 : -1;
                break;
            case CTState.Yellow:
                stateObs = team == Team.Yellow ? 1 : -1;
                break;

        }
        sensor.AddObservation(stateObs);

        Dictionary<GameObject, float> detectedEnemies = team == Team.Red ? envController.m_DetectedYellowEnemies : envController.m_DetectedRedEnemies;
        foreach (var agent in detectedEnemies.Keys.ToList())
        {
            Vector3 dir = transform.InverseTransformVector(Vector3.Normalize(agent.transform.localPosition - transform.localPosition));
            float[] dirObs = { dir.x, dir.y, dir.z };
            detectedEnemiesSensor.AppendObservation(dirObs);
        }

    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        tankController.Throttle = actions.DiscreteActions[0]-1;

        if (actions.DiscreteActions[1] - 1 == 0 || (actions.DiscreteActions[1] - 1) * tankController.Steer < 0)
        {
            tankController.Steer = 0;
        }
        else
        {
            tankController.Steer = Mathf.Lerp(tankController.Steer, actions.DiscreteActions[1] - 1, 0.1f);
        }


        tankController.HorizontalAimInput = actions.ContinuousActions[0];
        tankController.VerticalAimInput = actions.DiscreteActions[3] - 1;
        tankController.FireInput = actions.DiscreteActions[2];
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = inputController.ThrottleInput + 1;
        discreteActions[1] = inputController.SteerInput + 1;
        discreteActions[2] = inputController.FireInput ? 1 : 0;
        tankController.AimDirection = inputController.AimDirInput;
        inputController.FireInput = false;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag != "Bullet")
        {
            AddReward(-0.01f);
        }
    }


    public void FixedUpdate()
    {
        if (RegenHealthCooldown != 0) RegenHealthCooldown = Mathf.Max(0, RegenHealthCooldown - Time.fixedDeltaTime);

        if(health != 100 && RegenHealthCooldown == 0)
        {
            health = Mathf.Min(100, health + Time.fixedDeltaTime * 5.0f);
        }

        RayPerceptionInput spec = aimSensor.GetRayPerceptionInput();
        RayPerceptionOutput obs = RayPerceptionSensor.Perceive(spec);
        if (obs.RayOutputs[0].HitTagIndex == 0)
        {
             envController.EnemyDetected(obs.RayOutputs[0].HitGameObject.transform.parent.gameObject, this.team);
        }
    }


    public void Hit(int damage)
    {
        health = Mathf.Max(health - damage, 0);
        RegenHealthCooldown = 15.0f;
        AddReward(-0.005f);
        if(health <= 0)
        {
            Transform deadTankTransform = GameObject.Instantiate(DeadTankPrefab);
            DeadTankScript deadTank = deadTankTransform.gameObject.GetComponent<DeadTankScript>();
            deadTank.setTransform(tankController);

            DiedEvent.Invoke();
            if (inCT)
            {
                inCT = false;
                envController.AgentExitedCT(team);
            }
            envController.AgentDied(this);

        }
    }

    public void ResetAgent()
    {

        RespawnEvent.Invoke();
        tankController.setStartingState((int)team, memberID);
        health = 100;
        inCT = false;
    }

    public Vector2 GetScreenSpaceAimPos()
    {
        return Camera.main.WorldToScreenPoint(tankController.GetAimPos());
    }

    public float getHealth()
    {
       return health;
    }

    public float getCooldown()
    {
        return tankController.coolDownTime;
    }
}
