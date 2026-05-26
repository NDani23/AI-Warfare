using UnityEngine;
using Unity.MLAgents.Sensors;

public class MinimapGridSensorComponent : GridSensorComponent
{
    protected override GridSensorBase[] GetGridSensors()
    {
        return new GridSensorBase[] { new MinimapGridSensor(m_SensorName + "-Minimap", CellScale, GridSize, DetectableTags, CompressionType, AgentGameObject) };
    }

}