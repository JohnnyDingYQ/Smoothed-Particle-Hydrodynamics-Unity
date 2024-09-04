using Unity.Mathematics;
using Unity.Entities;

public struct ParticleComponent : IComponentData
{
    public float Density;
    public float Pressure;
    public float Mass;
    public float3 Velocity;
    public float3 Force;
}