using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using static Unity.Mathematics.math;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using static sapra.InfiniteLands.Noise;

namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    internal struct RandomJob : IJobFor
    {
        [ReadOnly] NativeArray<float3x4> vertices;

        [NativeDisableContainerSafetyRestriction] [WriteOnly]
        NativeSlice<float4> globalMap;
                
        float2 FromTo;
        float3 offset;
        float frequency;
        int seed;

        public void Execute(int i)
        {
            float4x3 pt = transpose(vertices[i]);
            pt.c0 += offset.x;
            pt.c2 += offset.z;

            var hash = SmallXXHash4.Seed(seed);
            float4 value = default(Random<Value>).GetNoise4(pt, hash, frequency, 0);

            globalMap[i] = lerp(FromTo.x, FromTo.y, value);
        }
       
        public static JobHandle ScheduleParallel(NativeArray<float3x4> vertices, NativeSlice<float4> globalMap,
            float2 FromTo, float3 offset, float size, 
            IndexAndResolution target, JobHandle dependency) => new RandomJob()
        {
            globalMap = globalMap,
            FromTo = FromTo,
            vertices = vertices,
            offset = offset,
            frequency = 1f / size,
        }.ScheduleParallel(vertices.Length, target.Resolution, dependency);
    }
}