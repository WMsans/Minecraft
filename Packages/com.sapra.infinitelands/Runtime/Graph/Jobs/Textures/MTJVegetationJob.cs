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
    public struct MTJVegetationJobFlat : IJobFor
    {
        [ReadOnly] NativeArray<float> globalArray;

        [NativeDisableContainerSafetyRestriction]
        NativeArray<Color32> normalColor;
        int ArrayLength;

        int indexR;
        int indexG;
        int indexB;
        int indexA;

        public void Execute(int i)
        {
            float basicR = globalArray[indexR * ArrayLength + i];
            float basicG = 0;
            float basicB = 0;
            float basicA = 0;
            if (indexG >= 0)
            {
                basicG = globalArray[indexG * ArrayLength + i];
                if (indexB >= 0)
                {
                    basicB = globalArray[indexB * ArrayLength + i];
                    if (indexA >= 0)
                    {
                        basicA = globalArray[indexA * ArrayLength + i];
                    }
                }
            }

            float4 basics = float4(basicR, basicG, basicB, basicA);
            normalColor[i] = JobExtensions.toColor32(saturate(basics));
        }

        public static JobHandle ScheduleParallel(NativeArray<float> globalArray, NativeArray<Color32> normalColor, 
            int indexR, int indexG, int indexB, int indexA,
            int ArrayLength, int resolution, JobHandle dependency) => new MTJVegetationJobFlat()
            {
                normalColor = normalColor,
                globalArray = globalArray,
                indexR = indexR,
                indexG = indexG,
                indexB = indexB,
                indexA = indexA,
                ArrayLength = ArrayLength,
            }.ScheduleParallel(normalColor.Length, resolution, dependency);
        
    }
}