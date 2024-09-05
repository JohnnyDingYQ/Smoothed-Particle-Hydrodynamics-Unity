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
        {
            for (int z = 0; z < maxZ; z++)
            {
                query.ResetFilter();
                query.AddSharedComponentFilter(new GridData() { x = x, z = z });
                NativeArray<Entity> neighbors = query.ToEntityArray(Allocator.Temp);

                foreach (var particle in neighbors)
                {
                    ParticleComponent particleComponent = state.EntityManager.GetComponentData<ParticleComponent>(particle);
                    particleComponent.Density = 0;

                    float3 particlePosition = state.EntityManager.GetComponentData<LocalTransform>(particle).Position;

                    foreach (var other in neighbors)
                    {
                        float3 otherPosition = state.EntityManager.GetComponentData<LocalTransform>(other).Position;
                        ParticleComponent otherComponent = state.EntityManager.GetComponentData<ParticleComponent>(other);

                        particleComponent.Density += otherComponent.Mass * Kernel(
                            particlePosition,
                            otherPosition,
                            config.SmoothingLength
                        );
                    }

                    particleComponent.Pressure = config.Stiffness * (math.pow(particleComponent.Density / config.DesiredRestDensity, 7) - 1);

                    state.EntityManager.SetComponentData(particle, particleComponent);
                }

                neighbors.Dispose();
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
                    ParticleComponent i = state.EntityManager.GetComponentData<ParticleComponent>(particle);
                    float3 sumPressure = float3.zero;
                    float3 sumViscosity = float3.zero;

                    foreach (var other in neighbors)
                    {
                        if (particle.Equals(other))
                            continue;

                        ParticleComponent j = state.EntityManager.GetComponentData<ParticleComponent>(other);
                        float3 iPos = state.EntityManager.GetComponentData<LocalTransform>(particle).Position;
                        float3 jPos = state.EntityManager.GetComponentData<LocalTransform>(other).Position;

                        sumPressure += j.Mass * (i.Pressure / math.pow(i.Density, 2) + j.Pressure / math.pow(j.Density, 2))
                                       * KernelGradient(iPos, jPos, config.SmoothingLength);

                        float3 velocityDifference = i.Velocity - j.Velocity;

                        float3 xij = iPos - jPos;
                        float distanceSquared = math.dot(xij, xij) + 0.01f * math.pow(config.SmoothingLength, 2);

                        sumViscosity += j.Mass * (velocityDifference / j.Density)
                                        * KernelGradient(iPos, jPos, config.SmoothingLength)
                                        / distanceSquared;
                    }

                    float3 forcePressure = -i.Mass * sumPressure;
                    float3 forceViscosity = i.Mass * config.KinematicViscosity * sumViscosity;
                    float3 forceGravity = i.Mass * config.Gravity * new float3(0, 0, -1);

                    i.Force = forcePressure + forceViscosity + forceGravity;
                    state.EntityManager.SetComponentData(particle, i);
                }

                neighbors.Dispose();
            }

        for (int x = 0; x < maxX; x++)
        {
            for (int z = 0; z < maxZ; z++)
            {
                query.ResetFilter();
                query.AddSharedComponentFilter(new GridData() { x = x, z = z });
                NativeArray<Entity> neighbors = query.ToEntityArray(Allocator.Temp);

                foreach (var particle in neighbors)
                {
                    float deltaTime = 0.0027f;
                    ParticleComponent i = state.EntityManager.GetComponentData<ParticleComponent>(particle);
                    i.Velocity += deltaTime * i.Force / i.Mass;
                    LocalTransform localTransform = state.EntityManager.GetComponentData<LocalTransform>(particle);

                    float3 nextPos = localTransform.Position + deltaTime * i.Velocity;
                    float dampingFactor = 0.5f;

                    if (nextPos.x < 0)
                    {
                        nextPos.x = 0; 
                        i.Velocity.x *= -dampingFactor;
                        nextPos = localTransform.Position + deltaTime * i.Velocity;
                    }
                    else if (nextPos.x >= config.NumRows * config.ParticleSeparation)
                    {
                        nextPos.x = config.NumRows * config.ParticleSeparation - float.Epsilon;
                        i.Velocity.x *= -dampingFactor;
                        nextPos = localTransform.Position + deltaTime * i.Velocity;
                    }

                    if (nextPos.z < 0)
                    {
                        nextPos.z = 0; 
                        i.Velocity.z *= -dampingFactor;
                        nextPos = localTransform.Position + deltaTime * i.Velocity;
                    }
                    else if (nextPos.z >= config.NumCols * config.ParticleSeparation)
                    {
                        nextPos.z = config.NumCols * config.ParticleSeparation - float.Epsilon;
                        i.Velocity.z *= -dampingFactor;
                        nextPos = localTransform.Position + deltaTime * i.Velocity;
                    }

                    localTransform.Position = nextPos;
                    state.EntityManager.SetComponentData(particle, i);
                    state.EntityManager.SetComponentData(particle, localTransform);

                    OutsideAssertIf.NotNaN(localTransform.Position);
                }

                neighbors.Dispose();
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
            fq *= (float)2 / 3 - math.pow(q, 2) + 0.5f * math.pow(q, 3);
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