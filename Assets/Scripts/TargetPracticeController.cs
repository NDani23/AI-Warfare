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
    [SerializeField] private Transform targetWallPrefab;
    [SerializeField] private Transform targetTankPrefab;
    [SerializeField] private Transform fakeTargetTankPrefab;
    [SerializeField] private Transform fakeTargetWallPrefab;
    [SerializeField] private CTController m_ControlPoint;
    [SerializeField] private EnvController m_EnvController;
    [SerializeField] private Agent playerSerialized;

    [SerializeField] private float PracticeAreaWidth;
    [SerializeField] private float PracticeAreaLength;

    [SerializeField] private float TargetWidth;
    [SerializeField] private float TargetHeight;

    [SerializeField] private bool MovingTargets;
    [SerializeField] private float TargetSpeed = 0.5f;
    [SerializeField] private float MoveDistance = 40f;

    [SerializeField] private bool FakeTargets;

    [SerializeField] private bool FloatingTargets;

    [SerializeField] private bool AutomaticProgression;

    [SerializeField] private Speed ProgressionSpeed = Speed.Normal;

    private float CTRearrangeCooldown;
    private static float CTRearrangeInterval = 60.0f;

    private TargetScript[] m_targets;
    private TargetScript[] m_fakeTargets;

    private int hitCount = 0;
    private int captureCount = 0;

    private IVehicleAgent player;

    // Start is called before the first frame update
    void Start()
    {
        if(!Active) return;

        if (playerSerialized is IVehicleAgent vehicleAgent)
        {
            player = vehicleAgent;
        }

        if (AutomaticProgression)
        {
            FloatingTargets = false;
            MovingTargets = false;
            FakeTargets = false;
            PracticeAreaLength = 600;
            PracticeAreaWidth = 600;
            TargetHeight = 50;
            TargetWidth = 120;
        }

        m_targets = new TargetScript[1];
        for (int i = 0; i < m_targets.Length; i++)
        {
            Transform newTarget = GameObject.Instantiate(targetWallPrefab, this.transform);
            m_targets[i] = newTarget.gameObject.GetComponent<TargetScript>();
            m_targets[i].setController(this, false);
            m_targets[i].Rearrange(TargetWidth, TargetHeight, PracticeAreaLength, PracticeAreaWidth, FloatingTargets);
        }


        if(FakeTargets)
        {
            m_fakeTargets = new TargetScript[1];
            for (int i = 0; i < m_fakeTargets.Length; i++)
            {
                Transform newFakeTarget = GameObject.Instantiate(fakeTargetWallPrefab, this.transform);
                m_fakeTargets[i] = newFakeTarget.gameObject.GetComponent<TargetScript>();
                m_fakeTargets[i].setController(this, true);
                m_fakeTargets[i].Rearrange(TargetWidth, TargetHeight, PracticeAreaLength, PracticeAreaWidth, FloatingTargets);
            }
        }

        //m_ControlPoint.StateChangedEvent.AddListener(handleCTStateChanged);

        RearrangeCT();
        CTRearrangeCooldown = CTRearrangeInterval;

    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (!Active) return;
        float t = Time.time;
        float dt = Time.deltaTime;

        CTRearrangeCooldown -= Time.deltaTime;

        if (CTRearrangeCooldown <= 0)
        {
            RearrangeCT();

            CTRearrangeCooldown = CTRearrangeInterval;
        }


    }

    public void HandleTargetHit(TargetScript target)
    {
        if(target.isFakeTarget())
        {
            player.gameObject.GetComponent<Agent>().AddReward(-1.0f);
        }
        else
        {
            hitCount++;
            player.gameObject.GetComponent<Agent>().AddReward(1.0f);
            if (AutomaticProgression)
            {
                HandleProgression();
            }
        }

        target.Rearrange(TargetWidth, TargetHeight, PracticeAreaLength, PracticeAreaWidth, FloatingTargets);
    }

    private void HandleProgression()
    {

        if (hitCount == 500)
        {
            m_EnvController.clearDetectedEnemies();
            for (int i = 0; i < m_targets.Length; i++)
            {
                Destroy(m_targets[i].gameObject);
            }


            m_targets = new TargetScript[3];
            for (int i = 0; i < m_targets.Length; i++)
            {
                Transform newTarget = GameObject.Instantiate(targetWallPrefab, this.transform);
                m_targets[i] = newTarget.gameObject.GetComponent<TargetScript>();
                m_targets[i].setController(this, false);
                m_targets[i].Rearrange(TargetWidth, TargetHeight, PracticeAreaLength, PracticeAreaWidth, FloatingTargets);
            }

            FakeTargets = true;
            m_fakeTargets = new TargetScript[2];
            for (int i = 0; i < m_fakeTargets.Length; i++)
            {
                Transform newFakeTarget = GameObject.Instantiate(fakeTargetWallPrefab, this.transform);
                m_fakeTargets[i] = newFakeTarget.gameObject.GetComponent<TargetScript>();
                m_fakeTargets[i].setController(this, true);
                m_fakeTargets[i].Rearrange(TargetWidth, TargetHeight, PracticeAreaLength, PracticeAreaWidth, FloatingTargets);
            }
        }



        if (hitCount == 1000)
        {
            m_EnvController.clearDetectedEnemies();
            for (int i = 0; i < m_targets.Length; i++)
            {
                Destroy(m_targets[i].gameObject);
            }

            for (int i = 0; i < m_fakeTargets.Length; i++)
            {
                Destroy(m_fakeTargets[i].gameObject);
            }

            m_targets = new TargetScript[5];
            for (int i = 0; i < m_targets.Length; i++)
            {
                Transform newTarget = GameObject.Instantiate(targetTankPrefab, this.transform);
                m_targets[i] = newTarget.gameObject.GetComponent<TargetScript>();
                m_targets[i].setController(this, false);
                m_targets[i].Rearrange(TargetWidth, TargetHeight, PracticeAreaLength, PracticeAreaWidth, FloatingTargets);
            }

            m_fakeTargets = new TargetScript[3];
            for (int i = 0; i < m_fakeTargets.Length; i++)
            {
                Transform newFakeTarget = GameObject.Instantiate(fakeTargetTankPrefab, this.transform);
                m_fakeTargets[i] = newFakeTarget.gameObject.GetComponent<TargetScript>();
                m_fakeTargets[i].setController(this, true);
                m_fakeTargets[i].Rearrange(TargetWidth, TargetHeight, PracticeAreaLength, PracticeAreaWidth, FloatingTargets);
            }

        }

        TargetWidth = TargetWidth <= 15 ? TargetWidth : TargetWidth - 0.15f;
        TargetHeight = TargetHeight <= 7 ? TargetHeight : TargetHeight - 0.04f;

    }

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
        return m_targets;
    }

    public TargetScript[] GetFakeTargets()
    {
        if (FakeTargets)
            return m_fakeTargets;
        else
            return null;
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
}
