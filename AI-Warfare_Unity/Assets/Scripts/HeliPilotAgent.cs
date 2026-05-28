using UnityEngine;
using Unity.MLAgents;
using Unity.MLAgents.Sensors;
using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents.Actuators;

public class HeliPilotAgent : Agent
{
    [SerializeField] private BufferSensorComponent _detectedEnemiesSensor;
    [SerializeField] private BufferSensorComponent _teammateSensor;

    private HeliManager _vehicleManager;
    private HeliController _vehicleController;
    private EnvController _envController;

    private Vector3 _mousePosDelta = Vector3.zero;

    public override void Initialize()
    {
        _vehicleManager = GetComponent<HeliManager>();
        _vehicleController = GetComponent<HeliController>();
        _envController = GetComponentInParent<EnvController>();
    }

    public override void OnEpisodeBegin()
    {
        _vehicleManager.ResetVehicle();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        //Observations for controlling the vehicle
        sensor.AddObservation(transform.position.y / 400.0f);
        sensor.AddObservation(Vector3.Dot(Vector3.up, transform.right)); //ROLL
        sensor.AddObservation(Vector3.Dot(Vector3.up, transform.forward)); //PITCH
        sensor.AddObservation(Vector3.Normalize(transform.InverseTransformDirection(_vehicleController.Rigidbody.linearVelocity)));
        sensor.AddObservation(transform.InverseTransformDirection(_vehicleController.Rigidbody.angularVelocity) / 3.0f);
        sensor.AddObservation(_vehicleController.getGunOverheatStatus());

        //Gameplay related observations
        sensor.AddObservation(_vehicleManager.Health / _vehicleManager.MaxHealth);
        sensor.AddObservation(_envController.ResetTimer / (float)_envController.timeLimit); //remaining time

        //Observations about other agents
        Dictionary<GameObject, float> detectedEnemies = _vehicleManager.Team == Team.Red ? _envController.DetectedYellowEnemies : _envController.DetectedRedEnemies;
        if (_vehicleManager.Health != 0)
        {
            foreach (var agent in detectedEnemies.Keys.ToList())
            {
                Vector3 dir = Vector3.Normalize(transform.InverseTransformDirection(agent.transform.localPosition - transform.localPosition));
                float dist = Vector3.Distance(agent.transform.localPosition, transform.localPosition) / 700.0f;
                float health = agent.GetComponent<VehicleManager>().Health * 0.01f;

                float[] Obs = { dir.x, dir.y, dir.z, dist, health, 1 };
                _detectedEnemiesSensor.AppendObservation(Obs);

            }

            foreach (var agent in _envController.VehicleList)
            {
                if (agent.Team != _vehicleManager.Team || agent.MemberID == _vehicleManager.MemberID)
                    continue;

                if (agent.Health == 0)
                    continue;

                Vector3 dir = Vector3.Normalize(transform.InverseTransformDirection(agent.gameObject.transform.localPosition - transform.localPosition));
                float dist = Vector3.Distance(agent.gameObject.transform.localPosition, transform.localPosition) / 700.0f;
                float health = agent.Health * 0.01f;

                float[] Obs = { dir.x, dir.y, dir.z, dist, health, (int)agent.VehicleType };
                _teammateSensor.AppendObservation(Obs);
            }
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        if (_vehicleManager.Health == 0) return;
        _vehicleManager.IsShooting = actions.DiscreteActions[0];

        _vehicleController.Pitch = actions.ContinuousActions[0];
        _vehicleController.Yaw = actions.ContinuousActions[1];
        _vehicleController.Roll = actions.ContinuousActions[2];
        _vehicleController.Throttle = actions.ContinuousActions[3];
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        ActionSegment<int> discreteActions = actionsOut.DiscreteActions;
        discreteActions[0] = Input.GetMouseButton(0) ? 1 : 0;

        ActionSegment<float> continousActions = actionsOut.ContinuousActions;
        continousActions[0] = Mathf.Clamp(-_mousePosDelta.y / Screen.height * 15.0f, -1.0f, 1.0f); //PITCH
        continousActions[1] = Mathf.Clamp(_mousePosDelta.x / Screen.width * 15.0f, -1.0f, 1.0f); //YAW
        continousActions[2] = Input.GetKey(KeyCode.A) ? 1.0f : (Input.GetKey(KeyCode.D) ? -1.0f : 0.0f); //ROLL
        continousActions[3] = Input.GetKey(KeyCode.W) ? 1.0f : (Input.GetKey(KeyCode.S) ? -1.0f : 0.0f); //THROTTLE


        _mousePosDelta = Vector3.zero;
    }

    public void Update()
    {
        _mousePosDelta += Input.mousePositionDelta;
    }

}
