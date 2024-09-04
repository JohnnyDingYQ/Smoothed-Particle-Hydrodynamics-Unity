using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(Setup))]
[UpdateBefore(typeof(SPHSystem))]
public partial struct GridCalculation : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ConfigSingleton>();
        state.RequireForUpdate<CameraSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        ConfigSingleton config = SystemAPI.GetSingleton<ConfigSingleton>();

        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (transform, entity) in
            SystemAPI.Query<RefRW<LocalTransform>>()
            .WithAll<ParticleComponent>()
            .WithEntityAccess())
        {
            var pos = transform.ValueRO.Position;
            ecb.SetSharedComponent(entity, new GridData()
            {
                x = (int)(pos.x / config.CellSize),
                z = (int)(pos.z / config.CellSize)
            });
        }
        ecb.Playback(state.EntityManager);
    }
}