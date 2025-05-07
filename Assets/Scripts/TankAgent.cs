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



public class TankAgent : Agent, IVehicleAgent
{
    [SerializeField] private TankController tankController;
    [SerializeField] private Rigidbody tankRB;
    [SerializeField] private Transform tankCannon;
    [SerializeField] private uint memberID;
    [SerializeField] private Transform DeadTankPrefab;
    [SerializeField] private BufferSensorComponent detectedEnemiesSensor;
    [SerializeField] private BufferSensorComponent teammateSensor;

    public DemonstrationRecorder? demonstrationRecorder;

    BehaviorParameters m_BehaviorParameters;
    RayPerceptionSensorComponent3D aimSensor = null;
    //BufferSensorComponent detectedEnemiesBufferSensor = null;

    private float health = 100;

    private float RegenHealthCooldown = 0;

    public bool inCT = false;

    private float DistanceToCT = 1000;

    public UnityEvent DiedEvent;
    public UnityEvent RespawnEvent;

    private Team _team;
    public Team Team => _team;

    public uint MemberID => memberID;

    private float _health = 100;
    public float Health => _health;

    private EnvController _envController;
    public EnvController EnvController => _envController;
    public GameObject GameObject => gameObject;

    public override void Initialize()
    {
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        _envController = GetComponentInParent<EnvController>();

        if (m_BehaviorParameters.TeamId == (int)Team.Red)
        {
            _team = Team.Red;
        }
        else if (m_BehaviorParameters.TeamId == (int)Team.Yellow)
        {
            _team = Team.Yellow;
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
        sensor.AddObservation(_health / 100.0f);
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
        sensor.AddObservation(_envController.m_ResetTimer / (float)_envController.timeLimit);

        if (_team == Team.Red)
        {
            sensor.AddObservation(_envController.RedTeamPoints * 0.01f);
            sensor.AddObservation(_envController.YellowTeamPoints * 0.01f);
        }
        else
        {
            sensor.AddObservation(_envController.YellowTeamPoints * 0.01f);
            sensor.AddObservation(_envController.RedTeamPoints * 0.01f);
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

    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if(health == 0.0f) return;
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;

        var aimDirection = Input.mousePosition;
        aimDirection.z = 500.0f;

        tankController.AimDirection = Camera.main.ScreenToWorldPoint(aimDirection);
        discreteActions[0] = Input.GetKey(KeyCode.W) ? 2 : (Input.GetKey(KeyCode.S) ? 0 : 1);
        discreteActions[1] = Input.GetKey(KeyCode.D) ? 2 : (Input.GetKey(KeyCode.A) ? 0 : 1);
        discreteActions[2] = Input.GetMouseButton(0) || Input.GetKeyDown("space") ? 1 : 0;
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

        if(_health != 100 && RegenHealthCooldown == 0)
        {
            _health = Mathf.Min(100, _health + Time.fixedDeltaTime * 10.0f);
        }

        RayPerceptionInput spec = aimSensor.GetRayPerceptionInput();
        RayPerceptionOutput obs = RayPerceptionSensor.Perceive(spec, false);
        if (obs.RayOutputs[0].HitTagIndex == 0)
        {
            _envController.EnemyDetected(obs.RayOutputs[0].HitGameObject.transform.parent.gameObject, this._team);
        }
    }


    public void Hit(int damage)
    {
        _health = Mathf.Max(_health - damage, 0);
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
            _envController.AgentExitedCT(_team);
        }

        //envController.resetCT();
    }

    public Vector2 GetScreenSpaceAimPos()
    {
        return Camera.main.WorldToScreenPoint(tankController.GetAimPos());
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
