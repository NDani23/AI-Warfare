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
    [SerializeField] private Transform _leftGunMuzzle;
    [SerializeField] private Transform _rightGunMuzzle;
    [SerializeField] private Transform _gridTag;


    private Agent _agent;
    private HeliController _heliController;
    private HitInfo _leftGunHitInfo;
    public HitInfo LeftGunHitInfo => _leftGunHitInfo;
    private HitInfo _rightGunHitInfo;
    public HitInfo RightGunHitInfo => _rightGunHitInfo;

    private float _maxHealth = 40.0f;
    public int IsShooting { get; set; } = 0;
    public override float MaxHealth => _maxHealth;

    void Awake()
    {
        _vehicleType = VehicleType.Heli;
        _vehicleController = this.gameObject.GetComponent<HeliController>();
        _heliController = (HeliController)_vehicleController;
        _agent = GetComponent<HeliPilotAgent>();
         _healthBar = GetComponentInChildren<agentInfoScript>().gameObject;

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

        _agentName = _team == Team.Red ? "R" + memberID.ToString() : "Y" + memberID.ToString();
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

        EvaluateWeapons();

        _heliController.IsShooting = IsShooting;
        if(IsShooting == 1)
        {
            _envController.EnemyDetected(this.gameObject);
        }

        _gridTag.position = new Vector3(transform.position.x, 3.0f, transform.position.z);
    }

    public override HitInfo RequestHitInfo()
    {
        EvaluateWeapons(1000.0f);
        return _leftGunHitInfo;
    }

    private void EvaluateWeapons(float range = 400.0f)
    {
        string EnemyTag = _team == Team.Red ? "YellowAgent" : "RedAgent";
        string AllyTag = _team == Team.Red ? "RedAgent" : "YellowAgent";
        // --- LEFT GUN ---
        if (Physics.Raycast(_leftGunMuzzle.position, _leftGunMuzzle.forward, out RaycastHit leftHit, range, _hitscanLayerMask))
        {
            _leftGunHitInfo.hitPosition = leftHit.point;
            _leftGunHitInfo.hitGameObject = leftHit.collider.gameObject;

            if (leftHit.collider.CompareTag(EnemyTag))
            {
                _leftGunHitInfo.hitTag = 0;
                _envController.EnemyDetected(leftHit.collider.transform.parent.gameObject);
            }
            else if (!leftHit.collider.CompareTag(AllyTag))
            {
                _leftGunHitInfo.hitTag = -1;
            }
        }
        else
        {
            _leftGunHitInfo.hitPosition = _leftGunMuzzle.position + (_leftGunMuzzle.forward * range);
            _leftGunHitInfo.hitGameObject = null;
            _leftGunHitInfo.hitTag = -1;
        }

        // --- RIGHT GUN ---
        if (Physics.Raycast(_rightGunMuzzle.position, _rightGunMuzzle.forward, out RaycastHit rightHit, range, _hitscanLayerMask))
        {
            _rightGunHitInfo.hitPosition = rightHit.point;
            _rightGunHitInfo.hitGameObject = rightHit.collider.gameObject;

            if (rightHit.collider.CompareTag(EnemyTag)) 
            {
                _rightGunHitInfo.hitTag = 0;
                _envController.EnemyDetected(rightHit.collider.transform.parent.gameObject);
            }
            else if (!rightHit.collider.CompareTag(AllyTag))
            {
                _rightGunHitInfo.hitTag = -1;
            }
        }
        else
        {
            _rightGunHitInfo.hitPosition = _rightGunMuzzle.position + (_rightGunMuzzle.forward * range);
            _rightGunHitInfo.hitGameObject = null;
            _rightGunHitInfo.hitTag = -1;
        }

        _heliController.LeftGunHitInfo = _leftGunHitInfo;
        _heliController.RightGunHitInfo = _rightGunHitInfo;
        _heliController.IsShooting = IsShooting;
    }

    public float getOverHeatStatus()
    {
        return _heliController.getGunOverheatStatus();
    }
}