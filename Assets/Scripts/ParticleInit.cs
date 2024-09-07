using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;

static class ParticleInit
{
    public static void Create(Entity particle, ConfigSingleton config, float3 pos, EntityCommandBuffer ecb)
    {
        ecb.SetComponent(particle, new LocalTransform()
        {
            Position = config.ParticleSeparation * pos,
            Scale = config.ParticleScale
        });
        float mass = math.pow(config.SmoothingLength, 3) * config.DesiredRestDensity;
        ecb.AddComponent(particle, new ParticleComponent()
        {
            Mass = mass,
            Density = mass / math.pow(config.ParticleSeparation, 2),

        });
        ecb.AddSharedComponent(particle, new GridData());
    }
}