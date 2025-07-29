using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct DivideJob : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> heightMap;

        IndexAndResolution dividend;
        IndexAndResolution divisor;

        IndexAndResolution target;

        public void Execute(int i)
        {
            int indexDividend = MapTools.RemapIndex(i, target.Resolution, dividend.Resolution);
            int indexDivisor = MapTools.RemapIndex(i, target.Resolution, divisor.Resolution);

            float valueDividend = heightMap[dividend.StartIndex + indexDividend];
            float valueDivisor = heightMap[divisor.StartIndex + indexDivisor];
                        heightMap[target.StartIndex + i] = valueDivisor != 0 ? valueDividend/valueDivisor : 0;
        }

        public static JobHandle ScheduleParallel(NativeArray<float> globalMap, 
            IndexAndResolution dividend, IndexAndResolution divisor, IndexAndResolution target, 
            JobHandle dependency) => new DivideJob()
        {
            heightMap = globalMap,
            target = target,
            dividend = dividend,
            divisor = divisor,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }
}