using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct ClampJob : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> heightMap;

        IndexAndResolution target;
        IndexAndResolution current;
        float2 minMaxValue;

        public void Execute(int i)
        {
            int index = MapTools.RemapIndex(i, target.Resolution, current.Resolution);
            float value = heightMap[current.StartIndex + index];
            heightMap[target.StartIndex + i] = clamp(value, minMaxValue.x, minMaxValue.y);
        }

        public static JobHandle ScheduleParallel(NativeArray<float> globalMap, IndexAndResolution current, IndexAndResolution target,
            float2 minMaxValue, JobHandle dependency) => new ClampJob()
        {
            heightMap = globalMap,
            current = current,
            target = target,
            minMaxValue = minMaxValue,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }
}