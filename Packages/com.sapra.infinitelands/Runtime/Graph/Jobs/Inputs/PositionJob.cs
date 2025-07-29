using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using static Unity.Mathematics.math;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    internal struct PositionJob : IJobFor
    {
        [ReadOnly] NativeArray<float3x4> vertices;

        [NativeDisableContainerSafetyRestriction] [WriteOnly]
        NativeSlice<float4> globalMap;
        
        float3 offset;
        bool XValue;

        public void Execute(int i)
        {
            float4x3 pt = transpose(vertices[i]);
            pt.c0 += offset.x;
            pt.c2 += offset.z;

            globalMap[i] = XValue ? pt.c0 : pt.c2;
        }

        public static JobHandle ScheduleParallel(NativeArray<float3x4> vertices, NativeSlice<float4> globalMap,
            float3 offset, bool XValue,
            IndexAndResolution target, JobHandle dependency) => new PositionJob()
        {
            globalMap = globalMap,
            vertices = vertices,
            offset = offset,
            XValue = XValue,
        }.ScheduleParallel(vertices.Length, target.Resolution, dependency);
    }
}