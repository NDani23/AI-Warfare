using NUnit;
using UnityEngine;
using UnityEngine.Events;

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

    [SerializeField] private UnityEngine.UI.Image RedState;
    [SerializeField] private UnityEngine.UI.Image YellowState;

    private EnvController m_envController;

    //public UnityEvent StateChangedEvent;

    private CTState m_state = CTState.Neutral;

    private void Start()
    {
        m_envController = GetComponentInParent<EnvController>();
    }

    private void Update()
    {
        if (m_envController.getStateNum() > 0)
        {
            YellowState.fillAmount = m_envController.getStateNum() / 10.0f;
            RedState.fillAmount = 0;
        }
        else if (m_envController.getStateNum() < 0)
        {
            RedState.fillAmount = Mathf.Abs(m_envController.getStateNum()) / 10.0f;
            YellowState.fillAmount = 0;
        }
        else
        {
            YellowState.fillAmount = 0;
            RedState.fillAmount = 0;
        }
    }

    private void OnTriggerEnter(Collider other)
    {

        if(other.tag == "YellowAgent")
        {
            if (other.transform.parent.GetComponent<VehicleManager>().InCT) return;
            m_envController.VehicleEnteredCT(Team.Yellow);
            other.transform.parent.GetComponent<VehicleManager>().InCT = true;
        }
        else if (other.tag == "RedAgent")
        {
            if (other.transform.parent.GetComponent<VehicleManager>().InCT) return;
            m_envController.VehicleEnteredCT(Team.Red);
            other.transform.parent.GetComponent<VehicleManager>().InCT = true;
        }
    }


    private void OnTriggerExit(Collider other)
    {

        if (other.tag == "YellowAgent")
        {
            if (!other.transform.parent.GetComponent<VehicleManager>().InCT) return;
            m_envController.VehicleExitedCT(Team.Yellow);
            other.transform.parent.GetComponent<VehicleManager>().InCT = false;
        }
        else if (other.tag == "RedAgent")
        {
            if (!other.transform.parent.GetComponent<VehicleManager>().InCT) return;
            m_envController.VehicleExitedCT(Team.Red);
            other.transform.parent.GetComponent<VehicleManager>().InCT = false;
        }
    }

    public void ChangeState(CTState state)
    {
        m_state = state;
        //StateChangedEvent.Invoke();

        if (m_state == CTState.Yellow)
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

    public CTState GetState()
    {
        return m_state;
    }
}
