using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(Setup))]
public partial struct UiSystem : ISystem
{
    public static bool RespawnParticlesFlag;
    EntityQuery query;
    float timeElapsed;
    public void OnCreate(ref SystemState state)
    {
        query = new EntityQueryBuilder(Allocator.Persistent).WithAll<ParticleComponent, GridData, LocalTransform>().Build(ref state);
        state.RequireForUpdate<ConfigSingleton>();
        state.RequireForUpdate<CameraSingleton>();
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
        if (RespawnParticlesFlag)
        {
            RespawnParticlesFlag = false;
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            foreach (var (partial, entity) in SystemAPI.Query<ParticleComponent>().WithEntityAccess())
                ecb.DestroyEntity(entity);
            ecb.Playback(state.EntityManager);
            Setup.SpawnParticles(SystemAPI.GetSingleton<ConfigSingleton>());
        }
    }
}