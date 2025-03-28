using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using System.Linq;
using UnityEngine.Events;
using Unity.VisualScripting;
using NUnit.Framework;
using System.Text;
using Unity.MLAgents.Demonstrations;

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
    [SerializeField] private VectorSensorComponent detectedEnemiesSensor;
    //[SerializeField] private BufferSensorComponent detectedEnemiesBufferSensor;
    [SerializeField] private VectorSensorComponent teammateSensor;

    public DemonstrationRecorder? demonstrationRecorder;

    private EnvController envController;

    BehaviorParameters m_BehaviorParameters;
    RayPerceptionSensorComponent3D aimSensor = null;
    //BufferSensorComponent detectedEnemiesSensor = null;

    private float health = 100;

    private float RegenHealthCooldown = 0;

    public bool inCT = false;

    private float DistanceToCT = 1000;
    private float DistanceToNearestFriendly = 1;

    public Team team { get; set; }

    //public UnityEvent DiedEvent;
    //public UnityEvent RespawnEvent;

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

        //if (team == Team.Red)
        //    detectedEnemiesBufferSensor = GetComponent<BufferSensorComponent>();
    }

    public override void OnEpisodeBegin()
    {
        ResetAgent();
    }

    public override void CollectObservations(VectorSensor sensor)
    {

        sensor.AddObservation(Vector3.Dot(transform.forward, tankRB.linearVelocity) / 30.0f);
        sensor.AddObservation(tankController.coolDownTime / 3.0f);
        sensor.AddObservation(health / 100.0f);
        sensor.AddObservation(transform.InverseTransformVector(tankCannon.forward));
        sensor.AddObservation(Vector3.Normalize(transform.InverseTransformVector(envController.GetCTPosition() - transform.localPosition)));
        DistanceToCT = Vector3.Distance(envController.GetCTPosition(), transform.localPosition) / 700.0f;
        sensor.AddObservation(DistanceToCT);
        sensor.AddObservation(0);
        sensor.AddObservation(envController.m_ResetTimer / (float)envController.timeLimit);

        if (team == Team.Red)
        {
            sensor.AddObservation(envController.RedTeamPoints * 0.01f);
            sensor.AddObservation(envController.YellowTeamPoints * 0.01f);
        }
        else
        {
            sensor.AddObservation(envController.YellowTeamPoints * 0.01f);
            sensor.AddObservation(envController.RedTeamPoints * 0.01f);
        }



        float stateNum = team == Team.Red ? -1 * envController.getStateNum() : envController.getStateNum();
        stateNum /= 10;


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
        sensor.AddObservation(stateNum);

        Dictionary<GameObject, float> detectedEnemies = team == Team.Red ? envController.m_DetectedYellowEnemies : envController.m_DetectedRedEnemies;

        //VectorSensor

        for (int i = 0; i < 5; i++)
        {
            if (detectedEnemies.Keys.Any(key => key.gameObject.GetComponent<TankAgent>()?.memberID == i))
            {
                var agent = detectedEnemies.Keys.First(key => key.gameObject.GetComponent<TankAgent>()?.memberID == i);
                Vector3 dir = Vector3.Normalize(transform.InverseTransformDirection(agent.transform.localPosition - transform.localPosition));
                float dist = Vector3.Distance(agent.transform.localPosition, transform.localPosition) / 700.0f;

                detectedEnemiesSensor.GetSensor().AddObservation(dir);
                detectedEnemiesSensor.GetSensor().AddObservation(dist);
                detectedEnemiesSensor.GetSensor().AddObservation(agent.GetComponent<TankAgent>().getHealth() * 0.01f);
            }
            else
            {
                detectedEnemiesSensor.GetSensor().AddObservation(0.0f);
                detectedEnemiesSensor.GetSensor().AddObservation(0.0f);
                detectedEnemiesSensor.GetSensor().AddObservation(0.0f);
                detectedEnemiesSensor.GetSensor().AddObservation(0.0f);
                detectedEnemiesSensor.GetSensor().AddObservation(0.0f);
            }
        }

        //for (int i = 0; i < 20; i++)
        //{
        //    teammateSensor.GetSensor().AddObservation(0.0f);
        //}

        DistanceToNearestFriendly = 1.0f;
        foreach (TankAgent agent in envController.AgentsList)
        {
            if (agent.team != team)
                continue;

            if (agent.memberID == memberID)
                continue;

            Vector3 dir = Vector3.Normalize(transform.InverseTransformDirection(agent.transform.localPosition - transform.localPosition));
            float dist = Vector3.Distance(agent.transform.localPosition, transform.localPosition) / 700.0f;

            if(dist < DistanceToNearestFriendly) DistanceToNearestFriendly = dist;

            teammateSensor.GetSensor().AddObservation(dir);
            teammateSensor.GetSensor().AddObservation(dist);
            teammateSensor.GetSensor().AddObservation(agent.GetComponent<TankAgent>().getHealth() * 0.01f);

        }




        //BufferSensor
        //if (team == Team.Red)
        //{
        //    foreach (var agent in detectedEnemies.Keys.ToList())
        //    {
        //        Vector3 dir = Vector3.Normalize(transform.InverseTransformDirection(agent.transform.localPosition - transform.localPosition));
        //        float dist = Vector3.Distance(agent.transform.localPosition, transform.localPosition) / 700.0f;

        //        float[] Obs = { dir.x, dir.y, dir.z };
        //        //if(this.team == Team.Yellow)
        //        //    Debug.Log("(" + dir.x + ", " + dir.y + ", " + dir.z + ")" + "dist: " + dist + " | health: " + agent.GetComponent<TankAgent>().getHealth());
        //        detectedEnemiesBufferSensor.AppendObservation(Obs);

        //    }
        //}
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

        //if (envController.ctState == CTState.Neutral && !inCT)
        //{
        //    AddReward(-0.5f / MaxStep);
        //}
        //else if (envController.ctState == CTState.Neutral && inCT)
        //{
        //    AddReward(2.0f / MaxStep);
        //}
        //else
        //{
        //    AddReward(0.5f / MaxStep);
        //}
        //AddReward(-1 / MaxStep);

        //Dictionary<GameObject, float> detectedEnemies = team == Team.Red ? envController.m_DetectedYellowEnemies : envController.m_DetectedRedEnemies;
        //if (detectedEnemies.Count > 0)
        //{
        //    Vector3 dir = Vector3.Normalize(transform.InverseTransformDirection(detectedEnemies.Keys.ToList().First().transform.localPosition - transform.localPosition));
        //    //if (this.team == Team.Yellow)
        //    //    Debug.Log(Vector3.Dot(transform.InverseTransformDirection(tankCannon.forward), dir));

        //    this.AddReward(Vector3.Dot(transform.InverseTransformDirection(tankCannon.forward), dir) * 0.1f * Time.fixedDeltaTime / envController.timeLimit);
        //}

        //if ((envController.ctState == CTState.Red && team == Team.Yellow)
        //|| (envController.ctState == CTState.Yellow && team == Team.Red)
        //|| (envController.ctState == CTState.Neutral))
        //{
        //    if (inCT)
        //        AddReward((Time.fixedDeltaTime * 10.0f) / envController.timeLimit);
        //}


        if(DistanceToCT < 0.2f)
            AddReward(0.3f * (Time.fixedDeltaTime / (float)envController.timeLimit));
        else
            AddReward(-((DistanceToCT - 0.2f) * (Time.fixedDeltaTime / (float)envController.timeLimit)));

        if (DistanceToNearestFriendly < 0.1f)
            AddReward(0.2f * (Time.fixedDeltaTime / (float)envController.timeLimit));
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
            health = Mathf.Min(100, health + Time.fixedDeltaTime * 10.0f);
        }

        RayPerceptionInput spec = aimSensor.GetRayPerceptionInput();
        RayPerceptionOutput obs = RayPerceptionSensor.Perceive(spec, false);
        if (obs.RayOutputs[0].HitTagIndex == 0)
        {
            envController.EnemyDetected(obs.RayOutputs[0].HitGameObject.transform.parent.gameObject, this.team);
            //envController.EnemyDetected(obs.RayOutputs[0].HitGameObject, this.team);
        }
    }


    public void Hit(int damage)
    {
        health = Mathf.Max(health - damage, 0);
        RegenHealthCooldown = 10.0f;
        AddReward(-0.01f);
        if(health <= 0)
        {
            if (GetComponent<BehaviorParameters>().BehaviorType != BehaviorType.Default)
            {
                Transform deadTankTransform = GameObject.Instantiate(DeadTankPrefab);
                DeadTankScript deadTank = deadTankTransform.gameObject.GetComponent<DeadTankScript>();
                deadTank.setTransform(tankController);
            }

            //DiedEvent.Invoke();
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

        //RespawnEvent.Invoke();
        //this.memberID = Random.Range(0, 5);
        tankController.setStartingState((int)team, memberID);
        health = 100;
        if (inCT)
        {
            inCT = false;
            envController.AgentExitedCT(team);
        }
        //envController.resetCT();
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
