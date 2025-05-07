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
using Unity.Sentis;

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
    [SerializeField] private BufferSensorComponent detectedEnemiesSensor;
    [SerializeField] private BufferSensorComponent teammateSensor;

    public DemonstrationRecorder? demonstrationRecorder;

    private EnvController envController;

    BehaviorParameters m_BehaviorParameters;
    RayPerceptionSensorComponent3D aimSensor = null;
    //BufferSensorComponent detectedEnemiesBufferSensor = null;

    private float health = 100;

    private float RegenHealthCooldown = 0;

    public bool inCT = false;

    private float DistanceToCT = 1000;
    private float DistanceToNearestFriendly = 1;
    private bool gotCloserToCT = false;

    public Team team { get; set; }

    //public UnityEvent DiedEvent;
    //public UnityEvent RespawnEvent;

    public override void Initialize()
    {
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        envController = this.transform.parent.GetComponent<EnvController>();

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
        if(health == 0)
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0.0f);
        }
        else
        {
            sensor.AddObservation(Vector3.Normalize(transform.InverseTransformVector(envController.GetCTPosition() - transform.localPosition)));
            DistanceToCT = Vector3.Distance(envController.GetCTPosition(), transform.localPosition) / 700.0f;
            sensor.AddObservation(DistanceToCT);
        }


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
        //float stateNum = team == Team.Red ? (envController.RedTeamPoints - envController.YellowTeamPoints) * 0.01f : (envController.YellowTeamPoints - envController.RedTeamPoints) * 0.01f;

        sensor.AddObservation(stateNum /= 10);

        Dictionary<GameObject, float> detectedEnemies = team == Team.Red ? envController.m_DetectedYellowEnemies : envController.m_DetectedRedEnemies;

        //BufferSensor

        if(health != 0)
        {
                foreach (var agent in detectedEnemies.Keys.ToList())
                {
                Vector3 dir = Vector3.Normalize(transform.InverseTransformDirection(agent.transform.localPosition - transform.localPosition));
                float dist = Vector3.Distance(agent.transform.localPosition, transform.localPosition) / 700.0f;
                float health = agent.GetComponent<TankAgent>().getHealth() * 0.01f;

                float[] Obs = { dir.x, dir.y, dir.z, dist, health, 0.0f };
                detectedEnemiesSensor.AppendObservation(Obs);
                }


                foreach (var agent in envController.AgentsList)
                {
                    if (agent.team != this.team || agent.memberID == this.memberID)
                        continue;

                    if (agent.health == 0)
                        continue;

                    Vector3 dir = Vector3.Normalize(transform.InverseTransformDirection(agent.transform.localPosition - transform.localPosition));
                    float dist = Vector3.Distance(agent.transform.localPosition, transform.localPosition) / 700.0f;
                    float health = agent.getHealth() * 0.01f;

                    float[] Obs = { dir.x, dir.y, dir.z, dist, health, 0.0f };
                    teammateSensor.AppendObservation(Obs);
                }
            
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (health == 0.0f)
            return;


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

        //Existential penalty
        //if(envController.ctState == (team == Team.Red ? CTState.Yellow : CTState.Red))
        //    AddReward(-1.0f * (Time.fixedDeltaTime / envController.timeLimit));
        //else
        //    AddReward(-0.5f * (Time.fixedDeltaTime / envController.timeLimit));


    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if(health == 0.0f) return;
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
            //AddReward(-0.01f);
        }
    }


    public void FixedUpdate()
    {
        if (health == 0.0f) return;
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
        if (health <= 0)
        {
            //AddReward(-0.1f);
            setDeadState();
        }
    }

    public void ResetAgent()
    {
        //RespawnEvent.Invoke();
        health = 100.0f;
        //this.memberID = UnityEngine.Random.Range(0, 5);

        gameObject.tag = team == Team.Red ? "RedAgent" : "YellowAgent";
        tankController.setStartingState((int)team, memberID);
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

    public void setDeadState()
    {
        if (GetComponent<BehaviorParameters>().BehaviorType != BehaviorType.Default)
        {
            Transform deadTankTransform = GameObject.Instantiate(DeadTankPrefab);
            DeadTankScript deadTank = deadTankTransform.gameObject.GetComponent<DeadTankScript>();
            deadTank.setTransform(tankController);
        }

        tankController.setDeadState();

        //DiedEvent.Invoke();
        if (inCT)
        {
            inCT = false;
            envController.AgentExitedCT(team);
        }
        health = 0;
        envController.AgentDied(this);
        gameObject.tag = "Untagged";
    }
}
