using UnityEngine;

public class agentInfoScript : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Image HealthBar;
    private Camera m_Camera;
    private TankAgent m_Agent;
    void Start()
    {
        m_Camera = Camera.main;
        m_Agent = GetComponentInParent<TankAgent>();
    }
    void Update()
    {
        //transform.rotation = Quaternion.LookRotation(transform.position - m_Camera.transform.position);
        if (HealthBar != null) HealthBar.fillAmount = m_Agent.getHealth() / 100;
    }
}
