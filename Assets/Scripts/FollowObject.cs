using UnityEngine;

public class FollowObject : MonoBehaviour
{
    public float smoothing;
    public float turnSmoothing;

    public Transform player;
    void Start()
    {
        transform.position = new Vector3(100, 800, 0);
        transform.LookAt(Vector3.zero);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if(player != null)
        {
            // Smoothly follow the helicopter's position
            transform.position = Vector3.Lerp(transform.position, player.position, smoothing);

            // Get the helicopter's rotation as Euler angles
            Vector3 playerEuler = player.rotation.eulerAngles;

            // Create a target rotation with only yaw (Y) and pitch (X), setting roll (Z) to 0
            Quaternion targetRotation = Quaternion.Euler(playerEuler.x, playerEuler.y, 0f);

            // Smoothly interpolate the camera's rotation to the target rotation
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSmoothing);
        }
    }
}
