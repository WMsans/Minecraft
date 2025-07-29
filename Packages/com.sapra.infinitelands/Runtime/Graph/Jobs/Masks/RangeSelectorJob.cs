using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct RangeSelectorJob : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> heightMap;

        public float2 MinMaxHeight;
        public float HeightBlendFactor;
        IndexAndResolution target;
        IndexAndResolution current;

        public void Execute(int i)
        {
            int index = MapTools.RemapIndex(i, target.Resolution, current.Resolution);
            float verticalPlace = heightMap[current.StartIndex + index];
            float height;
            if (HeightBlendFactor > 0)
            {
                height = smoothstep(MinMaxHeight.x - HeightBlendFactor, MinMaxHeight.x, verticalPlace) *
                        (1 - smoothstep(MinMaxHeight.y, MinMaxHeight.y + HeightBlendFactor, verticalPlace));
            }
            else
            {
                height = step(MinMaxHeight.x, verticalPlace) *
                        (1 - step(MinMaxHeight.y, verticalPlace));
            }

            heightMap[target.StartIndex + i] = height;
        }

        public static JobHandle ScheduleParallel(NativeArray<float> globalMap, IndexAndResolution current, IndexAndResolution target,
            float2 MinMaxHeight, float HeightBlendFactor,
            JobHandle dependency) => new RangeSelectorJob()
        {
            heightMap = globalMap,
            current = current,
            target = target,
            MinMaxHeight = MinMaxHeight,
            HeightBlendFactor = HeightBlendFactor,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct NormalizeRangeSelectorJob : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> heightMap;

        public float2 MinMaxHeight;
        public float2 InputRange;
        public float HeightBlendFactor;

        IndexAndResolution target;
        IndexAndResolution current;

        public void Execute(int i)
        {
            int index = MapTools.RemapIndex(i, target.Resolution, current.Resolution);
            float verticalPlace = heightMap[current.StartIndex + index];
            float normalized = JobExtensions.invLerp(InputRange.x, InputRange.y, verticalPlace);
            float height;
            if (HeightBlendFactor > 0)
            {
                height = smoothstep(MinMaxHeight.x - HeightBlendFactor, MinMaxHeight.x, normalized) *
                        (1 - smoothstep(MinMaxHeight.y, MinMaxHeight.y + HeightBlendFactor, normalized));
            }
            else
            {
                height = step(MinMaxHeight.x, normalized) *
                        (1 - step(MinMaxHeight.y, normalized));
            }

            heightMap[target.StartIndex + i] = height;
        }

        public static JobHandle ScheduleParallel(NativeArray<float> globalMap, IndexAndResolution current, IndexAndResolution target,
            float2 MinMaxHeight, float HeightBlendFactor, float2 inputRange,
            JobHandle dependency) => new NormalizeRangeSelectorJob()
        {
            heightMap = globalMap,
            current = current,
            target = target,
            MinMaxHeight = MinMaxHeight,
            HeightBlendFactor = HeightBlendFactor,
            InputRange = inputRange,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }    
}