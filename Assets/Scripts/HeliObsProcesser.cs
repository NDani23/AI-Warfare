using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public class HeliObsProcesser : MonoBehaviour
{
    public Team team;
    public Material EnemyMaterial;
    public Material FriendlyMaterial;

    public Material grayscaleMaterial;

    private Material defaultMaterial;
    private EnvController playEnv;
    private TargetPracticeController _targetPracticeController;

    private List<VehicleManager> Enemies = new List<VehicleManager>();
    private List<VehicleManager> Friendlies = new List<VehicleManager>();


    void OnRenderImage(RenderTexture source, RenderTexture destination)
    {
        if (grayscaleMaterial != null)
        {
            Graphics.Blit(source, destination, grayscaleMaterial);
        }
        else
        {
            Graphics.Blit(source, destination); // Fallback if material is missing
        }
    }
}
