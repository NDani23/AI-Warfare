using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public enum TargetType
{
    Wall = 0,
    Tank = 1,
    Heli = 2
}

public class TargetScript : MonoBehaviour, ITargetable
{
    [SerializeField] private Material MainMaterial;
    [SerializeField] private Transform _gridTag;

    public TargetType targetType;
    public TargetType TargetType
    {
        get { return targetType; }
        set { targetType = value; }
    }

    private TargetPracticeController targetController;

    private Team team;
    public Team Team => team;

    private float m_health = 1;
    public float Health
    {
        get { return m_health;  }
        set { m_health = value; }
    }

    protected bool _detected = false;
    public bool Detected => _detected;

    private float m_rearrangeInterval = 60.0f;
    private float m_rearrangeCooldown = 60.0f;

    public void setController(TargetPracticeController controller, Team team)
    {
        this.targetController = controller;
        this.team = team;
    }

    private void FixedUpdate()
    {
        m_rearrangeCooldown -= Time.fixedDeltaTime;
        if(m_rearrangeCooldown <= 0.0f)
        {
           targetController.RequestRearrange(this);
        }
       
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (targetController && collision.gameObject.tag == "Bullet")
        {
            targetController.HandleTargetHit(this);
            m_rearrangeCooldown = m_rearrangeInterval;
        }

    }

    //private void OnTriggerEnter(Collider other)
    //{
    //    Debug.Log("BEMEGY");
    //    if (targetController && other.gameObject.tag == "Bullet")
    //    {
    //        targetController.HandleTargetHit(this);
    //        m_rearrangeCooldown = m_rearrangeInterval;
    //    }
    //}

    public void Rearrange(float width, float height, float practiceAreaLenght, float practiceAreaWidth, bool canFloat)
    {
        m_health = targetType is TargetType.Tank ? 100.0f : 40.0f;

        if(targetType is TargetType.Tank) canFloat = false;

        float newHeight = canFloat ? Random.Range(0.0f, 50.0f) : 2.6f;

        if (targetType is TargetType.Wall)
        {
           transform.localScale = new Vector3(width, height, 7);
           //newHeight = height / 2;
        }

        transform.localPosition = GetRandomGoToLocalPosition(canFloat);
        transform.localRotation = Quaternion.Euler(transform.localRotation.x, Random.Range(0.0f, 360.0f), transform.localRotation.z);

         _gridTag.position = new Vector3(transform.position.x, 3.0f, transform.position.z);
         _detected = false;

         m_rearrangeCooldown = m_rearrangeInterval;

    }

    private Vector3 GetRandomGoToLocalPosition(bool canFloat)
    {
        Vector3 localCandidatePos = new Vector3(Random.Range(-300, 300), canFloat ? Random.Range(0.0f, 50.0f) : 2.6f, Random.Range(-300, 300));
        for(int i = 0; i < 100; i++)
        {
            if(!Physics.CheckSphere(this.transform.parent.TransformPoint(localCandidatePos), 15.0f, ~LayerMask.GetMask("Ground")))
                break;
            localCandidatePos = new Vector3(Random.Range(-300, 300), canFloat ? Random.Range(0.0f, 50.0f) : 2.6f, Random.Range(-300, 300));
        }

        return localCandidatePos;
    }



    public void SetMaterial(Material mat = null)
    {
        mat = mat is null ? MainMaterial : mat;

        this.gameObject.GetComponent<MeshRenderer>().material = mat;


        foreach (var renderer in this.gameObject.GetComponentsInChildren<MeshRenderer>())
        {
            renderer.material = mat;
        }
        
    }

    public void Hit(int damage)
    {
        m_health = Mathf.Max(0, m_health-damage);
        targetController.HandleTargetHit(this);
        m_rearrangeCooldown = m_rearrangeInterval;
    }

    public void setDetectedState(bool isDetected)
    {
        _detected = isDetected;
    }

    public float GetHealth()
    {
        return m_health;
    }
}
