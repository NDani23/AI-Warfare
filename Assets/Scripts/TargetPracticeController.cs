using NUnit.Framework;
using Unity.MLAgents;
using UnityEngine;
using System.Collections.Generic;

enum Speed
{
    Slow,
    Normal,
    Fast
}

public class TargetPracticeController : MonoBehaviour
{
    [SerializeField] private bool Active;
    [SerializeField] private Transform redWallPrefab;
    [SerializeField] private Transform redTankPrefab;
    [SerializeField] private Transform redHeliPrefab;
    [SerializeField] private Transform yellowTargetTankPrefab;
    [SerializeField] private Transform yellowTargetWallPrefab;
    [SerializeField] private CTController m_ControlPoint;
    [SerializeField] private EnvController m_EnvController;

    [SerializeField] private float PracticeAreaWidth;
    [SerializeField] private float PracticeAreaLength;

    [SerializeField] private float TargetWidth;
    [SerializeField] private float TargetHeight;

    [SerializeField] private bool UseTargetWall = false;

    [SerializeField] private uint RedTargetCount = 1;
    [SerializeField] private uint YellowTargetCount = 1;

    [SerializeField] private bool MovingTargets;
    [SerializeField] private float TargetSpeed = 0.5f;
    [SerializeField] private float MoveDistance = 40f;

    [SerializeField] private bool FloatingTargets;

    [SerializeField] private bool AutomaticProgression;

    [SerializeField] private float TargetHealth;

    [SerializeField] private Speed ProgressionSpeed = Speed.Normal;

    private float CTRearrangeCooldown;
    private static float CTRearrangeInterval = 60.0f;

    private TargetScript[] m_redTargets;
    private TargetScript[] m_yellowTargets;

    private int hitCount = 0;
    private int captureCount = 0;

    private VehicleAgent player;

    void Start()
    {
        if(!Active) return;

        m_EnvController.GameEnded.AddListener(RearrangeTargets);

        if (AutomaticProgression)
        {
            FloatingTargets = false;
            MovingTargets = false;
            UseTargetWall = true;
            PracticeAreaLength = 600;
            PracticeAreaWidth = 600;
            TargetHeight = 50;
            TargetWidth = 120;
        }

        m_redTargets = new TargetScript[RedTargetCount];
        for (int i = 0; i < m_redTargets.Length; i++)
        {
            //Transform newTarget = UseTargetWall ? GameObject.Instantiate(targetWallPrefab, this.transform) : 
            //           (i < m_redTargets.Length-1) ? GameObject.Instantiate(targetTankPrefab, this.transform) : GameObject.Instantiate(targetHeliPrefab, this.transform);
            Transform newTarget = GameObject.Instantiate(redTankPrefab, this.transform);
            m_redTargets[i] = newTarget.gameObject.GetComponent<TargetScript>();
            m_redTargets[i].setController(this, Team.Red);
            m_redTargets[i].Rearrange(TargetWidth, TargetHeight, PracticeAreaLength, PracticeAreaWidth, FloatingTargets);
        }

        m_yellowTargets = new TargetScript[YellowTargetCount];
        for (int i = 0; i < m_yellowTargets.Length; i++)
        {
            //Transform newFakeTarget = UseTargetWall ? GameObject.Instantiate(yellowTargetWallPrefab, this.transform) : GameObject.Instantiate(yellowTargetWallPrefab, this.transform);
            Transform newTarget = GameObject.Instantiate(yellowTargetTankPrefab, this.transform);
            m_yellowTargets[i] = newTarget.gameObject.GetComponent<TargetScript>();
            m_yellowTargets[i].setController(this, Team.Yellow);
            m_yellowTargets[i].Rearrange(TargetWidth, TargetHeight, PracticeAreaLength, PracticeAreaWidth, FloatingTargets);
        }

        //RearrangeCT();
        //CTRearrangeCooldown = CTRearrangeInterval;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!Active) return;
        float t = Time.time;
        float dt = Time.deltaTime;

        //CTRearrangeCooldown -= Time.deltaTime;

        //if (CTRearrangeCooldown <= 0)
        //{
        //    RearrangeCT();

        //    CTRearrangeCooldown = CTRearrangeInterval;
        //}


    }

    public void HandleTargetHit(TargetScript target)
    {
        if (target.Health == 0)
        {
            target.Rearrange(TargetWidth, TargetHeight, PracticeAreaLength, PracticeAreaWidth, FloatingTargets);
            m_EnvController.AddPointToTeam(target.Team == Team.Red ? Team.Yellow : Team.Red, target.AgentType == AgentType.Tank ? 1 : 2);
        }
    }

    //private void HandleProgression()
    //{

    //    if (hitCount == 500)
    //    {
    //        m_EnvController.clearDetectedEnemies();
    //        for (int i = 0; i < m_redTargets.Length; i++)
    //        {
    //            Destroy(m_redTargets[i].gameObject);
    //        }


