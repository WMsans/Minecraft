using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands{    
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct WarpPointsJob : IJobFor
    {
        [ReadOnly] NativeArray<float3> originalPoints;
        [WriteOnly] NativeArray<float3> targetPoints;

        [NativeDisableContainerSafetyRestriction] [ReadOnly]
        NativeArray<float> globalMap;
        IndexAndResolution warpX;
        IndexAndResolution warpY;

        int targetResolution;
        int originalResolution;
        public void Execute(int i)
        {
            int warpXIndex = MapTools.RemapIndex(i, targetResolution, warpX.Resolution);
            float warpValueX = globalMap[warpXIndex + warpX.StartIndex];
            warpValueX = warpValueX * 2f - 1f;

            int warpYIndex = MapTools.RemapIndex(i, targetResolution, warpY.Resolution);
            float warpValueY = globalMap[warpYIndex+warpY.StartIndex];
            warpValueY = warpValueY * 2f - 1f;
            
            float3 finalWarp = float3(warpValueX, 0, warpValueY);

            int pointIndex = MapTools.RemapIndex(i, targetResolution, originalResolution);
            targetPoints[i] = originalPoints[pointIndex]+finalWarp;
        }

        public static JobHandle ScheduleParallel(NativeArray<float3> originalPoints, int originalResolution,
            NativeArray<float3> targetPoints, int targetResolution,
            NativeArray<float> globalMap, IndexAndResolution warpX,IndexAndResolution warpY,
            JobHandle dependency) => new WarpPointsJob()
        {
            originalPoints = originalPoints,
            globalMap = globalMap,
            warpX = warpX,
            warpY = warpY,
            targetPoints = targetPoints,
            originalResolution = originalResolution,
            targetResolution = targetResolution,
        }.ScheduleParallel(targetPoints.Length, targetResolution, dependency);
    }


    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct WarpPointsMaskedJob : IJobFor
    {
        [ReadOnly] NativeArray<float3> originalPoints;
        [WriteOnly] NativeArray<float3> targetPoints;

        [NativeDisableContainerSafetyRestriction] [ReadOnly]
        NativeArray<float> globalMap;

        IndexAndResolution warpX;
        IndexAndResolution warpY;
        IndexAndResolution mask;

        int targetResolution;
        int originalResolution;
        public void Execute(int i)
        {
            int warpXIndex = MapTools.RemapIndex(i, targetResolution, warpX.Resolution);
            float warpValueX = globalMap[warpXIndex+ warpX.StartIndex];
            warpValueX = warpValueX * 2f - 1f;

            int warpYIndex = MapTools.RemapIndex(i, targetResolution, warpY.Resolution);
            float warpValueY = globalMap[warpYIndex+warpY.StartIndex];
            warpValueY = warpValueY * 2f - 1f;
            
            int maskIndex = MapTools.RemapIndex(i, targetResolution, mask.Resolution);
            float maskValue = globalMap[maskIndex+mask.StartIndex];
            warpValueX *= maskValue;
            warpValueY *= maskValue;
            

            float3 finalWarp = float3(warpValueX, 0, warpValueY);
            
            int pointIndex = MapTools.RemapIndex(i, targetResolution, originalResolution);
            targetPoints[i] = originalPoints[pointIndex] + finalWarp;
        }

        public static JobHandle ScheduleParallel(NativeArray<float3> originalPoints, int originalResolution,
            NativeArray<float3> targetPoints, int targetResolution,
            NativeArray<float> globalMap,IndexAndResolution warpX,IndexAndResolution warpY, IndexAndResolution mask,
            JobHandle dependency) => new WarpPointsMaskedJob()
        {
            originalPoints = originalPoints,
            globalMap = globalMap,
            targetPoints = targetPoints,
            warpX = warpX,
            warpY = warpY,
            mask = mask,
            originalResolution = originalResolution,
            targetResolution = targetResolution,
        }.ScheduleParallel(targetPoints.Length, targetResolution, dependency);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct WarpPointsNormalMapJob : IJobFor
    {
        [ReadOnly] NativeArray<float3> originalPoints;
        [WriteOnly] NativeArray<float3> targetPoints;

        [NativeDisableContainerSafetyRestriction] [ReadOnly]
        NativeArray<float3> normalMap;
        IndexAndResolution normalIndex;

        int targetResolution;
        int originalResolution;
        float strength;
        public void Execute(int i)
        {
            int warpIndex = MapTools.RemapIndex(i, targetResolution, normalIndex.Resolution);
            float3 warpValueX = normalMap[warpIndex];

            float3 finalWarp = float3(warpValueX.x, 0, warpValueX.z)*strength;
            int pointIndex = MapTools.RemapIndex(i, targetResolution, originalResolution);
            targetPoints[i] = originalPoints[pointIndex] + finalWarp;
        }

        public static JobHandle ScheduleParallel(NativeArray<float3> originalPoints, int originalResolution,
            NativeArray<float3> targetPoints, int targetResolution,
            NativeArray<float3> normalMap, IndexAndResolution normalIndex, float strength,
            JobHandle dependency) => new WarpPointsNormalMapJob()
        {
            strength = strength,
            originalPoints = originalPoints,
            normalMap = normalMap,
            targetPoints = targetPoints,
            normalIndex = normalIndex,
            targetResolution = targetResolution,
            originalResolution = originalResolution,
        }.ScheduleParallel(targetPoints.Length, targetResolution, dependency);
    }
    
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct WarpPointsNormalMaskedJob : IJobFor
    {
        [ReadOnly] NativeArray<float3> originalPoints;
        [WriteOnly] NativeArray<float3> targetPoints;

        [NativeDisableContainerSafetyRestriction] [ReadOnly]
        NativeArray<float> globalMapX;
        [NativeDisableContainerSafetyRestriction] [ReadOnly]
        NativeArray<float3> normalMap;
        IndexAndResolution normalIndex;
        IndexAndResolution mask;

        int targetResolution;
        int originalResolution;
        float strength;

        public void Execute(int i)
        {
            int warpIndex = MapTools.RemapIndex(i, targetResolution, normalIndex.Resolution);
            float3 warpValueX = normalMap[warpIndex];
    
            
            int maskIndex = MapTools.RemapIndex(i, targetResolution, mask.Resolution);
            float maskValue = globalMapX[maskIndex+mask.StartIndex];
            warpValueX *= maskValue;
            

            float3 finalWarp = float3(warpValueX.x, 0, warpValueX.z)*strength;
            int pointIndex = MapTools.RemapIndex(i, targetResolution, originalResolution);
            targetPoints[i] = originalPoints[pointIndex] + finalWarp;
        }

        public static JobHandle ScheduleParallel(NativeArray<float3> originalPoints, int originalResolution,
            NativeArray<float3> targetPoints, int targetResolution,
            NativeArray<float> globalMapX, NativeArray<float3> normalMap, IndexAndResolution normalIndex, float strength, IndexAndResolution mask,
            JobHandle dependency) => new WarpPointsNormalMaskedJob()
        {
            strength = strength,
            originalPoints = originalPoints,
            globalMapX = globalMapX,
            normalMap = normalMap,
            targetPoints = targetPoints,
            normalIndex = normalIndex,
            mask = mask,
            targetResolution = targetResolution,
            originalResolution = originalResolution,
        }.ScheduleParallel(targetPoints.Length, targetResolution, dependency);
    }
}