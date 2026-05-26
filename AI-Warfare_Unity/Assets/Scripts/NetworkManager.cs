using UnityEngine;
using SocketIOClient;
using System;
using System.Collections.Generic;

[Serializable]
public class VehicleStateData
{
    public string name;
    public string type;
    public float health;
    public string position;
}

[Serializable]
public class MatchStateData
{
    public float timeRemaining;
    public float redPoints;
    public float yellowPoints;
    public List<VehicleStateData> yellowVehicles;
    public List<VehicleStateData> detectedRedVehicles;
}

[Serializable]
public class RepositionCommandData
{
    public float x;
    public float y;
    public float z;
    public string vehicleName;
}

[Serializable]
public class EliminateCommandData
{
    public string targetName;
    public string vehicleName;
}

public class NetworkManager : MonoBehaviour
{

    [SerializeField] private GameManager gameManager;
    private SocketIOUnity socket;

    private readonly object queueLock = new object();
    private Queue<Action> queuedActions = new Queue<Action>();

    void Start()
    {
        socket = new SocketIOUnity("http://localhost:5000");

        socket.OnConnected += (sender, e) => {
            Debug.Log("Unity connected to Python MCP Server!");
        };

        // Listen for the tool command from Python
        socket.On("get_game_state", (response) => {
            lock (queueLock) {
                queuedActions.Enqueue(() => {

                    EnvController env = gameManager.GetPlayEnv();

                    MatchStateData state = new MatchStateData
                    {
                        timeRemaining = (int)env.getRemainingTime(),
                        redPoints = (int)env.RedTeamPoints,
                        yellowPoints = (int)env.YellowTeamPoints,
                        yellowVehicles = new List<VehicleStateData>(),
                        detectedRedVehicles = new List<VehicleStateData>()
                    };

                    foreach (VehicleManager v in env.VehicleList)
                    {
                        VehicleStateData vData = new VehicleStateData
                        {
                            name = v.AgentName,
                            type = v.VehicleType.ToString(),
                            health = v.Health,
                            position = $"{v.transform.position.x:F2}, {v.transform.position.y:F2}, {v.transform.position.z:F2}"
                        };

                        if (v.Team == Team.Yellow) {
                            state.yellowVehicles.Add(vData);
                        } 
                        else if (v.Team == Team.Red && v.Detected) {
                            state.detectedRedVehicles.Add(vData);
                        }
                    }

                    string jsonResponse = JsonUtility.ToJson(state, true);
                    response.CallbackAsync(jsonResponse);
                });
            }
        });

        socket.On("issue_reposition_command", (response) => {
            string jsonString = response.GetValue<string>();
            RepositionCommandData data = JsonUtility.FromJson<RepositionCommandData>(jsonString);

            lock (queueLock) {
                queuedActions.Enqueue(() => {

                    // 2. Create the Vector3 coordinate
                    Vector3 targetPosition = new Vector3(data.x, data.y, data.z);
                    // 3. Issue the command
                    // (If vehicleName is "all", it will command all eligible vehicles)
                    gameManager.IssueCommandToVehicle(targetPosition, data.vehicleName);

                    // 4. Send success confirmation back to Copilot
                    response.CallbackAsync($"Successfully issued move command to '{data.vehicleName}' to position ({data.x:F1}, {data.y:F1}, {data.z:F1}).");
                });
            }
        });

        socket.On("issue_eliminate_target_command", (response) => {
            string jsonString = response.GetValue<string>();
            EliminateCommandData data = JsonUtility.FromJson<EliminateCommandData>(jsonString);

            lock (queueLock) {
                queuedActions.Enqueue(() => {

                    String targetName = data.targetName;
                    GameObject targetObject = null;
                    foreach (VehicleManager v in gameManager.GetPlayEnv().VehicleList)
                    {
                        if (v.AgentName == targetName && v.Team == Team.Red && v.Detected)
                        {
                            targetObject = v.gameObject;
                            break;
                        } 
                    }

                    if(targetObject == null)
                    {
                        response.CallbackAsync($"Failed to issue eliminate command: Target '{targetName}' not found or not eligible.");
                        return;
                    }
                    else
                    {
                        gameManager.IssueCommandToVehicle(targetObject, data.vehicleName);
                        response.CallbackAsync($"Successfully issued eliminate command to '{data.vehicleName}' for target '{targetName}'.");
                    }
                });
            }
        });

        socket.Connect();
    }

    // Update is called once per frame
    void Update()
    {
        // Execute any waiting Socket.IO requests on Unity's Main Thread
        lock (queueLock) {
            while (queuedActions.Count > 0) {
                queuedActions.Dequeue().Invoke();
            }
        }
    }

    void OnApplicationQuit()
    {
        if (socket != null) {
            socket.Disconnect();
        }
    }
}
