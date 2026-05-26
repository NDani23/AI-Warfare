using UnityEngine;

public class CommandTargetMarkerController : MonoBehaviour
{
    [SerializeField] private GameObject GoToIcon;
    [SerializeField] private GameObject TargetIcon;
    [SerializeField] private GameObject gridComandMarker;
    [SerializeField] private float fixedZRotation = 0f;

    private TargetPracticeController _targetPracticeController;

    private TankManager _agent;
    private GameObject? _followTarget;
    public GameObject FollowTarget => _followTarget;
    private Vector3 _targetGlobalPosition;

    private float _timeInZone = 0.0f;
    private float _targetTimeInZone = 5.0f;

    private float distanceToGoToPoint = 0;
    private float startDistanceToGoToPoint = 0;
    
    void Awake()
    {
        _agent = GetComponentInParent<TankManager>();
        _targetPracticeController = GetComponentInParent<TargetPracticeController>();
    }
    void Start()
    {
        GoToIcon.SetActive(false);
        TargetIcon.SetActive(false);
    }
    void FixedUpdate()
    {
        if(_followTarget != null)
        {
            _targetGlobalPosition = _followTarget.transform.position;
            transform.rotation = Quaternion.Euler(0f, 0f, _followTarget.transform.eulerAngles.z);
            _agent.IsInZone = false;
        }
        else
        {
            transform.rotation = Quaternion.Euler(0f, 0f, fixedZRotation);
        }

        this.transform.position = _targetGlobalPosition;

        gridComandMarker.transform.position = transform.position;
        gridComandMarker.transform.localPosition = new Vector3(
            Mathf.Clamp(transform.localPosition.x, -400f, 400f),
            0.0f,
            Mathf.Clamp(transform.localPosition.z, -400f, 400f));

        UpdateAgentInZoneStatus();

        float currentDistanceToGoToPoint = Vector3.Distance(_agent.transform.position, _targetGlobalPosition);
        float distanceDelta = distanceToGoToPoint - currentDistanceToGoToPoint;
        if(!_agent.IsInZone)
        {
            float multiplier = _agent.ActiveCommand == CommandType.GoToPosition ? 1.0f : 0.5f;
            _agent.AddRewardToDriver(distanceDelta / startDistanceToGoToPoint * multiplier);
        }
        distanceToGoToPoint = currentDistanceToGoToPoint;

        if (_agent.ActiveCommand != CommandType.EliminateTarget && _agent.IsInZone)
        {
             _timeInZone += Time.fixedDeltaTime;
            _agent.AddRewardToDriver(Time.fixedDeltaTime * 0.25f);
            if (_timeInZone >= _targetTimeInZone)
            {
                _timeInZone = 0.0f;
                _targetPracticeController?.HandleGoToReached(_agent);
                _agent.IsInZone = false;
                _targetTimeInZone = Random.Range(3.0f, 15.0f);
            }
        }
    }

    public void SetFollowTarget(GameObject target)
    {
        _followTarget = target;
        _targetGlobalPosition = _followTarget.transform.position;

        startDistanceToGoToPoint = Mathf.Max(Vector3.Distance(_agent.transform.position, _targetGlobalPosition), 0.1f);
        distanceToGoToPoint = startDistanceToGoToPoint;
    }

    public void SetTargetGlobalPosition(Vector3 position)
    {
        _followTarget = null;
        _targetGlobalPosition = position;        
        startDistanceToGoToPoint = Mathf.Max(Vector3.Distance(_agent.transform.position, _targetGlobalPosition), 0.1f);
        distanceToGoToPoint = startDistanceToGoToPoint;
    }

    public void ShowCommandIcon(bool show)
    {
        if (!show)
        {
            GoToIcon.SetActive(false);
            TargetIcon.SetActive(false);
            return;       
        }

        if (_agent.ActiveCommand == CommandType.EliminateTarget)
        {
            GoToIcon.SetActive(false);
            TargetIcon.SetActive(true);
        }
        else if (_agent.ActiveCommand == CommandType.GoToPosition)
        {
            GoToIcon.SetActive(true);
            TargetIcon.SetActive(false);
        }
    }

    private void UpdateAgentInZoneStatus()
    {
        if(_agent.ActiveCommand == CommandType.EliminateTarget)
            return;

        if(Vector3.Distance(_agent.transform.position, transform.position) < 50.0f && !_agent.IsInZone)
        {
            AgentEnteredZone();
        }

        if(Vector3.Distance(_agent.transform.position, transform.position) >= 50.0f && _agent.IsInZone)
        {
            AgentExitedZone();
        }
    }

    private void AgentEnteredZone()
    {
        _agent.IsInZone = true;
        _timeInZone = 0.0f;
    }

    private void AgentExitedZone()
    {
        _agent.AddRewardToDriver(-0.5f);
        _agent.IsInZone = false;
        _timeInZone = 0.0f;
    }
}
