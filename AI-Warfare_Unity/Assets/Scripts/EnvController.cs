using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public enum GameMode
{
    Conquest,
    TDM
}

public class EnvController : MonoBehaviour
{
    [Tooltip("Time limit (seconds)")] public int timeLimit = 60;

    [SerializeField] private CTController _controlPoint;
    [SerializeField] private Transform _TopDownViewPoint;
    [SerializeField] private GameMode _gameMode = GameMode.Conquest;

    public GameMode GameMode => _gameMode;
    public Transform TopDownViewPoint => _TopDownViewPoint;

    private int _respawnCooldown;

    private List<VehicleManager> _vehicleList = new List<VehicleManager>();

    public List<VehicleManager> VehicleList => _vehicleList;

    private float _resetTimer;
    public float ResetTimer => _resetTimer;

    public Dictionary<GameObject,float> _detectedRedEnemies = new Dictionary<GameObject, float>();
    public Dictionary<GameObject, float> _detectedYellowEnemies = new Dictionary<GameObject, float>();
    public Dictionary<VehicleManager, float> _deadAgents = new Dictionary<VehicleManager, float>();

    public Dictionary<GameObject,float> DetectedRedEnemies => _detectedRedEnemies;
    public Dictionary<GameObject, float> DetectedYellowEnemies => _detectedYellowEnemies;
    public Dictionary<VehicleManager, float> DeadAgents => _deadAgents;

    public GameObject ControlPoint => _controlPoint.gameObject;

    public float RedTeamPoints = 0.0f;
    public float YellowTeamPoints = 0.0f;

    public CTState CtState = CTState.Neutral;

    private int _redAgentsOnCT = 0;
    private int _yellowAgentsOnCT = 0;

    private bool _capturing = false;
    private float _stateNum = 0.0f;
    public float StateNum => _stateNum;

    public UnityEvent RedWonEvent;
    public UnityEvent YellowWonEvent;
    public UnityEvent TieEvent;
    public UnityEvent GameEnded;


    private void Awake()
    {
        _vehicleList = this.GetComponentsInChildren<VehicleManager>().ToList();
        _respawnCooldown = GameMode == GameMode.Conquest ? 15 : 5;
    }


    void Start()
    {
        _resetTimer = timeLimit;

        if(_gameMode == GameMode.TDM)
        {
            _controlPoint.gameObject.SetActive(false);

            foreach(var vehicle in _vehicleList)
            {
                EnemyDetected(vehicle.gameObject, timeLimit);
            }
        }
        else
        {
            _controlPoint.gameObject.SetActive(true);
        }
    }

