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

    private List<IVehicleAgent> Enemies = new List<IVehicleAgent>();
    private List<IVehicleAgent> Friendlies = new List<IVehicleAgent>();



    private void Awake()
    {
        playEnv = GetComponentInParent<EnvController>();
    }

    private void Start()
    {
        foreach (var agent in playEnv.AgentsList)
        {
            if (agent.Team == team)
            {
                Friendlies.Add(agent);
            }
            else
            {
                Enemies.Add(agent);
            }
        }
    }

    private void OnPreRender()
    {
        foreach (var agent in Enemies)
        {
            if(agent.Health > 0)
                agent.SetMaterial(EnemyMaterial);
        }

        foreach (var agent in Friendlies)
        {
            if (agent.Health > 0)
                agent.SetMaterial(FriendlyMaterial);
        }
    }

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

    private void OnPostRender()
    {
        foreach (var agent in Enemies)
        {
            agent.SetMaterial();
        }

        foreach (var agent in Friendlies)
        {
            agent.SetMaterial();
        }
    }



}
