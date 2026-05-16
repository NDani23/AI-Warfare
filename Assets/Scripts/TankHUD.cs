using Unity.Burst.CompilerServices;
using UnityEngine;

public class TankHUD : MonoBehaviour, IVehicleUI
{
    [SerializeField] private UnityEngine.UI.Image CooldownForeground;
    [SerializeField] private GameObject fixedCursor;
    [SerializeField] private UnityEngine.UI.Image AimPointerImage;
    [SerializeField] private UnityEngine.UI.Text HealthText;
    [SerializeField] private UnityEngine.UI.Image HealthForeground;
    [SerializeField] private float aimPointerSmoothTime = 0.08f;

    private TankManager _vehicle;

    public TankManager vehicle
    {
        get => _vehicle;
        set => _vehicle = value;
    }

    private Vector2 _aimPointerVelocity;

    public void LateUpdate()
    {
        if (_vehicle != null)
        {
            int playerHealth = (int)_vehicle.Health;
            HealthText.text = playerHealth.ToString() + "%";
            if (HealthForeground != null) HealthForeground.fillAmount = _vehicle.Health / _vehicle.MaxHealth;
            CooldownForeground.fillAmount = _vehicle.Health <= 0.0f ? 1.0f : _vehicle.gameObject.GetComponent<TankManager>().getCooldown() / 3.0f;

            bool showAimPointer = _vehicle.IsCannonFacingCameraForward();
            AimPointerImage.enabled = showAimPointer;
            if (showAimPointer)
            {
                Vector2 targetPos = _vehicle.GetScreenSpaceAimPos();
                Vector2 currentPos = AimPointerImage.rectTransform.position;
                Vector2 smoothedPos = Vector2.SmoothDamp(currentPos, targetPos, ref _aimPointerVelocity, aimPointerSmoothTime);
                AimPointerImage.rectTransform.position = smoothedPos;
            }
        }

    }

    public void switchToControlUI()
    {
        Cursor.lockState =  CursorLockMode.Locked;
        fixedCursor.SetActive(true);
    }

    public void switchOutControlUI()
    {
        fixedCursor.SetActive(false);
    }
}
