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
    public Int3 Coord { get; set; }

    public Particle(float3 pos, Type type, Int3 coord)
    {
        Position = pos;
        Type = type;
        Coord = coord;
        Neighbors = new();
    }
}

public enum Type { Fluid, Solid }