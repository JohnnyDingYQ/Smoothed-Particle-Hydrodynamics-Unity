using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using UnityEngine;

[UpdateAfter(typeof(GridCalculation))]
public partial struct SPHSystem : ISystem
{
    EntityQuery query;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        query = new EntityQueryBuilder(Allocator.Persistent).WithAll<ParticleComponent, GridData, LocalTransform>().Build(ref state);
        state.RequireForUpdate<ConfigSingleton>();
        state.RequireForUpdate<CameraSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        ConfigSingleton config = SystemAPI.GetSingleton<ConfigSingleton>();
        int maxX = (int)(config.NumRows * config.ParticleSeparation / config.CellSize) + 1;
        int maxZ = (int)(config.NumCols * config.ParticleSeparation / config.CellSize) + 1;
        NativeList<JobHandle> allJobs = new(Allocator.Temp);
        NativeHashMap<GridData, NativeArray<ParticleComponent>> particlesMap = new(maxX * maxZ, Allocator.Temp);
        NativeHashMap<GridData, NativeArray<LocalTransform>> transformsMap = new(maxX * maxZ, Allocator.Temp);

        for (int x = 0; x < maxX; x++)
        {
            for (int z = 0; z < maxZ; z++)
            {
                query.ResetFilter();
                GridData grid = new() { x = x, z = z };
                query.AddSharedComponentFilter(grid);

                NativeArray<ParticleComponent> particleComponents = query.ToComponentDataArray<ParticleComponent>(Allocator.TempJob);
                NativeArray<ParticleComponent> updatedComponents = query.ToComponentDataArray<ParticleComponent>(Allocator.TempJob);
                NativeArray<LocalTransform> particleTransforms = query.ToComponentDataArray<LocalTransform>(Allocator.TempJob);

                var densityJob = new CalculateDensityJob
                {
                    particleTransforms = particleTransforms,
                    particleComponents = particleComponents,
                    config = config,
                    updatedParticleComponents = updatedComponents
                };
                JobHandle densityJobHandle = densityJob.Schedule(query.CalculateEntityCount(), state.Dependency);

                var forcesJob = new CalculateForcesJob
                {
                    particleTransforms = densityJob.particleTransforms,
                    particleComponents = densityJob.updatedParticleComponents,
                    config = config,
                    updatedParticleComponents = densityJob.particleComponents
                };
                JobHandle forcesJobHandle = forcesJob.Schedule(query.CalculateEntityCount(), densityJobHandle);

                var updateJob = new UpdatePositionJob
                {
                    particleComponents = forcesJob.updatedParticleComponents,
                    particleTransforms = particleTransforms,
                    config = config
                };
                JobHandle updateJobHandle = updateJob.Schedule(query.CalculateEntityCount(), forcesJobHandle);

                allJobs.Add(densityJobHandle);
                allJobs.Add(forcesJobHandle);
                allJobs.Add(updateJobHandle);

                particlesMap.Add(grid, particleComponents);
                transformsMap.Add(grid, particleTransforms);
            }
        }
        JobHandle.CompleteAll(allJobs.AsArray());
        for (int x = 0; x < maxX; x++)
        {
            for (int z = 0; z < maxZ; z++)
            {
                query.ResetFilter();
                GridData grid = new() { x = x, z = z };
                query.AddSharedComponentFilter(grid);

                NativeArray<ParticleComponent> particleComponents = particlesMap[grid];
                NativeArray<LocalTransform> particleTransforms = transformsMap[grid];
                NativeArray<Entity> entities = query.ToEntityArray(Allocator.Temp);

                for (int index = 0; index < entities.Length; index++)
                {
                    state.EntityManager.SetComponentData(entities[index], particleComponents[index]);
                    state.EntityManager.SetComponentData(entities[index], particleTransforms[index]);
                }
                particleComponents.Dispose();
                particleTransforms.Dispose();
            }
        }
        particlesMap.Dispose();
        transformsMap.Dispose();

    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        // query.Dispose();
    }


    [BurstCompile]
    public struct CalculateDensityJob : IJobFor
    {
        [ReadOnly] public NativeArray<LocalTransform> particleTransforms;
        [ReadOnly] public NativeArray<ParticleComponent> particleComponents;
        [ReadOnly] public ConfigSingleton config;
        public NativeArray<ParticleComponent> updatedParticleComponents;

        public void Execute(int index)
        {
            ParticleComponent i = particleComponents[index];
            i.Density = 0;

            float3 iPos = particleTransforms[index].Position;

            for (int otherIndex = 0; otherIndex < particleComponents.Length; otherIndex++)
            {
                float3 jPos = particleTransforms[otherIndex].Position;
                ParticleComponent j = particleComponents[otherIndex];

                i.Density += j.Mass * Kernel(
                    iPos,
                    jPos,
                    config.SmoothingLength
                );
            }

            i.Pressure = config.Stiffness * (math.pow(i.Density / config.DesiredRestDensity, 3) - 1);

            updatedParticleComponents[index] = i;
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
    }

    [BurstCompile]
    public struct CalculateForcesJob : IJobFor
    {
        [ReadOnly] public NativeArray<LocalTransform> particleTransforms;
        [DeallocateOnJobCompletion][ReadOnly] public NativeArray<ParticleComponent> particleComponents;
        [ReadOnly] public ConfigSingleton config;
        public NativeArray<ParticleComponent> updatedParticleComponents;

        public void Execute(int index)
        {
            ParticleComponent i = particleComponents[index];
            float3 iPos = particleTransforms[index].Position;
            float3 sumPressure = 0;
            float3 sumViscosity = 0;

            for (int otherIndex = 0; otherIndex < particleComponents.Length; otherIndex++)
            {
                if (otherIndex == index)
                    continue;

                ParticleComponent j = particleComponents[otherIndex];
                float3 jPos = particleTransforms[otherIndex].Position;

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
            updatedParticleComponents[index] = i;
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

    [BurstCompile]
    public struct UpdatePositionJob : IJobFor
    {
        public NativeArray<ParticleComponent> particleComponents;
        public NativeArray<LocalTransform> particleTransforms;
        [ReadOnly] public ConfigSingleton config;

        public void Execute(int index)
        {
            float deltaTime = config.TimeStep;
            ParticleComponent i = particleComponents[index];
            i.Velocity += deltaTime * i.Force / i.Mass;
            LocalTransform transform = particleTransforms[index];

            float3 nextPos = transform.Position + deltaTime * i.Velocity;
            float dampingFactor = config.DampingFactor;

            if (nextPos.x < 0)
            {
                nextPos.x = 0;
                i.Velocity.x *= -dampingFactor;
                nextPos = transform.Position + deltaTime * i.Velocity;
            }
            else if (nextPos.x >= config.NumRows * config.ParticleSeparation)
            {
                nextPos.x = config.NumRows * config.ParticleSeparation - float.Epsilon;
                i.Velocity.x *= -dampingFactor;
                nextPos = transform.Position + deltaTime * i.Velocity;
            }

            if (nextPos.z < 0)
            {
                nextPos.z = 0;
                i.Velocity.z *= -dampingFactor;
                nextPos = transform.Position + deltaTime * i.Velocity;
            }
            else if (nextPos.z >= config.NumCols * config.ParticleSeparation)
            {
                nextPos.z = config.NumCols * config.ParticleSeparation - float.Epsilon;
                i.Velocity.z *= -dampingFactor;
                nextPos = transform.Position + deltaTime * i.Velocity;
            }

            transform.Position = nextPos;
            particleComponents[index] = i;
            particleTransforms[index] = transform;

            OutsideAssertIf.NotNaN(transform.Position);
        }
    }
}