using UnityEngine;

public class HeliHUD : MonoBehaviour, IVehicleUI
{
    [SerializeField] private UnityEngine.UI.Text HealthText;
    [SerializeField] private UnityEngine.UI.Image HealthForeground;
    [SerializeField] private UnityEngine.UI.Image CooldownForeground;

    private HeliManager _agent;

    public HeliManager Agent
    {
        get => _agent;
        set => _agent = value;
    }

    public void Update()
    {
        if (_agent != null)
        {
            int playerHealth = (int)_agent.Health;
            HealthText.text = ((int)(_agent.Health / _agent.MaxHealth * 100.0f)).ToString() + "%";
            if (HealthForeground != null) HealthForeground.fillAmount = _agent.Health / _agent.MaxHealth;
            CooldownForeground.fillAmount = _agent.getOverHeatStatus();
        }

    }

    public void switchToControlUI()
    {
        Cursor.lockState =  CursorLockMode.Locked;
    }

}
