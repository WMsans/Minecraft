using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands
{
    
    #region WithoutWeights
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct AddJobWeights : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> globalArray;

        [ReadOnly] NativeArray<HeightData> indices;
        [ReadOnly] NativeArray<IndexAndResolution> targetWeights;

        IndexAndResolution target;

        int arrayCount;

        //Moved out into diferent jobs!
        public void Execute(int i)
        {
            float sum = 0;
            for (int x = 0; x < arrayCount; x++)
            {
                var indexAndRe = indices[x].indexData;
                int locator = MapTools.RemapIndex(i, target.Resolution, indexAndRe.Resolution);
                sum += globalArray[indexAndRe.StartIndex + locator];

                var targetWeight = targetWeights[x];
                int weightLocator = MapTools.RemapIndex(i, target.Resolution, targetWeight.Resolution);
                globalArray[targetWeight.StartIndex + weightLocator] = 1;

            }

            globalArray[target.StartIndex + i] = sum;
        }

        public static JobHandle ScheduleParallel(NativeArray<float> input, NativeArray<HeightData> indices, IndexAndResolution target, 
            NativeArray<IndexAndResolution> targetWeights,
            int arrayCount, JobHandle dependency) => new AddJobWeights()
        {
            globalArray = input,
            target = target,
            targetWeights = targetWeights,
            indices = indices,
            arrayCount = arrayCount,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct MaxJobWeights : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> globalArray;

        [ReadOnly] NativeArray<HeightData> indices;
        [ReadOnly] NativeArray<IndexAndResolution> targetWeights;

        IndexAndResolution target;

        int arrayCount;

        //Moved out into diferent jobs!
        public void Execute(int i)
        {
            float sum = float.MinValue;
            for (int x = 0; x < arrayCount; x++)
            {
                var indexAndRe = indices[x].indexData;
                int locator = MapTools.RemapIndex(i, target.Resolution, indexAndRe.Resolution);
                sum = max(globalArray[indexAndRe.StartIndex + locator], sum);
            }
            JobExtensions.ApplyFoundValue(i, sum, arrayCount, indices, globalArray, targetWeights, target);        
            globalArray[target.StartIndex + i] = sum;
        }

        public static JobHandle ScheduleParallel(NativeArray<float> input, NativeArray<HeightData> indices,IndexAndResolution target,
            NativeArray<IndexAndResolution> targetWeights, int arrayCount, JobHandle dependency) => new MaxJobWeights()
        {
            globalArray = input,
            target = target,
            targetWeights = targetWeights,
            indices = indices,
            arrayCount = arrayCount,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct MinJobWeights : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> globalArray;

        [ReadOnly] NativeArray<HeightData> indices;
        [ReadOnly] NativeArray<IndexAndResolution> targetWeights;

        IndexAndResolution target;

        int arrayCount;

        //Moved out into diferent jobs!
        public void Execute(int i)
        {
            float sum = float.MaxValue;
            for (int x = 0; x < arrayCount; x++)
            {
                var indexAndRe = indices[x].indexData;
                int locator = MapTools.RemapIndex(i, target.Resolution, indexAndRe.Resolution);
                sum = min(globalArray[indexAndRe.StartIndex + locator], sum);
            }
            JobExtensions.ApplyFoundValue(i, sum, arrayCount, indices, globalArray, targetWeights, target);        
            globalArray[target.StartIndex + i] = sum;
        }

        public static JobHandle ScheduleParallel(NativeArray<float> input, NativeArray<HeightData> indices,IndexAndResolution target,
            NativeArray<IndexAndResolution> targetWeights, int arrayCount, JobHandle dependency) => new MinJobWeights()
        {
            globalArray = input,
            target = target,
            targetWeights = targetWeights,
            indices = indices,
            arrayCount = arrayCount,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct HeightBlendWeights : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> globalArray;

        [ReadOnly] NativeArray<HeightData> indices;
        [ReadOnly] NativeArray<IndexAndResolution> targetWeights;

        IndexAndResolution target;
        public float BlendFactor;

        int arrayCount;
        float2 MinMax;

        //Moved out into diferent jobs!
        public void Execute(int i)
        {
            float maxValue = 0;
            NativeArray<float> remapedWeights = new NativeArray<float>(arrayCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int x = 0; x < arrayCount; x++)
            {
                var indexAndRe = indices[x].indexData;
                int locator = MapTools.RemapIndex(i, target.Resolution, indexAndRe.Resolution);
                remapedWeights[x] = JobExtensions.invLerp(MinMax.x, MinMax.y, globalArray[indexAndRe.StartIndex + locator]);
                maxValue = max(maxValue, remapedWeights[x]);
            }

            maxValue -= BlendFactor / (MinMax.y - MinMax.x);

            float totalSum = 0;
            for (int x = 0; x < arrayCount; x++)
            {
                remapedWeights[x] = max(remapedWeights[x] - maxValue, 0);
                totalSum += remapedWeights[x];
            }

            float sum = 0;
            for (int x = 0; x < arrayCount; x++)
            {
                var indexAndRe = indices[x].indexData;
                var density = remapedWeights[x] / totalSum;
                int locator = MapTools.RemapIndex(i, target.Resolution, indexAndRe.Resolution);
                sum += globalArray[indexAndRe.StartIndex + locator] * density;

                var targetWeight = targetWeights[x];
                int weightLocator = MapTools.RemapIndex(i, target.Resolution, targetWeight.Resolution);
                globalArray[targetWeight.StartIndex + weightLocator] = density;
            }

            globalArray[target.StartIndex + i] = sum;
        }

        public static JobHandle ScheduleParallel(NativeArray<float> input, NativeArray<HeightData> indices,float2 minMax, float BlendFactor,
            NativeArray<IndexAndResolution> targetWeights, IndexAndResolution target, int arrayCount, JobHandle dependency) => new HeightBlendWeights()
        {
            globalArray = input,
            target = target,
            indices = indices,
            targetWeights = targetWeights,
            arrayCount = arrayCount,
            MinMax = minMax,
            BlendFactor = BlendFactor,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct NormalizedMultiplyJobWeights : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> globalArray;

        [ReadOnly] NativeArray<HeightData> indices;
        [ReadOnly] NativeArray<IndexAndResolution> targetWeights;

        IndexAndResolution target;

        int arrayCount;

        //Moved out into diferent jobs!
        public void Execute(int i)
        {
            float mult = 1;
            for (int x = 0; x < arrayCount; x++)
            {
                var indexAndRe = indices[x].indexData;
                var minMax = indices[x].minMaxValue;
                int locator = MapTools.RemapIndex(i, target.Resolution, indexAndRe.Resolution);
                mult *= JobExtensions.invLerp(minMax.x, minMax.y,globalArray[indexAndRe.StartIndex + locator]);

                var targetWeight = targetWeights[x];
                int weightLocator = MapTools.RemapIndex(i, target.Resolution, targetWeight.Resolution);
                globalArray[targetWeight.StartIndex + weightLocator] = 1;
            }

            globalArray[target.StartIndex + i] = mult;
        }

        public static JobHandle ScheduleParallel(NativeArray<float> input, NativeArray<HeightData> indices, 
            NativeArray<IndexAndResolution> targetWeights, IndexAndResolution target,
            int arrayCount, JobHandle dependency) => new NormalizedMultiplyJobWeights()
        {
            globalArray = input,
            target = target,
            targetWeights = targetWeights,
            indices = indices,
            arrayCount = arrayCount,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }
    #endregion
    
}