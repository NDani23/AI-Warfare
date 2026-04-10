using Unity.Burst.CompilerServices;
using UnityEngine;

public class TankHUD : MonoBehaviour, IVehicleUI
{
    [SerializeField] private UnityEngine.UI.Image CooldownForeground;
    [SerializeField] private Texture2D aimCursor;
    [SerializeField] private UnityEngine.UI.Image AimPointerImage;
    [SerializeField] private UnityEngine.UI.Text HealthText;
    [SerializeField] private UnityEngine.UI.Image HealthForeground;

    private TankAgent _agent;

    public TankAgent Agent
    {
        get => _agent;
        set => _agent = value;
    }

    public void Update()
    {
        if (_agent != null)
        {
            int playerHealth = (int)_agent.Health;
            HealthText.text = playerHealth.ToString() + "%";
            if (HealthForeground != null) HealthForeground.fillAmount = _agent.Health / _agent.MaxHealth;
            CooldownForeground.fillAmount = _agent.Health <= 0.0f ? 1.0f : _agent.gameObject.GetComponent<TankAgent>().getCooldown() / 3.0f;
            AimPointerImage.transform.position = _agent.GetScreenSpaceAimPos();
        }

    }

    public void switchToControlUI()
    {
        Vector2 CursorHotspot = new Vector2(aimCursor.width / 2.0f, aimCursor.height / 2.0f);
        Cursor.SetCursor(aimCursor, CursorHotspot, CursorMode.Auto);
    }
}
