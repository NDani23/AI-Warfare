using Unity.Burst.CompilerServices;
using UnityEngine;

public class TankHUD : MonoBehaviour, IVehicleUI
{
    [SerializeField] private UnityEngine.UI.Image CooldownForeground;
    [SerializeField] private Texture2D aimCursor;
    [SerializeField] private UnityEngine.UI.Image AimPointerImage;
    [SerializeField] private UnityEngine.UI.Text HealthText;
    [SerializeField] private UnityEngine.UI.Image HealthForeground;

    private TankManager _vehicle;

    public TankManager vehicle
    {
        get => _vehicle;
        set => _vehicle = value;
    }

    public void Update()
    {
        if (_vehicle != null)
        {
            int playerHealth = (int)_vehicle.Health;
            HealthText.text = playerHealth.ToString() + "%";
            if (HealthForeground != null) HealthForeground.fillAmount = _vehicle.Health / _vehicle.MaxHealth;
            CooldownForeground.fillAmount = _vehicle.Health <= 0.0f ? 1.0f : _vehicle.gameObject.GetComponent<TankManager>().getCooldown() / 3.0f;
            AimPointerImage.transform.position = _vehicle.GetScreenSpaceAimPos();
        }

    }

    public void switchToControlUI()
    {
        Vector2 CursorHotspot = new Vector2(aimCursor.width / 2.0f, aimCursor.height / 2.0f);
        Cursor.SetCursor(aimCursor, CursorHotspot, CursorMode.Auto);
    }
}
