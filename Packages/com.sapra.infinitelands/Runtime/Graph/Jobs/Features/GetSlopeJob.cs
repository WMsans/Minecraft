using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands
{    
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct GetSlope : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> globalMap;

        [ReadOnly] NativeArray<float3> normalMap;
        IndexAndResolution normalIndex;
        IndexAndResolution target;

        float4x4 localToWorld;
        public void Execute(int i)
        {
            int index = MapTools.RemapIndex(i, target.Resolution, normalIndex.Resolution);
            float3 normal = normalMap[index];

            float4 inWorldSpace = mul(float4(normal.xyz, 1.0f),localToWorld);
            globalMap[target.StartIndex + i] = acos(inWorldSpace.y) * 0.636f;
        }
        
        public static JobHandle ScheduleParallel(
            NativeArray<float3> normalMap, IndexAndResolution normalIndex,
            NativeArray<float> globalMap, float4x4 localToWorld,
            IndexAndResolution targetIndex,
            JobHandle dependency) => new GetSlope()
        {
            localToWorld = localToWorld,
            normalMap = normalMap,
            normalIndex = normalIndex,
            target = targetIndex,
            globalMap = globalMap,
        }.ScheduleParallel(targetIndex.Length, targetIndex.Resolution, dependency);
    }
}