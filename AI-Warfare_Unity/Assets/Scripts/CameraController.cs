using UnityEngine;
using UnityEngine.Events;

public class CameraController : MonoBehaviour
{
    [SerializeField] private float _transitionDuration = 2.0f;

    private Transform _topDownParent;

    private Vector3 _transitionStartPos;
    private Quaternion _transitionStartRot;

    private bool _inAgentView = false;

    public bool InAgentView => _inAgentView;

    private bool _transitioning = false;
    private float _transitionTime = 0f;

    private Transform currentParent;

    public UnityEvent ViewTransitionEnded;
    public UnityEvent ViewTransitionStarted;

    void Start()
    {
        _topDownParent = transform.parent;

        currentParent = _topDownParent;
    }

    void Update()
    {

        if (_transitioning)
        {
            _transitionTime += Time.deltaTime;
            float rawT = Mathf.Clamp01(_transitionTime / _transitionDuration);
            float posT = Mathf.SmoothStep(0f, 1f, rawT);
            float rotT = (Mathf.Pow(10f, rawT) - 1f) / 9f;

            Vector3 targetPos = currentParent.position;

            Quaternion targetRot = currentParent.rotation;

            transform.position = Vector3.Lerp(_transitionStartPos, targetPos, posT);
            transform.rotation = Quaternion.Slerp(_transitionStartRot, targetRot, rotT);

            if (rawT >= 1f)
            {
                endTransition();
            }
        }
    }

    private void endTransition()
    {
        _transitioning = false;
        if (_inAgentView)
        {
            this.transform.SetParent(currentParent);
            this.transform.localPosition = Vector3.zero;
            this.transform.localRotation = Quaternion.identity;
            Camera.main.cullingMask |= (1 << LayerMask.NameToLayer("Agent"));
            Camera.main.cullingMask |= (1 << LayerMask.NameToLayer("HideFromVisObs"));
        }
        else
        {
            Camera.main.orthographic = true;
            Camera.main.cullingMask |= (1 << LayerMask.NameToLayer("Icon"));
        }
        ViewTransitionEnded.Invoke();
    }

    public void startTransition(VehicleManager? toVehicle)
    {
        if (toVehicle == null && !_inAgentView) return;
        if (toVehicle != null && _inAgentView) return;

        currentParent = toVehicle != null ? toVehicle.getGameCameraAnchor().transform : _topDownParent;

        _inAgentView = !_inAgentView;
        ViewTransitionStarted.Invoke();
        if (!_inAgentView)
        {
            this.transform.SetParent(currentParent);
            Camera.main.cullingMask &= ~(1 << LayerMask.NameToLayer("Agent"));
            Camera.main.cullingMask &= ~(1 << LayerMask.NameToLayer("HideFromVisObs"));
        }
        else
        {
            Camera.main.cullingMask &= ~(1 << LayerMask.NameToLayer("Icon"));
            Camera.main.orthographic = false;
        }
        _transitionStartPos = transform.position;
        _transitionStartRot = transform.rotation;
        _transitioning = true;
        _transitionTime = 0f;
    }
}
