using UnityEngine;

public class HeliVisualController : MonoBehaviour
{
    public Transform agent;
    void Update()
    {
        Vector3 agentEuler = agent.rotation.eulerAngles;

        // Create a target rotation with only yaw (Y) and pitch (X), setting roll (Z) to 0
        this.transform.rotation = Quaternion.Euler(agentEuler.x, agentEuler.y, 0f);
    }
}
