using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public unsafe struct ErosionTerrainLayer : ITerrainLayer
{
    [BurstCompile]
    public static void Apply(ref TerrainLayer layer, int seed, float* density, byte* voxelTypes, int densityLength, int chunkSize, in float3 offset, float scale, void* entities)
    {
        if (!layer.enabled) return;

        // Get properties from the layer struct
        float frequency = layer.properties[0];
        float amplitude = layer.properties[1];
        float warpStrength = layer.properties[2];
        int octaves = (int)layer.properties[3];
        float lacunarity = layer.properties[4];
        float gain = layer.properties[5];

        for (int i = 0; i < densityLength; i++)
        {
            int x = i % (chunkSize + 1);
            int y = (i / (chunkSize + 1)) % (chunkSize + 1);
            int z = i / ((chunkSize + 1) * (chunkSize + 1));

            float worldX = offset.x + (x / (float)chunkSize - 0.5f) * scale;
            float worldY = offset.y + (y / (float)chunkSize - 0.5f) * scale;
            float worldZ = offset.z + (z / (float)chunkSize - 0.5f) * scale;

            float3 p = new float3(worldX, worldY, worldZ);
            
            // Domain warping
            float warpX = BurstNoiseGenerator.Perlin(seed, p.y * frequency, p.z * frequency) * warpStrength;
            float warpZ = BurstNoiseGenerator.Perlin(seed, p.x * frequency + 100.0f, p.y * frequency + 100.0f) * warpStrength;

            float warpedX = p.x + warpX;
            float warpedZ = p.z + warpZ;

            float currentAmplitude = amplitude;
            float currentFrequency = frequency;
            float noise = 0;

            for (int j = 0; j < octaves; j++)
            {
                noise += BurstNoiseGenerator.Perlin(seed, warpedX * currentFrequency, warpedZ * currentFrequency) * currentAmplitude;
                currentAmplitude *= gain;
                currentFrequency *= lacunarity;
            }

            density[i] -= noise;
        }
    }

    public static TerrainLayer Create(params float[] properties)
    {
        float frequency = 0.02f;
        float amplitude = 15f;
        float warpStrength = 20f;
        int octaves = 4;
        float lacunarity = 2.0f;
        float gain = 0.5f;

        if (properties != null && properties.Length >= 6)
        {
            frequency = properties[0];
            amplitude = properties[1];
            warpStrength = properties[2];
            octaves = (int)properties[3];
            lacunarity = properties[4];
            gain = properties[5];
        }

        return Create(frequency, amplitude, warpStrength, octaves, lacunarity, gain);
    }

    public static TerrainLayer Create(float frequency = 0.02f, float amplitude = 15f, float warpStrength = 20f, int octaves = 4, float lacunarity = 2.0f, float gain = 0.5f)
    {
        var layer = new TerrainLayer
        {
            ApplyFunction = BurstCompiler.CompileFunctionPointer<TerrainLayer.ApplyDelegate>(Apply),
            enabled = true,
        };

        // Set the properties for this layer
        layer.properties[0] = frequency;
        layer.properties[1] = amplitude;
        layer.properties[2] = warpStrength;
        layer.properties[3] = octaves;
        layer.properties[4] = lacunarity;
        layer.properties[5] = gain;

        return layer;
    }

    public static string[] Fields() => new[] { "Frequency", "Amplitude", "Warp Strength", "Octaves", "Lacunarity", "Gain" };
}