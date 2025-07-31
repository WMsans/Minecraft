using Unity.Mathematics;
using System;

[Serializable]
public struct EntityData
{
    public EntityType.Type entityType;
    public float3 position;
    public float3 velocity;
    public float health;
}