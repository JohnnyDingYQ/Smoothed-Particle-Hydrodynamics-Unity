using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(Setup))]
public partial struct UiSystem : ISystem
{
    EntityQuery query;
    float timeElapsed;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        query = new EntityQueryBuilder(Allocator.Persistent).WithAll<ParticleComponent, GridData, LocalTransform>().Build(ref state);
        state.RequireForUpdate<ConfigSingleton>();
        state.RequireForUpdate<ActionFlags>();
    }

    public void OnUpdate(ref SystemState state)
    {
        UI.SetParticleCount(query.CalculateEntityCount());
        timeElapsed += SystemAPI.Time.DeltaTime;
        if (timeElapsed > 0.3f)
        {
            timeElapsed = 0;
            UI.SetSimsPerSecond((int)(1 / SystemAPI.Time.DeltaTime));
        }
        CheckResetParticle(ref state);
    }

    readonly void CheckResetParticle(ref SystemState state)
    {
        ActionFlags actionFlags = SystemAPI.GetSingleton<ActionFlags>();
        if (!actionFlags.RespawnParticles)
            return;
        actionFlags.RespawnParticles = false;
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        foreach (var (partial, entity) in SystemAPI.Query<ParticleComponent>().WithEntityAccess())
            ecb.DestroyEntity(entity);
        ecb.Playback(state.EntityManager);
        Setup.SpawnParticles(SystemAPI.GetSingleton<ConfigSingleton>());
        SystemAPI.SetSingleton(actionFlags);
    }
}