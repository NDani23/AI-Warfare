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

    bool InCT { get; set; }

    void Hit(int damage);
    void ResetAgent();

    public void SetMaterial(Material mat = null);
    public Vector2 GetScreenSpaceAimPos();
}
