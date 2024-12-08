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
            transform.position = Vector3.Lerp(transform.position, player.position, smoothing);
            transform.rotation = Quaternion.Slerp(transform.rotation, player.rotation, turnSmoothing);
        }
    }
}
