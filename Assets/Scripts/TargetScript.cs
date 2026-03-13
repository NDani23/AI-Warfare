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

    public AgentType agentType;
    public AgentType AgentType
    {
        get { return agentType; }
        set { agentType = value; }
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

    private float m_rearrangeInterval = 180.0f;
    private float m_rearrangeCooldown = 180.0f;

    public void setController(TargetPracticeController controller, Team team)
    {
        this.targetController = controller;
        this.team = team;
        this.agentType = AgentType.Tank;
    }

    private void FixedUpdate()
    {
        //m_rearrangeCooldown -= Time.fixedDeltaTime;
        //if(m_rearrangeCooldown <= 0.0f)
        //{
        //    targetController.RequestRearrange(this);
        //    m_rearrangeCooldown = m_rearrangeInterval;
        //}
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
        m_health = agentType is AgentType.Tank ? 100.0f : 40.0f;

        if(agentType is AgentType.Tank) canFloat = false;

        float newHeight = canFloat ? Random.Range(height / 2, 300.0f) : 2.6f;

        //if (targetType is TargetType.Wall)
        //{
        //    transform.localScale = new Vector3(width, height, 7);
        //    //newHeight = height / 2;
        //}

        transform.localPosition = new Vector3(Random.Range(-practiceAreaWidth / 2, practiceAreaWidth / 2),
                                                newHeight,
                                                Random.Range(-practiceAreaLenght / 2, practiceAreaLenght / 2));
        transform.localRotation = Quaternion.Euler(transform.localRotation.x, Random.Range(0.0f, 360.0f), transform.localRotation.z);

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

    public float GetHealth()
    {
        return m_health;
    }
}
