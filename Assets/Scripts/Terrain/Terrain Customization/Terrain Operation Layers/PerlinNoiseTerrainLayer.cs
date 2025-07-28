using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public unsafe struct PerlinNoiseTerrainLayer : ITerrainLayer
{
    [BurstCompile]
    public static void Apply(ref TerrainLayer layer, int seed, float* density, byte* voxelTypes, int densityLength, int chunkSize, in float3 offset, float scale)
    {
        if (!layer.enabled) return;

        // Get properties from the layer struct
        float noiseScale = layer.properties[0];
        float noiseStrength = layer.properties[1];

        for (int i = 0; i < densityLength; i++)
        {
            int x = i % (chunkSize + 1);
            int y = (i / (chunkSize + 1)) % (chunkSize + 1);
            int z = i / ((chunkSize + 1) * (chunkSize + 1));

            float worldX = offset.x + (x / (float)chunkSize - 0.5f) * scale;
            float worldY = offset.y + (y / (float)chunkSize - 0.5f) * scale;
            float worldZ = offset.z + (z / (float)chunkSize - 0.5f) * scale;

            float noiseValue = BurstNoiseGenerator.Perlin(seed, worldX * noiseScale, worldZ * noiseScale) * noiseStrength;

            density[i] = worldY - noiseValue;
        }
    }

    public static TerrainLayer Create(params float[] properties)
    {
        float noiseScale = 0.05f;
        float noiseStrength = 10f;
        if (properties != null && properties.Length >= 2)
        {
            noiseScale = properties[0];
            noiseStrength = properties[1];
        }

        return Create(noiseScale, noiseStrength);
    }

    public static TerrainLayer Create(float noiseScale = 0.05f, float noiseStrength = 10f)
    {
        var layer = new TerrainLayer
        {
            ApplyFunction = BurstCompiler.CompileFunctionPointer<TerrainLayer.ApplyDelegate>(Apply),
            enabled = true,
        };

        // Set the properties for this layer
        layer.properties[0] = noiseScale;
        layer.properties[1] = noiseStrength;

        return layer;
    }

    public static string[] Fields() => new[] { "Noise Scale", "Noise Strength" };
}