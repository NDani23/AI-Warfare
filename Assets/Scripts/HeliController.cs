using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;

public class HeliController : MonoBehaviour
{
    [SerializeField] private Rigidbody _rigidbody;
    [SerializeField] private Transform _rear_rotor;
    [SerializeField] private Transform _rotor;
    [SerializeField] private UnityEngine.UI.Image AimPointerImage;
    [SerializeField] private Transform _machineGunLeft;
    [SerializeField] private Transform _machineGunRight;
    [SerializeField] private Transform _leftShootPosition;
    [SerializeField] private Transform _rightShootPosition;
    [SerializeField] private TrailRenderer _bulletTrail;
    [SerializeField] private GameObject _colliders;
    [SerializeField] private float _shootDelay = 0.1f;
    [SerializeField] private ParticleSystem ExplodeParticles;
    [SerializeField] private ParticleSystem SmokeParticles;
    [SerializeField] private Material BurntMaterial;
    [SerializeField] private Material MainMaterial;

    private HitInfo _leftGunHitInfo;
    private HitInfo _rightGunHitInfo;

    public HitInfo LeftGunHitInfo
    {
        get { return _leftGunHitInfo; }
        set { _leftGunHitInfo = value; }
    }

    public HitInfo RightGunHitInfo
    {
        get { return _rightGunHitInfo; }
        set { _rightGunHitInfo = value; }
    }

    public Rigidbody Rigidbody
    {
        get { return _rigidbody; }
    }

    private HeliAgent _agent;

    public float rotor_speed = 1000.0f;
    public float base_acceleration;
    public float roll_speed;
    public float pitch_speed;
    public float yaw_speed;

    private float _throttle;
    public float Throttle
    {
        get { return _throttle; }
        set { _throttle = value; }
    }

    private float _roll;
    public float Roll
    {
        get { return _roll; }
        set { _roll = value; }
    }

    private float _pitch;
    public float Pitch
    {
        get { return _pitch; }
        set 
        {
            _pitch = value;
        }
    }

    private float _yaw;
    public float Yaw
    {
        get { return _yaw; }
        set 
        {
            _yaw = value;
        }
    }

    private float gravity_magnification = 6;

    private int _isShooting = 0;
    public int IsShooting
    {
        get { return _isShooting; }
        set
        {
            _isShooting = _overHeatCooldown == 0.0 ? value : 0;
        }
    }

    private float _gunOverHeatStatus = 0.0f;
    private float _overHeatCooldown = 0.0f;
    private float _lastShootTime = 0.0f;

    private Vector3? _aimDirection = null;

    private void Awake()
    {
        _agent = this.GetComponent<HeliAgent>();

    }
    void Start()
    {
    }

    public void setStartingState(int teamID, int memberID)
    {

        _colliders.tag = _agent.Team == Team.Red ? "RedAgent" : "YellowAgent";
        setMaterial();

        if (SmokeParticles.isPlaying)
        {
            SmokeParticles.Clear();
            SmokeParticles.Stop();
        }


        _rigidbody.linearVelocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;

        if (teamID == (int)Team.Red)
        {
            transform.localRotation = Quaternion.Euler(new Vector3(0f, 0f, 0f));
            transform.localPosition = new Vector3((200 - 100 * memberID) + UnityEngine.Random.Range(-40.0f, 40.0f), UnityEngine.Random.Range(10.0f, 200.0f), -300);
        }
        else
        {

            transform.localRotation = Quaternion.Euler(new Vector3(0f, 180f, 0f));
            transform.localPosition = new Vector3((200 - 100 * memberID) + UnityEngine.Random.Range(-40.0f, 40.0f), UnityEngine.Random.Range(10.0f, 200.0f), 300);
        }
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        _rigidbody.AddForce(Physics.gravity * (gravity_magnification - 1), ForceMode.Acceleration);

        if (_agent.Health == 0) return;
        
        _rotor.Rotate(Vector3.up * (rotor_speed * Time.fixedDeltaTime));
        _rear_rotor.Rotate(-Vector3.right * (rotor_speed * 2.5f * Time.fixedDeltaTime));
        _rigidbody.AddForce(transform.up * base_acceleration * gravity_magnification, ForceMode.Acceleration);

        if(Throttle > 0)
        {
            _rigidbody.AddForce(transform.up * 10 * gravity_magnification * Mathf.Abs(Throttle), ForceMode.Acceleration);
        }
        else if(Throttle < 0)
        {
            _rigidbody.AddForce(Vector3.down * 7 * gravity_magnification * Mathf.Abs(Throttle), ForceMode.Acceleration);
        }

        _rigidbody.AddTorque(transform.forward * _roll * roll_speed, ForceMode.Acceleration);
        _rigidbody.AddTorque(transform.right * _pitch * pitch_speed, ForceMode.Acceleration);
        _rigidbody.AddTorque(transform.up * _yaw * yaw_speed, ForceMode.Acceleration);

        HandleShooting();
    }

