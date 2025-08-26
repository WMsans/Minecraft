using System;
using Icaria.Engine.Procedural;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

[BurstCompile]
public unsafe struct ErosionHeightmapLayer : IHeightmapLayer
{
    private const float PI = 3.14159265359f;

    [BurstCompile]
    public static void Apply(ref HeightmapLayer layer, int seed, ref Heightmap heightmap, in float3 offset, float scale)
    {
        if (!layer.enabled) return;

        // --- Read properties from the layer ---
        float heightTiles = layer.properties[0];
        int heightOctaves = (int)layer.properties[1];
        float heightAmp = layer.properties[2];
        float heightGain = layer.properties[3];
        float heightLacunarity = layer.properties[4];
        float erosionSlopeStrength = layer.properties[5];
        float erosionTiles = layer.properties[6];
        float erosionStrength = layer.properties[7];
        int erosionOctaves = (int)layer.properties[8];
        float erosionGain = layer.properties[9];
        float erosionLacunarity = layer.properties[10];
        float erosionBranchStrength = layer.properties[11];
        float waterHeight = layer.properties[12];

        for (int x = 0; x < heightmap.size.x; x++)
        {
            for (int z = 0; z < heightmap.size.y; z++)
            {
                float worldX = offset.x + (x / (float)(heightmap.size.x - 1) - 0.5f) * scale;
                float worldZ = offset.z + (z / (float)(heightmap.size.y - 1) - 0.5f) * scale;
                
                float2 uv = new float2(worldX, worldZ);

                var height = HeightmapFunc(uv, seed, 
                    heightTiles, heightOctaves, heightAmp, heightGain, heightLacunarity,
                    erosionSlopeStrength, erosionTiles, erosionStrength, erosionOctaves,
                    erosionGain, erosionLacunarity, erosionBranchStrength, waterHeight);

                int index = x + z * heightmap.size.x;
                // Using += allows layering this effect on top of previous layers
                heightmap.heights[index] += height.x; 
            }
        }
    }

    private static float2 HeightmapFunc(float2 uv, int seed, 
        float heightTiles, int heightOctaves, float heightAmp, float heightGain, float heightLacunarity,
        float erosionSlopeStrength, float erosionTiles, float erosionStrength, int erosionOctaves,
        float erosionGain, float erosionLacunarity, float erosionBranchStrength, float waterHeight)
    {
        float2 p = uv * heightTiles;

        // FBM terrain
        float3 n = float3.zero;
        float nf = 1.0f;
        float na = heightAmp;
        for (int i = 0; i < heightOctaves; i++)
        {
            n += noised(p * nf, seed) * na * new float3(1.0f, nf, nf);
            na *= heightGain;
            nf *= heightLacunarity;
        }

        // [-1, 1] -> [0, 1]
        n.x = n.x * 0.5f + 0.5f;

        // Take the curl of the normal to get the gradient facing down the slope
        float2 dir = n.zy * new float2(1.0f, -1.0f) * erosionSlopeStrength;

        // Now we compute another fbm type noise
        float3 h = float3.zero;
        float a = 0.5f;
        float f = 1.0f;

        a *= math.smoothstep(waterHeight - 0.1f, waterHeight + 0.2f, n.x);

        for (int i = 0; i < erosionOctaves; i++)
        {
            h += Erosion(p * erosionTiles * f, dir + h.zy * new float2(1.0f, -1.0f) * erosionBranchStrength, seed) * a * new float3(1.0f, f, f);
            a *= erosionGain;
            f *= erosionLacunarity;
        }

        // Return only the height adjustment. The second component was for debugging/visualization.
        return new float2((h.x - 0.5f) * erosionStrength, h.x);
    }

    private static float3 Erosion(in float2 p, float2 dir, int seed)
    {
        float2 ip = math.floor(p);
        float2 fp = math.frac(p);
        float f = 2.0f * PI;
        float3 va = float3.zero;
        float wt = 0.0f;
        for (int i = -2; i <= 1; i++)
        {
            for (int j = -2; j <= 1; j++)
            {
                float2 o = new float2(i, j);
                float2 h = hash(ip - o, seed) * 0.5f;
                float2 pp = fp + o - h;
                float d = math.dot(pp, pp);
                float w = math.exp(-d * 2.0f);
                wt += w;
                float mag = math.dot(pp, dir);
                va += new float3(math.cos(mag * f), -math.sin(mag * f) * (pp.x * 0.0f + dir.x), -math.sin(mag * f) * (pp.y * 0.0f + dir.y)) * w;
            }
        }
        return va / wt;
    }

