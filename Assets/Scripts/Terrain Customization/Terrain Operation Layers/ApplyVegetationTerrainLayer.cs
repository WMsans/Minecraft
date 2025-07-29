using Unity.Burst;
using Unity.Mathematics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

[BurstCompile]
public unsafe struct ApplyVegetationTerrainLayer : ITerrainLayer
{
    [BurstCompile]
    public static void Apply(ref TerrainLayer layer, int seed, float* density, byte* voxelTypes,
        int densityLength, int chunkSize, in float3 offset, float scale,
        void* entities)
    {
        if (!layer.enabled) return;

        // --- Layer Properties ---
        float spawnChance = layer.properties[0];
        float treeSpacing = layer.properties[1];
        float minSlope = layer.properties[2];
        float maxSlope = layer.properties[3];

        int size = chunkSize + 1;
        float* surfaceHeights = stackalloc float[size * size];

        // --- 1. Surface Detection Pass ---
        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size; x++)
            {
                bool surfaceFound = false;
                for (int y = size - 1; y >= 0; y--)
                {
                    int index = x + y * size + z * size * size;
                    if (density[index] <= 0)
                    {
                        surfaceHeights[x + z * size] = offset.y + (y / (float)chunkSize - 0.5f) * scale;
                        surfaceFound = true;
                        break;
                    }
                }
                if (!surfaceFound)
                {
                    surfaceHeights[x + z * size] = float.MinValue;
                }
            }
        }

        // --- 2. Vegetation Spawning Pass ---
        var entitiesWriter = UnsafeUtility.AsRef<NativeList<EntityData>.ParallelWriter>(entities);
        for (int z = 1; z < chunkSize; z++)
        {
            for (int x = 1; x < chunkSize; x++)
            {
                float surfaceY = surfaceHeights[x + z * size];
                if (Mathf.Approximately(surfaceY, float.MinValue)) continue;

                // --- Slope Check ---
                float heightX1 = surfaceHeights[(x + 1) + z * size];
                float heightX_1 = surfaceHeights[(x - 1) + z * size];
                float heightZ1 = surfaceHeights[x + (z + 1) * size];
                float heightZ_1 = surfaceHeights[x + (z - 1) * size];

                float3 normal = math.normalize(new float3(heightX_1 - heightX1, 2 * scale, heightZ_1 - heightZ1));
                float slope = math.degrees(math.acos(math.dot(normal, new float3(0, 1, 0))));

                if (slope < minSlope || slope > maxSlope) continue;

                // --- Noise-based Placement ---
                float noiseValue = BurstNoiseGenerator.Perlin(seed,
                    (offset.x + (x / (float)chunkSize - 0.5f) * scale) * treeSpacing,
                    (offset.z + (z / (float)chunkSize - 0.5f) * scale) * treeSpacing);

                if (noiseValue < spawnChance)
                {
                    entitiesWriter.AddNoResize(new EntityData
                    {
                        entityType = EntityType.Type.Tree,
                        position = new float3(offset.x + (x / (float)chunkSize - 0.5f) * scale, surfaceY, offset.z + (z / (float)chunkSize - 0.5f) * scale),
                        velocity = float3.zero,
                        health = 100f
                    });
                }
            }
        }
    }

    public static TerrainLayer Create(params float[] properties)
    {
        // --- Default Values ---
        float spawnChance = 0.1f;
        float treeSpacing = 0.2f;
        float minSlope = 0f;
        float maxSlope = 30f;

        if (properties != null)
        {
            if (properties.Length >= 1) spawnChance = properties[0];
            if (properties.Length >= 2) treeSpacing = properties[1];
            if (properties.Length >= 3) minSlope = properties[2];
            if (properties.Length >= 4) maxSlope = properties[3];
        }

        var layer = new TerrainLayer
        {
            ApplyFunction = BurstCompiler.CompileFunctionPointer<TerrainLayer.ApplyDelegate>(Apply),
            enabled = true,
        };

        layer.properties[0] = spawnChance;
        layer.properties[1] = treeSpacing;
        layer.properties[2] = minSlope;
        layer.properties[3] = maxSlope;

        return layer;
    }

    public static string[] Fields() => new[] { "Spawn Chance", "Tree Spacing", "Min Slope", "Max Slope" };
}