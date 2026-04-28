using UnityEngine;
using UnityEngine.PlayerLoop;

public class MoveToMarkerMarkerController : MonoBehaviour
{
    private VehicleAgent _agent;
    private Vector3 _targetGlobalPosition;

    // Only for training
    private float distanceToGoToPoint = 0;

    void Awake()
    {
        _agent = GetComponentInParent<VehicleAgent>();
    }

    void Update()
    {
        transform.position = _targetGlobalPosition;
        transform.localPosition = new Vector3(
            Mathf.Clamp(transform.localPosition.x, -400f, 400f),
            0.0f,
            Mathf.Clamp(transform.localPosition.z, -400f, 400f));

        float currentDistanceToGoToPoint = Vector3.Distance(_agent.transform.position, _targetGlobalPosition);
        _agent.AddReward(Time.deltaTime / 60.0f * ((distanceToGoToPoint - currentDistanceToGoToPoint) * 10.0f));
        distanceToGoToPoint = currentDistanceToGoToPoint;
    }

    public void SetEnvSpacePosition(Vector3 envSpacePosition)
    {
        _targetGlobalPosition = envSpacePosition + _agent.EnvController.transform.position;
        distanceToGoToPoint = Vector3.Distance(_agent.transform.position, _targetGlobalPosition);
    }
}
