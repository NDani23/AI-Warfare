using UnityEngine;

public class GoToTrainerController : MonoBehaviour
{
    private TargetPracticeController _targetPracticeController;
    private Transform _followTarget;
    private Vector3 _localTargetPosition;
    [SerializeField] private TankManager _currentTank;
    private float _timeInZone = 0.0f;
    private float _targetTimeInZone = 5.0f;

    void Awake()
    {
        _targetPracticeController = GetComponentInParent<TargetPracticeController>();
    }

    void FixedUpdate()
    {
        if (_followTarget != null)
        {
            Vector3 followPos = _followTarget.position;
            followPos.y = 3.0f;
            transform.position = followPos;

            _currentTank.IsInZone = false;
            _timeInZone = 0.0f;
        }
        else
        {
            Vector3 localPos = _localTargetPosition;
            localPos.y = 3.0f;
            transform.localPosition = localPos;

            if (_currentTank != null && _currentTank.IsInZone)
            {
                _timeInZone += Time.fixedDeltaTime;
                _currentTank.AddRewardToDriver(Time.fixedDeltaTime * 0.25f);
                if (_timeInZone >= _targetTimeInZone)
                {
                    _timeInZone = 0.0f;
                    _currentTank.IsInZone = false;
                    _targetPracticeController?.HandleGoToReached(_currentTank);
                    _targetTimeInZone = Random.Range(1.0f, 10.0f);
                }
            }
        }
    }

    public void SetGoToLocalPosition(Vector3 localPosition)
    {
        _followTarget = null;
        _localTargetPosition = localPosition;
    }

    public void SetFollowTarget(Transform target)
    {
        _followTarget = target;
        if (_followTarget != null && transform.parent != null)
        {
            _localTargetPosition = transform.parent.InverseTransformPoint(_followTarget.position);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (_targetPracticeController == null || _followTarget != null)
            return;

        if (other.gameObject.tag != "RedAgent" && other.gameObject.tag != "YellowAgent")
            return;

        TankManager tank = other.transform.parent.GetComponent<TankManager>();
        if (tank == null || tank != _currentTank)
            return;

        //_targetPracticeController?.HandleGoToReached(tank);

        _currentTank.IsInZone = true;
        _timeInZone = 0.0f;
    }

    public TankManager GetCurrentTank()
    {
        return _currentTank;
    }

    void OnTriggerExit(Collider other)
    {
        if (_followTarget != null)
            return;

        if (other.transform.parent.GetComponent<TankManager>() == _currentTank)
        {
            _currentTank.IsInZone = false;
            _timeInZone = 0.0f;
        }
    }
}