    void FixedUpdate()
    {

        _resetTimer -= Time.fixedDeltaTime;
        HandleCaptureState();

        if (_resetTimer <= 0.0f)
        {
            Team? winnerTeam = RedTeamPoints > YellowTeamPoints ? Team.Red : (YellowTeamPoints > RedTeamPoints ? Team.Yellow : null);
            ResetEnv(winnerTeam, true);
            return;
        }

        foreach (var agent in _detectedRedEnemies.Keys.ToList())
        {
            _detectedRedEnemies[agent] = _detectedRedEnemies[agent] - Time.fixedDeltaTime;
            if(_detectedRedEnemies[agent] <= 0.0f)
            {
                agent.GetComponent<ITargetable>()?.setDetectedState(false);
                _detectedRedEnemies.Remove(agent);
            }
        }

        foreach (var agent in _detectedYellowEnemies.Keys.ToList())
        {
            _detectedYellowEnemies[agent] = _detectedYellowEnemies[agent] - Time.fixedDeltaTime;
            if (_detectedYellowEnemies[agent] <= 0.0f)
            {
                agent.GetComponent<ITargetable>()?.setDetectedState(false);
                _detectedYellowEnemies.Remove(agent);
            }
        }

        foreach (var agent in _deadAgents.Keys.ToList())
        {
            _deadAgents[agent] = _deadAgents[agent] - Time.fixedDeltaTime;
            if (_deadAgents[agent] <= 0.0f)
            {
                _deadAgents.Remove(agent);
                agent.ResetVehicle();
            }
        }

        if (_capturing)
        {
           if(_redAgentsOnCT < _yellowAgentsOnCT)
           {
               if (_stateNum < 0 && CtState != CTState.Red) _stateNum = 0;
               _stateNum = Mathf.Min(_stateNum + Time.fixedDeltaTime, 10.0f);

               if (_stateNum >= 0.0f && CtState == CTState.Red)
               {
                   CtState = CTState.Neutral;
                   _controlPoint.ChangeState(CtState);
               }

               if (_stateNum == 10.0f && CtState != CTState.Yellow)
               {

                   CtState = CTState.Yellow;
                   _controlPoint.ChangeState(CtState);
                   _capturing = false;
               }
           }
           else
           {
               if (_stateNum > 0 && CtState != CTState.Yellow) _stateNum = 0;
               _stateNum = Mathf.Max(_stateNum - Time.fixedDeltaTime, -10.0f);

               if (_stateNum <= 0.0 && CtState == CTState.Yellow)
               {
                   CtState = CTState.Neutral;
                   _controlPoint.ChangeState(CtState);
               }

               if (_stateNum == -10.0f && CtState != CTState.Red)
               {
                   CtState = CTState.Red;
                   _controlPoint.ChangeState(CtState);
                   _capturing = false;
               }
           }
        }
        else
        {
           if ((_redAgentsOnCT == 0 && _yellowAgentsOnCT == 0)
            || (_redAgentsOnCT == 0 && CtState == CTState.Yellow)
            || (_yellowAgentsOnCT == 0 && CtState == CTState.Red))
           {
               switch (CtState)
               {
                   case CTState.Red:
                       _stateNum = -10.0f;
                       break;
                   case CTState.Yellow:
                       _stateNum = 10.0f;
                       break;
                   case CTState.Neutral:
                       _stateNum = 0;
                       break;

               }
           }
        }


        float pointToAdd = (100.0f / 60.0f) * Time.fixedDeltaTime;
        if (CtState == CTState.Red)
        {
           RedTeamPoints += pointToAdd;
        }
        else if (CtState == CTState.Yellow)
        {
           YellowTeamPoints += pointToAdd;
        }

        if (RedTeamPoints >= 100.0f && YellowTeamPoints >= 100.0f)
        {
           //TieEvent.Invoke();
           ResetEnv(null);
           return;
        }
        else if (RedTeamPoints >= 100.0f)
        {
           //RedWonEvent.Invoke();
           ResetEnv(Team.Red);
           return;
        }
        else if (YellowTeamPoints >= 100.0f)
        {
           //YellowWonEvent.Invoke();
           ResetEnv(Team.Yellow);
           return;
        }
    }

    public void VehicleDied(VehicleManager vehicle)
    {
        if (_deadAgents.ContainsKey(vehicle)) return;
        _deadAgents.TryAdd(vehicle, _respawnCooldown);

        if (vehicle.Team == Team.Red)
        {

            _detectedRedEnemies.Remove(vehicle.gameObject);
            AddRewardToTeamMembers(Team.Yellow, vehicle.VehicleType == VehicleType.Tank ? 0.1f : 0.2f);
            AddRewardToTeamMembers(Team.Red, vehicle.VehicleType == VehicleType.Tank ? -0.1f : -0.2f);
            AddPointToTeam(Team.Yellow, _gameMode == GameMode.Conquest ? 1 : 10);

        }
        else if (vehicle.Team == Team.Yellow)
        {
            _detectedYellowEnemies.Remove(vehicle.gameObject);
            AddRewardToTeamMembers(Team.Red, vehicle.VehicleType == VehicleType.Tank ? 0.1f : 0.2f);
            AddRewardToTeamMembers(Team.Yellow, vehicle.VehicleType == VehicleType.Tank ? -0.1f : -0.2f);
            AddPointToTeam(Team.Red, _gameMode == GameMode.Conquest ? 1 : 10);
        }
    }

    public List<VehicleManager> GetAllVehicles()
    {
        return _vehicleList;
    }

    public void AddPointToTeam(Team team, int point)
    {
        if (team == Team.Red)
        {
            RedTeamPoints += point;
        }
        else
        {
            YellowTeamPoints += point;
        }

    }

