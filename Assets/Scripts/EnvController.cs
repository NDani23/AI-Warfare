using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.Events;

public class EnvController : MonoBehaviour
{
    [Tooltip("Time limit (seconds)")] public int timeLimit = 60;

    [SerializeField] private CTController m_ControlPoint;

    public static int RespawnCooldown = 5;

    public List<TankAgent> AgentsList = new List<TankAgent>();

    public SimpleMultiAgentGroup m_RedAgentGroup;
    public SimpleMultiAgentGroup m_YellowAgentGroup;

    public float m_ResetTimer = 0;

    public Dictionary<GameObject,float> m_DetectedRedEnemies;
    public Dictionary<GameObject, float> m_DetectedYellowEnemies;
    public Dictionary<TankAgent, float> m_DeadAgents;

    public float RedTeamPoints = 0.0f;
    public float YellowTeamPoints = 0.0f;

    public CTState ctState = CTState.Neutral;

    private int redAgentsOnCT = 0;
    private int yellowAgentsOnCT = 0;

    private bool capturing = false;
    private float StateNum = 0.0f;

    private bool gameEnded = false;

    public UnityEvent RedWonEvent;
    public UnityEvent YellowWonEvent;
    public UnityEvent TieEvent;
    private void Awake()
    {
        m_DetectedRedEnemies = new Dictionary<GameObject, float>();
        m_DetectedYellowEnemies = new Dictionary<GameObject, float>();
        m_DeadAgents = new Dictionary<TankAgent, float>();
    }


    void Start()
    {
        m_ResetTimer = timeLimit;
        m_RedAgentGroup = new SimpleMultiAgentGroup();
        m_YellowAgentGroup = new SimpleMultiAgentGroup();

        foreach (var agent in AgentsList)
        {
            if (agent.team == Team.Red)
            {
                m_RedAgentGroup.RegisterAgent(agent);
            }
            else
            {
                m_YellowAgentGroup.RegisterAgent(agent);
            }
        }
    }

