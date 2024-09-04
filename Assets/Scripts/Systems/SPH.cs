using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(GridCalculation))]
public partial struct SPHSystem : ISystem
{
    EntityQuery query;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        query = new EntityQueryBuilder(Allocator.Temp).WithAll<ParticleComponent, GridData>().Build(ref state);
        state.RequireForUpdate<ConfigSingleton>();
        state.RequireForUpdate<CameraSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        ConfigSingleton config = SystemAPI.GetSingleton<ConfigSingleton>();
        int maxX = (int)(config.NumRows * config.ParticleSeparation / config.SmoothingLength);
        int maxZ = (int)(config.NumCols * config.ParticleSeparation / config.SmoothingLength);

        for (int x = 0; x < maxX; x++)
            for (int z = 0; z < maxZ; z++)
            {
                query.ResetFilter();
                query.AddSharedComponentFilter(new GridData() { x = x, z = z });
                NativeArray<Entity> neighbors = query.ToEntityArray(Allocator.Temp);
                foreach (var particle in neighbors)
                {
                    ParticleComponent particleComponent = state.EntityManager.GetComponentData<ParticleComponent>(particle);
                    particleComponent.density = 0;
                    foreach (var other in neighbors)
                    {
                        if (particle.Equals(other))
                            continue;
                        particleComponent.density += state.EntityManager.GetComponentData<ParticleComponent>(other).mass * Kernel(
                            state.EntityManager.GetComponentData<LocalTransform>(particle).Position,
                            state.EntityManager.GetComponentData<LocalTransform>(other).Position,
                            config.SmoothingLength
                        );
                    }
                    particleComponent.pressure = config.Stiffness * (math.pow(particleComponent.density / config.DesiredRestDensity, 7) - 1);
                    state.EntityManager.SetComponentData(particle, particleComponent);

                }
                neighbors.Dispose();
            }
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        // query.Dispose();
    }

    [BurstCompile]
    float Kernel(float3 i, float3 j, float smoothingLength)
    {
        float q = math.sqrt(math.pow(i.x - j.x, 2) + math.pow(i.z - j.z, 2)) / smoothingLength;
        float fq = (float)3 / 2 / math.PI;
        if (q >= 2)
            fq = 0;
        else if (q < 2 && q >= 1)
            fq *= math.pow(2 - q, 3) / 6;
        else if (q < 1)
            fq *= (float)2 / 3 - math.pow(q, 2) + 0.5f * math.pow(q, 2);
        return fq / math.pow(smoothingLength, 3);
    }

    [BurstCompile]
    float3 KernelGradient(float3 i, float3 j, float smoothingLength)
    {
        float h = 0.1f;
        float h2 = h * 2;
        float3 gradient = new(0, 0, 0);
        float3 mask = new(1, 0, 0);
        gradient.x = (Kernel(i - h2 * mask, j, smoothingLength)
            - 8 * Kernel(i - h * mask, j, smoothingLength)
            + 8 * Kernel(i + h * mask, j, smoothingLength)
            - Kernel(i + h2 * mask, j, smoothingLength))
            / (h2 * 6); ;
        mask = new(0, 0, 1);
        gradient.z = (Kernel(i - h2 * mask, j, smoothingLength)
            - 8 * Kernel(i - h * mask, j, smoothingLength)
            + 8 * Kernel(i + h * mask, j, smoothingLength)
            - Kernel(i + h2 * mask, j, smoothingLength))
            / (h2 * 6);
        return gradient;
    }
}