    private static float3 noised(float2 p, int seed)
    {
        // Using GradientNoise as a substitute for the shader's noised function
        return IcariaNoise.GradientNoiseWithDerivatives(p.x, p.y, seed);
    }

    private static float2 hash(float2 p, int seed)
    {
        // A simple hash function
        p = new float2(math.dot(p, new float2(127.1f, 311.7f)),
                       math.dot(p, new float2(269.5f, 183.3f)));
        return -1.0f + 2.0f * math.frac(math.sin(p) * 43758.5453123f * (seed / 12.0f));
    }
    
    /// <summary>
    /// Defines the user-facing names for the properties.
    /// </summary>
    public static string[] Fields() => new[] { 
        "Height Tiling", "Height Octaves", "Height Amplitude", "Height Gain", "Height Lacunarity",
        "Slope Strength", "Erosion Tiling", "Erosion Strength", "Erosion Octaves", "Erosion Gain", "Erosion Lacunarity",
        "Branch Strength", "Water Height"
    };
    
    /// <summary>
    /// Creates a layer instance with properties set from an array, with default values.
    /// </summary>
    public static HeightmapLayer Create(params float[] properties)
    {
        // --- Default Values ---
        float heightTiles = 1.0f;
        float heightOctaves = 8.0f;
        float heightAmp = 1.0f;
        float heightGain = 0.5f;
        float heightLacunarity = 2.0f;
        float erosionSlopeStrength = 1.0f;
        float erosionTiles = 1.0f;
        float erosionStrength = 1.0f;
        float erosionOctaves = 6.0f;
        float erosionGain = 0.5f;
        float erosionLacunarity = 2.0f;
        float erosionBranchStrength = 1.0f;
        float waterHeight = 0.1f;

        if (properties != null)
        {
            if (properties.Length >= 1) heightTiles = properties[0];
            if (properties.Length >= 2) heightOctaves = properties[1];
            if (properties.Length >= 3) heightAmp = properties[2];
            if (properties.Length >= 4) heightGain = properties[3];
            if (properties.Length >= 5) heightLacunarity = properties[4];
            if (properties.Length >= 6) erosionSlopeStrength = properties[5];
            if (properties.Length >= 7) erosionTiles = properties[6];
            if (properties.Length >= 8) erosionStrength = properties[7];
            if (properties.Length >= 9) erosionOctaves = properties[8];
            if (properties.Length >= 10) erosionGain = properties[9];
            if (properties.Length >= 11) erosionLacunarity = properties[10];
            if (properties.Length >= 12) erosionBranchStrength = properties[11];
            if (properties.Length >= 13) waterHeight = properties[12];
        }

        return Create(heightTiles, heightOctaves, heightAmp, heightGain, heightLacunarity,
                      erosionSlopeStrength, erosionTiles, erosionStrength, erosionOctaves,
                      erosionGain, erosionLacunarity, erosionBranchStrength, waterHeight);
    }

    /// <summary>
    /// Creates a layer instance with explicitly defined properties.
    /// </summary>
    public static HeightmapLayer Create(
        float heightTiles, float heightOctaves, float heightAmp, float heightGain, float heightLacunarity,
        float erosionSlopeStrength, float erosionTiles, float erosionStrength, float erosionOctaves,
        float erosionGain, float erosionLacunarity, float erosionBranchStrength, float waterHeight)
    {
        var layer = new HeightmapLayer
        {
            ApplyFunction = BurstCompiler.CompileFunctionPointer<HeightmapLayer.ApplyDelegate>(Apply),
            enabled = true,
        };

        // --- Assign properties in the correct order ---
        layer.properties[0] = heightTiles;
        layer.properties[1] = heightOctaves;
        layer.properties[2] = heightAmp;
        layer.properties[3] = heightGain;
        layer.properties[4] = heightLacunarity;
        layer.properties[5] = erosionSlopeStrength;
        layer.properties[6] = erosionTiles;
        layer.properties[7] = erosionStrength;
        layer.properties[8] = erosionOctaves;
        layer.properties[9] = erosionGain;
        layer.properties[10] = erosionLacunarity;
        layer.properties[11] = erosionBranchStrength;
        layer.properties[12] = waterHeight;

        return layer;
    }
    
    public static string[] InputPorts() => Array.Empty<string>();
    public static string[] OutputPorts() => new[] { "Out" };
}