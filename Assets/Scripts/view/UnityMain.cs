using System.Linq;
using UnityEngine;

public class UnityMain : MonoBehaviour
{
    void Awake()
    {
        Grid.InitParticles();
    }
    void Start()
    {
        InvokeRepeating("Draw", 0f, 0.1f);
    }

    void Draw()
    {
        foreach (Particle p in Grid.Particles)
        {

            DebugExtension.DebugPoint(p.Position, Color.cyan, 20, 0.1f);
        }
    }
    void Update()
    {
        SPH.Step();
    }
}