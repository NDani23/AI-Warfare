using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using System.Linq;
using UnityEngine.Events;
using Unity.MLAgents.Demonstrations;
using Unity.MLAgents;
using Unity.VisualScripting;

public enum CommandType
{
    None = 0,
    GoToPosition = 1,
    EliminateTarget = 2
}

public class TankManager : VehicleManager, ITargetable
{

    [SerializeField] private RayPerceptionSensorComponent3D aimSensor;
    [SerializeField] private CommandTargetMarkerController _commandMarker;
    [SerializeField] private TankDriverAgent _driverAgent;
    [SerializeField] private TankShooterAgent _shooterAgent;
    [SerializeField] private Transform gameCameraPivot;

    BehaviorParameters m_BehaviorParameters;
    private float _maxHealth = 100.0f;
    public override float MaxHealth => _maxHealth;
    private float RegenHealthCooldown = 0;

    private TankController _tankController;

    private CommandType activeCommand = CommandType.None;
    public CommandType ActiveCommand => activeCommand;

    public CommandTargetMarkerController CommandMarker => _commandMarker;
    public float DriverThrottleAction => _driverAgent.LastThrottleAction;
    public float DriverSteerAction => _driverAgent.LastSteerAction;

    public UnityEvent DiedEvent;
    public UnityEvent RespawnEvent;

    private Vector3 _lookDirectionWorld = Vector3.forward;

    //private SimpleMultiAgentGroup _tankCrewAgentGroup;

    public void Awake()
    {
        _vehicleType = VehicleType.Tank;
        _vehicleController = this.gameObject.GetComponent<TankController>();
        _tankController = (TankController)_vehicleController;
        _healthBar = GetComponentInChildren<agentInfoScript>().gameObject;

        _health = MaxHealth;
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        _envController = GetComponentInParent<EnvController>();

        //_tankCrewAgentGroup = new SimpleMultiAgentGroup();

        _team = m_BehaviorParameters.TeamId == (int)Team.Red ? Team.Red : Team.Yellow;
        _agentName = _team == Team.Red ? "R" + memberID.ToString() : "Y" + memberID.ToString();
    }

    void Start()
    {
        //_tankCrewAgentGroup.RegisterAgent(_driverAgent);
        //_tankCrewAgentGroup.RegisterAgent(_shooterAgent);
    }


    public override void AddReward(float reward)
    {
        //_tankCrewAgentGroup.AddGroupReward(reward);
        _driverAgent.AddReward(reward);
        _shooterAgent.AddReward(reward);
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
        //_tankCrewAgentGroup.EndGroupEpisode();
        _driverAgent.EndEpisode();
        _shooterAgent.EndEpisode();
    }

    public override void SetPlayerControl(bool control)
    {
        _isPlayerControlled = control;
        _driverAgent.SetPlayerControl(control);
        _shooterAgent.SetPlayerControl(control);
    }

    protected override void extendSelectedStateChanged(bool isSelected)
    {
        if (!isSelected)
        {
            CommandMarker.ShowCommandIcon(false);
        }
        else
        {
            CommandMarker.ShowCommandIcon(true);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag != "Bullet")
        {
            AddRewardToDriver(-0.05f);
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

        // if (activeCommand.commandType == CommandType.EliminateTarget && _shooterAgent.Target == null)
        // {
        //     ClearCommand();
        // }

        AddRewardToDriver(Vector3.Dot(transform.forward, Vector3.Normalize(_commandMarker.transform.position - transform.position)) * 0.15f * (Time.fixedDeltaTime / 60.0f));
    }

    
    public void Update()
    {
        if(!_driverAgent.IsPlayerControlled() || _health == 0.0f)
        {
            gameCameraPivot.rotation = Quaternion.LookRotation(_shooterAgent.GetCannonForward(), Vector3.up);
            _lookDirectionWorld = _shooterAgent.GetCannonForward();
            return;
        }

        var cameraRotationVertical = Input.mousePositionDelta.y / Screen.height * 30.0f;
        var cameraRotationHorizontal = Input.mousePositionDelta.x / Screen.width * 60.0f;

        _lookDirectionWorld = Quaternion.AngleAxis(cameraRotationHorizontal, Vector3.up) * _lookDirectionWorld;
        _lookDirectionWorld = Quaternion.AngleAxis(-cameraRotationVertical, gameCameraPivot.right) * _lookDirectionWorld;
        _lookDirectionWorld.Normalize();
        _tankController.aimDirection = _lookDirectionWorld;
        
        gameCameraPivot.rotation = Quaternion.LookRotation(_lookDirectionWorld, Vector3.up);

        _tankController.lockTurret = false;
        if(Input.GetKey(KeyCode.LeftControl)) _tankController.lockTurret=true;
    }
    
    public bool IsCannonFacingCameraForward()
    {
        return Vector3.Dot(_shooterAgent.transform.forward, gameCameraPivot.forward) > 0.0f;
    }

    public bool IsHeuristicOnlyMode()
    {
        return _driverAgent.IsPlayerControlled();
    }

    public float getCooldown()
    {
        return _tankController.coolDownTime;
    }

    public void IssueCommand(GameObject commandTarget)
    {
        ClearCommand();

        if (commandTarget == null)
            return;

        if (commandTarget.GetComponent<ITargetable>() != null)
        {
            activeCommand = CommandType.EliminateTarget;
            _commandMarker.gameObject.SetActive(true);
            _commandMarker.SetFollowTarget(commandTarget);
        }
        else
        {
            activeCommand = CommandType.GoToPosition;
            _commandMarker.gameObject.SetActive(true);
            _commandMarker.SetFollowTarget(commandTarget);
        }
    }

    public void IssueCommand(Vector3 commandTargetGlobalPosition)
    {
        activeCommand = CommandType.GoToPosition;
        _commandMarker.gameObject.SetActive(true);
        _commandMarker.SetTargetGlobalPosition(commandTargetGlobalPosition);

    }
    private void ClearCommand()
    {
        activeCommand = CommandType.None;
        _commandMarker.gameObject.SetActive(false);
    }

    public void ResetTank()
    {
        IssueCommand(_envController.ControlPoint);
        ResetVehicle();
    }
}
