using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;


namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct GetMapBoundaries : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> MinMaxHeight;

        [ReadOnly] NativeArray<float> globalArray;
        IndexAndResolution origin;
        int resolution;

        public unsafe void Execute(int i)
        {
            int index = MapTools.RemapIndex(i, resolution, origin.Resolution);
            float current = globalArray[origin.StartIndex + index];

            JobExtensions.InterlockedMin(ref ((float*)MinMaxHeight.GetUnsafePtr())[0], current);
            JobExtensions.InterlockedMax(ref ((float*)MinMaxHeight.GetUnsafePtr())[1], current);
        }


        public static JobHandle ScheduleParallel(NativeArray<float> MinMaxHeight,
            NativeArray<float> globalArray,IndexAndResolution origin,
            int length, int resolution, JobHandle dependency
        ) => new GetMapBoundaries()
        {
            MinMaxHeight = MinMaxHeight,
            globalArray = globalArray,
            origin = origin,
            resolution = resolution,
        }.ScheduleParallel(length, resolution, dependency);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct GetMapMin : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> MinMaxHeight;

        [ReadOnly] NativeArray<float> globalArray;
        IndexAndResolution origin;
        int resolution;

        public unsafe void Execute(int i)
        {
            int index = MapTools.RemapIndex(i, resolution, origin.Resolution);
            float current = globalArray[origin.StartIndex + index];

            JobExtensions.InterlockedMin(ref ((float*)MinMaxHeight.GetUnsafePtr())[0], current);
        }


        public static JobHandle ScheduleParallel(NativeArray<float> MinMaxHeight,
            NativeArray<float> globalArray,IndexAndResolution origin,
            int length, int resolution, JobHandle dependency
        ) => new GetMapMin()
        {
            MinMaxHeight = MinMaxHeight,
            globalArray = globalArray,
            origin = origin,
            resolution = resolution,
        }.ScheduleParallel(length, resolution, dependency);
    }

     [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct CheckTreshold : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<int> Flag;

        [ReadOnly] NativeArray<float> globalArray;
        IndexAndResolution origin;
        int resolution;
        float threshold;

        public unsafe void Execute(int i)
        {
            if (Flag[0] == 1) return;

            int index = MapTools.RemapIndex(i, resolution, origin.Resolution);
            float current = globalArray[origin.StartIndex + index];
            if (current > threshold)
            {
                // Set cancel flag so other threads will exit early
                Flag[0] = 1;
            }
        }


        public static JobHandle ScheduleParallel(NativeArray<int> Flag,
            NativeArray<float> globalArray, IndexAndResolution origin,
            int length, int resolution, float threshold, JobHandle dependency
        ) => new CheckTreshold()
        {
            Flag = Flag,
            globalArray = globalArray,
            origin = origin,
            resolution = resolution,
            threshold = threshold,
        }.ScheduleParallel(length, resolution, dependency);
    }
}