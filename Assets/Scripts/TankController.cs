using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.MLAgents.Policies;
using Unity.VisualScripting;
using UnityEngine;

public class TankController : MonoBehaviour
{
    [SerializeField] private Transform tankTower;
    [SerializeField] private Transform tankCannon;
    [SerializeField]  private Transform centerOfMass;
    [SerializeField] private Transform firePosition;
    [SerializeField] private Transform recoilPosition;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private WheelScript[] wheels;
    [SerializeField] private ParticleSystem FireParticles;
    [SerializeField] private ParticleSystem DirtParticles1;
    [SerializeField] private ParticleSystem DirtParticles2;


    [SerializeField] private Transform bulletPrefab;

    private TankAgent tankAgent;

    public float motorTorque = 100f;
    public float breakTorque = 100f;
    public float turnSpeed = 10.0f;
    public float towerRotationSpeed = 10.0f;
    public float cannonRotationSpeed = 10.0f;
    public float BulletFoce = 10000.0f;

    private Vector3 towerTargetPosition;

    public float Steer { get; set; }
    public float Throttle { get; set; }
    public float Break { get; set; }
    public float HorizontalAimInput { get; set; }
    public float VerticalAimInput { get; set; }
    public int FireInput { get; set; }
    public Vector3? AimDirection { get; set; }
    private GameObject Bullet;
    public float coolDownTime { get; set; }

