using sapra.InfiniteLands;
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
    public struct RemapHeightJob : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> heightMap;

        IndexAndResolution current;
        IndexAndResolution target;

        float2 CurrentMinMax;
        float2 TargetMinMax;

        public void Execute(int i)
        {
            int index = MapTools.RemapIndex(i, target.Resolution, current.Resolution);
            float value = heightMap[current.StartIndex + index];
            float normalized = JobExtensions.invLerp(CurrentMinMax.x, CurrentMinMax.y, value);
            heightMap[target.StartIndex + i] = lerp(TargetMinMax.x, TargetMinMax.y, normalized);
        }

        public static JobHandle ScheduleParallel(NativeArray<float> globalMap, 
            IndexAndResolution current,IndexAndResolution target,
            float2 targetMinMax, float2 currentMinMax, JobHandle dependency) => new RemapHeightJob()
        {
            heightMap = globalMap,
            current = current,
            target = target,
            TargetMinMax = targetMinMax,
            CurrentMinMax = currentMinMax,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct RemapCurveJob : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> heightMap;

        [NativeDisableContainerSafetyRestriction]
        [ReadOnly] SampledAnimationCurve curve;

        float2 CurrentMinMax;

        IndexAndResolution current;
        IndexAndResolution target;

        public void Execute(int i)
        {
            int index = MapTools.RemapIndex(i, target.Resolution, current.Resolution);
            float value = heightMap[current.StartIndex + index];
            //float normalized = JobExtensions.invLerp(CurrentMinMax.x, CurrentMinMax.y, value);
            heightMap[target.StartIndex + i] = curve.EvaluateLerp(value, CurrentMinMax.x, CurrentMinMax.y, CurrentMinMax.x, CurrentMinMax.y);//               lerp(CurrentMinMax.x, CurrentMinMax.y, saturate(curve.EvaluateLerp(value, CurrentMinMax.x, CurrentMinMax.y)));
        }

        public static JobHandle ScheduleParallel(NativeArray<float> globalMap, 
            IndexAndResolution current,IndexAndResolution target,
            SampledAnimationCurve curve, float2 currentMinMax, JobHandle dependency) => new RemapCurveJob()
        {
            heightMap = globalMap,
            current = current,
            target = target,
            curve = curve,
            CurrentMinMax = currentMinMax,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }
}