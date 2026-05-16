using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;

public class TankDriverAgent : Agent
{
    [SerializeField] private Transform _tankCannon;

    private TankManager _vehicleManager;
    private TankController _vehicleController;
    private EnvController _envController;
    private Rigidbody _tankRB;

    private float _lastThrottleAction = 0.0f;
    private float _lastSteerAction = 0.0f;

    public float LastThrottleAction => _lastThrottleAction;
    public float LastSteerAction => _lastSteerAction;

    public override void Initialize()
    {
        _vehicleManager = GetComponent<TankManager>();
        _vehicleController = GetComponent<TankController>();
        _envController = GetComponentInParent<EnvController>();
        _tankRB = GetComponent<Rigidbody>();
    }

    public override void OnEpisodeBegin()
    {
        _vehicleManager.ResetTank();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(Vector3.Dot(transform.forward, _tankRB.linearVelocity) / 40.0f); // Forward velocity
        sensor.AddObservation(transform.InverseTransformDirection(_tankRB.angularVelocity).y); // Angular velocity (turn speed)
        sensor.AddObservation(_vehicleManager.Health * 0.01f); // Health
        sensor.AddObservation(transform.InverseTransformDirection(_tankCannon.forward)); // Cannon forward direction relative to the tank body
        sensor.AddObservation(_vehicleManager.ActiveCommand.commandType == CommandType.GoToPosition ? 1.0f : 0.0f); // is Go-to command active
        sensor.AddObservation(_vehicleManager.ActiveCommand.commandType == CommandType.EliminateTarget ? 1.0f : 0.0f); // is Kill-target command active
        sensor.AddObservation(_envController.m_ResetTimer / (float)_envController.timeLimit); // Remaining time of the episode
        sensor.AddObservation(0.0f); // Team score
        sensor.AddObservation(0.0f); // Enemy team score
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (_vehicleManager.Health == 0.0f)
            return;

        _lastThrottleAction = actions.DiscreteActions[0] - 1;
        _lastSteerAction = actions.DiscreteActions[1] - 1;

        _vehicleController.Throttle = _lastThrottleAction;
        
        if (_lastSteerAction == 0 || _lastSteerAction * _vehicleController.Steer < 0)
        {
            _vehicleController.Steer = 0;
        }
        else
        {
            _vehicleController.Steer = Mathf.Lerp(_vehicleController.Steer, _lastSteerAction, 0.075f);
        }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if(_vehicleManager.Health == 0.0f) return;
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;

        discreteActions[0] = Input.GetKey(KeyCode.W) ? 2 : (Input.GetKey(KeyCode.S) ? 0 : 1);
        discreteActions[1] = Input.GetKey(KeyCode.D) ? 2 : (Input.GetKey(KeyCode.A) ? 0 : 1);
    }

    public bool IsPlayerControlled()
    {
        var bp = GetComponent<Unity.MLAgents.Policies.BehaviorParameters>();
        return bp != null ? bp.BehaviorType == Unity.MLAgents.Policies.BehaviorType.HeuristicOnly : false;
    }

    public void SetPlayerControl(bool control)
    {
        var bp = GetComponent<Unity.MLAgents.Policies.BehaviorParameters>();
        if (bp != null)
        {
            bp.BehaviorType = control ? Unity.MLAgents.Policies.BehaviorType.HeuristicOnly : Unity.MLAgents.Policies.BehaviorType.Default;
        }
    }
}
