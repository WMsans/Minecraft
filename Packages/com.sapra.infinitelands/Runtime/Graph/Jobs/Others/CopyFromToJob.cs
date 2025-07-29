using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands{        
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct CopyToFrom : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> targetGlobalMap;

        [NativeDisableContainerSafetyRestriction] [ReadOnly]
        NativeArray<float> originalGlobalMap;

        IndexAndResolution original;
        IndexAndResolution target;
        float ScaleFactor;
        public void Execute(int i)
        {
            int index = MapTools.RemapIndex(i, target.Resolution, original.Resolution);
            targetGlobalMap[target.StartIndex + i] = originalGlobalMap[original.StartIndex + index]*ScaleFactor;
        }

        public static JobHandle ScheduleParallel(NativeArray<float> targetGlobalMap, NativeArray<float> originalGlobalMap,
            IndexAndResolution target, IndexAndResolution original, JobHandle dependency, float ScaleFactor = 1) => new CopyToFrom()
            {
                targetGlobalMap = targetGlobalMap,
                originalGlobalMap = originalGlobalMap,
                original = original,
                target = target,
                ScaleFactor = ScaleFactor,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }
}