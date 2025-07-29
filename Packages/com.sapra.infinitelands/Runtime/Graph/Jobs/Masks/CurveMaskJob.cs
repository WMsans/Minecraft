using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

namespace sapra.InfiniteLands{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct CurveMaskJob : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> heightMap;

        float2 CurrentMinMax;

        [NativeDisableContainerSafetyRestriction]
        [ReadOnly] SampledAnimationCurve animationCurve;
        
        IndexAndResolution target;
        IndexAndResolution current;

        public void Execute(int i)
        {
            int index = MapTools.RemapIndex(i, target.Resolution, current.Resolution);

            float value = heightMap[current.StartIndex + index];
            heightMap[target.StartIndex + i] = animationCurve.EvaluateLerp(value, CurrentMinMax.x, CurrentMinMax.y, 0, 1);
        }

        public static JobHandle ScheduleParallel(NativeArray<float> globalMap, 
            IndexAndResolution target, IndexAndResolution current,
            SampledAnimationCurve curve, float2 currentMinMax,
            JobHandle dependency) => new CurveMaskJob()
        {
            heightMap = globalMap,
            target = target,
            current = current,
            animationCurve = curve,
            CurrentMinMax = currentMinMax,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }
}