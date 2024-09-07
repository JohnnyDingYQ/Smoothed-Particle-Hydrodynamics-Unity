using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

[UpdateAfter(typeof(Setup))]
[UpdateBefore(typeof(GridCalculation))]
public partial struct ParticleSpawning : ISystem
{
    ConfigSingleton config;
    float frameElapsed;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

        state.RequireForUpdate<ConfigSingleton>();
        state.RequireForUpdate<CameraSingleton>();

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        config = SystemAPI.GetSingleton<ConfigSingleton>();
        if (!config.ContinuousSpawning)
            return;

        if (frameElapsed * config.TimeStep >= config.SpawnInterval)
        {
            frameElapsed = 0;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            for (int i = 0; i < config.SpawnWidth; i++)
            {
                Entity particle = ecb.Instantiate(config.ParticlePrefab);
                ParticleInit.Create(particle, config, new float3(1 + i * 0.5f, 0, config.NumRows - 1), ecb);
            }
            ecb.Playback(state.EntityManager);
            ecb.Dispose();
        }
        else
            frameElapsed++;
    }
}