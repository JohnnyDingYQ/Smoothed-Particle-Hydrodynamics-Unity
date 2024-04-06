using System.Collections.Generic;
using Unity.Mathematics;

public class Particle
{
    public float Density { get; set; }
    public float Pressure { get; set; }
    public float Mass { get; set; }
    public float3 Position { get; set; }
    public float3 Velocity { get; set; }
    public Type Type { get; set; }
    public List<Particle> Neighbors {get; set; }
    public int X { get; set; }
    public int Y { get; set; }

    public Particle(float3 pos, Type type, int x, int y)
    {
        Position = pos;
        Type = type;
        X = x;
        Y = y;
        Neighbors = new();
    }
}

public enum Type { Fluid, Solid }