    void HandleShooting()
    {
        if (_isShooting == 1)
        {
            float _gunRotateSpeed = 1000;
            _machineGunLeft.Rotate(-Vector3.forward * _gunRotateSpeed * Time.fixedDeltaTime, Space.Self);
            _machineGunRight.Rotate(-Vector3.forward * _gunRotateSpeed * Time.fixedDeltaTime, Space.Self);
            _gunOverHeatStatus += Time.fixedDeltaTime / 5.0f;
            if (_gunOverHeatStatus >= 1.0f)
            {
                _overHeatCooldown = 5.0f;
                _isShooting = 0;
                _gunOverHeatStatus = 1.0f;
                //_agent.AddReward(-1.0f);
            }

            if(_lastShootTime + _shootDelay < Time.time)
            {
                //var tracer = Instantiate(_bulletTrail, _leftShootPosition.position, Quaternion.identity, this.transform);
                //tracer.AddPosition(_leftShootPosition.position);
                //tracer.transform.position = _leftGunHitInfo.hitPosition;

                if (_leftGunHitInfo.hitTag != -1)
                {
                    _leftGunHitInfo.hitGameObject.transform.parent.GetComponent<IVehicleAgent>().Hit(1);
                }

                if (_rightGunHitInfo.hitTag != -1)
                {
                    _rightGunHitInfo.hitGameObject.transform.parent.GetComponent<IVehicleAgent>().Hit(1);
                }

                //var tracer2 = Instantiate(_bulletTrail, _rightShootPosition.position, Quaternion.identity, this.transform);
                //tracer2.AddPosition(_rightShootPosition.position);
                //tracer2.transform.position = _rightGunHitInfo.hitPosition;

                //if (_rightGunHitInfo.hitTag != -1)
                //{
                //    //_rightGunHitInfo.hitGameObject.transform.parent.GetComponent<IVehicleAgent>().Hit(1);
                //    if (_rightGunHitInfo.hitGameObject is not null)
                //        _rightGunHitInfo.hitGameObject.transform.parent.GetComponent<TargetScript>().Hit(1);
                //}

                //if (_leftGunHitInfo.hitTag != -1)
                //{
                //    //_rightGunHitInfo.hitGameObject.transform.parent.GetComponent<IVehicleAgent>().Hit(1);
                //    if (_leftGunHitInfo.hitGameObject is not null)
                //        _leftGunHitInfo.hitGameObject.transform.parent.GetComponent<TargetScript>().Hit(1);
                //}

                _lastShootTime = Time.time;
            }
        }
        else
        {
            if (_overHeatCooldown != 0.0f)
            {
                _overHeatCooldown = Mathf.Max(0.0f, _overHeatCooldown - Time.fixedDeltaTime);
            }
            else
            {
                _gunOverHeatStatus = Mathf.Max(0.0f, _gunOverHeatStatus - Time.fixedDeltaTime / 3.0f);
            }
        }
    }

    public Vector2 GetScreenSpaceAimPos()
    {
        return Camera.main.WorldToScreenPoint(_machineGunLeft.position + _machineGunLeft.forward * 400.0f);
    }

    public void setMaterial(Material mat = null)
    {
        if (mat == null)
        {
            if (_agent.Health > 0)
            {
                this.GetComponent<MeshRenderer>().material = MainMaterial;
            }
            else
            {
                this.GetComponent<MeshRenderer>().material = BurntMaterial;
            }
        }
        else
        {
            this.GetComponent<MeshRenderer>().material = mat;
        }
    }

    public void setDeadState()
    {
        _colliders.tag = "Untagged";
        setMaterial();
        //ExplodeParticles.Play();
        //SmokeParticles.Play();
    }

    public float getGunOverheatStatus()
    {
        return _gunOverHeatStatus;
    }

}
