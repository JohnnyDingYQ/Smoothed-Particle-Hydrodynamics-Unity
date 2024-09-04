using Unity.Mathematics;
using Unity.Entities;

public struct ParticleComponent : IComponentData
{
    public float density;
    public float pressure;
    public float mass;
    public float3 velocity;
    public float3 force;
}