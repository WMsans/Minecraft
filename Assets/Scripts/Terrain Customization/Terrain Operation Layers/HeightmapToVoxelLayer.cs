using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

[BurstCompile]
public unsafe struct HeightmapToVoxelLayer : ITerrainLayer
{
    [BurstCompile]
    public static void Apply(ref TerrainLayer layer, int seed, float* density, byte* voxelTypes, int densityLength, int chunkSize, in float3 offset, float scale, void* entities, float* heightmap, int heightmapLength)
    {
        if (!layer.enabled) return;

        int heightmapWidth = (int)math.sqrt(heightmapLength);

        for (int i = 0; i < densityLength; i++)
        {
            int x = i % (chunkSize + 1);
            int y = (i / (chunkSize + 1)) % (chunkSize + 1);
            int z = i / ((chunkSize + 1) * (chunkSize + 1));

            float worldY = offset.y + (y / (float)chunkSize - 0.5f) * scale;
            
            int heightmapX = (int)(((float)x / (chunkSize + 1)) * heightmapWidth);
            int heightmapZ = (int)(((float)z / (chunkSize + 1)) * heightmapWidth);
            int heightmapIndex = heightmapX + heightmapZ * heightmapWidth;
            
            float height = heightmap[heightmapIndex];
            
            density[i] = worldY - height;
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
}