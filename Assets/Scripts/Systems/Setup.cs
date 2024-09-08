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
    ConfigSingleton config;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ConfigSingleton>();
        state.RequireForUpdate<CameraSingleton>();
    }

    public void OnUpdate(ref SystemState state)
    {
        state.Enabled = false;

        config = SystemAPI.GetSingleton<ConfigSingleton>();

        CameraSingleton camera = SystemAPI.ManagedAPI.GetSingleton<CameraSingleton>();
        camera.camera.transform.SetPositionAndRotation(
            new(config.ParticleSeparation * config.NumRows / 2, 15, config.ParticleSeparation * config.NumCols / 2),
            Quaternion.Euler(90, 0, 0)
        );

        Entity actionFlagEntity = state.EntityManager.CreateEntity();
        state.EntityManager.AddComponentData(actionFlagEntity, new ActionFlags());

        // do not spawn stationary if continuous spawning
        if (config.ContinuousSpawning)
            return;

        SpawnParticles(config);
    }

    public static void SpawnParticles(ConfigSingleton config)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        var particles = new NativeArray<Entity>(config.NumCols * config.NumRows, Allocator.Temp);
        ecb.Instantiate(config.ParticlePrefab, particles);

        int count = 0;
        foreach (var particle in particles)
        {
            ParticleInit.Create(particle, config, new float3(count % config.NumCols, 0, count / config.NumRows), ecb);
            count++;
        }

        particles.Dispose();

        ecb.Playback(World.DefaultGameObjectInjectionWorld.EntityManager);
    }
}