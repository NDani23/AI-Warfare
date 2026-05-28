using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public enum MapType
{
    PineForest,
    Plain,
}

public class GameManager : MonoBehaviour
{
    [SerializeField] private GUIManager guiManager;
    [SerializeField] private GameObject playEnvs;
    [SerializeField] private Texture2D commandCursor;
    [SerializeField] private GameObject _pineForestMap;
    [SerializeField] private GameObject _plainMap;
    [SerializeField] private MapType _currentMapType;
    [SerializeField] private GameMode _currentGameMode;

    public VehicleManager SelectedAgent { get; private set; }

    public UnityEvent<bool> ControlModeChangedEvent;
    private EnvController _envController;
    private CameraController gameCamera;

    private bool inCommandMode = false;

    private void Awake()
    {
        gameCamera = Camera.main.GetComponent<CameraController>();
        gameCamera.ViewTransitionEnded.AddListener(HandleCameraTransitionEnd);
        gameCamera.ViewTransitionStarted.AddListener(HandleCameraTransitionStarted);
    }

    private void Start()
    {
        StartGame();
    }

    private void StartGame()
    {
        switch (_currentMapType)
        {
            case MapType.PineForest:
                _pineForestMap.SetActive(true);
                _plainMap.SetActive(false);
                _envController = _pineForestMap.GetComponentInChildren<EnvController>();
                break;
            case MapType.Plain:
                _pineForestMap.SetActive(false);
                _plainMap.SetActive(true);
                _envController = _plainMap.GetComponentInChildren<EnvController>();
                break;
        }

        switch (_currentGameMode)
        {
            case GameMode.TDM:
                _envController.SetGameMode(GameMode.TDM);
                break;
            case GameMode.Conquest:
                _envController.SetGameMode(GameMode.Conquest);
                break;
        }

        gameCamera.transform.SetParent(_envController.TopDownViewPoint);
        gameCamera.transform.localPosition = Vector3.zero;
        gameCamera.transform.localRotation = Quaternion.identity;

        guiManager.setPlayEnv(_envController);

        _envController.ResetEnv(null, false);
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
                {
                    return;
                }
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
                {
                    return;
                }
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

        if(!gameCamera.InAgentView)
        {
            UpdateCommandMarkerIcons();
        }
        else if(SelectedAgent != null)
        {
            HandleVehicleModeCommandAssignment();
        }
    }

    private void HandleVehicleModeCommandAssignment()
    {
        if(Input.GetMouseButtonDown(1))
        {
            VehicleManager selectedVehicle = SelectedAgent.GetComponent<VehicleManager>();
            HitInfo RayHitInfo = selectedVehicle.RequestHitInfo();
            if(RayHitInfo.hitTag == 0 && RayHitInfo.hitGameObject != null)
            {
                GameObject targetVehicle = RayHitInfo.hitGameObject.transform.parent.gameObject;
                if (targetVehicle != null)
                {
                    IssueCommandToVehicle(targetVehicle);
                }
            }
            else if(RayHitInfo.hitTag == -1)
            {
                IssueCommandToVehicle(RayHitInfo.hitPosition);
            }
        }
        else if(Input.GetKey(KeyCode.F) && SelectedAgent.VehicleType == VehicleType.Tank)
        {
            IssueCommandToVehicle(((TankManager)SelectedAgent).FollowPositionMarker);
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

    private void UpdateCommandMarkerIcons()
    {
        if (_envController == null || _envController.VehicleList == null) return;

        List<TankManager> aliveTanks = new List<TankManager>();
        foreach (VehicleManager vehicle in _envController.VehicleList)
        {
            if (vehicle.Team == Team.Yellow && vehicle.VehicleType == VehicleType.Tank && vehicle.Health > 0)
            {
                aliveTanks.Add((TankManager)vehicle);
            }
        }

        if (aliveTanks.Count == 0) return;

        bool allShareCommand = false;
        CommandType sharedType = aliveTanks[0].ActiveCommand;

        if (sharedType != CommandType.None)
        {
            allShareCommand = true;
            GameObject sharedTarget = aliveTanks[0].CommandMarker.FollowTarget;
            Vector3 sharedPos = aliveTanks[0].CommandMarker.TargetGlobalPosition;

            foreach (TankManager tank in aliveTanks)
            {
                if (tank.ActiveCommand != sharedType) 
                {
                    allShareCommand = false;
                    break;
                }

                if (sharedType == CommandType.EliminateTarget && tank.CommandMarker.FollowTarget != sharedTarget)
                {
                    allShareCommand = false;
                    break;
                }
                else if (sharedType == CommandType.GoToPosition && Vector3.Distance(tank.CommandMarker.TargetGlobalPosition, sharedPos) > 0.1f)
                {
                    allShareCommand = false;
                    break;
                }
            }
        }

        foreach (TankManager tank in aliveTanks)
        {
            if (SelectedAgent == tank)
            {
                tank.CommandMarker.ShowCommandIcon = true;
            }
            else if (SelectedAgent == null && allShareCommand)
            {
                tank.CommandMarker.ShowCommandIcon = true;
            }
            else
            {
                tank.CommandMarker.ShowCommandIcon = false;
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
