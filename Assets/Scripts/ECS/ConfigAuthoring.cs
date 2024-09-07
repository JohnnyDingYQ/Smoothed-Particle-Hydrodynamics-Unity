using Unity.Entities;
using UnityEngine;

public class ConfigAuthoring : MonoBehaviour
{
    public GameObject ParticlePrefab;
    public int NumRows;
    public int NumCols;
    public float ParticleSeparation;
    public float ParticleScale;
    public float SmoothingLength;
    public float DesiredRestDensity;
    public float KinematicViscosity;
    public float Stiffness;
    public float Gravity;
    public float TimeStep;
    public float DampingFactor;
    public float CellSize;
    public bool ContinuousSpawning;
    public float SpawnInterval;
    public float SpawnWidth;

    class Baker : Baker<ConfigAuthoring>
    {
        public override void Bake(ConfigAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new ConfigSingleton
            {
                ParticlePrefab = GetEntity(authoring.ParticlePrefab, TransformUsageFlags.Dynamic),
                NumRows = authoring.NumRows,
                NumCols = authoring.NumCols,
                ParticleSeparation = authoring.ParticleSeparation,
                ParticleScale = authoring.ParticleScale,
                SmoothingLength = authoring.SmoothingLength,
                DesiredRestDensity = authoring.DesiredRestDensity,
                KinematicViscosity = authoring.KinematicViscosity,
                Gravity = authoring.Gravity,
                TimeStep = authoring.TimeStep,
                CellSize = authoring.CellSize,
                Stiffness = authoring.Stiffness,
                DampingFactor = authoring.DampingFactor,
                ContinuousSpawning = authoring.ContinuousSpawning,
                SpawnInterval = authoring.SpawnInterval,
                SpawnWidth = authoring.SpawnWidth
            });
        }
    }
}

public struct ConfigSingleton : IComponentData
{
    public Entity ParticlePrefab;
    public int NumRows;
    public int NumCols;
    public float ParticleSeparation;
    public float ParticleScale;
    public float SmoothingLength;
    public float DesiredRestDensity;
    public float KinematicViscosity;
    public float Gravity;
    public float TimeStep;
    public float CellSize;
    public float Stiffness;
    public float DampingFactor;
    public bool ContinuousSpawning;
    public float SpawnInterval;
    public float SpawnWidth;
}
