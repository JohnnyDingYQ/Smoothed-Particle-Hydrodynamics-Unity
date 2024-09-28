using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(SPHSystem))]
public partial struct ExternalForce : ISystem
{
    EntityQuery query;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        query = new EntityQueryBuilder(Allocator.Persistent).WithAll<ParticleComponent, GridData, LocalTransform>().Build(ref state);
        state.RequireForUpdate<ConfigSingleton>();
        state.RequireForUpdate<ClickData>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        ClickData clickData = SystemAPI.GetSingleton<ClickData>();
        if (clickData.clicked)
        {
            ConfigSingleton config = SystemAPI.GetSingleton<ConfigSingleton>();
            float3 pos = clickData.pos;
            GridData centerGrid = new() { x = (int)(pos.x / config.CellSize), z = (int)(pos.z / config.CellSize) };
            ApplyExternalForce(centerGrid, ref state);
        }
    }

    [BurstCompile]
    void ApplyExternalForce(GridData centerGrid, ref SystemState state)
    {
        ConfigSingleton config = SystemAPI.GetSingleton<ConfigSingleton>();

        float3 center = config.CellSize * new float3(centerGrid.x, 0, centerGrid.z);
        center += 0.5f * config.CellSize * new float3(1, 0, 1);

        for (int x = centerGrid.x - 1; x <= centerGrid.x + 1; x++)
            for (int z = centerGrid.z - 1; z <= centerGrid.z + 1; z++)
            {
                if (x < 0 && x > config.NumCols && z < 0 && z > config.NumRows)
                    continue;
                query.ResetFilter();
                query.AddSharedComponentFilter(new GridData() { x = x, z = z });
                NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);
                foreach (var entity in entities)
                {
                    LocalTransform localTransform = state.EntityManager.GetComponentData<LocalTransform>(entity);
                    ParticleComponent particleComponent = state.EntityManager.GetComponentData<ParticleComponent>(entity);

                    float distance = math.distance(localTransform.Position, center);
                    particleComponent.Velocity += 35 * (localTransform.Position - center) / (1 + math.pow(distance, 2));

                    state.EntityManager.SetComponentData(entity, particleComponent);
                }
                entities.Dispose();
            }

    }
}