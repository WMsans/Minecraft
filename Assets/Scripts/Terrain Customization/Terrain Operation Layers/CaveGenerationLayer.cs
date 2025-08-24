using System;
using Icaria.Engine.Procedural;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

[BurstCompile]
public unsafe struct CaveGenerationLayer : ITerrainLayer
{
    [BurstCompile]
    public static void Apply(ref TerrainLayer layer, int seed, float* density, byte* voxelTypes, int densityLength, int chunkSize, in float3 offset, float scale, float* heightmap, int heightmapLength)
    {
        if (!layer.enabled) return;

        float noiseScale = layer.properties[0];
        float threshold = layer.properties[1];

        for (int i = 0; i < densityLength; i++)
        {
            int x = i % (chunkSize + 1);
            int y = (i / (chunkSize + 1)) % (chunkSize + 1);
            int z = i / ((chunkSize + 1) * (chunkSize + 1));

            float worldX = offset.x + (x / (float)chunkSize - 0.5f) * scale;
            float worldY = offset.y + (y / (float)chunkSize - 0.5f) * scale;
            float worldZ = offset.z + (z / (float)chunkSize - 0.5f) * scale;

            float noiseValue = IcariaNoise.GradientNoise3D(worldX * noiseScale, worldY * noiseScale, worldZ * noiseScale, seed);

            if (noiseValue > threshold)
            {
                // Carve out a cave by setting the density to a positive value
                density[i] = 1.0f;
            }
        }
    }

    public static TerrainLayer Create(params float[] properties)
    {
        float noiseScale = 0.05f;
        float threshold = 0.5f;

        if (properties != null && properties.Length >= 2)
        {
            noiseScale = properties[0];
            threshold = properties[1];
        }
        else if (properties != null && properties.Length >= 1)
        {
            noiseScale = properties[0];
        }

        return Create(noiseScale, threshold);
    }

    public static TerrainLayer Create(float noiseScale = 0.05f, float threshold = 0.5f)
    {
        var layer = new TerrainLayer
        {
            ApplyFunction = BurstCompiler.CompileFunctionPointer<TerrainLayer.ApplyDelegate>(Apply),
            enabled = true,
        };

        layer.properties[0] = noiseScale;
        layer.properties[1] = threshold;

        return layer;
    }

    public static string[] Fields() => new[] { "Noise Scale", "Threshold" };
    public static string[] InputPorts() => new[] { "In" };
    public static string[] OutputPorts() => new[] { "Out" };
}