using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public unsafe struct ApplySnowTerrainLayer : ITerrainLayer
{
    [BurstCompile]
    public static void Apply(ref TerrainLayer layer, int seed, float* density, byte* voxelTypes, int densityLength, int chunkSize, in float3 offset, float scale, void* entities)
    {
        if (!layer.enabled) return;

        // Get properties from the layer struct
        float snowLevel = layer.properties[0];
        float snowDepth = layer.properties[1];
        float noiseScale = layer.properties[2];
        float noiseStrength = layer.properties[3];

        for (int i = 0; i < densityLength; i++)
        {
            int x = i % (chunkSize + 1);
            int y = (i / (chunkSize + 1)) % (chunkSize + 1);
            int z = i / ((chunkSize + 1) * (chunkSize + 1));

            // Calculate world coordinates for the voxel
            float worldX = offset.x + (x / (float)chunkSize - 0.5f) * scale;
            float worldY = offset.y + (y / (float)chunkSize - 0.5f) * scale;
            float worldZ = offset.z + (z / (float)chunkSize - 0.5f) * scale;
            
            float noiseValue = BurstNoiseGenerator.Perlin(seed, worldX * noiseScale, worldZ * noiseScale) * noiseStrength;

            // Add the noise value to the base snow level
            float noisySnowLevel = snowLevel + noiseValue;
            
            // Check if the voxel is above the new, noisy snow line
            if (worldY > noisySnowLevel - snowDepth)
            {
                voxelTypes[i] = 3; // Set voxel type to snow
            }
        }
    }

    /// <summary>
    /// Creates a snow layer from an array of properties.
    /// </summary>
    public static TerrainLayer Create(params float[] properties)
    {
        // Default values
        float snowLevel = 30f;
        float snowDepth = 1f;
        float noiseScale = 0.01f;
        float noiseStrength = 5f;

        // Override defaults with provided properties
        if (properties != null)
        {
            if (properties.Length >= 1) snowLevel = properties[0];
            if (properties.Length >= 2) snowDepth = properties[1];
            if (properties.Length >= 3) noiseScale = properties[2];
            if (properties.Length >= 4) noiseStrength = properties[3];
        }

        return Create(snowLevel, snowDepth, noiseScale, noiseStrength);
    }

    /// <summary>
    /// Creates a snow layer with specified properties.
    /// </summary>
    public static TerrainLayer Create(float snowLevel = 30f, float snowDepth = 1f, float noiseScale = 0.01f, float noiseStrength = 5f)
    {
        var layer = new TerrainLayer
        {
            ApplyFunction = BurstCompiler.CompileFunctionPointer<TerrainLayer.ApplyDelegate>(Apply),
            enabled = true,
        };

        // Set the properties for this layer
        layer.properties[0] = snowLevel;
        layer.properties[1] = snowDepth;
        layer.properties[2] = noiseScale;
        layer.properties[3] = noiseStrength;

        return layer;
    }
    
    /// <summary>
    /// Defines the user-facing names for the layer's properties.
    /// </summary>
    public static string[] Fields() => new[] { "Snow Level", "Snow Depth", "Noise Scale", "Noise Strength" };
}