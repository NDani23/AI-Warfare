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
    [SerializeField] protected int memberID;
    public int MemberID => memberID;

    protected IVehicleController _vehicleController;

    protected float _health;
    public float Health => _health;
    public abstract float MaxHealth { get; }

    protected Team _team;
    public Team Team => _team;

    protected bool _detected = false;
    public bool Detected
    {
        get => _detected;
        set => _detected = value;
    }

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

    public void Hit(int damage)
    {
        _health = Mathf.Max(_health - damage, 0);
        _regenHealthCooldown = 10.0f;
        if (_health <= 0)
        {
            setDeadState();
        }
    }

    public void ResetAgent()
    {
        _health = MaxHealth;
        _vehicleController.setStartingState((int)_team, memberID);
        HitBoxMeshes.SetActive(true);
        gameObject.tag = _team == Team.Red ? "RedAgent" : "YellowAgent";
        if (_inCT)
        {
            _inCT = false;
            _envController.AgentExitedCT(_team);
        }
    }

    public Vector2 GetScreenSpaceAimPos()
    {
        return _vehicleController.GetScreenSpaceAimPos();
    }

    public void setDeadState()
    {
        _vehicleController.setDeadState();
        HitBoxMeshes.SetActive(false);
        if (_inCT)
        {
            _inCT = false;
            _envController.AgentExitedCT(_team);
        }
        _envController.AgentDied(this);
        _health = 0;
        gameObject.tag = "Untagged";
    }

    public void SetMaterial(Material mat = null)
    {
        _vehicleController.setMaterial(mat);
    }
}