    private void Start()
    {
        tankAgent = GetComponent<TankAgent>();
        _rigidbody.centerOfMass = centerOfMass.localPosition;
        towerTargetPosition = tankTower.position + tankTower.forward;

        Bullet = GameObject.Instantiate(bulletPrefab, this.transform).gameObject;
        Bullet.GetComponent<bullet_script>().SetShooter(tankAgent);
        Bullet.SetActive(false);

        AimDirection = null;


        coolDownTime = 3.0f;
    }
    public void setStartingState(int teamID, int memberID)
    {

        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

        foreach (var wheel in wheels)
        {
            wheel.BreakTorque = 0;
            wheel.Torque = 0;
            wheel.StopRotation();
        }

        coolDownTime = 3.0f;

        if (teamID == (int)Team.Yellow)
        {
            transform.localPosition = new Vector3((200 - 100 * memberID) + Random.Range(-40.0f, 40.0f), 3f, -300);
            transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 0f));
        }
        else
        {

            transform.localRotation = Quaternion.Euler(new Vector3(0f, 180f, 0f));
            transform.localPosition = new Vector3((200 - 100 * memberID) + Random.Range(-40.0f, 40.0f), 3f, 300);
        }

        tankCannon.localRotation = Quaternion.Euler(0, 0, 0);
        tankTower.localRotation = Quaternion.Euler(0, 0, 0);
    }

    public float getTowerRotation()
    {
        return Vector3.SignedAngle(transform.forward, tankTower.forward, transform.up) / 180;
    }

    public float getCannonRotation()
    {
        return -tankCannon.localRotation.x;
    }

    private void FixedUpdate()
    {
        foreach (var wheel in wheels)
        {
            if (_rigidbody.velocity.magnitude > 2.0f && transform.InverseTransformDirection(_rigidbody.velocity).z * Throttle < 0)
            {
                wheel.Torque = 0;
                wheel.BreakTorque = breakTorque;
            }
            else
            {
                wheel.BreakTorque = 0;
                wheel.Torque = Mathf.Clamp(Throttle, -0.5f, 1.0f) * motorTorque * Time.fixedDeltaTime * 100;
            }
        }


        if (_rigidbody.angularVelocity.magnitude < 0.8f)
        {

            _rigidbody.AddRelativeTorque(Vector3.up * Steer * turnSpeed, ForceMode.Acceleration);

            if (_rigidbody.angularVelocity.magnitude < 0.01f && Steer != 0 && _rigidbody.velocity.magnitude < 0.1f)
            {
                wheels[0].Torque = motorTorque * Time.fixedDeltaTime * 50;

            }
        }

        float aimCurve = 1.0f;

        if (AimDirection != null)
        {
            var aimDirection = Input.mousePosition;
            aimDirection.z = 500.0f;
            aimDirection = Camera.main.ScreenToWorldPoint(aimDirection);
            towerTargetPosition = aimDirection;
            HandleCannon(towerTargetPosition);
            aimCurve = Mathf.Sqrt(Mathf.Abs(Input.mousePosition.x / Screen.width * 2 - 1.0f));
        }
        else
        {
            towerTargetPosition = tankTower.position + tankTower.forward;
            towerTargetPosition = RotatePointAroundPivot(towerTargetPosition, tankTower.position, tankTower.up * HorizontalAimInput);

            tankCannon.localRotation = new Quaternion(Mathf.Clamp(tankCannon.localRotation.x + -0.1f * VerticalAimInput * (cannonRotationSpeed * 0.1f) * Time.fixedDeltaTime, -0.15f, 0.01f),
                                                      0.0f,
                                                      0.0f,
                                                      tankCannon.localRotation.w);
        }
        HandleTower(towerTargetPosition, aimCurve);
        HandleShooting();


        if (Vector3.Dot(transform.forward, _rigidbody.velocity) >= 0)
        {
            DirtParticles1.transform.localRotation = Quaternion.Euler(-160, 0, 0);
            DirtParticles2.transform.localRotation = Quaternion.Euler(-160, 0, 0);
        }
        else
        {
            DirtParticles1.transform.localRotation = Quaternion.Euler(-40, 0, 0);
            DirtParticles2.transform.localRotation = Quaternion.Euler(-40, 0, 0);
        }

        DirtParticles1.startSpeed = _rigidbody.velocity.magnitude;
        DirtParticles2.startSpeed = _rigidbody.velocity.magnitude;


        coolDownTime = Mathf.Max(0, coolDownTime - Time.fixedDeltaTime);
    }

    private void HandleTower(Vector3 targetPosition, float aimCurve)
    {
        Vector3 directionToTarget = Vector3.ProjectOnPlane(targetPosition - tankTower.position, tankTower.up);
        Quaternion towerTargetDirection = Quaternion.LookRotation(directionToTarget, tankTower.up);

        Quaternion from = Quaternion.LookRotation(tankTower.forward, tankTower.up);

        tankTower.rotation = Quaternion.RotateTowards(from, towerTargetDirection, (aimCurve * towerRotationSpeed) * Time.fixedDeltaTime);
    }

    private void HandleCannon(Vector3 targetPosition)
    {
        Vector3 directionToTarget = Vector3.ProjectOnPlane(targetPosition - tankCannon.position, tankTower.right);
        Quaternion towerTargetDirection = Quaternion.LookRotation(directionToTarget, tankTower.right);

        Quaternion from = Quaternion.LookRotation(tankCannon.forward, tankTower.right);

        tankCannon.rotation = Quaternion.RotateTowards(from, towerTargetDirection, cannonRotationSpeed * Time.fixedDeltaTime);

        tankCannon.localRotation = new Quaternion(Mathf.Clamp(tankCannon.localRotation.x, -0.1f, 0.01f),
                                          0.0f,
                                          0.0f,
                                          tankCannon.localRotation.w);

    }
    private Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Vector3 angles)
    {
        return Quaternion.Euler(angles) * (point - pivot) + pivot;
    }

    private void HandleShooting()
    {

        if (FireInput == 1 && coolDownTime == 0)
        {
            Bullet.GetComponent<bullet_script>().Shoot(firePosition.position, firePosition.rotation, tankCannon.forward, BulletFoce);
            _rigidbody.AddForceAtPosition(-tankCannon.forward * 250000.0f, recoilPosition.position);
            FireParticles.Play();
            coolDownTime = 3f;

        }
        FireInput = 0;
    }

    public Vector3 GetAimPos()
    {

        return tankCannon.position + tankCannon.forward * 500.0f;
    }

    public Transform getTowerTransform()
    {
        return tankTower.transform;
    }

    public Transform getCannonTransform()
    {
        return tankCannon.transform;
    }
}
