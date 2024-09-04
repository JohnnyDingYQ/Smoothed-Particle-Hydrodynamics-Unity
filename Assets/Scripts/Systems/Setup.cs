using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateBefore(typeof(GridCalculation))]
public partial struct Setup : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ConfigSingleton>();
        state.RequireForUpdate<CameraSingleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;

        ConfigSingleton config = SystemAPI.GetSingleton<ConfigSingleton>();
        CameraSingleton camera = SystemAPI.ManagedAPI.GetSingleton<CameraSingleton>();

        camera.camera.transform.SetPositionAndRotation(
            new(config.ParticleSeparation * config.NumRows / 2, 40, config.ParticleSeparation * config.NumCols / 2),
            Quaternion.Euler(90, 0, 0)
        );
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        var particles = new NativeArray<Entity>(config.NumCols * config.NumRows, Allocator.Temp);
        ecb.Instantiate(config.ParticlePrefab, particles);

        int count = 0;
        foreach (var particle in particles)
        {
            ecb.SetComponent(particle, new LocalTransform()
            {
                Position = config.ParticleSeparation * new float3(count % config.NumCols, 0, count / config.NumRows),
                Scale = config.ParticleScale
            });
            float mass = math.pow(config.SmoothingLength, 3) * config.DesiredRestDensity;
            ecb.AddComponent(particle, new ParticleComponent()
            {
                Mass = mass,
                Density = mass / math.pow(config.ParticleSeparation, 2),

            });
            ecb.AddSharedComponent(particle, new GridData());
            count++;
        }

        particles.Dispose();

        ecb.Playback(state.EntityManager);
    }
}