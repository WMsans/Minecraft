using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct DensityModificationJob : IJob
{
    [ReadOnly] public float3 worldPos;
    [ReadOnly] public float strength;
    [ReadOnly] public float radius;
    [ReadOnly] public Bounds nodeBounds;
    public NativeArray<float> densityMap;
    public NativeArray<byte> voxelTypes; // New
    [ReadOnly] public byte newVoxelType; // New

    public void Execute()
    {
        int buildRadius = (int)math.ceil(radius);
        for (int x = -buildRadius; x <= buildRadius; x++)
        {
            for (int y = -buildRadius; y <= buildRadius; y++)
            {
                for (int z = -buildRadius; z <= buildRadius; z++)
                {
                    float3 offset = new float3(x, y, z);
                    if (math.length(offset) > radius) continue;

                    float3 modifiedPos = math.floor(worldPos) + offset;

                    // Use the new constant
                    int densityX = (int)((modifiedPos.x - nodeBounds.min.x) / nodeBounds.size.x * TerrainSettings.MIN_NODE_SIZE);
                    int densityY = (int)((modifiedPos.y - nodeBounds.min.y) / nodeBounds.size.x * TerrainSettings.MIN_NODE_SIZE);
                    int densityZ = (int)((modifiedPos.z - nodeBounds.min.z) / nodeBounds.size.x * TerrainSettings.MIN_NODE_SIZE);

                    if (densityX >= 0 && densityX <= TerrainSettings.MIN_NODE_SIZE &&
                        densityY >= 0 && densityY <= TerrainSettings.MIN_NODE_SIZE &&
                        densityZ >= 0 && densityZ <= TerrainSettings.MIN_NODE_SIZE)
                    {
                        int index = densityX + (TerrainSettings.MIN_NODE_SIZE + 1) * (densityY + (TerrainSettings.MIN_NODE_SIZE + 1) * densityZ);
                        if (index >= 0 && index < densityMap.Length)
                        {
                            float falloff = 1 - (math.length(offset) / radius);
                            densityMap[index] += strength * falloff;
                            if(strength < 0)
                            {
                                voxelTypes[index] = newVoxelType;
                            }
                        }
                    }
                }
            }
        }
    }
}