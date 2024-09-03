using Unity.Entities;
using UnityEngine;

public class ConfigAuthoring : MonoBehaviour
{
    public GameObject ParticlePrefab;
    public int NumRows;
    public int NumCols;
    public int ParticleSeparation;
    public float SmoothingLength;
    public float DesiredRestDensity;
    public float InitialDensity;
    public float KinematicViscosity;
    public float Gravity;
    public float TimeStep;
    public float CellSize;

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
                SmoothingLength = authoring.SmoothingLength,
                DesiredRestDensity = authoring.DesiredRestDensity,
                InitialDensity = authoring.InitialDensity,
                KinematicViscosity = authoring.KinematicViscosity,
                Gravity = authoring.Gravity,
                TimeStep = authoring.TimeStep,
                CellSize = authoring.CellSize,
            });
        }
    }
}

public struct ConfigSingleton : IComponentData
{
    public Entity ParticlePrefab;
    public int NumRows;
    public int NumCols;
    public int ParticleSeparation;
    public float SmoothingLength;
    public float DesiredRestDensity;
    public float InitialDensity;
    public float KinematicViscosity;
    public float Gravity;
    public float TimeStep;
    public float CellSize;

}
