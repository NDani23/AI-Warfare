using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Unity.MLAgents;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public class EnvController : MonoBehaviour
{
    [Tooltip("Time limit (seconds)")] public int timeLimit = 90;

    [SerializeField] private CTController m_ControlPoint;

    public static int RespawnCooldown = 15;

    public List<IVehicleAgent> AgentsList = new List<IVehicleAgent>();

    //public SimpleMultiAgentGroup m_RedAgentGroup;
    //public SimpleMultiAgentGroup m_YellowAgentGroup;

    public float m_ResetTimer = 5;

    public Dictionary<GameObject,float> m_DetectedRedEnemies;
    public Dictionary<GameObject, float> m_DetectedYellowEnemies;
    public Dictionary<IVehicleAgent, float> m_DeadAgents;

    public float RedTeamPoints = 0.0f;
    public float YellowTeamPoints = 0.0f;

    private int m_MatchTotalKills = 0;
    private int m_MatchTankKills = 0;
    private int m_MatchHeliKills = 0;

    public CTState ctState = CTState.Neutral;

    private int redAgentsOnCT = 0;
    private int yellowAgentsOnCT = 0;

    private bool capturing = false;
    private float StateNum = 0.0f;

    private bool gameEnded = false;

    public UnityEvent RedWonEvent;
    public UnityEvent YellowWonEvent;
    public UnityEvent TieEvent;
    public UnityEvent GameEnded;

    private int winBalance = 0;
    private void Awake()
    {
        m_DetectedRedEnemies = new Dictionary<GameObject, float>();
        m_DetectedYellowEnemies = new Dictionary<GameObject, float>();
        m_DeadAgents = new Dictionary<IVehicleAgent, float>();
        AgentsList = this.GetComponentsInChildren<IVehicleAgent>().ToList();
    }


    void Start()
    {
        m_ResetTimer = timeLimit;
        //m_RedAgentGroup = new SimpleMultiAgentGroup();
        //m_YellowAgentGroup = new SimpleMultiAgentGroup();

        //foreach (var agent in AgentsListSerialized)
        //{

        //    if (agent is IVehicleAgent vehicleAgent)
        //    {
        //        AgentsList.Add(vehicleAgent);
        //    }
        //}

    //    foreach (var agent in AgentsList)
    //    {
    //        if (agent.Team == Team.Red)
    //        {
    //            m_RedAgentGroup.RegisterAgent((Agent)agent);
    //        }
    //        else
    //        {
    //            m_YellowAgentGroup.RegisterAgent((Agent)agent);
    //        }
    //    }
    }

    void FixedUpdate()
    {

        m_ResetTimer -= Time.fixedDeltaTime;
        //HandleCaptureState();

        //Only for pre training!!
        if (m_ResetTimer <= 0.0f)
        {
            Team? winnerTeam = RedTeamPoints > YellowTeamPoints ? Team.Red : (YellowTeamPoints > RedTeamPoints ? Team.Yellow : null);
            ResetEnv(winnerTeam, true);
            return;
        }
        //////////////////////////////////////////////////////////////////////////////////////////

        foreach (var agent in m_DetectedRedEnemies.Keys.ToList())
        {
            m_DetectedRedEnemies[agent] = m_DetectedRedEnemies[agent] - Time.fixedDeltaTime;
            if(m_DetectedRedEnemies[agent] <= 0.0f) m_DetectedRedEnemies.Remove(agent);
        }

        foreach (var agent in m_DetectedYellowEnemies.Keys.ToList())
        {
            m_DetectedYellowEnemies[agent] = m_DetectedYellowEnemies[agent] - Time.fixedDeltaTime;
            if (m_DetectedYellowEnemies[agent] <= 0.0f) m_DetectedYellowEnemies.Remove(agent);
        }

        foreach (var agent in m_DeadAgents.Keys.ToList())
        {
            m_DeadAgents[agent] = m_DeadAgents[agent] - Time.fixedDeltaTime;
            if (m_DeadAgents[agent] <= 0.0f)
            {
                m_DeadAgents.Remove(agent);
                agent.ResetAgent();
            }
        }

        //if (capturing)
        //{
        //    if(redAgentsOnCT < yellowAgentsOnCT)
        //    {
        //        if (StateNum < 0 && ctState != CTState.Red) StateNum = 0;
        //        StateNum = Mathf.Min(StateNum + Time.fixedDeltaTime, 10.0f);

        //        if (StateNum >= 0.0f && ctState == CTState.Red)
        //        {
        //            ctState = CTState.Neutral;
        //            m_ControlPoint.ChangeState(ctState);
        //        }

        //        if (StateNum == 10.0f && ctState != CTState.Yellow)
        //        {

        //            ctState = CTState.Yellow;
        //            m_ControlPoint.ChangeState(ctState);
        //            //AddRewardToTeamMembers(Team.Yellow, 2.0f - (1.0f - m_ResetTimer / timeLimit) * 2);
        //            //m_YellowAgentGroup.AddGroupReward(0.5f - (1.0f - m_ResetTimer / timeLimit) * 0.5f);
        //            //m_RedAgentGroup.AddGroupReward(-(0.5f - (1.0f - m_ResetTimer / timeLimit) * 0.5f));
        //            //m_RedAgentGroup.AddGroupReward(-1.0f);
        //            //ResetEnv(Team.Yellow);
        //            capturing = false;
        //            Debug.Log("CAPTURED!");
        //        }

        //        //if (capturing)
        //        //    m_YellowAgentGroup.AddGroupReward((100 / 60) * Time.fixedDeltaTime * 0.01f);
        //    }
        //    else
        //    {
        //        if (StateNum > 0 && ctState != CTState.Yellow) StateNum = 0;
        //        StateNum = Mathf.Max(StateNum - Time.fixedDeltaTime, -10.0f);

        //        if (StateNum <= 0.0 && ctState == CTState.Yellow)
        //        {
        //            ctState = CTState.Neutral;
        //            m_ControlPoint.ChangeState(ctState);
        //        }

        //        if (StateNum == -10.0f && ctState != CTState.Red)
        //        {
        //            ctState = CTState.Red;
        //            m_ControlPoint.ChangeState(ctState);
        //            //AddRewardToTeamMembers(Team.Red, 2.0f - (1.0f - m_ResetTimer / timeLimit) * 2);
        //            //m_RedAgentGroup.AddGroupReward(0.5f - (1.0f - m_ResetTimer / timeLimit) * 0.5f);
        //            //m_YellowAgentGroup.AddGroupReward(-(0.5f - (1.0f - m_ResetTimer / timeLimit) * 0.5f));
        //            //m_YellowAgentGroup.AddGroupReward(-1.0f);
        //            //ResetEnv(Team.Red);
        //            capturing = false;
        //            Debug.Log("CAPTURED!");
        //        }

        //        //if (capturing)
        //        //    m_RedAgentGroup.AddGroupReward((100 / 60) * Time.fixedDeltaTime * 0.01f);
        //    }
        //}
        //else
        //{
        //    if ((redAgentsOnCT == 0 && yellowAgentsOnCT == 0)
        //     || (redAgentsOnCT == 0 && ctState == CTState.Yellow)
        //     || (yellowAgentsOnCT == 0 && ctState == CTState.Red))
        //    {
        //        switch (ctState)
        //        {
        //            case CTState.Red:
        //                StateNum = -10.0f;
        //                break;
        //            case CTState.Yellow:
        //                StateNum = 10.0f;
        //                break;
        //            case CTState.Neutral:
        //                StateNum = 0;
        //                break;

        //        }
        //    }
        //}


        //float pointToAdd = (100.0f / 40.0f) * Time.fixedDeltaTime;
        //if (ctState == CTState.Red)
        //{
        //    RedTeamPoints += pointToAdd;
        //    AddRewardToTeamMembers(Team.Red, pointToAdd * 0.01f);
        //}
        //else if (ctState == CTState.Yellow)
        //{
        //    YellowTeamPoints += pointToAdd;
        //    AddRewardToTeamMembers(Team.Yellow, pointToAdd * 0.01f);
        //}

        //if (m_ResetTimer <= 0.0f)
        //{


        //    if (RedTeamPoints == YellowTeamPoints || Mathf.Max(RedTeamPoints, YellowTeamPoints) < 50)
        //        ResetEnv(null, true);
        //    else if (RedTeamPoints > YellowTeamPoints)
        //        ResetEnv(Team.Red, true);
        //    else if (YellowTeamPoints > RedTeamPoints)
        //        ResetEnv(Team.Yellow, true);

        //    return;
        //}


        //if (RedTeamPoints >= 100.0f && YellowTeamPoints >= 100.0f)
        //{
        //    //TieEvent.Invoke();
        //    //m_YellowAgentGroup.AddGroupReward(0);
        //    //m_RedAgentGroup.AddGroupReward(0);
        //    gameEnded = true;
        //    //m_YellowAgentGroup.EndGroupEpisode();
        //    //m_RedAgentGroup.EndGroupEpisode();
        //    //ResetEnv();
        //    ResetEnv(null);
        //    return;
        //}
        //else if (RedTeamPoints >= 100.0f)
        //{
        //    //RedWonEvent.Invoke();
        //    //m_RedAgentGroup.AddGroupReward(1.0f);
        //    gameEnded = true;
        //    //m_YellowAgentGroup.EndGroupEpisode();
        //    //m_RedAgentGroup.EndGroupEpisode();
        //    //ResetEnv();
        //    ResetEnv(Team.Red);
        //    return;
        //}
        //else if (YellowTeamPoints >= 100.0f)
        //{
        //    //YellowWonEvent.Invoke();
        //    //m_YellowAgentGroup.AddGroupReward(1.0f);
        //    gameEnded = true;
        //    //m_YellowAgentGroup.EndGroupEpisode();
        //    //m_RedAgentGroup.EndGroupEpisode();
        //    //ResetEnv();
        //    ResetEnv(Team.Yellow);
        //    return;
        //}
    }

    public void AgentDied(IVehicleAgent agent)
    {
        if (m_DeadAgents.ContainsKey(agent)) return;
        m_DeadAgents.TryAdd(agent, RespawnCooldown);

        RegisterKillForStats(agent.AgentType);

        //float timeMultiplier = (m_ResetTimer / timeLimit) + 1.0f;

        if (agent.Team == Team.Red)
        {

            m_DetectedRedEnemies.Remove(agent.gameObject);
            AddRewardToTeamMembers(Team.Yellow, agent.AgentType == AgentType.Tank ? 0.1f : 0.2f);
            AddRewardToTeamMembers(Team.Red, agent.AgentType == AgentType.Tank ? -0.1f : -0.2f);
            AddPointToTeam(Team.Yellow, 1);

        }
        else if (agent.Team == Team.Yellow)
        {
            m_DetectedYellowEnemies.Remove(agent.gameObject);
            AddRewardToTeamMembers(Team.Red, agent.AgentType == AgentType.Tank ? 0.1f : 0.2f);
            AddRewardToTeamMembers(Team.Yellow, agent.AgentType == AgentType.Tank ? -0.1f : -0.2f);
            AddPointToTeam(Team.Red, 1);
        }

        ////agent.ResetAgent();
        //ResetEnv(agent.Team == Team.Red? Team.Yellow : Team.Red);

        //ResetEnv(null, true);
        ////agent.gameObject.SetActive(false);
    }

    public void AddPointToTeam(Team team, int point)
    {
        // float timeMultiplier = (m_ResetTimer / timeLimit) + 1.0f;
        // AddRewardToTeamMembers(team == Team.Red ? Team.Red : Team.Yellow, (float)point * 0.1f * timeMultiplier);
        // AddRewardToTeamMembers(team == Team.Red ? Team.Yellow : Team.Red, -(float)point * 0.1f * timeMultiplier);

        // AddRewardToTeamMembers(team == Team.Red ? Team.Red : Team.Yellow, (float)point * 0.1f);
        // AddRewardToTeamMembers(team == Team.Red ? Team.Yellow : Team.Red, -(float)point * 0.1f);

        if (team == Team.Red)
        {
            RedTeamPoints += point;
            if (RedTeamPoints >= 4)
               ResetEnv(Team.Red, false);
        }
        else
        {
            YellowTeamPoints += point;
            if (YellowTeamPoints >= 4)
               ResetEnv(Team.Yellow, false);
        }

    }

    public void ResetEnv(Team? winningTeam, bool TimeIsUp = false)
    {
        WriteMatchKillStats();

        if (winningTeam is null)
        {
            AddRewardToTeamMembers(Team.Red, 0.0f);
            AddRewardToTeamMembers(Team.Yellow, 0.0f);
        }
        else
        {
            //float pointDiff = winningTeam == Team.Yellow ? YellowTeamPoints - RedTeamPoints : RedTeamPoints - YellowTeamPoints;
            float timeBonus = Mathf.Clamp01(m_ResetTimer / ((float)timeLimit / 2.0f));
            AddRewardToTeamMembers(Team.Red, winningTeam == Team.Red ? 0.5f + timeBonus : -0.5f - timeBonus);
            AddRewardToTeamMembers(Team.Yellow, winningTeam == Team.Yellow ? 0.5f + timeBonus : -0.5f - timeBonus);
        }

        foreach (var agent in AgentsList)
        {
            if(TimeIsUp)
                agent.gameObject.GetComponent<Agent>().EpisodeInterrupted();
            else
                agent.gameObject.GetComponent<Agent>().EndEpisode();
        }

        m_ResetTimer = timeLimit;
        m_DetectedRedEnemies.Clear();
        m_DetectedYellowEnemies.Clear();
        m_DeadAgents.Clear();
        redAgentsOnCT = 0;
        yellowAgentsOnCT = 0;
        capturing = false;
        ctState = CTState.Neutral;
        m_ControlPoint.ChangeState(ctState);
        RedTeamPoints = 0.0f;
        YellowTeamPoints = 0.0f;
        m_MatchTotalKills = 0;
        m_MatchTankKills = 0;
        m_MatchHeliKills = 0;
    }

    public Vector3 GetCTPosition()
    {
        return m_ControlPoint.transform.localPosition;
    }

    public void AgentEnteredCT(Team team)
    {
        if (team == Team.Red)
            redAgentsOnCT++;
        else
            yellowAgentsOnCT++;
    }

    public void AgentExitedCT(Team team)
    {

        if (team == Team.Red)
            redAgentsOnCT--;
        else
            yellowAgentsOnCT--;

    }

    public void HandleCaptureState()
    {
        if (!capturing)
        {
            //if ((redAgentsOnCT > 0 && yellowAgentsOnCT == 0 && ctState != CTState.Red) 
            // || (redAgentsOnCT == 0 && yellowAgentsOnCT > 0 && ctState != CTState.Yellow))
            //{
            //    capturing = true;
            //}

            if ((redAgentsOnCT > yellowAgentsOnCT && ctState != CTState.Red)
             || (yellowAgentsOnCT > redAgentsOnCT && ctState != CTState.Yellow))
            {
                capturing = true;
            }
        }
        else
        {
            //if (redAgentsOnCT > 0 && yellowAgentsOnCT > 0
            // || redAgentsOnCT == 0 && yellowAgentsOnCT == 0
            // || ctState == CTState.Yellow && redAgentsOnCT == 0
            // || ctState == CTState.Red && yellowAgentsOnCT == 0)
            //{
            //    capturing = false;
            //}

            if (redAgentsOnCT == yellowAgentsOnCT)
            {
                capturing = false;
            }
        }
    }

    public float getTeamPoints(Team team)
    {
        if (team == Team.Red) return RedTeamPoints;
        else return YellowTeamPoints;
    }

    public void EnemyDetected(GameObject agent, Team team)
    {

        if (team == Team.Yellow)
        {
            if(m_DetectedRedEnemies.ContainsKey(agent))
            {
                m_DetectedRedEnemies[agent] = 10.0f;
            }
            else
            {
                m_DetectedRedEnemies.TryAdd(agent, 10.0f);
            }
        }
        else
        {
            if (m_DetectedYellowEnemies.ContainsKey(agent))
            {
                m_DetectedYellowEnemies[agent] = 10.0f;
            }
            else
            {
                m_DetectedYellowEnemies.TryAdd(agent, 10.0f);
            }
        }
    }

    public void resetCT()
    {
        StateNum = 0;
        ctState = CTState.Neutral;
        m_ControlPoint.ChangeState(ctState);
    }

    public float getStateNum()
    {
        return StateNum;
    }

    public float getRemainingTime()
    {
        return m_ResetTimer;
    }

    public void clearDetectedEnemies()
    {
        m_DetectedRedEnemies.Clear();
        m_DetectedYellowEnemies.Clear();
    }

    private void AddRewardToTeamMembers(Team team, float reward)
    {
        foreach(var agent in AgentsList)
        {
            if(agent.Team == team)
            {
                agent.gameObject.GetComponent<Agent>().AddReward(reward);
            }
        }
    }

    private void RegisterKillForStats(AgentType agentType)
    {
        m_MatchTotalKills++;

        if (agentType == AgentType.Tank)
        {
            m_MatchTankKills++;
        }
        else if (agentType == AgentType.Heli)
        {
            m_MatchHeliKills++;
        }
    }

    private void WriteMatchKillStats()
    {
        var statsRecorder = Academy.Instance.StatsRecorder;
        statsRecorder.Add("Match/Kills/Total", m_MatchTotalKills, StatAggregationMethod.Average);
        statsRecorder.Add("Match/Kills/Tank", m_MatchTankKills, StatAggregationMethod.Average);
        statsRecorder.Add("Match/Kills/Heli", m_MatchHeliKills, StatAggregationMethod.Average);
    }
}
