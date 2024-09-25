using Unity.Entities;
using Unity.Mathematics;

public struct ClickData : IComponentData
{
    public float3 pos;
    public bool clicked;
}