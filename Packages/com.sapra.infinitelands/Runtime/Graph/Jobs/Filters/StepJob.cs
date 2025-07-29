using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    internal static class StepJobGlobal
    {
        public static float4 Execute(float4 value, int steps, float Flatness, float Stepness, float2 CurrentMinMax,
            NativeArray<float> levelHeight)
        {
            float4 normalized = JobExtensions.invLerp(CurrentMinMax.x, CurrentMinMax.y, value);
            return lerp(value, getValue(normalized * (steps - 1), .5f-Stepness, levelHeight), Flatness);
        }

        public static float Execute(float value, int steps, float Flatness, float Stepness, float2 CurrentMinMax,
            NativeArray<float> levelHeight)
        {
            float normalized = JobExtensions.invLerp(CurrentMinMax.x, CurrentMinMax.y, value);
            return lerp(value, getValue(normalized * (steps - 1), .5f-Stepness, levelHeight), Flatness);
        }

        static float getValue(float value, float Stepness, NativeArray<float> levelHeight)
        {
            float smoothed = smoothstep(.5f - Stepness, .5f + Stepness, frac(value));
            int floorIndex = (int)floor(value);
            int ceilIndex = (int)ceil(value);

            float lowerValue = levelHeight[floorIndex];
            float higherValue = levelHeight[ceilIndex];
            return math.lerp(lowerValue, higherValue, smoothed);
        }

        static float4 getValue(float4 value, float Stepness, NativeArray<float> levelHeight)
        {
            float4 smoothed = smoothstep(.5f - Stepness, .5f + Stepness, frac(value));
            int4 floorIndex = (int4)floor(value);
            int4 ceilIndex = (int4)ceil(value);

            float4 lowerValue = 0f;
            lowerValue.x = levelHeight[floorIndex.x];
            lowerValue.y = levelHeight[floorIndex.y];
            lowerValue.z = levelHeight[floorIndex.z];
            lowerValue.w = levelHeight[floorIndex.w];

            float4 higherValue = 0f;
            higherValue.x = levelHeight[ceilIndex.x];
            higherValue.y = levelHeight[ceilIndex.y];
            higherValue.z = levelHeight[ceilIndex.z];
            higherValue.w = levelHeight[ceilIndex.w];
            return math.lerp(lowerValue, higherValue, smoothed);
        }
    }
    
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct StepJobMasked : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> heightMap;

        IndexAndResolution current;
        IndexAndResolution target;
        IndexAndResolution mask;

        float2 CurrentMinMax;
        int steps;

        float Stepness;
        float Flatness;
        [ReadOnly] NativeArray<float> levelHeight;

        public void Execute(int i)
        {
            int currentIndex = MapTools.RemapIndex(i, target.Resolution, current.Resolution);
            int maskIndex = MapTools.RemapIndex(i, target.Resolution, mask.Resolution);
            float value = heightMap[current.StartIndex + currentIndex];
            float maskValue = heightMap[mask.StartIndex + maskIndex];
            heightMap[target.StartIndex + i] = lerp(value,
                StepJobGlobal.Execute(value, steps, Flatness, Stepness, CurrentMinMax, levelHeight),maskValue);
        }

        public static JobHandle ScheduleParallel(NativeArray<float> globalMap, 
            IndexAndResolution current, IndexAndResolution target, IndexAndResolution mask, 
            int steps, float Stepness, float Flatness, float2 currentMinMax,
            NativeArray<float> levelHeight, JobHandle dependency) => new StepJobMasked()
        {
            heightMap = globalMap,
            current = current,
            target = target,
            CurrentMinMax = currentMinMax,
            steps = steps,
            levelHeight = levelHeight,
            mask = mask,
            Stepness = Stepness * .5f,
            Flatness = Flatness
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct StepJob : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> heightMap;

        IndexAndResolution current;
        IndexAndResolution target;
        float2 CurrentMinMax;
        int steps;

        float Stepness;
        float Flatness;
        [ReadOnly] NativeArray<float> levelHeight;

        public void Execute(int i)
        {
            int currentIndex = MapTools.RemapIndex(i, target.Resolution, current.Resolution);
            float value = heightMap[current.StartIndex + currentIndex];
            heightMap[target.StartIndex + i] = StepJobGlobal.Execute(value, steps, Flatness, Stepness, CurrentMinMax, levelHeight);
        }

        public static JobHandle ScheduleParallel(NativeArray<float> globalMap, IndexAndResolution current, IndexAndResolution target,
            int steps, float Stepness, float Flatness, float2 currentMinMax, NativeArray<float> levelHeight, JobHandle dependency) => new StepJob()
        {
            heightMap = globalMap,
            current = current,
            target = target,
            CurrentMinMax = currentMinMax,
            steps = steps,
            levelHeight = levelHeight,
            Stepness = Stepness * .5f,
            Flatness = Flatness
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }
}