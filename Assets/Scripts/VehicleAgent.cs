using Unity.MLAgents;
using UnityEngine;

public enum Team
{
    Red = 0,
    Yellow = 1
}

public enum AgentType
{
    Tank = 0,
    Heli = 1
}

public abstract class VehicleAgent : Agent
{
    [SerializeField] protected GameObject HitBoxMeshes;
    [SerializeField] protected GameObject MinimapIcon;
    [SerializeField] protected GameObject SelectedIcon;
    [SerializeField] protected GameObject DeadStateIcon;
    [SerializeField] protected GameObject GameCameraAnchor;
    [SerializeField] protected GameObject VehicleUI;
    [SerializeField] protected int memberID;
    public int MemberID => memberID;

    protected IVehicleController _vehicleController;

    protected float _health;
    public float Health => _health;
    public abstract float MaxHealth { get; }

    protected Team _team;
    public Team Team => _team;

    protected bool _detected = false;

    protected bool _selected = false;

    protected float _regenHealthCooldown = 0;

    protected AgentType _agentType = AgentType.Tank;
    public AgentType AgentType => _agentType;

    protected EnvController _envController;
    public EnvController EnvController => _envController;

    protected GameObject _gameObject { get; }
    public GameObject GameObject => gameObject;

    protected bool _inCT = false;
    public bool InCT
    {
        get => _inCT;
        set => _inCT = value;
    }

    protected bool _isPlayerControlled = false;
    public bool IsPlayerControlled => _isPlayerControlled;

    public void SetPlayerControl(bool control)
    {
        _isPlayerControlled = control;
        var bp = GetComponent<Unity.MLAgents.Policies.BehaviorParameters>();
        if (bp != null)
        {
            bp.BehaviorType = _isPlayerControlled ? Unity.MLAgents.Policies.BehaviorType.HeuristicOnly : Unity.MLAgents.Policies.BehaviorType.Default;
        }
    }

    public void Hit(int damage)
    {
        _health = Mathf.Max(_health - damage, 0);
        _regenHealthCooldown = 10.0f;
        if (_health <= 0)
        {
            setDeadState();
        }
    }

    private void UpdateIconsVisibility()
    {
        if (MinimapIcon != null)
        {
            MinimapIcon.SetActive((_detected || _team == Team.Yellow) && Health != 0);
        }

        if (SelectedIcon != null)
        {
            SelectedIcon.SetActive(_selected && _team == Team.Yellow && Health != 0);
        }

        if (DeadStateIcon != null)
        {
            DeadStateIcon.SetActive(_health == 0);
        }
    }

    public void ResetAgent()
    {
        _health = MaxHealth;
        _vehicleController.setStartingState((int)_team, memberID);
        _detected = false;
        HitBoxMeshes.SetActive(true);
        gameObject.tag = _team == Team.Red ? "RedAgent" : "YellowAgent";
        if (_inCT)
        {
            _inCT = false;
            _envController.AgentExitedCT(_team);
        }
        UpdateIconsVisibility();
    }

    public Vector2 GetScreenSpaceAimPos()
    {
        return _vehicleController.GetScreenSpaceAimPos();
    }

    public void setDeadState()
    {
        _vehicleController.setDeadState();
        _detected = false;
        HitBoxMeshes.SetActive(false);
        if (_inCT)
        {
            _inCT = false;
            _envController.AgentExitedCT(_team);
        }
        _envController.AgentDied(this);
        _health = 0;
        gameObject.tag = "Untagged";
        UpdateIconsVisibility();
    }

    public void SetMaterial(Material mat = null)
    {
        _vehicleController.setMaterial(mat);
    }

    public void setDetectedState(bool isDetected)
    {
        _detected = isDetected;
        UpdateIconsVisibility();
    }

    public GameObject getGameCameraAnchor()
    {
        return GameCameraAnchor;
    }

    public IVehicleUI getVehicleUI()
    {
        return VehicleUI.GetComponent<IVehicleUI>();
    }

    public void setSelectedState(bool isSelected)
    {
        _selected = isSelected;
        UpdateIconsVisibility();
    }
}
