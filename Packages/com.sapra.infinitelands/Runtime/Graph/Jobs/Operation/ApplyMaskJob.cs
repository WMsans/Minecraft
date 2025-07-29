using System.Numerics;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands{
    public interface MaskMultiplyMode{
        public float GetValue(float2 minMax, float value, float mask);
    }
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct ApplyMaskJob<T> : IJobFor where T: MaskMultiplyMode
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> heightMap;

        float2 CurrentMinMax;

        IndexAndResolution mask;
        IndexAndResolution current;

        IndexAndResolution target;
        public void Execute(int i)
        {
            int index = MapTools.RemapIndex(i, target.Resolution, current.Resolution);
            int indexmask = MapTools.RemapIndex(i, target.Resolution, mask.Resolution);

            float value = heightMap[current.StartIndex + index];
            float maskValue = saturate(heightMap[mask.StartIndex + indexmask]);

            heightMap[target.StartIndex + i] = default(T).GetValue(CurrentMinMax, value,maskValue);
        }


        public static JobHandle ScheduleParallel(NativeArray<float> globalMap, 
            IndexAndResolution mask, IndexAndResolution current,
            IndexAndResolution target, float2 currentMinMax, JobHandle dependency) => new ApplyMaskJob<T>()
        {
            heightMap = globalMap,
            mask = mask,
            target = target,
            current = current,
            CurrentMinMax = currentMinMax,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }
}