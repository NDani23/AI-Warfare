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

enum TrainingCommandMode
{
    GoTo = 0,
    KillTarget = 1
}

public class TargetPracticeController : MonoBehaviour
{
    [SerializeField] private bool Active;
    [SerializeField] private Transform redWallPrefab;
    [SerializeField] private Transform redTankPrefab;
    [SerializeField] private Transform redHeliPrefab;
    [SerializeField] private Transform yellowTankPrefab;
    [SerializeField] private Transform yellowWallPrefab;
    [SerializeField] private Transform yellowHeliPrefab;
    [SerializeField] private CTController m_ControlPoint;
    [SerializeField] private EnvController m_EnvController;

    [SerializeField] private float PracticeAreaWidth;
    [SerializeField] private float PracticeAreaLength;

    [SerializeField] private float TargetWidth;
    [SerializeField] private float TargetHeight;
    [SerializeField] private TrainingCommandMode CommandToTrain = TrainingCommandMode.KillTarget;

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
    [SerializeField] private bool PlaceObstacles = false;
    [SerializeField] private int ObstacleCount = 10;
    [SerializeField] private List<GameObject> ObstaclePrefabs = new List<GameObject>();

    private float CTRearrangeCooldown;
    private static float CTRearrangeInterval = 60.0f;

    public TargetScript[] m_redTargets;
    public TargetScript[] m_yellowTargets;
    private GameObject[] m_obstacles;

    private int hitCount = 0;
    private int captureCount = 0;

    private VehicleManager player;

    private TrainingCommandMode _currentCommandMode;

