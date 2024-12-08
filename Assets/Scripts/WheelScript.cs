using UnityEngine;

public class WheelScript : MonoBehaviour
{

    public bool steer;
    public bool invertSteer;
    public bool power;

    public float SteerAngle { get; set; }
    public float Torque { get; set; }
    public float BreakTorque { get; set; }


    [SerializeField] private WheelCollider wheelCollider;
    [SerializeField] private Transform wheelTransform;
    public void StopRotation()
    {
        wheelTransform.rotation = Quaternion.Euler(0, 0, 0);
    }

    void Start()
    {
        wheelCollider = GetComponentInChildren<WheelCollider>();
        wheelTransform = GetComponentInChildren<MeshRenderer>().GetComponent<Transform>();
    }


    void Update()
    {
        wheelCollider.GetWorldPose(out Vector3 pos, out Quaternion rot);
        wheelTransform.position = pos;
        wheelTransform.rotation = rot;
    }

    void FixedUpdate()
    {
        
        if (steer)
        {
            wheelCollider.steerAngle = Mathf.Lerp(wheelCollider.steerAngle, SteerAngle * (invertSteer ? -1 : 1), 0.5f);
        }

        if (power)
        {
            wheelCollider.brakeTorque = BreakTorque;
            wheelCollider.motorTorque = Torque;
        }

    }

    public WheelCollider GetCollider()
    {
        return wheelCollider;
    }
}
