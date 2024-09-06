using System;
using Unity.Entities;

public struct GridData : ISharedComponentData, IEquatable<GridData>
{
    public int x;
    public int z;

    public bool Equals(GridData other)
    {
        return x == other.x && z == other.z;
    }

    public override int GetHashCode()
    {
        return x * 1000 + z;
    }
}