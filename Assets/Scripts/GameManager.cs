using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    [SerializeField] private CameraController gameCamera;
    [SerializeField] private GUIManager guiManager;

    public VehicleManager SelectedAgent { get; private set; }

    public UnityEvent<bool> ControlModeChangedEvent;

    private void Awake()
    {
        gameCamera.ViewTransitionEnded.AddListener(HandleCameraTransitionEnd);
        gameCamera.ViewTransitionStarted.AddListener(HandleCameraTransitionStarted);
    }


    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !gameCamera.InAgentView)
        {
            HandleAgentSelection();
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            gameCamera.startTransition(gameCamera.InAgentView ? null : SelectedAgent);

            if(gameCamera.InAgentView && SelectedAgent != null)
            {
                SelectedAgent.GetComponent<VehicleAgent>().getVehicleUI().switchOutControlUI();
            }
        }

        if (Input.GetKeyDown(KeyCode.C) && gameCamera.InAgentView && SelectedAgent != null)
        {
            bool isManualControlMode = !SelectedAgent.IsPlayerControlled;
            SelectedAgent.SetPlayerControl(isManualControlMode);
            if (!isManualControlMode)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                SelectedAgent.GetComponent<VehicleAgent>().getVehicleUI().switchOutControlUI();
            }
            else
            {
                SelectedAgent.GetComponent<VehicleManager>().getVehicleUI().switchToControlUI();
            }
            //Cursor.lockState = isManualControlMode && SelectedAgent.AgentType == AgentType.Heli ? CursorLockMode.Locked : CursorLockMode.None;
            ControlModeChangedEvent.Invoke(isManualControlMode);
        }
    }

    private void HandleAgentSelection()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        foreach (RaycastHit hit in Physics.RaycastAll(ray))
        {
            VehicleManager agent = hit.collider.GetComponentInParent<VehicleManager>();
            if (agent != null)
            {
                Debug.Log($"Agent Selected");
                SelectedAgent?.setSelectedState(false);
                SelectedAgent = agent;
                SelectedAgent.setSelectedState(true);
                return;
            }
        }
    }

    private void HandleCameraTransitionEnd()
    {
        if(gameCamera.InAgentView)
            guiManager.SwitchGUIMode(SelectedAgent);
    }

    private void HandleCameraTransitionStarted()
    {
        if (!gameCamera.InAgentView)
        {
            if (SelectedAgent != null)
            {
                SelectedAgent.SetPlayerControl(false);
                Cursor.lockState = CursorLockMode.None;
                ControlModeChangedEvent.Invoke(false);
            }
            guiManager.SwitchGUIMode(null);
        }
    }
}
