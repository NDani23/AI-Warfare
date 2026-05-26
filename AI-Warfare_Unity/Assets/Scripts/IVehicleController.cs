using UnityEngine;

public interface IVehicleController
{
    public void setStartingState(int teamID, int memberID);
    public Vector2 GetScreenSpaceAimPos();
    public void setMaterial(Material mat = null);
    public void setDeadState();
}
