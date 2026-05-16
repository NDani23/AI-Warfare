using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    [SerializeField] private GUIManager guiManager;
    [SerializeField] private GameObject playEnvs;
    [SerializeField] private Texture2D commandCursor;


    public VehicleManager SelectedAgent { get; private set; }

    public UnityEvent<bool> ControlModeChangedEvent;

    private EnvController _envController;
    private CameraController gameCamera;

    private GameObject goToCommandMarker;
    private Vector3 globalGoToPosition = Vector3.zero;
    private bool isGlobalGoToCommand = false;

    private bool inCommandMode = false;

    private void Awake()
    {
        _envController = playEnvs.GetComponentInChildren<EnvController>();
        guiManager.setPlayEnv(_envController);
        goToCommandMarker = _envController.transform.Find("GoToCommandMarker").gameObject;
        gameCamera = Camera.main.GetComponent<CameraController>();
        gameCamera.ViewTransitionEnded.AddListener(HandleCameraTransitionEnd);
        gameCamera.ViewTransitionStarted.AddListener(HandleCameraTransitionStarted);
    }

    public void IssueCommandToVehicle(GameObject commandTarget, String agentName = "all")
    {
        List<VehicleManager> vehicles = _envController.GetAllVehicles();

        foreach (VehicleManager vehicle in vehicles)
        {
            if (vehicle.VehicleType == VehicleType.Heli || vehicle.Team == Team.Red) continue;
            TankManager tank = (TankManager)vehicle;
            if (vehicle.AgentName == agentName || agentName == "all")
            {
                tank.IssueCommand(commandTarget);
                if(agentName != "all")
                    return;
            }
        }
    }


    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !gameCamera.InAgentView)
        {
            HandleCommanderClick();
        }

        if (Input.GetKeyDown(KeyCode.V))
        {
            if(inCommandMode)
            {
                inCommandMode = false;
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }

            gameCamera.startTransition(gameCamera.InAgentView ? null : SelectedAgent);

            if(gameCamera.InAgentView && SelectedAgent != null)
            {
                SelectedAgent.GetComponent<VehicleManager>().getVehicleUI().switchOutControlUI();
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
                SelectedAgent.GetComponent<VehicleManager>().getVehicleUI().switchOutControlUI();
            }
            else
            {
                SelectedAgent.GetComponent<VehicleManager>().getVehicleUI().switchToControlUI();
            }
            //Cursor.lockState = isManualControlMode && SelectedAgent.AgentType == AgentType.Heli ? CursorLockMode.Locked : CursorLockMode.None;
            ControlModeChangedEvent.Invoke(isManualControlMode);
        }

        if(Input.GetKeyDown(KeyCode.C) && !gameCamera.InAgentView)
        {
            inCommandMode = !inCommandMode;
            if (!inCommandMode)            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
            }
            else
            {
                Cursor.SetCursor(commandCursor, new Vector2(commandCursor.width / 2f, commandCursor.height / 2f), CursorMode.Auto);
            }
        }
    }

    private void HandleCommanderClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        foreach (RaycastHit hit in Physics.RaycastAll(ray))
        {
            VehicleManager agent = hit.collider.GetComponentInParent<VehicleManager>();
            if (agent != null)
            {
                SelectedAgent?.setSelectedState(false);
                SelectedAgent = agent;
                SelectedAgent.setSelectedState(true);
                return;
            }
            else if (inCommandMode)
            {
                var hitParent = hit.collider.transform.parent.gameObject;
                if (hitParent != null && hitParent == goToCommandMarker)
                {
                    goToCommandMarker.SetActive(false);
                    IssueCommandToVehicle(_envController.ControlPoint, SelectedAgent != null ? SelectedAgent.AgentName : "all");
                    return;
                }

                if (Mathf.Abs(hit.point.x) > 350f || Mathf.Abs(hit.point.z) > 350f)
                {
                    return;
                }
                goToCommandMarker.SetActive(true);
                goToCommandMarker.transform.localPosition = new Vector3(hit.point.x, 3f, hit.point.z);
                IssueCommandToVehicle(goToCommandMarker, SelectedAgent != null ? SelectedAgent.AgentName : "all");
                return;
            }
        }
    }

    private void RepositionGoToMarker()
    {
        if(SelectedAgent != null)
        {
            goToCommandMarker.transform.localPosition = globalGoToPosition - _envController.transform.position;
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
