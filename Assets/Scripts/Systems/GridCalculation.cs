using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(Setup))]
[UpdateBefore(typeof(SPHSystem))]
public partial struct GridCalculation : ISystem
{
    EntityCommandBuffer ecb;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ConfigSingleton>();
        state.RequireForUpdate<CameraSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        ecb = new EntityCommandBuffer(Allocator.TempJob);
        ConfigSingleton config = SystemAPI.GetSingleton<ConfigSingleton>();
        GridJob gridJob = new() { ECB = ecb.AsParallelWriter(), config = config };
        JobHandle jobHandle = gridJob.Schedule(state.Dependency);
        jobHandle.Complete();
        state.Dependency = jobHandle;

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        ecb.Dispose();
    }

    [BurstCompile]
    public partial struct GridJob : IJobEntity
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public ConfigSingleton config;

        void Execute(Entity entity, ref ParticleComponent particleComponent, ref LocalTransform transform)
        {
            var pos = transform.Position;
            ECB.SetSharedComponent(entity.Index, entity, new GridData()
            {
                x = (int)(pos.x / config.CellSize),
                z = (int)(pos.z / config.CellSize)
            });
        }
    }
}