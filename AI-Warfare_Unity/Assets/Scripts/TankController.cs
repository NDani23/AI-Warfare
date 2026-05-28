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
    [SerializeField] private GameObject LeftTankTrack;
    [SerializeField] private GameObject RightTankTrack;
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

    private TankManager tankAgent;

    public Transform FirePosition => firePosition;

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
    public bool lockTurret { get; set; }
    private GameObject Bullet;
    public float coolDownTime { get; set; }

    private WheelCollider leftReferenceWheelCollider;
    private WheelCollider rightReferenceWheelCollider;

    private Vector4 leftTrackTextureOffset;
    private Vector4 rightTrackTextureOffset;
    private MaterialPropertyBlock trackMaterialPropertyBlock;

    private int baseTankTrackMapId = Shader.PropertyToID("_BaseMap_ST");
    private int TankTrackTextureMapId = Shader.PropertyToID("_MainTex_ST");

    private void Start()
    {
        tankAgent = GetComponent<TankManager>();
        _rigidbody.centerOfMass = centerOfMass.localPosition;
        towerTargetPosition = tankTower.position + tankTower.forward;

        Bullet = GameObject.Instantiate(bulletPrefab, this.transform).gameObject;
        Bullet.GetComponent<bullet_script>().SetShooter(tankAgent);
        Bullet.SetActive(false);

        aimDirection = tankCannon.forward;

        leftReferenceWheelCollider = wheels[4].GetComponentInChildren<WheelCollider>();
        rightReferenceWheelCollider = wheels[0].GetComponentInChildren<WheelCollider>();

        leftTrackTextureOffset = new Vector4(1.0f, 1.0f, 0.0f, 0.0f);
        rightTrackTextureOffset = new Vector4(1.0f, 1.0f, 0.0f, 0.0f);

        trackMaterialPropertyBlock = new MaterialPropertyBlock();


        coolDownTime = 3.0f;
    }
    public void setStartingState(int teamID, int memberID)
    {
        BottomCollider.tag = tankAgent.Team == Team.Red ? "RedAgent" : "YellowAgent";
        BodyCollider.tag = tankAgent.Team == Team.Red ? "RedAgent" : "YellowAgent";
        tankTower.gameObject.tag = tankAgent.Team == Team.Red ? "RedAgent" : "YellowAgent";
        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        lockTurret = false;

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

        if (teamID == (int)Team.Red)
        {
            transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 0f));
            transform.localPosition = new Vector3((150 - 150 * memberID) + Random.Range(-65.0f, 65.0f), 3f, -300);
        }
        else
        {

            transform.localRotation = Quaternion.Euler(new Vector3(0f, 180f, 0f));
            transform.localPosition = new Vector3((150 - 150 * memberID) + Random.Range(-65.0f, 65.0f), 3f, 300);
        }

        // Physics.SyncTransforms();
        // Vector3 localCandidatePos = new Vector3(Random.Range(-300, 300), 3.0f, Random.Range(-300, 300));
        // for(int i = 0; i < 100; i++)
        // {
        //     if(!Physics.CheckSphere(this.transform.parent.TransformPoint(localCandidatePos), 10.0f, ~LayerMask.GetMask("Ground")))
        //         break;
        //     localCandidatePos = new Vector3(Random.Range(-300, 300), 3.0f, Random.Range(-300, 300));
        // }

        // transform.localRotation = Quaternion.Euler(new Vector3(0f, Random.Range(0.0f, 360.0f), 0f));
        // transform.localPosition = localCandidatePos;

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

        if (tankAgent != null && tankAgent.IsHeuristicOnlyMode() && !lockTurret)
        {
            RotateTower(aimDirection);
            RotateCannon(aimDirection);
        }
        else
        {
            float currentPitch = tankCannon.localEulerAngles.x;
            if (currentPitch > 180f) currentPitch -= 360f;
            currentPitch += -VerticalAimInput * cannonRotationSpeed * Time.fixedDeltaTime;
            tankCannon.localRotation = Quaternion.Euler(Mathf.Clamp(currentPitch, -25f, 5f), 0f, 0f);

            tankTower.Rotate(Vector3.up * (towerRotationSpeed * HorizontalAimInput * Time.fixedDeltaTime));
        }

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

        rotateTankTracks();

        coolDownTime = Mathf.Max(0, coolDownTime - Time.fixedDeltaTime);
    }

    private void RotateTower(Vector3 targetDirection)
    {
        Vector3 directionToTarget = Vector3.ProjectOnPlane(targetDirection, tankTower.up);
        Quaternion towerTargetDirection = Quaternion.LookRotation(directionToTarget, tankTower.up);
        Quaternion from = Quaternion.LookRotation(tankTower.forward, tankTower.up);

        tankTower.rotation = Quaternion.RotateTowards(from, towerTargetDirection, towerRotationSpeed * Time.fixedDeltaTime);
    }

    private void RotateCannon(Vector3 targetDirection)
    {
        float currentPitch = tankCannon.localEulerAngles.x;
        if (currentPitch > 180f) currentPitch -= 360f;

        Vector3 localTargetDirection = tankTower.InverseTransformDirection(targetDirection.normalized);
        float targetPitch = Mathf.Atan2(-localTargetDirection.y, localTargetDirection.z) * Mathf.Rad2Deg;
        targetPitch = Mathf.Clamp(targetPitch, -25f, 5f);

        float maxPitchStep = cannonRotationSpeed * Time.fixedDeltaTime;
        float nextPitch = Mathf.MoveTowards(currentPitch, targetPitch, maxPitchStep);
        tankCannon.localRotation = Quaternion.Euler(nextPitch, 0f, 0f);

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

    private void rotateTankTracks()
    {
        if(leftReferenceWheelCollider != null && LeftTankTrack != null)
        {
            leftTrackTextureOffset.w -= leftReferenceWheelCollider.rpm * Time.fixedDeltaTime * 0.01f;
            leftTrackTextureOffset.w %= 1;
            trackMaterialPropertyBlock.SetVector(TankTrackTextureMapId, leftTrackTextureOffset);
            LeftTankTrack.GetComponent<Renderer>().SetPropertyBlock(trackMaterialPropertyBlock);
        }

        if(rightReferenceWheelCollider != null && RightTankTrack != null)
        {
            rightTrackTextureOffset.w -= rightReferenceWheelCollider.rpm * Time.fixedDeltaTime * 0.01f;
            rightTrackTextureOffset.w %= 1;
            trackMaterialPropertyBlock.SetVector(TankTrackTextureMapId, rightTrackTextureOffset);
            RightTankTrack.GetComponent<Renderer>().SetPropertyBlock(trackMaterialPropertyBlock);
        }
    }
}
