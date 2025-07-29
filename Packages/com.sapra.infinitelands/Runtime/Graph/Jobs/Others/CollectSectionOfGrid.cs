using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct CollectSectionOfGrid : IJobFor
    {
        [ReadOnly] NativeArray<float3> allPoints;
        public int OriginalResolution;
        [WriteOnly] NativeArray<float3> targetPoints;
        public int TargetResolution;

        public void Execute(int index)
        {
            int mappedIndex = MapTools.RemapIndex(index, TargetResolution, OriginalResolution);
            targetPoints[index] = allPoints[mappedIndex];
        }

         public static JobHandle ScheduleParallel(NativeArray<float3> allPoints, int OriginalResolution,
            NativeArray<float3> targetPoints, int TargetResolution,
            JobHandle dependency
        ) => new CollectSectionOfGrid()
        {
            allPoints = allPoints,
            OriginalResolution = OriginalResolution,
            TargetResolution = TargetResolution,
            targetPoints = targetPoints,
        }.ScheduleParallel(targetPoints.Length, TargetResolution, dependency);
    }
}