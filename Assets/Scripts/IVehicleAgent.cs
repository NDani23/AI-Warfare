using Unity.MLAgents;
using UnityEngine;

public enum Team
{
    Red = 0,
    Yellow = 1
}

public enum AgentType
{
    Tank = 0,
    Heli = 1
}

public interface IVehicleAgent
{
    float Health { get; }
    Team Team { get; }
    int MemberID { get; }
    AgentType AgentType { get; }
    EnvController EnvController { get; }
    GameObject gameObject { get; }

    void Hit(int damage);
    void ResetAgent();

    public Vector2 GetScreenSpaceAimPos();
}
