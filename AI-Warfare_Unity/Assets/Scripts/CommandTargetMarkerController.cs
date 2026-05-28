using UnityEngine;

public class CommandTargetMarkerController : MonoBehaviour
{
    [SerializeField] private GameObject GoToIcon;
    [SerializeField] private GameObject TargetIcon;
    [SerializeField] private GameObject gridComandMarker;
    [SerializeField] private float fixedZRotation = 0f;

    private TankManager _agent;
    private GameObject? _followTarget;
    public GameObject FollowTarget => _followTarget;
    private Vector3 _targetGlobalPosition;

    public Vector3 TargetGlobalPosition => _targetGlobalPosition;

    private bool showCommandIcon = false;
    public bool ShowCommandIcon
    {
        get => showCommandIcon;
        set => showCommandIcon = value;
    }

    void Awake()
    {
        _agent = GetComponentInParent<TankManager>();
    }
    void Start()
    {
        GoToIcon.SetActive(false);
        TargetIcon.SetActive(false);
    }
    void Update()
    {
        if(_agent.Health <= 0)
        {
            ShowCommandIcon = false;
            if(GoToIcon.activeSelf != false || TargetIcon.activeSelf != false)
            {
                GoToIcon.SetActive(false);
                TargetIcon.SetActive(false);   
            }
            return;
        }

        if(_followTarget != null)
        {
            _targetGlobalPosition = _followTarget.transform.position;
            transform.rotation = Quaternion.Euler(0f, _followTarget.transform.rotation.eulerAngles.y, 0f);
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

        if (!showCommandIcon)
        {
            if(GoToIcon.activeSelf != false || TargetIcon.activeSelf != false)
            {
                GoToIcon.SetActive(false);
                TargetIcon.SetActive(false);   
            }
        }
        else if (_agent.ActiveCommand == CommandType.EliminateTarget && TargetIcon.activeSelf == false)
        {
            GoToIcon.SetActive(false);
            TargetIcon.SetActive(true);
        }
        else if (_agent.ActiveCommand == CommandType.GoToPosition && GoToIcon.activeSelf == false)
        {
            GoToIcon.SetActive(true);
            TargetIcon.SetActive(false);
        }
    }

    public void SetFollowTarget(GameObject target)
    {
        _followTarget = target;
    }

    public void SetTargetGlobalPosition(Vector3 position)
    {
        _followTarget = null;
        _targetGlobalPosition = position;
    }
}