    void FixedUpdate()
    {

        if (gameEnded) return;

        m_ResetTimer -= Time.fixedDeltaTime;
        HandleCaptureState();


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

        //foreach (var agent in m_DeadAgents.Keys.ToList())
        //{
        //    m_DeadAgents[agent] = m_DeadAgents[agent] - Time.fixedDeltaTime;
        //    if (m_DeadAgents[agent] <= 0.0f)
        //    {
        //        m_DeadAgents.Remove(agent);
        //        agent.ResetAgent();
        //        agent.gameObject.SetActive(true);
        //        if (agent.team == Team.Red)
        //        {
        //            m_RedAgentGroup.RegisterAgent(agent);
        //        }
        //        else
        //        {
        //            m_YellowAgentGroup.RegisterAgent(agent);
        //        }
        //    }
        //}

        if (capturing)
        {
            if(redAgentsOnCT < yellowAgentsOnCT)
            {
                if (StateNum < 0 && ctState != CTState.Red) StateNum = 0;
                StateNum = Mathf.Min(StateNum + Time.fixedDeltaTime, 10.0f);

                if (StateNum >= 0.0f && ctState == CTState.Red)
                {
                    ctState = CTState.Neutral;
                    m_ControlPoint.ChangeState(ctState);
                }

                if (StateNum == 10.0f && ctState != CTState.Yellow)
                {

                    ctState = CTState.Yellow;
                    m_ControlPoint.ChangeState(ctState);
                    //AddRewardToTeamMembers(Team.Yellow, 0.5f);
                    m_YellowAgentGroup.AddGroupReward(0.5f);
                    capturing = false;
                }
            }
            else
            {
                if (StateNum > 0 && ctState != CTState.Yellow) StateNum = 0;
                StateNum = Mathf.Max(StateNum - Time.fixedDeltaTime, -10.0f);

                if (StateNum <= 0.0 && ctState == CTState.Yellow)
                {
                    ctState = CTState.Neutral;
                    m_ControlPoint.ChangeState(ctState);
                }

                if (StateNum == -10.0f && ctState != CTState.Red)
                {
                    ctState = CTState.Red;
                    m_ControlPoint.ChangeState(ctState);
                    //AddRewardToTeamMembers(Team.Red, 0.5f);
                    m_RedAgentGroup.AddGroupReward(0.5f);
                    capturing = false;
                }
            }
        }
        else
        {
            if ((redAgentsOnCT == 0 && yellowAgentsOnCT == 0)
             || (redAgentsOnCT == 0 && ctState == CTState.Yellow)
             || (yellowAgentsOnCT == 0 && ctState == CTState.Red))
            {
                switch (ctState)
                {
                    case CTState.Red:
                        StateNum = -10.0f;
                        break;
                    case CTState.Yellow:
                        StateNum = 10.0f;
                        break;
                    case CTState.Neutral:
                        StateNum = 0;
                        break;

                }
            }
        }

        //float pointDifferenceForRed = (RedTeamPoints - YellowTeamPoints) * 0.01f;
        //m_RedAgentGroup.AddGroupReward(pointDifferenceForRed * (Time.fixedDeltaTime / timeLimit));
        //float pointDifferenceForYellow = (YellowTeamPoints - RedTeamPoints) * 0.01f;
        //m_YellowAgentGroup.AddGroupReward(pointDifferenceForYellow * (Time.fixedDeltaTime / timeLimit));

        float pointToAdd = (100.0f / 45.0f) * Time.fixedDeltaTime;
        if (ctState == CTState.Red)
        {
            RedTeamPoints += pointToAdd;
            //AddRewardToTeamMembers(Team.Red, pointToAdd * 0.01f);
            //AddRewardToTeamMembers(Team.Yellow, -pointToAdd * 0.001f);
            m_RedAgentGroup.AddGroupReward(pointToAdd * 0.01f);
            m_YellowAgentGroup.AddGroupReward(pointToAdd * -0.005f);
        }
        else if (ctState == CTState.Yellow)
        {
            YellowTeamPoints += pointToAdd;
            //AddRewardToTeamMembers(Team.Yellow, pointToAdd * 0.01f);
            //AddRewardToTeamMembers(Team.Red, -pointToAdd * 0.001f);
            m_RedAgentGroup.AddGroupReward(pointToAdd * -0.005f);
            m_YellowAgentGroup.AddGroupReward(pointToAdd * 0.01f);
        }
        //else
        //{
        //    AddRewardToTeamMembers(Team.Yellow, -(Time.fixedDeltaTime * 0.2f) / timeLimit);
        //    AddRewardToTeamMembers(Team.Red, -(Time.fixedDeltaTime * 0.2f) / timeLimit);
        //}

        if (m_ResetTimer <= 0.0f)
        {

            //if (RedTeamPoints > YellowTeamPoints)
            //{
            //    RedWonEvent.Invoke();
            //    m_RedAgentGroup.AddGroupReward(1.0f);
            //}
            //else if (RedTeamPoints < YellowTeamPoints)
            //{
            //    YellowWonEvent.Invoke();
            //    m_YellowAgentGroup.AddGroupReward(1.0f);
            //}
            //else
            //{
            //    TieEvent.Invoke();
            //    m_YellowAgentGroup.AddGroupReward(0);
            //    m_RedAgentGroup.AddGroupReward(0);
            //}
            //gameEnded = true;
            //m_YellowAgentGroup.GroupEpisodeInterrupted();
            //m_RedAgentGroup.GroupEpisodeInterrupted();
            //ResetEnv();
            if (RedTeamPoints == YellowTeamPoints)
                ResetEnv(null, true);
            else if (RedTeamPoints > YellowTeamPoints)
                ResetEnv(Team.Red, true);
            else if (YellowTeamPoints > RedTeamPoints)
                ResetEnv(Team.Yellow, true);

            return;
        }


        if (RedTeamPoints >= 100.0f && YellowTeamPoints >= 100.0f)
        {
            //TieEvent.Invoke();
            //m_YellowAgentGroup.AddGroupReward(0);
            //m_RedAgentGroup.AddGroupReward(0);
            //gameEnded = true;
            //m_YellowAgentGroup.EndGroupEpisode();
            //m_RedAgentGroup.EndGroupEpisode();
            //ResetEnv();
            ResetEnv(null);
            return;
        }
        else if (RedTeamPoints >= 100.0f)
        {
            //RedWonEvent.Invoke();
            //m_RedAgentGroup.AddGroupReward(1.0f);
            //gameEnded = true;
            //m_YellowAgentGroup.EndGroupEpisode();
            //m_RedAgentGroup.EndGroupEpisode();
            //ResetEnv();
            ResetEnv(Team.Red);
            return;
        }
        else if (YellowTeamPoints >= 100.0f)
        {
            //YellowWonEvent.Invoke();
            //m_YellowAgentGroup.AddGroupReward(1.0f);
            //gameEnded = true;
            //m_YellowAgentGroup.EndGroupEpisode();
            //m_RedAgentGroup.EndGroupEpisode();
            //ResetEnv();
            ResetEnv(Team.Yellow);
            return;
        }
    }

