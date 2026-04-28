using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.MLAgents.Policies;
using Unity.VisualScripting;
using UnityEngine;

public class TankController : MonoBehaviour, IVehicleController
{
    [SerializeField] private Transform tankTower;
    [SerializeField] private Transform tankCannon;
    [SerializeField]  private Transform centerOfMass;
    [SerializeField] private Transform firePosition;
    [SerializeField] private Transform recoilPosition;
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private GameObject BodyCollider;
    [SerializeField] private GameObject BottomCollider;
    [SerializeField] private WheelScript[] wheels;
    [SerializeField] private ParticleSystem FireParticles;
    [SerializeField] private ParticleSystem DirtParticles1;
    [SerializeField] private ParticleSystem DirtParticles2;
    [SerializeField] private ParticleSystem ExplodeParticles;
    [SerializeField] private ParticleSystem SmokeParticles;
    [SerializeField] private Material BurntMaterial;
    [SerializeField] private Material MainMaterial;
    [SerializeField] private Material WheelMaterial;

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
    public Vector3 aimDirection { get; set; }
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

        aimDirection = tankCannon.forward;

        coolDownTime = 3.0f;
    }
    public void setStartingState(int teamID, int memberID)
    {
        BottomCollider.tag = tankAgent.Team == Team.Red ? "RedAgent" : "YellowAgent";
        BodyCollider.tag = tankAgent.Team == Team.Red ? "RedAgent" : "YellowAgent";
        tankTower.gameObject.tag = tankAgent.Team == Team.Red ? "RedAgent" : "YellowAgent";
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

        setMaterial();

        if (SmokeParticles.isPlaying)
        {
            SmokeParticles.Clear();
            SmokeParticles.Stop();
        }

        foreach (var wheel in wheels)
        {
            wheel.BreakTorque = 0;
            wheel.Torque = 0;
            wheel.StopRotation();
        }

        coolDownTime = 3.0f;

        // if (teamID == (int)Team.Red)
        // {
        //     transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 0f));
        //     transform.localPosition = new Vector3((200 - 100 * memberID) + Random.Range(-40.0f, 40.0f), 3f, -300);
        // }
        // else
        // {

        //     transform.localRotation = Quaternion.Euler(new Vector3(0f, 180f, 0f));
        //     transform.localPosition = new Vector3((200 - 100 * memberID) + Random.Range(-40.0f, 40.0f), 3f, 300);
        // }

        Physics.SyncTransforms();
        Vector3 localCandidatePos = new Vector3(Random.Range(-300, 300), 3.0f, Random.Range(-300, 300));
        for(int i = 0; i < 100; i++)
        {
            if(!Physics.CheckSphere(this.transform.parent.TransformPoint(localCandidatePos), 10.0f, ~LayerMask.GetMask("Ground")))
                break;
            localCandidatePos = new Vector3(Random.Range(-300, 300), 3.0f, Random.Range(-300, 300));
        }

        transform.localRotation = Quaternion.Euler(new Vector3(0f, Random.Range(0.0f, 360.0f), 0f));
        transform.localPosition = localCandidatePos;

        tankCannon.localRotation = Quaternion.Euler(0, 0, 0);
        tankTower.localRotation = Quaternion.Euler(0, 0, 0);

        aimDirection = tankCannon.forward;
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
        //if (tankAgent.getHealth() == 0) return;
        foreach (var wheel in wheels)
        {
            if (_rigidbody.linearVelocity.magnitude > 2.0f && transform.InverseTransformDirection(_rigidbody.linearVelocity).z * Throttle < 0)
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


        if (_rigidbody.angularVelocity.magnitude < 0.7f)
        {

            _rigidbody.AddRelativeTorque((Vector3.up * Steer * turnSpeed), ForceMode.Acceleration);

            if (_rigidbody.angularVelocity.magnitude < 0.01f && Steer != 0 && _rigidbody.linearVelocity.magnitude < 0.1f)
            {
                wheels[0].Torque = motorTorque * Time.fixedDeltaTime;

            }
        }

        float currentPitch = tankCannon.localEulerAngles.x;
        if (currentPitch > 180f) currentPitch -= 360f;
        currentPitch += -VerticalAimInput * cannonRotationSpeed * Time.fixedDeltaTime;
        tankCannon.localRotation = Quaternion.Euler(Mathf.Clamp(currentPitch, -25f, 5f), 0f, 0f);

        tankTower.Rotate(Vector3.up * (towerRotationSpeed * HorizontalAimInput * Time.fixedDeltaTime));

        HandleShooting();


        if (Vector3.Dot(transform.forward, _rigidbody.linearVelocity) >= 0)
        {
            DirtParticles1.transform.localRotation = Quaternion.Euler(-160, 0, 0);
            DirtParticles2.transform.localRotation = Quaternion.Euler(-160, 0, 0);
        }
        else
        {
            DirtParticles1.transform.localRotation = Quaternion.Euler(-40, 0, 0);
            DirtParticles2.transform.localRotation = Quaternion.Euler(-40, 0, 0);
        }

        DirtParticles1.startSpeed = _rigidbody.linearVelocity.magnitude;
        DirtParticles2.startSpeed = _rigidbody.linearVelocity.magnitude;

        coolDownTime = Mathf.Max(0, coolDownTime - Time.fixedDeltaTime);
    }

    private void RotateTower(Vector3 targetDirection, float aimCurve)
    {
        Vector3 directionToTarget = Vector3.ProjectOnPlane(targetDirection, tankTower.up);
        Quaternion towerTargetDirection = Quaternion.LookRotation(directionToTarget, tankTower.up);
        Quaternion from = Quaternion.LookRotation(tankTower.forward, tankTower.up);

        tankTower.rotation = Quaternion.RotateTowards(from, towerTargetDirection, (aimCurve * towerRotationSpeed) * Time.fixedDeltaTime);
    }

    private void RotateCannon(Vector3 targetDirection)
    {
        Vector3 directionToTarget = Vector3.ProjectOnPlane(targetDirection, tankTower.right);
        Quaternion towerTargetDirection = Quaternion.LookRotation(directionToTarget, tankTower.right);

        Quaternion from = Quaternion.LookRotation(tankCannon.forward, tankTower.right);

        tankCannon.rotation = Quaternion.RotateTowards(from, towerTargetDirection, cannonRotationSpeed * Time.fixedDeltaTime);

        float currentPitch = tankCannon.localEulerAngles.x;
        if (currentPitch > 180f) currentPitch -= 360f;
        tankCannon.localRotation = Quaternion.Euler(Mathf.Clamp(currentPitch, -20f, 5f), 0f, 0f);

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

        return tankCannon.position + tankCannon.forward * 300.0f;
    }

    public Transform getTowerTransform()
    {
        return tankTower.transform;
    }

    public Transform getCannonTransform()
    {
        return tankCannon.transform;
    }

    public Vector2 GetScreenSpaceAimPos()
    {
        return Camera.main.WorldToScreenPoint(GetAimPos());
    }

    public void setMaterial(Material mat = null)
    {
        if(mat == null)
        {
            if (tankAgent.Health > 0)
            {
                this.GetComponent<MeshRenderer>().material = MainMaterial;
                tankTower.gameObject.GetComponent<MeshRenderer>().material = MainMaterial;
                tankCannon.gameObject.GetComponent<MeshRenderer>().material = MainMaterial;
            }
            else
            {
                this.GetComponent<MeshRenderer>().material = BurntMaterial;
                tankTower.gameObject.GetComponent<MeshRenderer>().material = BurntMaterial;
                tankCannon.gameObject.GetComponent<MeshRenderer>().material = BurntMaterial;
            }


            foreach (var wheel in wheels)
            {
                wheel.gameObject.GetComponentInChildren<MeshRenderer>().material = WheelMaterial;
            }
        }    
        else
        {
            this.GetComponent<MeshRenderer>().material = mat;
            tankTower.gameObject.GetComponent<MeshRenderer>().material = mat;
            tankCannon.gameObject.GetComponent<MeshRenderer>().material = mat;

            foreach(var wheel in wheels)
            {
                wheel.gameObject.GetComponentInChildren<MeshRenderer>().material = mat;
            }
        }
    }

    public void setDeadState()
    {
        HorizontalAimInput = 0.0f;
        VerticalAimInput = 0.0f;
        FireInput = 0;
        Steer = 0;
        Throttle = 0;

        BodyCollider.tag = "Untagged";
        BottomCollider.tag = "Untagged";
        tankTower.gameObject.tag = "Untagged";

        setMaterial();
        ExplodeParticles.Play();
        SmokeParticles.Play();
    }
}
