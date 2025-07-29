using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct AddBiomeJob : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> globalArray;

        [ReadOnly] NativeArray<IndexAndResolution> indices;
        [ReadOnly] NativeArray<IndexAndResolution> masksIndices;
        [ReadOnly] NativeArray<IndexAndResolution> newMasks;

        float2 MinMaxHeightValue;

        [ReadOnly] IndexAndResolution target;
        int biomeCount;

        //Moved out into diferent jobs!
        public void Execute(int i)
        {
            JobExtensions.normalizeArray(globalArray, masksIndices, newMasks, target, biomeCount, i);

            float currentHeight = 0;
            for (int m = 0; m < biomeCount; m++)
            {
                int heightIndex = MapTools.RemapIndex(i, target.Resolution, indices[m].Resolution);
                int maskIndex = MapTools.RemapIndex(i, target.Resolution, masksIndices[m].Resolution);

                float heightValue = globalArray[indices[m].StartIndex + heightIndex];
                float maskValue = globalArray[masksIndices[m].StartIndex + maskIndex];
                currentHeight += JobExtensions.invLerp(MinMaxHeightValue.x, MinMaxHeightValue.y, heightValue) * maskValue;
            }

            globalArray[target.StartIndex + i] = lerp(MinMaxHeightValue.x, MinMaxHeightValue.y, currentHeight);
        }

        public static JobHandle ScheduleParallel(NativeArray<float> input, NativeArray<IndexAndResolution> indices,
            NativeArray<IndexAndResolution> masksIndices, NativeArray<IndexAndResolution> masksTargets, float2 MinMaxHeightValue,
            IndexAndResolution target, int biomeCount, 
            JobHandle dependency) => new AddBiomeJob()
        {
            globalArray = input,
            target = target,
            indices = indices,
            newMasks = masksTargets,
            masksIndices = masksIndices,
            MinMaxHeightValue = MinMaxHeightValue,
            biomeCount = biomeCount,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct MaxHeightBiomeJob : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> globalArray;

        [ReadOnly] NativeArray<IndexAndResolution> indices;
        [ReadOnly] NativeArray<IndexAndResolution> masksIndices;
        [ReadOnly] NativeArray<IndexAndResolution> masksTargets;

        float2 MinMaxHeightValue;

        [ReadOnly] IndexAndResolution target;
        int biomeCount;

        float HeightBlending;

        //Moved out into diferent jobs!
        public void Execute(int i)
        {
            JobExtensions.normalizeArray(globalArray, masksIndices, masksTargets, target, biomeCount, i);

            NativeArray<float> currentHeight = new NativeArray<float>(biomeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            float maxHeight = 0;
            for (int m = 0; m < biomeCount; m++)
            {
                int heightIndex = MapTools.RemapIndex(i, target.Resolution, indices[m].Resolution);
                int maskIndex = MapTools.RemapIndex(i, target.Resolution, masksIndices[m].Resolution);

                float heightValue = globalArray[indices[m].StartIndex + heightIndex];
                float maskValue = globalArray[masksIndices[m].StartIndex  + maskIndex];
                currentHeight[m] = JobExtensions.invLerp(MinMaxHeightValue.x, MinMaxHeightValue.y, heightValue) * maskValue;
                maxHeight = max(maxHeight, currentHeight[m]);
            }

            maxHeight -= HeightBlending / (MinMaxHeightValue.y - MinMaxHeightValue.x);

            float totalSum = 0;
            for (int x = 0; x < biomeCount; x++)
            {
                currentHeight[x] = max(currentHeight[x] - maxHeight, 0);
                totalSum += currentHeight[x];
            }

            float sum = 0;
            for (int x = 0; x < biomeCount; x++)
            {
                int heightIndex = MapTools.RemapIndex(i, target.Resolution, indices[x].Resolution);
                float heightValue = globalArray[indices[x].StartIndex + heightIndex];
                sum += heightValue * currentHeight[x] / totalSum;
                
                int maskIndex = MapTools.RemapIndex(i, target.Resolution, masksTargets[x].Resolution);
                globalArray[masksTargets[x].StartIndex + maskIndex] = currentHeight[x] / totalSum;
            }

            globalArray[target.StartIndex + i] = sum;
        }

        public static JobHandle ScheduleParallel(NativeArray<float> input, NativeArray<IndexAndResolution> indices,
            NativeArray<IndexAndResolution> masksIndices, NativeArray<IndexAndResolution> masksTargets, float2 MinMaxHeightValue,
            float HeightBlending,
            IndexAndResolution target, int biomeCount, JobHandle dependency) => new MaxHeightBiomeJob()
        {
            globalArray = input,
            target = target,
            indices = indices,
            masksTargets = masksTargets,
            masksIndices = masksIndices,
            MinMaxHeightValue = MinMaxHeightValue,
            biomeCount = biomeCount,
            HeightBlending = HeightBlending
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct MaxWeightBiomeJob : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> globalArray;

        [ReadOnly] NativeArray<IndexAndResolution> indices;
        [ReadOnly] NativeArray<IndexAndResolution> masksIndices;
        [ReadOnly] NativeArray<IndexAndResolution> masksTargets;

        [ReadOnly] IndexAndResolution target;
        int biomeCount;

        float HeightBlending;

        //Moved out into diferent jobs!
        public void Execute(int i)
        {
            JobExtensions.normalizeArray(globalArray, masksIndices, masksTargets, target, biomeCount, i);

            NativeArray<float> currentHeight = new NativeArray<float>(biomeCount, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            float maxHeight = 0;
            for (int m = 0; m < biomeCount; m++)
            {
                int maskIndex = MapTools.RemapIndex(i, target.Resolution, masksIndices[m].Resolution);
                currentHeight[m] = globalArray[masksIndices[m].StartIndex + maskIndex];
                maxHeight = max(maxHeight, currentHeight[m]);
            }

            maxHeight -= HeightBlending;

            float totalSum = 0;
            for (int x = 0; x < biomeCount; x++)
            {
                currentHeight[x] = max(currentHeight[x] - maxHeight, 0);
                totalSum += currentHeight[x];
            }

            float sum = 0;
            for (int x = 0; x < biomeCount; x++)
            {
                int heightIndex = MapTools.RemapIndex(i, target.Resolution, indices[x].Resolution);
                float heightValue = globalArray[indices[x].StartIndex + heightIndex];
                sum += heightValue * currentHeight[x] / totalSum;

                int maskIndex = MapTools.RemapIndex(i, target.Resolution, masksTargets[x].Resolution);
                globalArray[masksTargets[x].StartIndex + maskIndex] = currentHeight[x] / totalSum;
            }

            globalArray[target.StartIndex + i] = sum;
        }

        public static JobHandle ScheduleParallel(NativeArray<float> input, NativeArray<IndexAndResolution> indices,
            NativeArray<IndexAndResolution> masksIndices, NativeArray<IndexAndResolution> masksTargets,
            float HeightBlending,
            IndexAndResolution target, int biomeCount, JobHandle dependency) => new MaxWeightBiomeJob()
        {
            globalArray = input,
            target = target,
            indices = indices,
            masksTargets = masksTargets,
            masksIndices = masksIndices,
            biomeCount = biomeCount,
            HeightBlending = HeightBlending
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }
}