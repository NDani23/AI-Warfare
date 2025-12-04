using System.Collections.Generic;
using UnityEngine;
using Unity.MLAgents.Sensors;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Policies;
using System.Linq;
using UnityEngine.Events;
using Unity.MLAgents.Demonstrations;

public class TankAgent : VehicleAgent
{
    [SerializeField] private Rigidbody tankRB;
    [SerializeField] private Transform tankCannon;
    [SerializeField] private GameObject _healthBar;
    [SerializeField] private RayPerceptionSensorComponent3D aimSensor;

    public DemonstrationRecorder? demonstrationRecorder;
    BehaviorParameters m_BehaviorParameters;
    private float _maxHealth = 100.0f;
    public override float MaxHealth => _maxHealth;

    private float DistanceToCT = 1000;

    private TankController _tankController;

    public UnityEvent DiedEvent;
    public UnityEvent RespawnEvent;

    public override void Initialize()
    {
        _agentType = AgentType.Tank;
        _vehicleController = this.gameObject.GetComponent<TankController>();
        _tankController = (TankController)_vehicleController;

        _health = MaxHealth;
        m_BehaviorParameters = gameObject.GetComponent<BehaviorParameters>();
        _envController = GetComponentInParent<EnvController>();

        _team = m_BehaviorParameters.TeamId == (int)Team.Red ? Team.Red : Team.Yellow;
    }

    public override void OnEpisodeBegin()
    {
        ResetAgent();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(Vector3.Dot(transform.forward, tankRB.linearVelocity) / 40.0f); // Forward velocity
        sensor.AddObservation(transform.InverseTransformDirection(tankRB.angularVelocity).y); // Angular velocity (turn speed)
        sensor.AddObservation(_tankController.coolDownTime / 3.0f); // Shoot cooldown time
        sensor.AddObservation(_health * 0.01f); // Health
        sensor.AddObservation(transform.InverseTransformVector(tankCannon.forward)); // Cannon forward direction relative to the tank body
        sensor.AddObservation(_envController.m_ResetTimer / (float)_envController.timeLimit); // Remaining time of the episode
        sensor.AddObservation(_team == Team.Red ? _envController.RedTeamPoints * 0.01f : _envController.YellowTeamPoints * 0.01f); // Team score
        sensor.AddObservation(_team == Team.Red ? _envController.YellowTeamPoints * 0.01f : _envController.RedTeamPoints * 0.01f); // Enemy team score
        sensor.AddObservation(Vector3.zero); // Placeholder for target direction
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (_health == 0.0f)
            return;

        _tankController.Throttle = actions.DiscreteActions[0] - 1;
        
        if (actions.DiscreteActions[1] - 1 == 0 || (actions.DiscreteActions[1] - 1) * _tankController.Steer < 0)
        {
            _tankController.Steer = 0;
        }
        else
        {
            _tankController.Steer = Mathf.Lerp(_tankController.Steer, actions.DiscreteActions[1] - 1, 0.075f);
        }

        _tankController.FireInput = actions.DiscreteActions[2];
        _tankController.HorizontalAimInput = actions.ContinuousActions[0];
        _tankController.VerticalAimInput = actions.ContinuousActions[1];
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        if(_health == 0.0f) return;
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;

        discreteActions[0] = Input.GetKey(KeyCode.W) ? 2 : (Input.GetKey(KeyCode.S) ? 0 : 1);
        discreteActions[1] = Input.GetKey(KeyCode.D) ? 2 : (Input.GetKey(KeyCode.A) ? 0 : 1);
        discreteActions[2] = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space) ? 1 : 0;

        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = Input.GetKey(KeyCode.LeftArrow) ? -1 : (Input.GetKey(KeyCode.RightArrow) ? 1 : 0); // Horizontal aim
        continuousActions[1] = Input.GetKey(KeyCode.UpArrow) ? 1 : (Input.GetKey(KeyCode.DownArrow) ? -1 : 0); // Vertical aim
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.tag != "Bullet")
        {
            //AddReward(-0.01f);
        }
    }

    public void FixedUpdate()
    {
        if (_health == 0.0f) return;
        //if (_regenHealthCooldown != 0) _regenHealthCooldown = Mathf.Max(0, _regenHealthCooldown - Time.fixedDeltaTime);

        //if(_health != 100 && _regenHealthCooldown == 0)
        //{
        //    _health = Mathf.Min(100, _health + Time.fixedDeltaTime * 10.0f);
        //}

        RayPerceptionInput spec = aimSensor.GetRayPerceptionInput();
        RayPerceptionOutput obs = RayPerceptionSensor.Perceive(spec, false);
        if (obs.RayOutputs[0].HitTagIndex == 0)
        {
            _envController.EnemyDetected(obs.RayOutputs[0].HitGameObject.transform.parent.gameObject, this._team);
        }
    }

    // public void Update()
    // {
    //     if (_tankController.aimDirection != Vector3.zero && m_BehaviorParameters.BehaviorType == BehaviorType.HeuristicOnly)
    //     {

    //         Quaternion targetRotation = Quaternion.LookRotation(_tankController.aimDirection);
    //         cameraPivot.transform.rotation = Quaternion.Slerp(cameraPivot.transform.rotation, targetRotation, 0.05f);
    //         //cameraPivot.transform.rotation = Quaternion.LookRotation(_tankController.aimDirection);
    //     }
    // }

    public float getCooldown()
    {
        return _tankController.coolDownTime;
    }
}
