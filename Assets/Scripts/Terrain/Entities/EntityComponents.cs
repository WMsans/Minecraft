using Unity.Entities;
using Unity.Mathematics;
using System;

// Component for movement.
public struct Velocity : IComponentData
{
    public float3 Value;
}

// Component for health.
public struct Health : IComponentData
{
    public float Value;
    public float MaxValue;
}

// A component to link an entity to the terrain chunk it belongs to.
// This is crucial for managing the entity's lifecycle.
public struct ChunkOwner : ISharedComponentData, IEquatable<ChunkOwner>
{
    public int NodeIndex;

    public bool Equals(ChunkOwner other)
    {
        return NodeIndex == other.NodeIndex;
    }

    public override int GetHashCode()
    {
        return NodeIndex;
    }
}

// A tag component to mark entities that should be fully simulated.
public struct HighDetail : IComponentData { }