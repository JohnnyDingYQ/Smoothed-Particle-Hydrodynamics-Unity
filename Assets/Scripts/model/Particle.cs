using System.Collections.Generic;
using Unity.Mathematics;

public class Particle
{
    public float Density { get; set; }
    public float Pressure { get; set; }
    public float Mass { get; set; }
    public float3 Position { get; set; }
    public float3 Velocity { get; set; }
    public float3 Force { get; set; }
    public List<Particle> Neighbors { get; set; }
    public int X { get; set; }
    public int Y { get; set; }
    // public int InitialX { get; set; }
    // public int InitialY { get; set; }
    // public bool IsTagged { get; set; }

    public Particle(float3 pos, int x, int y)
    {
        Position = pos;
        X = x;
        Y = y;
        // InitialX = X;
        // InitialY = Y;
        Velocity = 0;
        Pressure = 0;
        Density = Parameters.InitialDensity;
        Mass = Parameters.Mass;
        Neighbors = new();
        // IsTagged = false;
    }
}