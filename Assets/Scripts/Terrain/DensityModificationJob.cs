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

                    int densityX = (int)((modifiedPos.x - nodeBounds.min.x) / nodeBounds.size.x * 16);
                    int densityY = (int)((modifiedPos.y - nodeBounds.min.y) / nodeBounds.size.x * 16);
                    int densityZ = (int)((modifiedPos.z - nodeBounds.min.z) / nodeBounds.size.x * 16);

                    if (densityX >= 0 && densityX <= 16 &&
                        densityY >= 0 && densityY <= 16 &&
                        densityZ >= 0 && densityZ <= 16)
                    {
                        int index = densityX + (16 + 1) * (densityY + (16 + 1) * densityZ);
                        if (index >= 0 && index < densityMap.Length)
                        {
                            float falloff = 1 - (math.length(offset) / radius);
                            densityMap[index] += strength * falloff;
                        }
                    }
                }
            }
        }
    }
}