using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

[BurstCompile]
public unsafe struct HeightmapToVoxelLayer : ITerrainLayer
{
    [BurstCompile]
    public static void Apply(ref TerrainLayer layer, int seed, float* density, byte* voxelTypes, int densityLength, int chunkSize, in float3 offset, float scale, float* heightmap, int heightmapLength)
    {
        if (!layer.enabled) return;

        int heightmapWidth = (int)math.sqrt(heightmapLength);
        if (heightmapWidth == 0) return;

        for (int i = 0; i < densityLength; i++)
        {
            int x = i % (chunkSize + 1);
            int y = (i / (chunkSize + 1)) % (chunkSize + 1);
            int z = i / ((chunkSize + 1) * (chunkSize + 1));

            float worldY = offset.y + (y / (float)chunkSize - 0.5f) * scale;

            // Use floating point coordinates for interpolation
            float hx = ((float)x / chunkSize) * (heightmapWidth - 1);
            float hz = ((float)z / chunkSize) * (heightmapWidth - 1);

            int hx0 = (int)hx;
            int hz0 = (int)hz;

            // Clamp coordinates to prevent reading out of bounds
            int hx1 = math.min(hx0 + 1, heightmapWidth - 1);
            int hz1 = math.min(hz0 + 1, heightmapWidth - 1);

            // Interpolation factors
            float tx = hx - hx0;
            float tz = hz - hz0;

            // Sample four nearest heightmap values
            float h00 = heightmap[hx0 + hz0 * heightmapWidth];
            float h10 = heightmap[hx1 + hz0 * heightmapWidth];
            float h01 = heightmap[hx0 + hz1 * heightmapWidth];
            float h11 = heightmap[hx1 + hz1 * heightmapWidth];

            // Bilinear interpolation for smooth height
            float height = math.lerp(math.lerp(h00, h10, tx), math.lerp(h01, h11, tx), tz);

            // This is the key change: we create a "transition band" 
            // of 2 voxels around the surface for a smoother gradient.
            float voxelSize = scale / chunkSize;
            float transition_band = 2.0f * voxelSize;
            float distance_from_surface = worldY - height;

            density[i] = math.clamp(distance_from_surface / transition_band, -1.0f, 1.0f);
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