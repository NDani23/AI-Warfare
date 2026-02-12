using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.VisualScripting;
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
public class HeliAgent : Agent, IVehicleAgent
{
    [SerializeField] private HeliController _heliController;
    [SerializeField] private int memberID;
    [SerializeField] private RayPerceptionSensorComponent3D _leftAimSensor;
    [SerializeField] private RayPerceptionSensorComponent3D _rightAimSensor;
    [SerializeField] private BufferSensorComponent _detectedEnemiesSensor;
    [SerializeField] private BufferSensorComponent _teammateSensor;
    [SerializeField] private GameObject HitBoxMeshes;

    private Team _team;
    public Team Team => _team;

    private bool inCT = false;
    public bool InCT
    {
        get => inCT;
        set => inCT = value;
    }

    private AgentType _agentType = AgentType.Heli;
    public AgentType AgentType => _agentType;

    private float _health = 50;
    public float Health => _health;
    private EnvController _envController;
    public EnvController EnvController => _envController;
    public GameObject GameObject => gameObject;
    public int MemberID => memberID;
    BehaviorParameters m_BehaviorParameters;
    private float RegenHealthCooldown = 0;
    private Vector3 _mousePosDelta = Vector3.zero;
    private HitInfo _leftGunHitInfo;
    private HitInfo _rightGunHitInfo;


    public override void Initialize()
    {
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

    public void Hit(int damage)
    {
        _health = Mathf.Max(_health - damage, 0);
        RegenHealthCooldown = 10.0f;
        if (_health <= 0)
        {
            setDeadState();
        }
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
        sensor.AddObservation(_health / 50.0f);
        sensor.AddObservation(_envController.m_ResetTimer / (float)_envController.timeLimit); //remaining time


        //Observations about other agents
        Dictionary<GameObject, float> detectedEnemies = _team == Team.Red ? _envController.m_DetectedYellowEnemies : _envController.m_DetectedRedEnemies;
        //TargetScript[] Targets = transform.parent.GetComponentsInChildren<TargetScript>();  
        if (_health != 0)
        {
            foreach (var agent in detectedEnemies.Keys.ToList())
            {
                Vector3 dir = Vector3.Normalize(transform.InverseTransformDirection(agent.transform.localPosition - transform.localPosition));
                float dist = Vector3.Distance(agent.transform.localPosition, transform.localPosition) / 700.0f;
                float health = agent.GetComponent<IVehicleAgent>().Health * 0.01f;

                //float[] Obs = { dir.x, dir.y, dir.z, dist, health, (int)agent.GetComponent<IVehicleAgent>().AgentType };
                float[] Obs = { dir.x, dir.y, dir.z, dist, health, 1.0f };
                _detectedEnemiesSensor.AppendObservation(Obs);

            }

            //foreach (var Target in Targets)
            //{
            //    if (Target.Detected && !Target.isFakeTarget())
            //    {
            //        Vector3 dir = Vector3.Normalize(transform.InverseTransformDirection(Target.transform.localPosition - transform.localPosition));
            //        float dist = Vector3.Distance(Target.transform.localPosition, transform.localPosition) / 700.0f;

            //        float[] Obs = { dir.x, dir.y, dir.z, dist, Target.Health * 0.01f, Target.targetType is TargetType.Heli ? 1.0f : 0.0f };
            //        _detectedEnemiesSensor.AppendObservation(Obs);
            //    }
            //}

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
        _heliController.IsShooting = actions.DiscreteActions[0];

        _heliController.Pitch = actions.ContinuousActions[0];
        _heliController.Yaw = actions.ContinuousActions[1];
        _heliController.Roll = actions.ContinuousActions[2];
        _heliController.Throttle = actions.ContinuousActions[3];

        //AddReward((Time.fixedDeltaTime / EnvController.timeLimit) * 1.0f);

        //if (transform.position.y <= 100.0f) AddReward(-(5.0f + (100.0f - transform.position.y)) * (Time.fixedDeltaTime / EnvController.timeLimit));
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

        //AddReward(-1.0f);

        _health = Mathf.Max(0.0f, _health - impactRelativeVelocity);
        if (_health <= 0)
        {
            AddReward(-EnvController.m_ResetTimer / EnvController.timeLimit);
            _envController.ResetEnv(this.Team == Team.Red ? Team.Yellow : Team.Red);
            //setDeadState();
        }
    }

    public void ResetAgent()
    {
        _health = 50;
        _heliController.setStartingState((int)_team, Random.Range(0, 5));
        HitBoxMeshes.SetActive(true);

        if (inCT)
        {
            inCT = false;
            _envController.AgentExitedCT(_team);
        }

        gameObject.tag = _team == Team.Red ? "RedAgent" : "YellowAgent";
    }

    private void FixedUpdate()
    {
        if (RegenHealthCooldown != 0) RegenHealthCooldown = Mathf.Max(0, RegenHealthCooldown - Time.fixedDeltaTime);

        if (_health == 0) return;
        if (_health != 50 && RegenHealthCooldown == 0)
        {
            _health = Mathf.Min(50, _health + Time.deltaTime * 5.0f);
        }

        if (_heliController.IsShooting == 1)
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
                //AddReward(0.01f);
            }

            if (_rightGunHitInfo.hitTag == 0)
            {
                _envController.EnemyDetected(_rightGunHitInfo.hitGameObject.transform.parent.gameObject, this.Team);
                //AddReward(0.01f);

            }

        }
    }

    public void Update()
    {
        _mousePosDelta += Input.mousePositionDelta;
    }

    public Vector2 GetScreenSpaceAimPos()
    {
        return _heliController.GetScreenSpaceAimPos();
    }

    public void setDeadState()
    {

        HitBoxMeshes.SetActive(false);

        //DiedEvent.Invoke();
        if (inCT)
        {
            inCT = false;
            _envController.AgentExitedCT(_team);
        }
        //AddReward(-10.0f);
        _envController.AgentDied(this);
        //EndEpisode();
        _health = 0;
        gameObject.tag = "Untagged";
        _heliController.setDeadState();
    }

    public void SetMaterial(Material mat = null)
    {
        _heliController.setMaterial(mat);
    }

}
