using UnityEngine;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;

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
public class HeliManager : VehicleManager, ITargetable
{
    [SerializeField] private RayPerceptionSensorComponent3D _leftAimSensor;
    [SerializeField] private RayPerceptionSensorComponent3D _rightAimSensor;
    [SerializeField] private Transform _gridTag;


    private Agent _agent;
    private HeliController _heliController;
    private HitInfo _leftGunHitInfo;
    private HitInfo _rightGunHitInfo;

    private float _maxHealth = 40.0f;
    public int IsShooting { get; set; } = 0;
    public override float MaxHealth => _maxHealth;

    void Awake()
    {
        _vehicleType = VehicleType.Heli;
        _vehicleController = this.gameObject.GetComponent<HeliController>();
        _heliController = (HeliController)_vehicleController;

        _health = MaxHealth;
        _envController = GetComponentInParent<EnvController>();

        _leftGunHitInfo.hitTag = -1;
        _rightGunHitInfo.hitTag = -1;

        _heliController.LeftGunHitInfo = _leftGunHitInfo;
        _heliController.RightGunHitInfo = _rightGunHitInfo;

        BehaviorParameters behaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        if (behaviorParameters.TeamId == (int)Team.Red)
        {
            _team = Team.Red;
        }
        else if (behaviorParameters.TeamId == (int)Team.Yellow)
        {
            _team = Team.Yellow;
        }
    }

    public override void AddReward(float reward)
    {
        if (_agent != null)
        {
            _agent.AddReward(reward);
        }
    }

    public override void EndEpisode()
    {
        if (_agent != null)
        {
            _agent.EndEpisode();
        }
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

        if (IsShooting == 1)
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

        _heliController.IsShooting = IsShooting;
        _gridTag.position = new Vector3(transform.position.x, 3.0f, transform.position.z);
    }

    public float getOverHeatStatus()
    {
        return _heliController.getGunOverheatStatus();
    }
}