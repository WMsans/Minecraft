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
            for (int i = 0; i < modifications.Length; i++) 
            {
                var mod = modifications[i];
                
                Bounds modBounds = new Bounds(mod.worldPos, new float3(mod.radius, mod.radius, mod.radius) * 2);
                if (!modBounds.Intersects(nodeBounds)) continue;

                int buildRadius = (int)math.ceil(mod.radius);
                for (int x = -buildRadius; x <= buildRadius; x++)
                {
                    for (int y = -buildRadius; y <= buildRadius; y++)
                    {
                        for (int z = -buildRadius; z <= buildRadius; z++)
                        {
                            float3 offset = new float3(x, y, z);
                            if (math.length(offset) > mod.radius) continue;

                            float3 modifiedPos = math.floor(mod.worldPos) + offset;

                            if (modifiedPos.x < nodeBounds.min.x || modifiedPos.x > nodeBounds.max.x ||
                                modifiedPos.y < nodeBounds.min.y || modifiedPos.y > nodeBounds.max.y ||
                                modifiedPos.z < nodeBounds.min.z || modifiedPos.z > nodeBounds.max.z)
                            {
                                continue;
                            }

                            int densityX = (int)((modifiedPos.x - nodeBounds.min.x) / nodeBounds.size.x * chunkSize);
                            int densityY = (int)((modifiedPos.y - nodeBounds.min.y) / nodeBounds.size.y * chunkSize);
                            int densityZ = (int)((modifiedPos.z - nodeBounds.min.z) / nodeBounds.size.z * chunkSize);

                            if (densityX >= 0 && densityX <= chunkSize &&
                                densityY >= 0 && densityY <= chunkSize &&
                                densityZ >= 0 && densityZ <= chunkSize)
                            {
                                int index = densityX + (chunkSize + 1) * (densityY + (chunkSize + 1) * densityZ);
                                if (index >= 0 && index < densityMap.Length)
                                {
                                    float falloff = 1 - (math.length(offset) / mod.radius);
                                    densityMap[index] += mod.strength * falloff;
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
}