    void Start()
    {
        if(!Active) return;

        if (m_EnvController != null)
        {
            m_EnvController.GameEnded.AddListener(HandleEpisodeEnded);
        }

       // m_EnvController.GameEnded.AddListener(RearrangeTargets);

        if(PlaceObstacles)
            m_EnvController.GameEnded.AddListener(RepositionObstacles);

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

        _currentCommandMode = CommandToTrain;

        m_redTargets = new TargetScript[RedTargetCount];
        for (int i = 0; i < m_redTargets.Length; i++)
        {
            Transform newTarget = UseTargetWall ? GameObject.Instantiate(redWallPrefab, this.transform) : 
                      (i < m_redTargets.Length-1) ? GameObject.Instantiate(redTankPrefab, this.transform) : GameObject.Instantiate(redHeliPrefab, this.transform);
            m_redTargets[i] = newTarget.gameObject.GetComponent<TargetScript>();
            m_redTargets[i].setController(this, Team.Red);
            m_redTargets[i].Rearrange(TargetWidth, TargetHeight, PracticeAreaLength, PracticeAreaWidth, FloatingTargets);
        }

        m_yellowTargets = new TargetScript[YellowTargetCount];
        for (int i = 0; i < m_yellowTargets.Length; i++)
        {
            Transform newTarget = UseTargetWall ? GameObject.Instantiate(yellowWallPrefab, this.transform) : 
                      (i < m_yellowTargets.Length-1) ? GameObject.Instantiate(yellowTankPrefab, this.transform) : GameObject.Instantiate(yellowHeliPrefab, this.transform);
            m_yellowTargets[i] = newTarget.gameObject.GetComponent<TargetScript>();
            m_yellowTargets[i].setController(this, Team.Yellow);
            m_yellowTargets[i].Rearrange(TargetWidth, TargetHeight, PracticeAreaLength, PracticeAreaWidth, FloatingTargets);
        }

        if (PlaceObstacles && ObstaclePrefabs.Count > 0)
        {
            m_obstacles = new GameObject[ObstacleCount];
            for (int i = 0; i < ObstacleCount; i++)
            {
                m_obstacles[i] = GameObject.Instantiate(ObstaclePrefabs[i % ObstaclePrefabs.Count], this.transform);
            }
        }

        if(PlaceObstacles)
            RepositionObstacles();

        ApplyCommandsToAllTanks();

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
            //m_EnvController.AddPointToTeam(target.Team == Team.Red ? Team.Yellow : Team.Red, target.TargetType == TargetType.Tank ? 1 : 2);
        }
    }

    public void HandleGoToReached(TankManager tank)
    {
        if (!Active || tank == null)
            return;

        if (_currentCommandMode != TrainingCommandMode.GoTo)
            return;

        Debug.Log("Go-to point reached!");
        tank.AddRewardToDriver(2.0f);
        AssignCommandForTank(tank);
    }

    public void HandleMarkedTargetHit(TankManager tank)
    {
        if (!Active || tank == null)
            return;

        if (_currentCommandMode != TrainingCommandMode.KillTarget)
            return;

        AssignCommandForTank(tank);
    }

    public void AssignCommandForTank(TankManager tank)
    {
        if (!Active || tank == null)
            return;

        if (_currentCommandMode == TrainingCommandMode.GoTo)
        {
            Vector3 localPos = GetRandomGoToLocalPosition();
            tank.IssueCommand(localPos + this.transform.position);
        }
        else
        {
            TargetScript target = GetRandomTargetForTeam(tank.Team);
            if (target == null)
                return;

            target.GetComponent<ITargetable>()?.setDetectedState(true);
            tank.IssueCommand(target.gameObject);
        }
    }

    private void ApplyCommandsToAllTanks()
    {
        if (!Active || m_EnvController == null)
            return;

        foreach (VehicleManager agent in m_EnvController.VehicleList)
        {
            if (agent.VehicleType != VehicleType.Tank)
                continue;

            AssignCommandForTank((TankManager)agent);
        }
    }

    private void HandleEpisodeEnded()
    {
        if (!Active)
            return;

        //_currentCommandMode = _currentCommandMode == TrainingCommandMode.GoTo ? TrainingCommandMode.KillTarget : TrainingCommandMode.GoTo;
        ApplyCommandsToAllTanks();
    }

    private Vector3 GetRandomGoToLocalPosition()
    {
        Vector3 localCandidatePos = new Vector3(Random.Range(-300, 300), 3.0f, Random.Range(-300, 300));
        for(int i = 0; i < 100; i++)
        {
            if(!Physics.CheckSphere(this.transform.TransformPoint(localCandidatePos), 15.0f, ~LayerMask.GetMask("Ground")))
                break;
            localCandidatePos = new Vector3(Random.Range(-300, 300), 3.0f, Random.Range(-300, 300));
        }

        return localCandidatePos;
    }

    private TargetScript GetRandomTargetForTeam(Team team)
    {
        TargetScript[] targets = team == Team.Red ? m_yellowTargets : m_redTargets;
        if (targets == null || targets.Length == 0)
            return null;

        return targets[Random.Range(0, targets.Length)];
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
        Debug.Log("REARRANGE");
        foreach (TargetScript target in m_redTargets) 
            target.Rearrange(TargetWidth, TargetHeight, PracticeAreaLength, PracticeAreaWidth, FloatingTargets);

        foreach (TargetScript target in m_yellowTargets)
            target.Rearrange(TargetWidth, TargetHeight, PracticeAreaLength, PracticeAreaWidth, FloatingTargets);
    }

    public float GetTargetHealth()
    {
        return TargetHealth;
    }

    private void RepositionObstacles()
    {
        Physics.SyncTransforms();

        int layersToCheck = ~LayerMask.GetMask("Ground");

        foreach (GameObject obstacle in m_obstacles)
        {
            for(int i = 0; i < 100; i++)
            {
                float xPos = Random.Range(-PracticeAreaWidth / 2, PracticeAreaWidth / 2);
                float zPos = Random.Range(-PracticeAreaLength / 2, PracticeAreaLength / 2);
                Vector3 localCandidatePos = new Vector3(xPos, 0, zPos);
                Vector3 worldCandidatePos = obstacle.transform.parent.TransformPoint(localCandidatePos);

                if(!Physics.CheckSphere(worldCandidatePos, GetComplexPrefabRadius(obstacle), layersToCheck))
                {
                    obstacle.transform.localPosition =  localCandidatePos;
                    obstacle.transform.localRotation = Quaternion.Euler(0, Random.Range(0, 360), 0);
                    break;
                }
            }
        }
    }

    private float GetComplexPrefabRadius(GameObject prefab)
    {
        Collider[] allColliders = prefab.GetComponentsInChildren<Collider>();

        if (allColliders.Length == 0)
        {
            return 0f;
        }

        Bounds totalBounds = allColliders[0].bounds;

        for (int i = 1; i < allColliders.Length; i++)
        {
            totalBounds.Encapsulate(allColliders[i].bounds);
        }

        float maxRadius = Mathf.Max(totalBounds.extents.x, totalBounds.extents.z);

        return maxRadius;
    }
}
