using Unity.Entities;

public struct ActionFlags : IComponentData
{
    public bool RespawnParticles;
    public bool ApplyForce;
}