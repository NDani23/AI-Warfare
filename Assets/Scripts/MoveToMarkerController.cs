using UnityEngine;
using UnityEngine.PlayerLoop;

public class MoveToMarkerMarkerController : MonoBehaviour
{
    private VehicleAgent _agent;
    private Vector3 _targetGlobalPosition;

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
    }

    public void SetEnvSpacePosition(Vector3 envSpacePosition)
    {
        _targetGlobalPosition = envSpacePosition + _agent.EnvController.transform.position;
    }
}
