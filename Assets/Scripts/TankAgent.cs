using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using System.Linq;
using UnityEngine.Events;
using Unity.MLAgents.Demonstrations;

public class TankAgent : VehicleAgent
{
    [SerializeField] private Rigidbody tankRB;
    [SerializeField] private Transform tankCannon;
    [SerializeField] private Transform DeadTankPrefab;
    [SerializeField] private BufferSensorComponent detectedEnemiesSensor;
    [SerializeField] private BufferSensorComponent teammateSensor;
    [SerializeField] private GameObject _healthBar;

    public DemonstrationRecorder? demonstrationRecorder;

    BehaviorParameters m_BehaviorParameters;
    RayPerceptionSensorComponent3D aimSensor = null;

    private float _maxHealth = 40.0f;
    public override float MaxHealth => _maxHealth;

    private float DistanceToCT = 1000;

    private TankController _tankController;

    public UnityEvent DiedEvent;
    public UnityEvent RespawnEvent;

    public override void Initialize()
    {
        _vehicleController = this.gameObject.GetComponent<TankController>();
        _tankController = (TankController)_vehicleController;

        _health = MaxHealth;
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
        sensor.AddObservation(_tankController.coolDownTime / 3.0f);
        sensor.AddObservation(_health / 100.0f);
        sensor.AddObservation(transform.InverseTransformVector(tankCannon.forward));
        if(_health == 0)
        {
            sensor.AddObservation(Vector3.zero);
            sensor.AddObservation(0.0f);
        }
        else
        {
            sensor.AddObservation(Vector3.Normalize(transform.InverseTransformVector(_envController.GetCTPosition() - transform.localPosition)));
            DistanceToCT = Vector3.Distance(_envController.GetCTPosition(), transform.localPosition) / 700.0f;
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

        float stateNum = _team == Team.Red ? -1 * _envController.getStateNum() : _envController.getStateNum();
        //float stateNum = team == Team.Red ? (envController.RedTeamPoints - envController.YellowTeamPoints) * 0.01f : (envController.YellowTeamPoints - envController.RedTeamPoints) * 0.01f;

        sensor.AddObservation(stateNum /= 10);

        Dictionary<GameObject, float> detectedEnemies = _team == Team.Red ? _envController.m_DetectedYellowEnemies : _envController.m_DetectedRedEnemies;

        //BufferSensor

        if(_health != 0)
        {
                foreach (var agent in detectedEnemies.Keys.ToList())
                {
                    Vector3 dir = Vector3.Normalize(transform.InverseTransformDirection(agent.transform.localPosition - transform.localPosition));
                    float dist = Vector3.Distance(agent.transform.localPosition, transform.localPosition) / 700.0f;
                    float health = agent.GetComponent<VehicleAgent>().Health * 0.01f;

                    float[] Obs = { dir.x, dir.y, dir.z, dist, health, (int)agent.GetComponent<VehicleAgent>().AgentType };
                    detectedEnemiesSensor.AppendObservation(Obs);
    
                }

                foreach (var agent in _envController.AgentsList)
                {
                    if (agent.Team != this._team || agent.MemberID == this.memberID)
                        continue;

                    if (agent.Health == 0)
                        continue;

                    Vector3 dir = Vector3.Normalize(transform.InverseTransformDirection(agent.gameObject.transform.localPosition - transform.localPosition));
                    float dist = Vector3.Distance(agent.gameObject.transform.localPosition, transform.localPosition) / 700.0f;
                    float health = agent.Health * 0.01f;

                    float[] Obs = { dir.x, dir.y, dir.z, dist, health, (int)agent.AgentType };
                    teammateSensor.AppendObservation(Obs);
                }
                
            
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (_health == 0.0f)
            return;


        _tankController.Throttle = actions.DiscreteActions[0] - 1;

        if (actions.DiscreteActions[1] - 1 == 0 || (actions.DiscreteActions[1] - 1) * _tankController.Steer < 0)
        {
            _tankController.Steer = 0;
        }
        else
        {
            _tankController.Steer = Mathf.Lerp(_tankController.Steer, actions.DiscreteActions[1] - 1, 0.1f);
        }


        _tankController.HorizontalAimInput = actions.ContinuousActions[0];
        _tankController.VerticalAimInput = actions.DiscreteActions[3] - 1;
        _tankController.FireInput = actions.DiscreteActions[2];

        //_tankController.HorizontalAimInput = 1.0f;
        //_tankController.Steer = 1;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if(_health == 0.0f) return;
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;

        var aimDirection = Input.mousePosition;
        aimDirection.z = 500.0f;

        _tankController.AimDirection = Camera.main.ScreenToWorldPoint(aimDirection);
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
        if (_health == 0.0f) return;
        //if (_regenHealthCooldown != 0) _regenHealthCooldown = Mathf.Max(0, _regenHealthCooldown - Time.fixedDeltaTime);

        //if(_health != 100 && _regenHealthCooldown == 0)
        //{
        //    _health = Mathf.Min(100, _health + Time.fixedDeltaTime * 10.0f);
        //}

        RayPerceptionInput spec = aimSensor.GetRayPerceptionInput();
        RayPerceptionOutput obs = RayPerceptionSensor.Perceive(spec, false);
        if (obs.RayOutputs[0].HitTagIndex == 0)
        {
            _envController.EnemyDetected(obs.RayOutputs[0].HitGameObject.transform.parent.gameObject, this._team);
        }
    }

    public float getCooldown()
    {
        return _tankController.coolDownTime;
    }
}
