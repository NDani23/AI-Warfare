using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class TargetScript : MonoBehaviour
{
    [SerializeField] private Material MainMaterial;

    private TargetPracticeController targetController;
    public bool isWall = false;
    private bool isFake = false;

    private float m_rearrangeInterval = 60.0f;
    private float m_rearrangeCooldown = 60.0f;

    private bool m_detected = false;
    public bool Detected
    {
        get { return m_detected; }
        set { m_detected = value; }
    }

    private float m_detectedCoolDown = 0.0f;

    public void setController(TargetPracticeController controller, bool isFake)
    {
        this.targetController = controller;
        this.isFake = isFake;
    }

    private void FixedUpdate()
    {
        m_rearrangeCooldown -= Time.fixedDeltaTime;
        if(m_rearrangeCooldown <= 0.0f)
        {
            targetController.RequestRearrange(this);
            m_rearrangeCooldown = m_rearrangeInterval;
        }

        if(m_detected)
        {
            m_detectedCoolDown = Mathf.Max(0.0f, m_detectedCoolDown - Time.fixedDeltaTime);
            if (m_detectedCoolDown == 0.0f) m_detected = false;
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
        float newHeight = 2.6f;

        if (isWall)
        {
            transform.localScale = new Vector3(width, height, 7);
            newHeight = canFloat? Random.Range(height/2, 300.0f) : height / 2;
        }

        transform.localPosition = new Vector3(Random.Range(-practiceAreaWidth / 2, practiceAreaWidth / 2),
                                                newHeight,
                                                Random.Range(-practiceAreaLenght / 2, practiceAreaLenght / 2));
        transform.localRotation = Quaternion.Euler(transform.localRotation.x, Random.Range(0.0f, 360.0f), transform.localRotation.z);

    }

    public bool isFakeTarget()
    {
        return isFake;
    }

    public void SetMaterial(Material mat = null)
    {
        if(mat is null)
        {
            this.gameObject.GetComponent<MeshRenderer>().material = MainMaterial;
        }
        else
        {
            this.gameObject.GetComponent<MeshRenderer>().material = mat;
        }
    }

    public void Hit()
    {
        targetController.HandleTargetHit(this);
        m_rearrangeCooldown = m_rearrangeInterval;
    }

    public void Detect()
    {
        m_detected = true;
        m_detectedCoolDown = 15.0f;
    }
}
