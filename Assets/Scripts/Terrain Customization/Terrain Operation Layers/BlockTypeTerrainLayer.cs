using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public unsafe struct BlockTypeTerrainLayer : ITerrainLayer
{
    [BurstCompile]
    public static void Apply(ref TerrainLayer layer, int seed, float* density, byte* voxelTypes, int densityLength, int chunkSize, in float3 offset, float scale)
    {
        if (!layer.enabled) return;

        // Get properties from the layer struct.
        // For this logic to work, stoneDepth should be greater than dirtDepth.
        float dirtDepth = layer.properties[0];
        float stoneDepth = layer.properties[1];

        int size = chunkSize + 1;
        
        // Allocate a temporary array on the stack to store the surface height for each column.
        float* surfaceHeights = stackalloc float[size * size];

        // 1. Pre-computation Pass: Find the surface height for each (x, z) column.
        // We now correctly assume the surface is the first voxel from the top where density is <= 0 (solid).
        for (int z = 0; z < size; z++)
        {
            for (int x = 0; x < size; x++)
            {
                bool surfaceFound = false;
                // Scan downwards from the top of the chunk.
                for (int y = size - 1; y >= 0; y--)
                {
                    int index = x + y * size + z * size * size;
                    // ***FIX***: Check for density <= 0 to find solid ground.
                    if (density[index] <= 0) 
                    {
                        // Calculate and store the world Y coordinate of the surface.
                        float worldY = offset.y + (y / (float)chunkSize - 0.5f) * scale;
                        surfaceHeights[x + z * size] = worldY;
                        surfaceFound = true;
                        break; // Move to the next column.
                    }
                }

                if (!surfaceFound)
                {
                    // If a column is all air, mark it with a special value.
                    surfaceHeights[x + z * size] = float.MinValue;
                }
            }
        }
        
        // 2. Main Pass: Assign voxel types based on depth from the surface.
        for (int i = 0; i < densityLength; i++)
        {
            // ***FIX***: Only process solid voxels (density <= 0). Skip air voxels (density > 0).
            if (density[i] > 0) continue;

            int x = i % size;
            int y = (i / size) % size;
            int z = i / (size * size);

            float surfaceY = surfaceHeights[x + z * size];
            
            // Skip if this voxel is in a column that was determined to be all air.
            if (surfaceY == float.MinValue) continue;

            // Calculate the current voxel's world position and its depth below the surface.
            float worldY = offset.y + (y / (float)chunkSize - 0.5f) * scale;
            float depth = surfaceY - worldY;

            // Clamp depth to be non-negative to handle floating-point inaccuracies at the surface.
            if (depth < 0) depth = 0;

            // Assign block type based on depth.
            if (depth > stoneDepth)
            {
                voxelTypes[i] = 2; // Stone
            }
            else if (depth > dirtDepth)
            {
                voxelTypes[i] = 1; // Dirt
            }
            else
            {
                voxelTypes[i] = 0; // Grass (top layer)
            }
        }
    }

    public static TerrainLayer Create(params float[] properties)
    {
        // Default depths from the surface.
        float dirtDepth = 1f;
        float stoneDepth = 5f;

        if (properties != null && properties.Length >= 2)
        {
            dirtDepth = properties[0];
            stoneDepth = properties[1];
        }

        return Create(dirtDepth, stoneDepth);
    }

    public static TerrainLayer Create(float dirtDepth = 1f, float stoneDepth = 5f)
    {
        var layer = new TerrainLayer
        {
            ApplyFunction = BurstCompiler.CompileFunctionPointer<TerrainLayer.ApplyDelegate>(Apply),
            enabled = true,
        };

        // Set the properties for this layer.
        layer.properties[0] = dirtDepth;
        layer.properties[1] = stoneDepth;

        return layer;
    }

    // Updated field names for clarity in the editor/UI.
    public static string[] Fields() => new[] { "Dirt Depth", "Stone Depth" };
}