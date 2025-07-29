using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

// A component to identify the type of entity.
public struct EntityType : IComponentData
{
    // You can use an enum for predefined types.
    public enum Type
    {
        Player,
        Enemy,
        Item
    }
    public Type Value;
}
