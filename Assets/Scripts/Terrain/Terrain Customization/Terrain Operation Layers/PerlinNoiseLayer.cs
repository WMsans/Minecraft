using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public static unsafe class PerlinNoiseLayer
{
    // The Apply method now takes a pointer and length for the density map 
    [BurstCompile]
    public static void Apply(ref TerrainLayer layer, int seed, float* density, byte* voxelTypes, int densityLength, int chunkSize, in float3 offset, float scale)
    {
        if (!layer.enabled) return;

        for (int i = 0; i < densityLength; i++)
        {
            int x = i % (chunkSize + 1);
            int y = (i / (chunkSize + 1)) % (chunkSize + 1);
            int z = i / ((chunkSize + 1) * (chunkSize + 1));

            float worldX = offset.x + (x / (float)chunkSize - 0.5f) * scale;
            float worldY = offset.y + (y / (float)chunkSize - 0.5f) * scale;
            float worldZ = offset.z + (z / (float)chunkSize - 0.5f) * scale;

            // Calculate 2D Perlin noise for the heightmap using worldX and worldZ
            float noiseValue = BurstNoiseGenerator.Perlin(seed, worldX * layer.noiseScale, worldZ * layer.noiseScale) * layer.noiseStrength;
            
            // The density is the current world height minus the noise-generated terrain height
            density[i] = worldY - noiseValue;
        }
    }

    public static TerrainLayer Create()
    {
        return new TerrainLayer
        {
            ApplyFunction = BurstCompiler.CompileFunctionPointer<TerrainLayer.ApplyDelegate>(Apply),
            enabled = true,
            noiseScale = 0.05f,
            noiseStrength = 10f
        };
    }
}