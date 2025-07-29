using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;
using Unity.Collections.LowLevel.Unsafe;

namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct MTJTextureSingleChannel : IJobFor
    {
        [ReadOnly] NativeArray<float> globalArray;

        [NativeDisableContainerSafetyRestriction]
        NativeArray<Color32> normalColor;

        int ArrayLength;
        int target;

        public void Execute(int i)
        {
            float basicR = globalArray[target * ArrayLength + i];
            float4 basics = float4(basicR, basicR, basicR, 1);
            normalColor[i] = JobExtensions.toColor32(saturate(basics));
        }

        public static JobHandle ScheduleParallel(NativeArray<float> globalArray,
            NativeArray<Color32> normalColor, int target,
            int ArrayLength, int resolution, JobHandle dependency)
        {
            return new MTJTextureSingleChannel()
            {
                normalColor = normalColor,
                globalArray = globalArray,
                target = target,
                ArrayLength = ArrayLength,
            }.ScheduleParallel(normalColor.Length, resolution, dependency);
        }
    }
}