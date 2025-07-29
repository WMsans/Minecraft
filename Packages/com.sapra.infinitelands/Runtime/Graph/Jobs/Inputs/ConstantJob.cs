using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    internal struct ConstantJob : IJobFor
    {
        float Value;

        [NativeDisableContainerSafetyRestriction] [WriteOnly]
        NativeSlice<float4> globalMap;

        public void Execute(int i)
        {
            globalMap[i] = Value;
        }

        public static JobHandle ScheduleParallel(NativeSlice<float4> globalMap, 
            float Value, IndexAndResolution target,
            JobHandle dependency) => new ConstantJob()
        {
            globalMap = globalMap,
            Value = Value,
        }.ScheduleParallel(globalMap.Length, target.Resolution/4, dependency);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    internal struct ConstantJobSlow : IJobFor
    {
        float Value;

        [NativeDisableContainerSafetyRestriction] [WriteOnly]
        NativeArray<float> globalMap;

        IndexAndResolution target;
        
        public void Execute(int i)
        {
            globalMap[i + target.StartIndex] = Value;
        }

        public static JobHandle ScheduleParallel(NativeArray<float> globalMap, 
            float Value, IndexAndResolution target,
            JobHandle dependency) => new ConstantJobSlow()
        {
            globalMap = globalMap,
            target = target,
            Value = Value,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }
}