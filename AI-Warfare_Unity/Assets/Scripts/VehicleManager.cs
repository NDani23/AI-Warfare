using System;
using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

public enum Team
{
    Red = 0,
    Yellow = 1
}

public enum VehicleType
{
    Tank = 0,
    Heli = 1
}

public abstract class VehicleManager : MonoBehaviour
{
    [SerializeField] protected GameObject HitBoxMeshes;
    [SerializeField] protected GameObject MinimapIcon;
    [SerializeField] protected GameObject VehicleUI;
    [SerializeField] protected GameObject SelectedIcon;
    [SerializeField] protected GameObject DeadStateIcon;
    [SerializeField] protected GameObject GameCameraAnchor;
    [SerializeField] protected int memberID;
    public int MemberID => memberID;

    protected IVehicleController _vehicleController;

    protected float _health;
    public float Health => _health;
    public abstract float MaxHealth { get; }

    protected Team _team;
    public Team Team => _team;

    protected bool _detected = false;
    public bool Detected => _detected;

    protected bool _selected = false;

    protected float _regenHealthCooldown = 0;

    protected GameObject _healthBar;

    protected VehicleType _vehicleType = VehicleType.Tank;
    public VehicleType VehicleType => _vehicleType;

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

    protected String _agentName;
    public String AgentName => _agentName;

    public abstract void AddReward(float reward);
    public abstract void EndEpisode();

    public virtual void SetPlayerControl(bool control)
    {
        _isPlayerControlled = control;
        var bp = GetComponent<Unity.MLAgents.Policies.BehaviorParameters>();
        if (bp != null)
        {
            bp.BehaviorType = _isPlayerControlled ? Unity.MLAgents.Policies.BehaviorType.HeuristicOnly : Unity.MLAgents.Policies.BehaviorType.Default;
        }
    }

    protected virtual void extendSelectedStateChanged(bool isSelected)
    {
        // This method can be overridden by derived classes to implement additional behavior when the selected state changes.
    }

    protected virtual void extendResetVehicle()
    {
        // This method can be overridden by derived classes to implement additional behavior when the vehicle is reset.
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

    public void ResetVehicle()
    {
        _health = MaxHealth;
        _vehicleController.setStartingState((int)_team, memberID);
        _detected = false;
        if(!_selected) _healthBar.SetActive(true);
        HitBoxMeshes.SetActive(true);
        gameObject.tag = _team == Team.Red ? "RedAgent" : "YellowAgent";
        if (_inCT)
        {
            _inCT = false;
            _envController.VehicleExitedCT(_team);
        }
        extendResetVehicle();
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
        _healthBar.SetActive(false);
        if (_inCT)
        {
            _inCT = false;
            _envController.VehicleExitedCT(_team);
        }
        _envController.VehicleDied(this);
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
        if(isSelected)
        {
            _healthBar.SetActive(false);
        }
        else
        {
            _healthBar.SetActive(true);
        }
        extendSelectedStateChanged(isSelected);
        UpdateIconsVisibility();
    }
}