    //        m_redTargets = new TargetScript[3];
    //        for (int i = 0; i < m_redTargets.Length; i++)
    //        {
    //            Transform newTarget = GameObject.Instantiate(targetWallPrefab, this.transform);
    //            m_redTargets[i] = newTarget.gameObject.GetComponent<TargetScript>();
    //            m_redTargets[i].setController(this, false);
    //            m_redTargets[i].Rearrange(TargetWidth, TargetHeight, PracticeAreaLength, PracticeAreaWidth, FloatingTargets);
    //            m_redTargets[i].Health = TargetHealth;
    //        }

    //        FakeTargets = true;
    //        m_yellowTargets = new TargetScript[2];
    //        for (int i = 0; i < m_yellowTargets.Length; i++)
    //        {
    //            Transform newFakeTarget = GameObject.Instantiate(fakeTargetWallPrefab, this.transform);
    //            m_yellowTargets[i] = newFakeTarget.gameObject.GetComponent<TargetScript>();
    //            m_yellowTargets[i].setController(this, true);
    //            m_yellowTargets[i].Rearrange(TargetWidth, TargetHeight, PracticeAreaLength, PracticeAreaWidth, FloatingTargets);
    //            m_redTargets[i].Health = TargetHealth;
    //        }
    //    }



    //    if (hitCount == 1000)
    //    {
    //        m_EnvController.clearDetectedEnemies();
    //        for (int i = 0; i < m_redTargets.Length; i++)
    //        {
    //            Destroy(m_redTargets[i].gameObject);
    //        }

    //        for (int i = 0; i < m_yellowTargets.Length; i++)
    //        {
    //            Destroy(m_yellowTargets[i].gameObject);
    //        }

    //        m_redTargets = new TargetScript[5];
    //        for (int i = 0; i < m_redTargets.Length; i++)
    //        {
    //            Transform newTarget = GameObject.Instantiate(targetTankPrefab, this.transform);
    //            m_redTargets[i] = newTarget.gameObject.GetComponent<TargetScript>();
    //            m_redTargets[i].setController(this, false);
    //            m_redTargets[i].Rearrange(TargetWidth, TargetHeight, PracticeAreaLength, PracticeAreaWidth, FloatingTargets);
    //            m_redTargets[i].Health = TargetHealth;
    //        }

    //        m_yellowTargets = new TargetScript[3];
    //        for (int i = 0; i < m_yellowTargets.Length; i++)
    //        {
    //            Transform newFakeTarget = GameObject.Instantiate(fakeTargetTankPrefab, this.transform);
    //            m_yellowTargets[i] = newFakeTarget.gameObject.GetComponent<TargetScript>();
    //            m_yellowTargets[i].setController(this, true);
    //            m_yellowTargets[i].Rearrange(TargetWidth, TargetHeight, PracticeAreaLength, PracticeAreaWidth, FloatingTargets);
    //            m_redTargets[i].Health = TargetHealth;
    //        }

    //    }

    //    TargetWidth = TargetWidth <= 15 ? TargetWidth : TargetWidth - 0.15f;
    //    TargetHeight = TargetHeight <= 7 ? TargetHeight : TargetHeight - 0.04f;

    //}

    private void RearrangeCT()
    {
        m_ControlPoint.transform.localPosition = new Vector3(Random.Range(-PracticeAreaWidth / 2, PracticeAreaWidth / 2),
                                                0.0f,
                                                Random.Range(-PracticeAreaLength / 2, PracticeAreaLength / 2));

        m_EnvController.resetCT();
    }

    public void RequestRearrange(TargetScript target)
    {
        target.Rearrange(TargetWidth, TargetHeight, PracticeAreaLength, PracticeAreaWidth, FloatingTargets);
    }

    //private void handleCTStateChanged()
    //{
    //    if(m_ControlPoint.GetState() == CTState.Yellow && agent.Team == Team.Yellow
    //    || m_ControlPoint.GetState() == CTState.Red && agent.Team == Team.Red)
    //    {
    //        captureCount++;
    //        HandleProgression();
    //    }
    //}

    public TargetScript[] GetTargets()
    {
        return m_redTargets;
    }

    public void SetPlayerMaterial(Material mat = null)
    {
        if(mat is null)
        {
            player.SetMaterial();
        }
        else
        {
            player.SetMaterial(mat);
        }
    }

    public void RearrangeTargets()
    {
        foreach (TargetScript target in m_redTargets) 
            target.Rearrange(TargetWidth, TargetHeight, PracticeAreaLength, PracticeAreaWidth, FloatingTargets);

        foreach (TargetScript target in m_yellowTargets)
            target.Rearrange(TargetWidth, TargetHeight, PracticeAreaLength, PracticeAreaWidth, FloatingTargets);
    }

    public float GetTargetHealth()
    {
        return TargetHealth;
    }
}
