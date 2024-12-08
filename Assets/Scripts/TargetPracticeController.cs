using UnityEngine;

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

    [SerializeField] private TankAgent agent;

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

    [SerializeField] private bool UseRearrange;

    [SerializeField] private Speed ProgressionSpeed = Speed.Normal;

    [SerializeField] private CTController m_ControlPoint;

    public float RearrangeTime { get; set; }
    private float CTRearrangeCooldown;
    private static float RearrangeInterval = 350.0f;
    private static float CTRearrangeInterval = 7.0f;

    private TargetScript targetWall;
    private TargetScript targetTank;
    private TargetScript currentTarget;

    private Vector3 basePos;

    private int hitCount = 0;

    private Transform fakeTargetWall;
    private Transform fakeTargetTank;
    private Transform currentFakeTarget;

    public bool agentInCT = false;



    // Start is called before the first frame update
    void Start()
    {
        if(!Active) return;

        if (AutomaticProgression)
        {
            FloatingTargets = false;
            MovingTargets = false;
            FakeTargets = false;
            UseRearrange = true;
            PracticeAreaLength = 600;
            PracticeAreaWidth = 600;
            TargetHeight = 80;
            TargetWidth = 200;
        }

        Transform newTarget = GameObject.Instantiate(targetWallPrefab, this.transform);
        targetWall = newTarget.gameObject.GetComponent<TargetScript>();
        targetWall.setController(this);


        if (FakeTargets)
        {
            fakeTargetWall = GameObject.Instantiate(fakeTargetWallPrefab, this.transform);
            currentFakeTarget = fakeTargetWall;
        }

        currentTarget = targetWall;
        basePos = newTarget.localPosition;

        RearrangeTargets();
        RearrangeCT();

        RearrangeTime = RearrangeInterval;
        CTRearrangeCooldown = 0;

    }

    // Update is called once per frame
    void Update()
    {
        if (!Active) return;
        float t = Time.time;
        float dt = Time.deltaTime;

        if (MovingTargets)
        {
            currentTarget.transform.localPosition = basePos + new Vector3(Mathf.Sin(t * (TargetSpeed / 10)) * MoveDistance, 0, 0);
        }

        RearrangeTime = Mathf.Max(0, RearrangeTime - Time.deltaTime);

        if (RearrangeTime == 0 && UseRearrange)
        {
            RearrangeTargets();

            RearrangeTime = RearrangeInterval;
        }

        if(agentInCT)
        {

            CTRearrangeCooldown -= Time.deltaTime;
            agent.AddReward(0.001f);

            if (CTRearrangeCooldown <= 0)
            {
                CTRearrangeCooldown = 0;
                agentInCT = false;
                RearrangeCT();
                Debug.Log("CAPTURED");
                agent.AddReward(5.0f);
            }
        }

    }

    public void HandleTargetHit()
    {
        if (AutomaticProgression)
        {
            HandleProgression();
        }

        RearrangeTargets();

        basePos = currentTarget.transform.localPosition;

        hitCount++;

        if(UseRearrange)    
            RearrangeTime = RearrangeInterval;

    }

    public void HandleCTEnter()
    {
        CTRearrangeCooldown = CTRearrangeInterval;
        agentInCT = true;
    }

    public void HandleCTExit()
    {

        CTRearrangeCooldown = 0;
        agentInCT = false;
    }

    private void HandleProgression()
    {
        //if(hitCount == 3000)
        //{
        //    FakeTargets = true;
        //    fakeTargetWall = GameObject.Instantiate(fakeTargetWallPrefab, this.transform);
        //    SetTargetTransforms(fakeTargetWall);
        //    currentFakeTarget = fakeTargetWall;
        //}

        if(hitCount == 1)
        {
            //FloatingTargets = true;
            Transform newTarget = GameObject.Instantiate(targetTankPrefab, this.transform);
            targetTank = newTarget.gameObject.GetComponent<TargetScript>();
            targetTank.setController(this);
            currentTarget = targetTank;
            targetWall.gameObject.SetActive(false);
            //fakeTargetWall.gameObject.SetActive(false);

            RearrangeTargets();

        }

        if (hitCount == 1000)
        {
            FakeTargets = true;
            fakeTargetTank = GameObject.Instantiate(fakeTargetTankPrefab, this.transform);
            currentFakeTarget = fakeTargetTank;
            RearrangeTargets();
            //fakeTargetWall.gameObject.SetActive(false);
        }

        //if(hitCount == 30000)
        //{
        //    MovingTargets = true;
        //    TargetSpeed = 1;
        //    MoveDistance = 50;
        //}

        if (MovingTargets)
        {
            TargetSpeed = TargetSpeed >= 5 ? TargetSpeed : TargetSpeed + 0.0005f;
        }

        TargetWidth = TargetWidth <= 15 ? TargetWidth : TargetWidth - 0.01f;
        TargetHeight = TargetHeight <= 15 ? TargetHeight : TargetHeight - 0.0035f;

        //PracticeAreaLength = PracticeAreaLength == 300 ? PracticeAreaLength : PracticeAreaLength + 0.5f;
        //PracticeAreaWidth = PracticeAreaWidth == 400 ? PracticeAreaWidth : PracticeAreaWidth + 0.5f;
    }

    private void SetTargetTransforms(Transform target, int AreaID)
    {
        float newHeight = 3.0f;
        if (target == targetWall.transform || target == fakeTargetWall)
        {
            target.localScale = new Vector3(TargetWidth, TargetHeight, 3);
            newHeight = FloatingTargets ? Random.Range(TargetHeight / 2, 50.0f) : TargetHeight / 2;
        }

        switch(AreaID)
        {
            case 0:
                target.transform.localPosition = new Vector3(Random.Range(-PracticeAreaWidth / 2, 0),
                                                 newHeight,
                                                 Random.Range(-PracticeAreaLength / 2, 0));
                break;
            case 1:
                target.transform.localPosition = new Vector3(Random.Range(-PracticeAreaWidth / 2, 0),
                                                  newHeight,
                                                  Random.Range(0, PracticeAreaLength / 2));
                break;
            case 2:
                target.transform.localPosition = new Vector3(Random.Range(0, PracticeAreaWidth / 2),
                                                 newHeight,
                                                 Random.Range(0, PracticeAreaLength / 2));
                break;
            case 3:
                target.transform.localPosition = new Vector3(Random.Range(0, PracticeAreaWidth / 2),
                                                  newHeight,
                                                  Random.Range(-PracticeAreaLength / 2, 0));
                break;
            default:
                target.transform.localPosition = new Vector3(Random.Range(-PracticeAreaWidth / 2, PracticeAreaWidth / 2),
                                                  newHeight,
                                                  Random.Range(-PracticeAreaLength / 2, PracticeAreaLength / 2));
                break;
        }

    }

    private void RearrangeTargets()
    {
        if (!FakeTargets)
        {
            SetTargetTransforms(currentTarget.transform, -1);
        }
        else
        {
            int areaId = Random.Range(0, 3);
            SetTargetTransforms(currentTarget.transform, areaId);

            int newId = Random.Range(0, 3);
            if (newId == areaId) newId = newId += 1 % 4;

            SetTargetTransforms(currentFakeTarget, newId);
        }
    }

    private void RearrangeCT()
    {
        m_ControlPoint.transform.localPosition = new Vector3(Random.Range(-200, 200), 0, Random.Range(-200, 200));
        //agentInCT = false;
    }
}
