using UnityEngine;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using System.Collections.Generic;
using System.Linq;

public struct HitInfo
{
    public HitInfo(int hitTag, Vector3 hitPosition, GameObject? hitGameObject)
    {
        this.hitTag = hitTag;
        this.hitPosition = hitPosition;
        this.hitGameObject = hitGameObject;
    }

    public int hitTag;
    public Vector3 hitPosition;
    public GameObject? hitGameObject;
}
public class HeliAgent : VehicleAgent
{
    [SerializeField] private RayPerceptionSensorComponent3D _leftAimSensor;
    [SerializeField] private RayPerceptionSensorComponent3D _rightAimSensor;
    [SerializeField] private BufferSensorComponent _detectedEnemiesSensor;
    [SerializeField] private BufferSensorComponent _teammateSensor;
    [SerializeField] private Transform _gridTag;

    private HeliController _heliController;

    BehaviorParameters m_BehaviorParameters;
    private Vector3 _mousePosDelta = Vector3.zero;
    private HitInfo _leftGunHitInfo;
    private HitInfo _rightGunHitInfo;

    private float _maxHealth = 40.0f;
    private int isShooting = 0;
    public override float MaxHealth => _maxHealth;

    public override void Initialize()
    {
        _agentType = AgentType.Heli;
        _vehicleController = this.gameObject.GetComponent<HeliController>();
        _heliController = (HeliController)_vehicleController;

        _health = MaxHealth;
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        _envController = GetComponentInParent<EnvController>();

        _leftGunHitInfo.hitTag = -1;
        _rightGunHitInfo.hitTag = -1;

        _heliController.LeftGunHitInfo = _leftGunHitInfo;
        _heliController.RightGunHitInfo = _rightGunHitInfo;

        if (m_BehaviorParameters.TeamId == (int)Team.Red)
        {
            _team = Team.Red;
        }
        else if (m_BehaviorParameters.TeamId == (int)Team.Yellow)
        {
            _team = Team.Yellow;
        }
    }

    public override void OnEpisodeBegin()
    {
        ResetAgent();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //Observations for controlling the vehicle
        sensor.AddObservation(transform.position.y / 400.0f);
        sensor.AddObservation(Vector3.Dot(Vector3.up, transform.right)); //ROLL
        sensor.AddObservation(Vector3.Dot(Vector3.up, transform.forward)); //PITCH
        sensor.AddObservation(Vector3.Normalize(transform.InverseTransformDirection(_heliController.Rigidbody.linearVelocity)));
        sensor.AddObservation(transform.InverseTransformDirection(_heliController.Rigidbody.angularVelocity) / 3.0f);
        sensor.AddObservation(_heliController.getGunOverheatStatus());

        //Gameplay related observations
        sensor.AddObservation(_health / 40.0f);
        sensor.AddObservation(_envController.m_ResetTimer / (float)_envController.timeLimit); //remaining time


        //Observations about other agents
        Dictionary<GameObject, float> detectedEnemies = _team == Team.Red ? _envController.m_DetectedYellowEnemies : _envController.m_DetectedRedEnemies;
        if (_health != 0)
        {
            foreach (var agent in detectedEnemies.Keys.ToList())
            {
                Vector3 dir = Vector3.Normalize(transform.InverseTransformDirection(agent.transform.localPosition - transform.localPosition));
                float dist = Vector3.Distance(agent.transform.localPosition, transform.localPosition) / 700.0f;
                float health = agent.GetComponent<VehicleAgent>().Health * 0.01f;

                float[] Obs = { dir.x, dir.y, dir.z, dist, health, 1 };
                _detectedEnemiesSensor.AppendObservation(Obs);

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
                _teammateSensor.AppendObservation(Obs);
            }

        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (_health == 0) return;
        isShooting = actions.DiscreteActions[0];

        _heliController.Pitch = actions.ContinuousActions[0];
        _heliController.Yaw = actions.ContinuousActions[1];
        _heliController.Roll = actions.ContinuousActions[2];
        _heliController.Throttle = actions.ContinuousActions[3];
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = Input.GetMouseButton(0) ? 1 : 0;

        ActionSegment<float> continousActions = actionsOut.ContinuousActions;
        continousActions[0] = (-_mousePosDelta.y / Screen.height) * 15.0f; //PITCH
        continousActions[1] = (_mousePosDelta.x / Screen.width) * 15.0f; //YAW
        continousActions[2] = Input.GetKey(KeyCode.A) ? 1.0f : (Input.GetKey(KeyCode.D) ? -1.0f : 0.0f); //ROLL
        continousActions[3] = Input.GetKey(KeyCode.W) ? 1.0f : (Input.GetKey(KeyCode.S) ? -1.0f : 0.0f); //THROTTLE


        _mousePosDelta = Vector3.zero;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_health == 0) return;

        float impactRelativeVelocity = Vector3.Magnitude(collision.relativeVelocity) / 2.0f;

        if (impactRelativeVelocity < 2.0f) return;

        _health = Mathf.Max(0.0f, _health - impactRelativeVelocity);
        if (_health <= 0)
        {
            setDeadState();
        }
    }

    private void FixedUpdate()
    {
        if (_regenHealthCooldown != 0) _regenHealthCooldown = Mathf.Max(0, _regenHealthCooldown - Time.fixedDeltaTime);

        if (_health == 0) return;
        if (_health != MaxHealth && _regenHealthCooldown == 0)
        {
            _health = Mathf.Min(MaxHealth, _health + Time.deltaTime * 5.0f);
        }

        if (isShooting == 1)
        {
            RayPerceptionInput spec = _leftAimSensor.GetRayPerceptionInput();
            RayPerceptionOutput obs = RayPerceptionSensor.Perceive(spec, false);
            _leftGunHitInfo.hitPosition = obs.RayOutputs[0].EndPositionWorld;
            _leftGunHitInfo.hitTag = obs.RayOutputs[0].HitTagIndex;
            _leftGunHitInfo.hitGameObject = obs.RayOutputs[0].HitGameObject;

            spec = _rightAimSensor.GetRayPerceptionInput();
            obs = RayPerceptionSensor.Perceive(spec, false);
            _rightGunHitInfo.hitPosition = obs.RayOutputs[0].EndPositionWorld;
            _rightGunHitInfo.hitTag = obs.RayOutputs[0].HitTagIndex;
            _rightGunHitInfo.hitGameObject = obs.RayOutputs[0].HitGameObject;

            _heliController.LeftGunHitInfo = _leftGunHitInfo;
            _heliController.RightGunHitInfo = _rightGunHitInfo;

            if(_leftGunHitInfo.hitTag == 0)
            {
                _envController.EnemyDetected(_leftGunHitInfo.hitGameObject.transform.parent.gameObject, this.Team);
                _envController.EnemyDetected(this.gameObject, this.Team == Team.Yellow ? Team.Red : Team.Yellow);
            }

            if (_rightGunHitInfo.hitTag == 0)
            {
                _envController.EnemyDetected(_rightGunHitInfo.hitGameObject.transform.parent.gameObject, this.Team);

            }

        }

        _heliController.IsShooting = isShooting;
        _gridTag.position = new Vector3(transform.position.x, 3.0f, transform.position.z);
    }

    public void Update()
    {
        _mousePosDelta += Input.mousePositionDelta;
    }

    public float getOverHeatStatus()
    {
        return _heliController.getGunOverheatStatus();
    }
}
