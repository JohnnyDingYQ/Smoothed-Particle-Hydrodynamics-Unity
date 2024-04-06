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
        foreach (Particle p in Grid.Particles)
        {
            Color c = Color.blue;
            if (p.Type == Type.Solid)
                c = Color.yellow;
            DebugExtension.DebugPoint(p.Position, c, 4, 1000);
        }
    }
    void FixedUpdate()
    {
        // SPH.FindNeigbors();
    }
}