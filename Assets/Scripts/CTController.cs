using UnityEngine;

public enum CTState
{
    Red,
    Yellow,
    Neutral
}

public class CTController : MonoBehaviour
{

    [SerializeField] private GameObject FlagObject;
    [SerializeField] private Material NeutralMaterial;
    [SerializeField] private Material RedMaterial;
    [SerializeField] private Material YellowMaterial;

    private EnvController m_envController;

    private CTState m_state = CTState.Neutral;

    private void Start()
    {
        m_envController = GetComponentInParent<EnvController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if(other.tag == "YellowAgent")
        {
            if (other.transform.parent.GetComponent<TankAgent>().inCT) return;
            m_envController.AgentEnteredCT(Team.Yellow);
            other.transform.parent.GetComponent<TankAgent>().inCT = true;
        }
        else if (other.tag == "RedAgent")
        {
            if (other.transform.parent.GetComponent<TankAgent>().inCT) return;
            m_envController.AgentEnteredCT(Team.Red);
            other.transform.parent.GetComponent<TankAgent>().inCT = true;
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "YellowAgent")
        {
            if (!other.transform.parent.GetComponent<TankAgent>().inCT) return;
            m_envController.AgentExitedCT(Team.Yellow);
            other.transform.parent.GetComponent<TankAgent>().inCT = false;
        }
        else if (other.tag == "RedAgent")
        {
            if (!other.transform.parent.GetComponent<TankAgent>().inCT) return;
            m_envController.AgentExitedCT(Team.Red);
            other.transform.parent.GetComponent<TankAgent>().inCT = false;
        }
    }

    public void ChangeState(CTState state)
    {
        m_state = state;

        if(m_state == CTState.Yellow)
        {
            FlagObject.GetComponent<Renderer>().material = YellowMaterial;
        }
        else if(m_state == CTState.Red)
        {
            FlagObject.GetComponent<Renderer>().material = RedMaterial;
        }
        else
        {
            FlagObject.GetComponent<Renderer>().material = NeutralMaterial;
        }
    }
}