    public void AgentDied(TankAgent agent)
    {
        if (agent.team == Team.Red)
        {
            YellowTeamPoints += 10;
            //AddRewardToTeamMembers(Team.Yellow, 0.5f);
            //AddRewardToTeamMembers(Team.Red, -0.1f);
            m_YellowAgentGroup.AddGroupReward(0.1f);
            m_RedAgentGroup.AddGroupReward(-0.01f);
            m_DetectedRedEnemies.Remove(agent.gameObject);

        }
        else if (agent.team == Team.Yellow)
        {
            RedTeamPoints += 10;
            //AddRewardToTeamMembers(Team.Red, 0.5f);
            //AddRewardToTeamMembers(Team.Yellow, -0.1f);
            m_YellowAgentGroup.AddGroupReward(-0.01f);
            m_RedAgentGroup.AddGroupReward(0.1f);
            m_DetectedYellowEnemies.Remove(agent.gameObject);

        }
        agent.ResetAgent();
        //ResetEnv(agent.team == Team.Red? Team.Yellow : Team.Red);

        //m_DeadAgents.TryAdd(agent, RespawnCooldown);
        //agent.gameObject.SetActive(false);
    }

    private void ResetEnv(Team? winningTeam, bool TimeIsUp = false)
    {
        //foreach (var agent in AgentsList)
        //{
        //    //agent.ResetAgent();
        //    //agent.gameObject.SetActive(true);
        //    //if (agent.team == Team.Red)
        //    //{
        //    //    //m_RedAgentGroup.RegisterAgent(agent);
        //    //}
        //    //else
        //    //{
        //    //    //m_YellowAgentGroup.RegisterAgent(agent);
        //    //}

        //    if (winningTeam is null)
        //    {
        //        //YellowWonEvent.Invoke();
        //        //m_YellowAgentGroup.AddGroupReward(1.0f);
        //        //gameEnded = true;
        //        //m_YellowAgentGroup.EndGroupEpisode();
        //        //m_RedAgentGroup.EndGroupEpisode();
        //        //ResetEnv();
        //        agent.AddReward(0.0f);
        //    }
        //    else
        //    {

        //        if (agent.team == winningTeam)
        //        {
        //            //agent.AddReward(1.0f - (1 - (m_ResetTimer / timeLimit) * 0.5f));
        //            agent.AddReward(1.0f);
        //        }
        //        else
        //        {
        //            agent.AddReward(-1.0f);
        //        }
        //    }
        //    //if (agent.team == Team.Yellow)
        //    //    Debug.Log(agent.GetCumulativeReward());

        //    agent.EndEpisode();
        //}


        if (winningTeam is null)
        {
            m_YellowAgentGroup.AddGroupReward(0.0f);
            m_RedAgentGroup.AddGroupReward(0.0f);
        }
        else
        {
            m_YellowAgentGroup.AddGroupReward(winningTeam == Team.Red ? -1.0f : 1.0f);
            m_RedAgentGroup.AddGroupReward(winningTeam == Team.Red ? 1.0f : -1.0f);
        }

        if (TimeIsUp)
        {
            m_YellowAgentGroup.GroupEpisodeInterrupted();
            m_RedAgentGroup.GroupEpisodeInterrupted();
        }
        else
        {
            m_YellowAgentGroup.EndGroupEpisode();
            m_RedAgentGroup.EndGroupEpisode();
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
            if ((redAgentsOnCT > 0 && yellowAgentsOnCT == 0 && ctState != CTState.Red) 
             || (redAgentsOnCT == 0 && yellowAgentsOnCT > 0 && ctState != CTState.Yellow))
            {
                capturing = true;
            }
        }
        else
        {
            if (redAgentsOnCT > 0 && yellowAgentsOnCT > 0
             || redAgentsOnCT == 0 && yellowAgentsOnCT == 0
             || ctState == CTState.Yellow && redAgentsOnCT == 0
             || ctState == CTState.Red && yellowAgentsOnCT == 0)
            {
                capturing = false;
            }
        }
    }

    public void EnemyDetected(GameObject agent, Team team)
    {

        if (team == Team.Yellow)
        {
            if(m_DetectedRedEnemies.ContainsKey(agent))
            {
                m_DetectedRedEnemies[agent] = 15.0f;
            }
            else
            {
                m_DetectedRedEnemies.TryAdd(agent, 15.0f);
            }
        }
        else
        {
            if (m_DetectedYellowEnemies.ContainsKey(agent))
            {
                m_DetectedYellowEnemies[agent] = 15.0f;
            }
            else
            {
                m_DetectedYellowEnemies.TryAdd(agent, 15.0f);
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
        foreach(TankAgent agent in AgentsList)
        {
            if(agent.team == team)
            {
                agent.AddReward(reward);
            }
        }
    }
}
