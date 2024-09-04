using System;
using NUnit.Framework;
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
        int maxX = (int)(config.NumRows * config.ParticleSeparation / config.CellSize) + 1;
        int maxZ = (int)(config.NumCols * config.ParticleSeparation / config.CellSize) + 1;

        for (int x = 0; x < maxX; x++)
            for (int z = 0; z < maxZ; z++)
            {
                query.ResetFilter();
                query.AddSharedComponentFilter(new GridData() { x = x, z = z });
                NativeArray<Entity> neighbors = query.ToEntityArray(Allocator.Temp);
                foreach (var particle in neighbors)
                {
                    ParticleComponent particleComponent = state.EntityManager.GetComponentData<ParticleComponent>(particle);
                    particleComponent.Density = 0;
                    foreach (var other in neighbors)
                    {
                        if (particle.Equals(other))
                            continue;
                        particleComponent.Density += state.EntityManager.GetComponentData<ParticleComponent>(other).Mass * Kernel(
                            state.EntityManager.GetComponentData<LocalTransform>(particle).Position,
                            state.EntityManager.GetComponentData<LocalTransform>(other).Position,
                            config.SmoothingLength
                        );
                    }
                    particleComponent.Pressure = config.Stiffness * (math.pow(particleComponent.Density / config.DesiredRestDensity, 7) - 1);
                    state.EntityManager.SetComponentData(particle, particleComponent);

                }
                neighbors.Dispose();
            }


        for (int x = 0; x < maxX; x++)
            for (int z = 0; z < maxZ; z++)
            {
                query.ResetFilter();
                query.AddSharedComponentFilter(new GridData() { x = x, z = z });
                NativeArray<Entity> neighbors = query.ToEntityArray(Allocator.Temp);

                foreach (var particle in neighbors)
                {
                    ParticleComponent i = state.EntityManager.GetComponentData<ParticleComponent>(particle);
                    float3 sumPressure = 0;
                    float3 sumVisocity = 0;
                    foreach (var other in neighbors)
                    {
                        if (particle.Equals(other))
                            continue;
                        ParticleComponent j = state.EntityManager.GetComponentData<ParticleComponent>(particle);
                        float3 iPos = state.EntityManager.GetComponentData<LocalTransform>(particle).Position;
                        float3 jPos = state.EntityManager.GetComponentData<LocalTransform>(other).Position;

                        // OutsideAssertIf.IsTrue(math.pow(i.Density, 2) != 0);
                        // OutsideAssertIf.IsTrue(math.pow(j.Density, 2) != 0);
                        // OutsideAssertIf.NotNaN(j.Mass * (i.Pressure / math.pow(i.Density, 2) + j.Pressure / math.pow(j.Density, 2))
                        //     * KernelGradient(
                        //         iPos,
                        //         jPos,
                        //         config.SmoothingLength
                        //     ));

                        sumPressure += j.Mass * (i.Pressure / math.pow(i.Density, 2) + j.Pressure / math.pow(j.Density, 2))
                            * KernelGradient(iPos, jPos, config.SmoothingLength);

                        float3 xij = iPos -jPos;
                        sumVisocity += j.Mass / j.Density * (i.Velocity - j.Velocity) * xij
                            * KernelGradient(iPos, jPos, config.SmoothingLength)
                            / (math.dot(xij, xij) + 0.01f * math.pow(config.SmoothingLength, 2));
                    }
                    float3 forcePressure = -i.Mass * sumPressure;


                    float3 doubleGradientVelocity = 2 * sumPressure;
                    float3 forceVisocity = i.Mass * config.KinematicViscosity * doubleGradientVelocity;

                    float3 forceGravity = i.Mass * 9.8f * new float3(0, 0, -1);

                    i.Force = forcePressure + forceVisocity + forceGravity;
                    // OutsideAssertIf.IsTrue(!float.IsNaN(forcePressure.x));
                    // OutsideAssertIf.IsTrue(!float.IsNaN(forcePressure.z));

                    state.EntityManager.SetComponentData(particle, i);
                }
            }

        for (int x = 0; x < maxX; x++)
            for (int z = 0; z < maxZ; z++)
            {
                query.ResetFilter();
                query.AddSharedComponentFilter(new GridData() { x = x, z = z });
                NativeArray<Entity> neighbors = query.ToEntityArray(Allocator.Temp);

                foreach (var particle in neighbors)
                {
                    // float deltaTime = SystemAPI.Time.DeltaTime;
                    float deltaTime = 0.01f;
                    ParticleComponent i = state.EntityManager.GetComponentData<ParticleComponent>(particle);
                    i.Velocity += deltaTime * i.Force / i.Mass;
                    LocalTransform localTransform = state.EntityManager.GetComponentData<LocalTransform>(particle);

                    float3 nextPos = localTransform.Position + deltaTime * i.Velocity;
                    float gridX = (int)(nextPos.x / config.CellSize);
                    float gridZ = (int)(nextPos.z / config.CellSize);

                    if (gridX < 0 || gridZ < 0 || gridX >= maxX || gridZ >= maxX)
                    {

                    }
                    else
                    {
                        localTransform.Position = nextPos;
                    }
                    
                    state.EntityManager.SetComponentData(particle, i);
                    state.EntityManager.SetComponentData(particle, localTransform);

                    OutsideAssertIf.NotNaN(localTransform.Position);
                }
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
        return fq / math.pow(smoothingLength, 2);
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