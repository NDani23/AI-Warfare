using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using System.Collections.Generic;
using System.Linq;

public class TankShooterAgent : Agent
{
    [SerializeField] private Transform _tankCannon;

    [SerializeField] private BufferSensorComponent _detectedEnemiesSensor;
    [SerializeField] private BufferSensorComponent _teammateSensor;

    private TankManager _vehicleManager;
    private TankController _vehicleController;
    private EnvController _envController;
    private Rigidbody _tankRB;

    private GameObject _target;
    public GameObject Target => _target;

    public override void Initialize()
    {
        _vehicleManager = GetComponentInParent<TankManager>();
        _vehicleController = GetComponentInParent<TankController>();
        _envController = GetComponentInParent   <EnvController>();
        _tankRB = GetComponentInParent<Rigidbody>();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        GameObject activeTarget = _vehicleManager.ActiveCommand == TankManager.CommandType.KillTarget ? _target : null;

        sensor.AddObservation(Vector3.Dot(transform.parent.forward, _tankRB.linearVelocity) / 40.0f); // Forward velocity
        sensor.AddObservation(transform.InverseTransformDirection(_tankRB.angularVelocity).y); // Angular velocity (turn speed)
        sensor.AddObservation(_vehicleManager.DriverThrottleAction); // Driver throttle action (-1..1)
        sensor.AddObservation(_vehicleManager.DriverSteerAction); // Driver steer action (-1..1)
        sensor.AddObservation(transform.InverseTransformDirection(_vehicleManager.transform.forward)); // Body forward direction relative to the tank turret
        sensor.AddObservation(transform.InverseTransformDirection(_tankCannon.transform.forward).y); // Pitch of the cannon
        sensor.AddObservation(_vehicleController.coolDownTime / 3.0f); // Shoot cooldown time
        sensor.AddObservation(activeTarget != null ? Vector3.Normalize(_tankCannon.transform.InverseTransformDirection(activeTarget.gameObject.transform.localPosition - transform.parent.localPosition)) : Vector3.zero); // Direction to active target
        sensor.AddObservation(activeTarget != null ? Vector3.Distance(activeTarget.gameObject.transform.localPosition, transform.parent.localPosition) / 700.0f : 0); // Distance to active target
        sensor.AddObservation(Vector3.zero); // direction to enemy spawn (right edge)
        sensor.AddObservation(Vector3.zero); // direction to enemy spawn (left edge)
        sensor.AddObservation(0.0f); // distance to enemy spawn

        //Observations about other agents
        Dictionary<GameObject, float> detectedEnemies = _vehicleManager.Team == Team.Red ? _envController.m_DetectedYellowEnemies : _envController.m_DetectedRedEnemies;
        if (_vehicleManager.Health != 0)
        {
            foreach (var agent in detectedEnemies.Keys.ToList())
            {
                Vector3 dir = Vector3.Normalize(_tankCannon.transform.InverseTransformDirection(agent.transform.localPosition - transform.parent.localPosition));
                float dist = Vector3.Distance(agent.transform.localPosition, transform.parent.localPosition) / 700.0f;
                float health = _vehicleManager.Health * 0.01f;

                float[] Obs = { dir.x, dir.y, dir.z, dist, health};
                _detectedEnemiesSensor.AppendObservation(Obs);

            }

            foreach (var agent in _envController.VehicleList)
            {
                if (agent.Team != _vehicleManager.Team || agent.MemberID == _vehicleManager.MemberID)
                    continue;

                if (agent.Health == 0)
                    continue;

                Vector3 dir = Vector3.Normalize(_tankCannon.transform.InverseTransformDirection(agent.gameObject.transform.localPosition - transform.parent.localPosition));
                float dist = Vector3.Distance(agent.gameObject.transform.localPosition, transform.parent.localPosition) / 700.0f;
                float health = agent.Health * 0.01f;

                float[] Obs = { dir.x, dir.y, dir.z, dist, health};
                _teammateSensor.AppendObservation(Obs);
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (_vehicleManager.Health == 0.0f)
            return;

        _vehicleController.FireInput = actions.DiscreteActions[0];
        _vehicleController.HorizontalAimInput = actions.ContinuousActions[0];
        _vehicleController.VerticalAimInput = actions.ContinuousActions[1];
    }

    public void FixedUpdate()
    {
        // if(_target != null && _vehicleManager.ActiveCommand == TankManager.CommandType.KillTarget)
        // {
        //     Vector3 toTarget = Vector3.Normalize(transform.InverseTransformVector(_target.gameObject.transform.localPosition - transform.parent.localPosition));
        //     Vector3 aimDirection = Vector3.Normalize(transform.InverseTransformVector(_tankCannon.transform.forward));
        //     _vehicleManager.AddRewardToShooter(Time.fixedDeltaTime / 60.0f * Vector3.Dot(toTarget, aimDirection) * 0.5f);
        // }
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if(_vehicleManager.Health == 0.0f) return;

        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space) ? 1 : 0;

        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetKey(KeyCode.LeftArrow) ? -1 : (Input.GetKey(KeyCode.RightArrow) ? 1 : 0); // Horizontal aim
        continuousActions[1] = Input.GetKey(KeyCode.UpArrow) ? 1 : (Input.GetKey(KeyCode.DownArrow) ? -1 : 0); // Vertical aim
    }

    public void SetPlayerControl(bool control)
    {
        var bp = GetComponent<Unity.MLAgents.Policies.BehaviorParameters>();
        if (bp != null)
        {
            bp.BehaviorType = control ? Unity.MLAgents.Policies.BehaviorType.HeuristicOnly : Unity.MLAgents.Policies.BehaviorType.Default;
        }
    }

    public void SetTarget(GameObject target)
    {
        if (target == null || target.GetComponent<ITargetable>() == null) _target = null;
        else _target = target;
    }
}
