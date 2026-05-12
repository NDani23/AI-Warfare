using UnityEngine;
using UnityEngine.PlayerLoop;

public class CommandMarkerController : MonoBehaviour
{
    private TankManager _agent;
    private Vector3 _targetGlobalPosition;
    private Transform _followTarget;

    // Only for training
    private float distanceToGoToPoint = 0;
    private float startDistanceToGoToPoint = 0;

    void Awake()
    {
        _agent = GetComponentInParent<TankManager>();
    }

    void FixedUpdate()
    {
        if (_followTarget != null)
        {
            _targetGlobalPosition = _followTarget.position;
        }

        transform.position = _targetGlobalPosition;
        transform.localPosition = new Vector3(
            Mathf.Clamp(transform.localPosition.x, -400f, 400f),
            0.0f,
            Mathf.Clamp(transform.localPosition.z, -400f, 400f));

        // float currentDistanceToGoToPoint = Vector3.Distance(_agent.transform.position, _targetGlobalPosition);
        // float distanceDelta = Mathf.Max(distanceToGoToPoint - currentDistanceToGoToPoint, 0);
        // _agent.AddRewardToDriver(distanceDelta / startDistanceToGoToPoint * 0.4f);
        // distanceToGoToPoint = currentDistanceToGoToPoint;
    }

    public void SetEnvSpacePosition(Vector3 envSpacePosition)
    {
        _followTarget = null;
        _targetGlobalPosition = envSpacePosition + _agent.EnvController.transform.position;
        startDistanceToGoToPoint = Mathf.Max(Vector3.Distance(_agent.transform.position, _targetGlobalPosition), 0.1f);
        distanceToGoToPoint = startDistanceToGoToPoint;
    }

    public void SetFollowTarget(Transform target)
    {
        _followTarget = target;
        if (_followTarget != null)
        {
            _targetGlobalPosition = _followTarget.position;
            startDistanceToGoToPoint = Mathf.Max(Vector3.Distance(_agent.transform.position, _targetGlobalPosition), 0.1f);
            distanceToGoToPoint = startDistanceToGoToPoint;
        }
    }
}
