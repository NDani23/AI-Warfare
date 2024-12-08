using System.Collections.Generic;
using System.Linq;
using Unity.MLAgents;
using UnityEngine;
using UnityEngine.Events;

public class EnvController : MonoBehaviour
{
    [Tooltip("Time limit (seconds)")] public int timeLimit = 180;

    [SerializeField] private CTController m_ControlPoint;

    public static int RespawnCooldown = 5;

    public List<TankAgent> AgentsList = new List<TankAgent>();

    private SimpleMultiAgentGroup m_RedAgentGroup;
    private SimpleMultiAgentGroup m_YellowAgentGroup;

    private float m_ResetTimer = 0;

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

        foreach (var agent in m_DeadAgents.Keys.ToList())
        {
            m_DeadAgents[agent] = m_DeadAgents[agent] - Time.fixedDeltaTime;
            if (m_DeadAgents[agent] <= 0.0f)
            {
                m_DeadAgents.Remove(agent);
                agent.ResetAgent();
                agent.gameObject.SetActive(true);
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

        float pointToAdd = (100.0f / 60.0f) * Time.fixedDeltaTime;
        if (ctState == CTState.Red)
        {
            RedTeamPoints += pointToAdd;
            m_RedAgentGroup.AddGroupReward(pointToAdd * 0.01f);
            m_YellowAgentGroup.AddGroupReward(pointToAdd * -0.01f);
        }
        else if (ctState == CTState.Yellow)
        {
            YellowTeamPoints += pointToAdd;
            m_RedAgentGroup.AddGroupReward(pointToAdd * -0.01f);
            m_YellowAgentGroup.AddGroupReward(pointToAdd * 0.01f);
        }

        if (m_ResetTimer <= 0.0f)
        {

            if (RedTeamPoints > YellowTeamPoints)
            {
                RedWonEvent.Invoke();
                m_RedAgentGroup.AddGroupReward(1.0f);
            }
            else if (RedTeamPoints < YellowTeamPoints)
            {
                YellowWonEvent.Invoke();
                m_YellowAgentGroup.AddGroupReward(1.0f);
            }
            else
            {
                TieEvent.Invoke();
                m_YellowAgentGroup.AddGroupReward(0);
                m_RedAgentGroup.AddGroupReward(0);
            }
            gameEnded = true;
            //m_YellowAgentGroup.GroupEpisodeInterrupted();
            //m_RedAgentGroup.GroupEpisodeInterrupted();
            //ResetEnv();
            return;
        }


        if (RedTeamPoints >= 100.0f && YellowTeamPoints >= 100.0f)
        {
            TieEvent.Invoke();
            m_YellowAgentGroup.AddGroupReward(0);
            m_RedAgentGroup.AddGroupReward(0);
            gameEnded = true;
            //m_YellowAgentGroup.EndGroupEpisode();
            //m_RedAgentGroup.EndGroupEpisode();
            //ResetEnv();
            return;
        }
        else if (RedTeamPoints >= 100.0f)
        {
            RedWonEvent.Invoke();
            m_RedAgentGroup.AddGroupReward(1.0f);
            gameEnded = true;
            //m_YellowAgentGroup.EndGroupEpisode();
            //m_RedAgentGroup.EndGroupEpisode();
            //ResetEnv();
            return;
        }
        else if (YellowTeamPoints >= 100.0f)
        {
            YellowWonEvent.Invoke();
            m_YellowAgentGroup.AddGroupReward(1.0f);
            gameEnded = true;
            //m_YellowAgentGroup.EndGroupEpisode();
            //m_RedAgentGroup.EndGroupEpisode();
            //ResetEnv();
            return;
        }
    }

    public void AgentDied(TankAgent agent)
    {
        if (agent.team == Team.Red)
        {
            YellowTeamPoints += 2;
            m_YellowAgentGroup.AddGroupReward(0.02f);
            m_RedAgentGroup.AddGroupReward(-0.02f);
            m_DetectedRedEnemies.Remove(agent.gameObject);
        }
        else if (agent.team == Team.Yellow)
        {
            RedTeamPoints += 2;
            m_YellowAgentGroup.AddGroupReward(-0.02f);
            m_RedAgentGroup.AddGroupReward(0.02f);
            m_DetectedYellowEnemies.Remove(agent.gameObject);

        }

        m_DeadAgents.TryAdd(agent, RespawnCooldown);
        agent.gameObject.SetActive(false);
    }

    private void ResetEnv()
    {
        foreach (var agent in AgentsList)
        {

            agent.ResetAgent();
            agent.gameObject.SetActive(true);
            if (agent.team == Team.Red)
            {
                m_RedAgentGroup.RegisterAgent(agent);
            }
            else
            {
                m_YellowAgentGroup.RegisterAgent(agent);
            }

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
                m_DetectedRedEnemies[agent] = 20.0f;
            }
            else
            {
                m_DetectedRedEnemies.TryAdd(agent, 20.0f);
            }
        }
        else
        {
            if (m_DetectedYellowEnemies.ContainsKey(agent))
            {
                m_DetectedYellowEnemies[agent] = 20.0f;
            }
            else
            {
                m_DetectedYellowEnemies.TryAdd(agent, 20.0f);
            }
        }
    }

    public float getStateNum()
    {
        return StateNum;
    }

    public float getRemainingTime()
    {
        return m_ResetTimer;
    }
}
