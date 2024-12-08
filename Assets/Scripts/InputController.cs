using UnityEngine;

public class InputController : MonoBehaviour
{
    public string inputSteerAxis = "Horizontal";
    public string inputThrottleAxis = "Vertical";
    public string aimXAxis = "Mouse X";
    public string aimYAxis = "Mouse Y";

    public int ThrottleInput { get; private set; }
    public int SteerInput { get; private set; }

    public float HorizontalAimInput { get; private set; }
    public float VerticalAimInput { get; private set; }

    public bool FireInput { get; set; }

    public float BreakInput { get; private set; }

    public Vector3 AimDirInput { get; private set; }

    void Start()
    {

    }

    void Update()
    {
        var aimDirection = Input.mousePosition;
        aimDirection.z = 500.0f;
        AimDirInput = Camera.main.ScreenToWorldPoint(aimDirection);

        if (FireInput ==  false)
        {
            FireInput = Input.GetMouseButtonDown(0) || Input.GetKeyDown("space");
        }
        ThrottleInput = 0;
        if (Input.GetKey(KeyCode.S))
        {
            ThrottleInput += -1;
        }

        if(Input.GetKey(KeyCode.W))
        {
            ThrottleInput += 1;
        }

        SteerInput = 0;
        if(Input.GetKey(KeyCode.D))
        {
            SteerInput += 1;
        }

        if (Input.GetKey(KeyCode.A))
        {
            SteerInput += -1;
        }
    }
}
