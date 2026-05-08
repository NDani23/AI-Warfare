using UnityEngine;

public class GoToTrainerController : MonoBehaviour
{
    private TargetPracticeController _targetPracticeController;
    private Transform _followTarget;
    private Vector3 _localTargetPosition;
    private TankManager _currentTank;
    private float _timeInZone = 0.0f;
    private bool _isInZone = false;

    void Awake()
    {
        _targetPracticeController = GetComponentInParent<TargetPracticeController>();
    }

    void Update()
    {
        if (_followTarget != null)
        {
            Vector3 followPos = _followTarget.position;
            followPos.y = 3.0f;
            transform.position = followPos;

            _isInZone = false;
            _timeInZone = 0.0f;
        }
        else
        {
            Vector3 localPos = _localTargetPosition;
            localPos.y = 3.0f;
            transform.localPosition = localPos;

            if (_isInZone && _currentTank != null)
            {
                _timeInZone += Time.deltaTime;
                if (_timeInZone >= 5.0f)
                {
                    _timeInZone = 0.0f;
                    _isInZone = false;
                    _targetPracticeController?.HandleGoToReached(_currentTank);
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
        if (tank == null)
            return;

        //_targetPracticeController?.HandleGoToReached(tank);

        _currentTank = tank;
        _isInZone = true;
        _timeInZone = 0.0f;
    }

    void OnTriggerExit(Collider other)
    {
        if (_followTarget != null)
            return;

        if (other.transform.parent.GetComponent<TankManager>() == _currentTank)
        {
            _isInZone = false;
            _timeInZone = 0.0f;
        }
    }
}
