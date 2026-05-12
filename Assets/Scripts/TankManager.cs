using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using System.Linq;
using UnityEngine.Events;
using Unity.MLAgents.Demonstrations;
using Unity.MLAgents;

public class TankManager : VehicleManager, ITargetable
{
    public enum CommandType
    {
        None = 0,
        GoToPoint = 1,
        KillTarget = 2
    }

    [SerializeField] private GameObject _healthBar;
    [SerializeField] private RayPerceptionSensorComponent3D aimSensor;
    [SerializeField] private CommandMarkerController _commandMarker;
    [SerializeField] private TankDriverAgent _driverAgent;
    [SerializeField] private TankShooterAgent _shooterAgent;

    BehaviorParameters m_BehaviorParameters;
    private float _maxHealth = 100.0f;
    public override float MaxHealth => _maxHealth;
    private float RegenHealthCooldown = 0;

    private TankController _tankController;

    private CommandType _activeCommand = CommandType.None;
    public CommandType ActiveCommand => _activeCommand;
    public GameObject CommandTarget => _shooterAgent.Target;
    public float DriverThrottleAction => _driverAgent.LastThrottleAction;
    public float DriverSteerAction => _driverAgent.LastSteerAction;

    public UnityEvent DiedEvent;
    public UnityEvent RespawnEvent;

    private SimpleMultiAgentGroup _tankCrewAgentGroup;

    public void Awake()
    {
        _vehicleType = VehicleType.Tank;
        _vehicleController = this.gameObject.GetComponent<TankController>();
        _tankController = (TankController)_vehicleController;

        _health = MaxHealth;
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        _envController = GetComponentInParent<EnvController>();

        _tankCrewAgentGroup = new SimpleMultiAgentGroup();

        _team = m_BehaviorParameters.TeamId == (int)Team.Red ? Team.Red : Team.Yellow;
    }

    void Start()
    {
        _tankCrewAgentGroup.RegisterAgent(_driverAgent);
        _tankCrewAgentGroup.RegisterAgent(_shooterAgent);
    }


    public override void AddReward(float reward)
    {
        _tankCrewAgentGroup.AddGroupReward(reward);
    }

    public void AddRewardToShooter(float reward)
    {
        _shooterAgent.AddReward(reward);
    }

    public void AddRewardToDriver(float reward)
    {
        _driverAgent.AddReward(reward);
    }

    public override void EndEpisode()
    {
        _tankCrewAgentGroup.EndGroupEpisode();
    }

    public override void SetPlayerControl(bool control)
    {
        _isPlayerControlled = control;
        _driverAgent.SetPlayerControl(control);
        _shooterAgent.SetPlayerControl(control);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag != "Bullet")
        {
            AddRewardToDriver(-0.01f);
        }
    }

    public void FixedUpdate()
    {
        if (_health == 0.0f) return;
        if (_regenHealthCooldown != 0) _regenHealthCooldown = Mathf.Max(0, _regenHealthCooldown - Time.fixedDeltaTime);

        if(_health != _maxHealth && _regenHealthCooldown == 0)
        {
           _health = Mathf.Min(_maxHealth, _health + Time.fixedDeltaTime * 10.0f);
        }

        RayPerceptionInput spec = aimSensor.GetRayPerceptionInput();
        RayPerceptionOutput obs = RayPerceptionSensor.Perceive(spec, false);
        if (obs.RayOutputs[0].HitTagIndex == 1)
        {
            _envController.EnemyDetected(obs.RayOutputs[0].HitGameObject.transform.parent.gameObject, this._team);
        }

        if (_activeCommand == CommandType.KillTarget && _shooterAgent.Target == null)
        {
            ClearCommand();
        }

        //Existential penalty
        AddReward(-(Time.fixedDeltaTime / 60.0f) * 0.5f);
    }

    public float getCooldown()
    {
        return _tankController.coolDownTime;
    }

    public void SetTarget(GameObject target)
    {
        if (target == null || target.GetComponent<ITargetable>() == null)
        {
            return;
        }

        ClearCommand();
        _activeCommand = CommandType.KillTarget;
        _shooterAgent.SetTarget(target);
        _commandMarker.gameObject.SetActive(true);
        _commandMarker.SetFollowTarget(target.transform);
    }

    public GameObject GetTarget()
    {
        return _shooterAgent.Target;
    }

    public void SetGoToPoint(Vector3 envSpacePoint)
    {
        if (Mathf.Abs(envSpacePoint.x) > 340 || Mathf.Abs(envSpacePoint.z) > 340) return;
        _activeCommand = CommandType.GoToPoint;
        _shooterAgent.SetTarget(null);
        _commandMarker.gameObject.SetActive(true);
        _commandMarker.SetEnvSpacePosition(envSpacePoint);
    }
    public void ClearCommand()
    {
        _activeCommand = CommandType.None;
        _shooterAgent.SetTarget(null);
        _commandMarker.gameObject.SetActive(false);
        _commandMarker.SetFollowTarget(null);
    }

    public void ResetTank()
    {
        //ClearCommand();
        ResetVehicle();
    }
}
