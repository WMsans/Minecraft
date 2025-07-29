using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using Unity.Collections.LowLevel.Unsafe;
using static sapra.InfiniteLands.Noise;


namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    internal struct NoiseJob<T> : IJobFor where T : struct, INoise
    {
        NoiseSettings settings;

        [ReadOnly] NativeArray<float3x4> vertices;
        [NativeDisableContainerSafetyRestriction] [WriteOnly]
        NativeSlice<float4> globalMap;


        float3 offset;
        int seed;

        public void Execute(int i)
        {
            float4x3 pt = transpose(vertices[i]);
            pt.c0 += offset.x;
            pt.c2 += offset.z;
            float4 result = GetFractalNoise<T>(pt, seed, settings);
            globalMap[i] = result;
        }

        public static JobHandle ScheduleParallel(NativeArray<float3x4> vertices, NativeSlice<float4> globalMap,
            NoiseSettings settings, float3 offset,
            IndexAndResolution target,
            int seed, JobHandle dependency) => new NoiseJob<T>()
        {
            offset = offset,
            vertices = vertices,
            globalMap = globalMap,
            settings = settings,
            seed = seed,
        }.ScheduleParallel(vertices.Length, target.Resolution/4, dependency);
    }
}