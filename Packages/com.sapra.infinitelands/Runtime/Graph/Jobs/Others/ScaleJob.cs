using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands{    
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct ScaleJob : IJobFor
    {
        [ReadOnly] NativeArray<float3> originalPoints;
        [WriteOnly] NativeArray<float3> targetPoints;

        float ScaleFactor;

        int targetResolution;
        int originalResolution;
        float3 chunkPosition;
        public void Execute(int i)
        {
            int pointIndex = MapTools.RemapIndex(i, targetResolution, originalResolution);
            targetPoints[i] = originalPoints[pointIndex] * ScaleFactor+chunkPosition*(ScaleFactor-1);
        }

        public static JobHandle ScheduleParallel(NativeArray<float3> originalPoints, int originalResolution,
            NativeArray<float3> targetPoints, int targetResolution,
            float ScaleFactor, float3 chunkPosition,
            JobHandle dependency) => new ScaleJob()
            {
                originalPoints = originalPoints,
                ScaleFactor = ScaleFactor,
                targetPoints = targetPoints,
                originalResolution = originalResolution,
                targetResolution = targetResolution,
                chunkPosition = chunkPosition,
        }.ScheduleParallel(targetPoints.Length, targetResolution, dependency);
    }
}