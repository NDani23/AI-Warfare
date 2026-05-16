using UnityEngine;

public class IconController : MonoBehaviour
{

    [SerializeField] private float fixedHeight = 600.0f;
    [SerializeField] private UnityEngine.UI.Image agentHealthForeground;
    [SerializeField] private UnityEngine.UI.Text agentNameText;

    private VehicleManager vehicleManager;

    void Awake()
    {
        vehicleManager = GetComponentInParent<VehicleManager>();
    }

    void Update()
    {
        this.transform.position = new Vector3(transform.parent.transform.position.x, fixedHeight, transform.parent.transform.position.z);
        this.transform.rotation = Quaternion.Euler(90.0f, transform.parent.transform.rotation.eulerAngles.y, 0.0f);

        if(vehicleManager != null && agentHealthForeground != null && agentNameText != null)
        {
            agentHealthForeground.fillAmount = vehicleManager.Health / vehicleManager.MaxHealth;
            agentNameText.text = vehicleManager.AgentName;
        }
    }
}
