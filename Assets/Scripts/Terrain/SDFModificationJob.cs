using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct SDFModificationJob : IJob
{
    // A single modification to apply
    [ReadOnly] public OctreeTerrainManager.TerrainModification modification;
    [ReadOnly] public Bounds nodeBounds;
    public NativeArray<float> densityMap;
    [ReadOnly] public int chunkSize;

    public void Execute()
    {
        // Calculate the size of a single voxel in world space
        float voxelSize = nodeBounds.size.x / chunkSize;

        // Broad-phase check to see if the modification affects this node at all
        Bounds modBounds = new Bounds(modification.worldPos, new float3(modification.radius) * 2);
        if (!modBounds.Intersects(nodeBounds)) return;

        // Convert the modification's world space bounds to grid indices
        float3 worldMin = modification.worldPos - modification.radius;
        float3 worldMax = modification.worldPos + modification.radius;
        
        float3 modMinGrid = (worldMin - (float3)nodeBounds.min) / voxelSize;
        float3 modMaxGrid = (worldMax - (float3)nodeBounds.min) / voxelSize;

        // Clamp the grid indices to the bounds of the chunk
        int3 min = (int3)math.max(0, math.floor(modMinGrid));
        int3 max = (int3)math.min(chunkSize, math.ceil(modMaxGrid));

        // Iterate over the affected voxels in the grid
        for (int z = min.z; z <= max.z; z++)
        {
            for (int y = min.y; y <= max.y; y++)
            {
                for (int x = min.x; x <= max.x; x++)
                {
                    // Convert grid point back to world position to calculate its distance from the modification center
                    float3 gridPointWorld = (float3)nodeBounds.min + new float3(x, y, z) * voxelSize;
                    
                    // Calculate the SDF value for a sphere at the modification's position
                    float distanceToCenter = math.distance(gridPointWorld, modification.worldPos);
                    float sphereSDF = distanceToCenter - modification.radius;

                    int index = x + (chunkSize + 1) * (y + (chunkSize + 1) * z);

                    if (index >= 0 && index < densityMap.Length)
                    {
                        // Apply the SDF operation
                        if (modification.strength > 0) // Additive operation (Union)
                        {
                            // The new density is the minimum of the old density and the sphere's SDF
                            densityMap[index] = math.min(densityMap[index], sphereSDF);
                        }
                        else // Subtractive operation (Subtraction)
                        {
                            // The new density is the maximum of the old density and the inverted sphere's SDF
                            densityMap[index] = math.max(densityMap[index], -sphereSDF);
                        }
                    }
                }
            }
        }
    }
}