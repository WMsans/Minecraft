using Icaria.Engine.Procedural;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

[BurstCompile]
public unsafe struct PerlinNoiseHeightmapLayer : IHeightmapLayer
{
    [BurstCompile]
    public static void Apply(ref HeightmapLayer layer, int seed, ref Heightmap heightmap, in float3 offset, float scale)
    {
        if (!layer.enabled) return;

        float noiseScale = layer.properties[0];
        float noiseStrength = layer.properties[1];

        for (int x = 0; x < heightmap.size.x; x++)
        {
            for (int z = 0; z < heightmap.size.y; z++)
            {
                // Corrected world coordinate calculation
                float worldX = offset.x + (x / (float)(heightmap.size.x - 1) - 0.5f) * scale;
                float worldZ = offset.z + (z / (float)(heightmap.size.y - 1) - 0.5f) * scale;
                
                float noiseValue = IcariaNoise.GradientNoise(worldX * noiseScale, worldZ * noiseScale, seed) * noiseStrength;
                
                int index = x + z * heightmap.size.x;
                heightmap.heights[index] += noiseValue;
            }
        }
    }

    public static HeightmapLayer Create(params float[] properties)
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

    public static HeightmapLayer Create(float noiseScale = 0.05f, float noiseStrength = 10f)
    {
        var layer = new HeightmapLayer
        {
            ApplyFunction = BurstCompiler.CompileFunctionPointer<HeightmapLayer.ApplyDelegate>(Apply),
            enabled = true,
        };

        layer.properties[0] = noiseScale;
        layer.properties[1] = noiseStrength;

        return layer;
    }

    public static string[] Fields() => new[] { "Noise Scale", "Noise Strength" };
}