using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using static Unity.Mathematics.math;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using Unity.Mathematics;

namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    internal struct MJDensityCombine : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> globalArray;

        [ReadOnly] NativeArray<DataToManage> fromIndices;

        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> targetArray;
        IndexAndResolution targetIndex;

        public void Execute(int i)
        {
            float currentDensity = 0;
            for (int x = 0; x < fromIndices.Length; x++)
            {
                var indexData = fromIndices[x].indexData;
                var action = fromIndices[x].action;

                int index = MapTools.RemapIndex(i, targetIndex.Resolution, indexData.Resolution);
                var value = globalArray[indexData.StartIndex + index];
                switch (action)
                {
                    case ILoadAsset.Operation.Remove:
                        currentDensity -= value;
                        break;
                    default:
                        currentDensity += value;
                        break;
                }
            }
            targetArray[targetIndex.StartIndex + i] = saturate(currentDensity);
        }

        public static JobHandle ScheduleParallel(NativeArray<float> globalArray, NativeArray<float> targetArray,
            NativeArray<DataToManage> targetData, IndexAndResolution targetIndex, JobHandle dependency)
        {
            return new MJDensityCombine()
            {
                globalArray = globalArray,
                targetArray = targetArray,
                fromIndices = targetData,
                targetIndex = targetIndex,
            }.ScheduleParallel(targetIndex.Length, targetIndex.Resolution, dependency);
        }
    }
}