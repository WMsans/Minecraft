using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using UnityEngine;

[BurstCompile]
public static class TerrainSettings
{
    public const int MIN_NODE_SIZE = 32; // Or any other power of 2
}
