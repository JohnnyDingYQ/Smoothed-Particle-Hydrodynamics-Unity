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
            Color c = Color.blue;
            if (p.Type == Type.Solid)
                c = Color.yellow;
            if (p.IsTagged)
                c = Color.green;
            DebugExtension.DebugPoint(p.Position, c, 20, 0.1f);
        }
    }
    void Update()
    {
        SPH.Step();
    }
}