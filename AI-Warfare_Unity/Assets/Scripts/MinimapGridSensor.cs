using UnityEngine;
using Unity.MLAgents.Sensors;
using Unity.MLAgents;
using System;

public class MinimapGridSensor : GridSensorBase
{
    private GameObject agentGameObject;
    public MinimapGridSensor(string name, Vector3 cellScale, Vector3Int gridSize, string[] detectableTags, SensorCompressionType compression, GameObject agent) 
        : base(name, cellScale, gridSize, detectableTags, compression)
    {
        agentGameObject = agent;
    }

    protected override int GetCellObservationSize()
    {
        return DetectableTags == null ? 1 : DetectableTags.Length + 1;
    }

    protected override bool IsDataNormalized()
    {
        return true;
    }

    protected override ProcessCollidersMethod GetProcessCollidersMethod()
    {
         return ProcessCollidersMethod.ProcessAllColliders;
    }

    protected override void GetObjectData(GameObject detectedObject, int tagIndex, float[] dataBuffer)
    {
        if (tagIndex == 2 && detectedObject.transform.parent.gameObject == agentGameObject) return; // Ignore the agent itself
        if (tagIndex == 3 && detectedObject.transform.parent.gameObject != agentGameObject) return; // Only process the move to marker of the agent itself
        if (tagIndex == 1 && !detectedObject.transform.parent.GetComponent<ITargetable>().Detected) return; // Only process enemies that are detected

        dataBuffer[tagIndex] = 1;

        if(tagIndex == 1 || tagIndex == 2)
        {
            dataBuffer[DetectableTags.Length] = detectedObject.transform.parent.position.y / 600.0f;
        }

        //Debug.Log("[" + dataBuffer[0] + ", " + dataBuffer[1] + ", " + dataBuffer[2] + ", " + dataBuffer[3] + ", " + dataBuffer[4] + ", " + dataBuffer[5] + ", " + dataBuffer[6] + ", " + dataBuffer[7] + "]");
    }
}