    public void AssignDefaultCommand(TankManager tank)
    {
        if (tank.Health <= 0) return;

        if (_gameMode == GameMode.Conquest)
        {
            tank.IssueCommand(ControlPoint);
        }
        else if (_gameMode == GameMode.TDM)
        {
            GameObject closestEnemy = GetClosestEnemy(tank);
            if (closestEnemy != null)
            {
                tank.IssueCommand(closestEnemy);
            }
        }
    }

    private GameObject GetClosestEnemy(TankManager tank)
    {
        GameObject closest = null;
        float minDist = float.MaxValue;

        foreach (VehicleManager vehicle in _vehicleList)
        {
            if (vehicle.VehicleType != VehicleType.Tank) continue;
            if (vehicle.Health > 0 && vehicle.Team != tank.Team)
            {
                float dist = Vector3.Distance(tank.transform.position, vehicle.transform.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    closest = vehicle.gameObject;
                }
            }
        }
        return closest;
    }

    public void ResetEnv(Team? winningTeam, bool TimeIsUp = false)
    {
        foreach (var vehicle in _vehicleList)
        {
            vehicle.EndEpisode();
        }

        GameEnded.Invoke();
        _resetTimer = timeLimit;
        _detectedRedEnemies.Clear();
        _detectedYellowEnemies.Clear();
        _deadAgents.Clear();
        _redAgentsOnCT = 0;
        _yellowAgentsOnCT = 0;
        _capturing = false;
        CtState = CTState.Neutral;
        _controlPoint?.ChangeState(CtState);
        RedTeamPoints = 0.0f;
        YellowTeamPoints = 0.0f;
        _respawnCooldown = GameMode == GameMode.Conquest ? 15 : 5;

        if(_gameMode == GameMode.TDM)
        {
            foreach(var vehicle in _vehicleList)
            {
                EnemyDetected(vehicle.gameObject, timeLimit);
            }
        }
        else
        {
            _controlPoint.gameObject.SetActive(true);
        }
    }

    public void SetGameMode(GameMode mode)
    {
        _gameMode = mode;
    }

    public Vector3 GetCTPosition()
    {
        return _controlPoint.transform.localPosition;
    }

    public void VehicleEnteredCT(Team team)
    {
        if (team == Team.Red)
            _redAgentsOnCT++;
        else
            _yellowAgentsOnCT++;
    }

    public void VehicleExitedCT(Team team)
    {

        if (team == Team.Red)
            _redAgentsOnCT--;
        else
            _yellowAgentsOnCT--;

    }

    public void HandleCaptureState()
    {
        if (!_capturing)
        {
            if ((_redAgentsOnCT > _yellowAgentsOnCT && CtState != CTState.Red)
             || (_yellowAgentsOnCT > _redAgentsOnCT && CtState != CTState.Yellow))
            {
                _capturing = true;
            }
        }
        else
        {
            if (_redAgentsOnCT == _yellowAgentsOnCT)
            {
                _capturing = false;
            }
        }
    }

    public float getTeamPoints(Team team)
    {
        if (team == Team.Red) return RedTeamPoints;
        else return YellowTeamPoints;
    }

    public void EnemyDetected(GameObject target, float detectionDuration = 15.0f)
    {
        var vehicleManager = target.GetComponent<VehicleManager>();
        vehicleManager.setDetectedState(true);

        if (vehicleManager.Team == Team.Red)
        {
            if(_detectedRedEnemies.ContainsKey(target))
            {
                _detectedRedEnemies[target] = detectionDuration;
            }
            else
            {
                _detectedRedEnemies.TryAdd(target, detectionDuration);
            }
        }
        else
        {
            if (_detectedYellowEnemies.ContainsKey(target))
            {
                _detectedYellowEnemies[target] = detectionDuration;
            }
            else
            {
                _detectedYellowEnemies.TryAdd(target, detectionDuration);
            }
        }
    }

    public void resetCT()
    {
        _stateNum = 0;
        CtState = CTState.Neutral;
        _controlPoint.ChangeState(CtState);
    }

    public void clearDetectedEnemies()
    {
        _detectedRedEnemies.Clear();
        _detectedYellowEnemies.Clear();
    }

    private void AddRewardToTeamMembers(Team team, float reward)
    {
        foreach(var vehicle in _vehicleList)
        {
            if(vehicle.Team == team)
            {
                vehicle.AddReward(reward);
            }
        }
    }
}
