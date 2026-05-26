using System;
using System.Collections.Generic;
using NUnit.Framework;
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

    private bool inCommandMode = false;

    private void Awake()
    {
        _envController = playEnvs.GetComponentInChildren<EnvController>(false);
        guiManager.setPlayEnv(_envController);
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

    public void IssueCommandToVehicle(Vector3 commandTargetPosition, String agentName = "all")
    {

        List<VehicleManager> vehicles = _envController.GetAllVehicles();

        foreach (VehicleManager vehicle in vehicles)
        {
            if (vehicle.VehicleType == VehicleType.Heli || vehicle.Team == Team.Red) continue;
            TankManager tank = (TankManager)vehicle;
            if (vehicle.AgentName == agentName || agentName == "all")
            {
                tank.IssueCommand(commandTargetPosition);
                if(agentName != "all")
                    return;
            }
        }
    }

    public EnvController GetPlayEnv()
    {
        return _envController;
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
        int mask = ~LayerMask.GetMask("PlayEnv");
        if (!Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, mask))
        {
            return;
        }

        if (!inCommandMode)
        {

            SelectedAgent?.setSelectedState(false);
            SelectedAgent = null;

            VehicleManager agent = hit.collider.GetComponentInParent<VehicleManager>();
            if (agent != null && agent.Team == Team.Yellow)
            {
                SelectedAgent = agent;
                SelectedAgent.setSelectedState(true);
            }
            return;
        }
        else
        {
            var hitParent = hit.collider.transform.parent.gameObject;
            if (hitParent != null && hitParent.GetComponent<CommandTargetMarkerController>() != null)
            {
                if(SelectedAgent != null && SelectedAgent.VehicleType != VehicleType.Heli)
                {
                    _envController.AssignDefaultCommand((TankManager)SelectedAgent);
                }
                else
                {
                    foreach (var vehicle in _envController.GetAllVehicles())
                    {
                        if (vehicle.Team == Team.Red || vehicle.VehicleType == VehicleType.Heli) continue;
                        _envController.AssignDefaultCommand((TankManager)vehicle);
                    }
                }
                return;
            }

            if (Mathf.Abs(hit.point.x) > 350f || Mathf.Abs(hit.point.z) > 350f)
            {
                return;
            }
            
            if(hitParent != null && hitParent.GetComponent<VehicleManager>() != null)
            {
                VehicleManager targetVehicle = hitParent.GetComponent<VehicleManager>();
                if (targetVehicle.Team == Team.Red && targetVehicle.Health > 0 && targetVehicle.Detected)
                {
                    IssueCommandToVehicle(targetVehicle.gameObject, SelectedAgent != null ? SelectedAgent.AgentName : "all");
                    return;
                }
            }
            IssueCommandToVehicle(hit.point, SelectedAgent != null ? SelectedAgent.AgentName : "all");
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
