using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands
{
    public static class FoldIt{
        public static float4 Folded(float4 value, float2 MinMax, float heightLine){
            float4 normalized = abs(JobExtensions.invLerp(MinMax.x, MinMax.y, value)-heightLine);
            return lerp(MinMax.x, MinMax.y, normalized);
        }

        public static float Folded(float value, float2 MinMax, float heightLine){
            float normalized = abs(JobExtensions.invLerp(MinMax.x, MinMax.y, value)-heightLine);
            return lerp(MinMax.x, MinMax.y, normalized);
        }
    }
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct FoldJobBottom : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> heightMap;

        IndexAndResolution current;
        IndexAndResolution target;
        float heightLine;
        float2 MinMax;

        public void Execute(int i)
        {
            int index = MapTools.RemapIndex(i, target.Resolution, current.Resolution);
            float value = heightMap[current.StartIndex + index] ;
            heightMap[target.StartIndex + i] = FoldIt.Folded(value, MinMax, heightLine);
        }

        public static JobHandle ScheduleParallel(NativeArray<float> globalMap, IndexAndResolution current, IndexAndResolution target,
            float heightLine, float2 MinMax,
            JobHandle dependency) => new FoldJobBottom()
        {
            heightMap = globalMap,
            current = current,
            target = target,
            MinMax = MinMax,
            heightLine = heightLine,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct FoldJobTop : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> heightMap;

        IndexAndResolution current;
        IndexAndResolution target;
        float heightLine;
        float2 MinMax;


        public void Execute(int i)
        {
            int index = MapTools.RemapIndex(i, target.Resolution, current.Resolution);
            float value = heightMap[current.StartIndex + index];
            float NewHeightLine = .5f-abs(heightLine-.5f);
            float displacement = MinMax.y-MinMax.x;
            heightMap[target.StartIndex + i] = FoldIt.Folded(value, MinMax, heightLine)+displacement*NewHeightLine;
        }

        public static JobHandle ScheduleParallel(NativeArray<float> globalMap, IndexAndResolution current, IndexAndResolution target,
            float heightLine, float2 MinMax, 
            JobHandle dependency) => new FoldJobTop()
        {
            heightMap = globalMap,
            current = current,
            target = target,
            heightLine = heightLine,
            MinMax = MinMax,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }
}