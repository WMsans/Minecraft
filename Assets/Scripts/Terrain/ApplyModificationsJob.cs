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
        // Calculate the scale of the chunk's grid points relative to world space.
        float scale = nodeBounds.size.x / chunkSize;

        // Process each modification.
        for (int i = 0; i < modifications.Length; i++)
        {
            var mod = modifications[i];
            
            // Create bounds for the modification to check for intersection with the current node.
            Bounds modBounds = new Bounds(mod.worldPos, new float3(mod.radius, mod.radius, mod.radius) * 2);
            if (!modBounds.Intersects(nodeBounds)) continue;

            // Calculate the iteration bounds in the density grid's coordinates.
            float3 modMinGrid = (new float3(mod.worldPos) - new float3(mod.radius) - new float3(nodeBounds.min)) / scale;
            float3 modMaxGrid = (new float3(mod.worldPos) + new float3(mod.radius) - new float3(nodeBounds.min)) / scale;

            // Clamp the iteration bounds to the chunk's dimensions.
            int3 min = (int3)math.max(0, math.floor(modMinGrid));
            int3 max = (int3)math.min(chunkSize, math.ceil(modMaxGrid));

            // Iterate through the affected density grid cells.
            for (int z = min.z; z <= max.z; z++)
            {
                for (int y = min.y; y <= max.y; y++)
                {
                    for (int x = min.x; x <= max.x; x++)
                    {
                        // Calculate the world position of the current density grid cell.
                        float3 gridPointPos = new float3(nodeBounds.min) + new float3(x, y, z) * scale;
                        
                        // Check if the cell is within the modification's spherical radius.
                        float distance = math.distance(gridPointPos, mod.worldPos);
                        if (distance > mod.radius) continue;

                        int index = x + (chunkSize + 1) * (y + (chunkSize + 1) * z);

                        if (index >= 0 && index < densityMap.Length)
                        {
                            // Apply the modification with a falloff based on distance.
                            float falloff = 1 - (distance / mod.radius);
                            densityMap[index] += mod.strength * falloff;
                            
                            // If digging, update the voxel type.
                            if(mod.strength < 0)
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