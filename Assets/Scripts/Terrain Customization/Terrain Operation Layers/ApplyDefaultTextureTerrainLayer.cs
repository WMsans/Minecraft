using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public unsafe struct ApplyDefaultTextureTerrainLayer : ITerrainLayer
{
    [BurstCompile]
    public static void Apply(ref TerrainLayer layer, int seed, float* density, byte* voxelTypes, int densityLength, int chunkSize, in float3 offset, float scale, void* entities, float* heightmap, int heightmapLength)
    {
        if (!layer.enabled) return;

        for (int i = 0; i < densityLength; i++)
        {
            voxelTypes[i] = 0;
        }
    }

    public static TerrainLayer Create(params float[] properties)
    {
        return Create();
    }

    public static TerrainLayer Create()
    {
        var layer = new TerrainLayer
        {
            ApplyFunction = BurstCompiler.CompileFunctionPointer<TerrainLayer.ApplyDelegate>(Apply),
            enabled = true,
        };

        return layer;
    }
    
    public static string[] Fields() => Array.Empty<string>();
    public static string[] InputPorts() => new[] { "In" };
    public static string[] OutputPorts() => new[] { "Out" };
}