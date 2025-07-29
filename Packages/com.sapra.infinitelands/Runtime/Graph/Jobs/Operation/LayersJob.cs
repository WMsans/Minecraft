using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands
{

    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct LayersJob : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> globalArray;

        [ReadOnly] NativeArray<HeightData> indices;
        [ReadOnly] NativeArray<IndexAndResolution> targetWeights;

        int arrayCount;
        IndexAndResolution reference;
        public void Execute(int i)
        {
            float currentSum = 0;
            for (int x = arrayCount - 1; x >= 0; x--)
            {
                var indexAndRe = indices[x].indexData;
                var targetWeight = targetWeights[x];
                int locator = MapTools.RemapIndex(i, reference.Resolution, indexAndRe.Resolution);
                var weight = globalArray[indexAndRe.StartIndex + locator];
                float preSum = currentSum;

                currentSum = saturate(currentSum + weight);
                float layerWeight = currentSum - preSum;
                int weightLocator = MapTools.RemapIndex(i, reference.Resolution, targetWeight.Resolution);
                globalArray[targetWeight.StartIndex + weightLocator] = layerWeight;
            }
        }

        public static JobHandle ScheduleParallel(NativeArray<float> input, NativeArray<HeightData> indices, IndexAndResolution reference,
            NativeArray<IndexAndResolution> targetWeights, int arrayCount, JobHandle dependency) => new LayersJob()
        {
            globalArray = input,
            targetWeights = targetWeights,
            indices = indices,
            reference = reference,
            arrayCount = arrayCount,
        }.ScheduleParallel(reference.Length, reference.Resolution, dependency);
    }    
}