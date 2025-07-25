using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

public abstract class TerrainLayer
{
    public bool enabled = true;
    public abstract void Apply(NativeArray<float> density, int chunkSize, float3 offset, float scale);
}
