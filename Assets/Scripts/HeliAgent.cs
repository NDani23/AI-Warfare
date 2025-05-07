using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using Unity.VisualScripting;

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
    [SerializeField] private uint memberID;
    [SerializeField] private RayPerceptionSensorComponent3D _leftAimSensor;
    [SerializeField] private RayPerceptionSensorComponent3D _rightAimSensor;

    private Team _team;
    public Team Team => _team;
    private float _health = 50;
    public float Health => _health;
    private EnvController _envController;
    public EnvController EnvController => _envController;
    public GameObject GameObject => gameObject;
    public uint MemberID => memberID;
    BehaviorParameters m_BehaviorParameters;
    private float RegenHealthCooldown = 0;
    private Vector3 _mousePosDelta = Vector3.zero;
    private HitInfo _leftGunHitInfo;
    private HitInfo _rightGunHitInfo;


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
            _envController.AgentDied(this);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (_health == 0) return;
        _heliController.Throttle = actions.DiscreteActions[0] - 1;
        _heliController.Roll = actions.DiscreteActions[1] - 1;
        _heliController.IsShooting = actions.DiscreteActions[2];

        _heliController.Pitch = actions.ContinuousActions[0];
        _heliController.Yaw = actions.ContinuousActions[1];
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;

        discreteActions[0] = Input.GetKey(KeyCode.W) ? 2 : (Input.GetKey(KeyCode.S) ? 0 : 1);
        discreteActions[1] = Input.GetKey(KeyCode.A) ? 2 : (Input.GetKey(KeyCode.D) ? 0 : 1);
        discreteActions[2] = Input.GetMouseButton(0) ? 1 : 0;

        ActionSegment<float> continousActions = actionsOut.ContinuousActions;
        continousActions[0] = (-_mousePosDelta.y / Screen.height) * 15.0f;
        continousActions[1] = (_mousePosDelta.x / Screen.width) * 15.0f;

        _mousePosDelta = Vector3.zero;
    }

    private void OnCollisionEnter(Collision collision)
    {
        float impactRelativeVelocity = Vector3.Magnitude(collision.relativeVelocity) / 2.0f;

        if (impactRelativeVelocity < 2.0f) return;

        _health = Mathf.Max(0.0f, _health - impactRelativeVelocity);
        if (_health <= 0)
        {
            _envController.AgentDied(this);
        }
    }

    public void ResetAgent()
    {
        _heliController.setStartingState((int)_team, memberID);
        _health = 50;
    }

    private void FixedUpdate()
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
        _heliController.RightGunHitInfo= _rightGunHitInfo;
    }

    public void Update()
    {
        _mousePosDelta += Input.mousePositionDelta;

        if (RegenHealthCooldown != 0) RegenHealthCooldown = Mathf.Max(0, RegenHealthCooldown - Time.deltaTime);

        if (_health != 100 && RegenHealthCooldown == 0)
        {
            _health = Mathf.Min(30, _health + Time.deltaTime * 5.0f);
        }
    }

    public Vector2 GetScreenSpaceAimPos()
    {
        return _heliController.GetScreenSpaceAimPos();
    }



}
