using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct ApplyModificationsJob : IJob
{
    [ReadOnly] public NativeList<OctreeTerrainManager.TerrainModification> modifications;
    [ReadOnly] public Bounds nodeBounds;
    public NativeArray<float> densityMap;
    public NativeArray<byte> voxelTypes;
    [ReadOnly] public int chunkSize;

    public void Execute()
    {
        // Calculate the voxel size in world space
        // This is the distance between adjacent points in the density grid
        float voxelSize = nodeBounds.size.x / chunkSize;

        // Process each modification.
        for (int i = 0; i < modifications.Length; i++)
        {
            var mod = modifications[i];
            
            // Quick bounds check for early rejection
            Bounds modBounds = new Bounds(mod.worldPos, new float3(mod.radius, mod.radius, mod.radius) * 2);
            if (!modBounds.Intersects(nodeBounds)) continue;

            // Convert modification bounds from world space to grid indices
            float3 worldMin = new float3(mod.worldPos) - new float3(mod.radius);
            float3 worldMax = new float3(mod.worldPos) + new float3(mod.radius);
            
            // Calculate grid coordinates (0 to chunkSize)
            float3 modMinGrid = (worldMin - new float3(nodeBounds.min)) / voxelSize;
            float3 modMaxGrid = (worldMax - new float3(nodeBounds.min)) / voxelSize;

            // Clamp to valid grid indices
            int3 min = (int3)math.max(0, math.floor(modMinGrid));
            int3 max = (int3)math.min(chunkSize, math.ceil(modMaxGrid));

            // Iterate through affected grid points
            for (int z = min.z; z <= max.z; z++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    for (int x = min.x; x <= max.x; x++)
                    {
                        // Convert grid position back to world position
                        float3 gridPointWorld = new float3(nodeBounds.min) + new float3(x, y, z) * voxelSize;
                        
                        // Calculate distance from modification center
                        float distance = math.distance(gridPointWorld, mod.worldPos);
                        if (distance > mod.radius) continue;

                        // Calculate the grid index
                        int index = x + (chunkSize + 1) * (y + (chunkSize + 1) * z);

                        if (index >= 0 && index < densityMap.Length)
                        {
                            // Apply modification with smooth falloff
                            float falloff = 1f - (distance / mod.radius);
                            // Use a smoother falloff curve for better results
                            falloff = falloff * falloff * (3f - 2f * falloff); // Smoothstep
                            
                            densityMap[index] += mod.strength * falloff;
                            
                            // Update voxel type for digging operations
                            if (mod.strength < 0 && densityMap[index] > 0)
                            {
                                voxelTypes[index] = mod.newVoxelType;
                            }
                        }
                    }
                }
            }
        }
    }
}