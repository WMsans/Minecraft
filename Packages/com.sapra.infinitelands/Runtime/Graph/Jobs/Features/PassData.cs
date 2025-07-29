using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace sapra.InfiniteLands
{    
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct PassDataJob : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> globalMap;
        IndexAndResolution target;
        IndexAndResolution original;

        public void Execute(int i)
        {
            int index = MapTools.RemapIndex(i, target.Resolution, original.Resolution);
            globalMap[target.StartIndex + i] = globalMap[original.StartIndex + index];
        }

        public static JobHandle ScheduleParallel(NativeArray<float> globalMap,
            IndexAndResolution target, IndexAndResolution Original, JobHandle dependency) => new PassDataJob()
        {
            target = target,
            original = Original,
            globalMap = globalMap,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }
}