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
    public struct MTJGeneral : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<Color32> textureArray;
        [ReadOnly] NativeArray<float> MinMaxValue;

        [ReadOnly] NativeArray<float> globalArray;
        IndexAndResolution origin;
        int resolution;
        bool colorfulMode;
        public void Execute(int i)
        {
            int index = MapTools.RemapIndex(i, resolution, origin.Resolution);
            float current = globalArray[origin.StartIndex + index];
            float colorValue = 0;
            float min = MinMaxValue[0];
            float max = MinMaxValue[1];

            if (max - min != 0)
            {
                colorValue = (current - min) / (max - min);
            }
            Color32 color;
            if (colorfulMode)
            {
                color = JobExtensions.toColor(getColor(colorValue));
            }
            else
            {
                color = JobExtensions.toColor(saturate(colorValue));
            }
            textureArray[i] = color;
        }
        
        float4 getColor(float t) {
            float r, g, b;

            // Red: 0 in [0, 0.5], increases to 1 in [0.5, 1]
            r = saturate(4.0f * (t - 0.5f));

            // Green: 0 to 1 in [0, 0.5], 1 to 0 in [0.5, 0.75]
            g = saturate(4.0f * t) * saturate(4.0f * (0.75f - t));

            // Blue: 1 to 0 in [0, 0.5], 0 in [0.5, 1]
            b = saturate(4.0f * (0.5f - t));

            return new float4(r, g, b, 1.0f);
        }

        public static JobHandle ScheduleParallel(NativeArray<Color32> textureArray, NativeArray<float> MinMaxValue,
            NativeArray<float> globalArray, IndexAndResolution origin,
            int resolution, JobHandle dependency, bool colorfulMode = false) => new MTJGeneral()
            {
                textureArray = textureArray,
                globalArray = globalArray,
                MinMaxValue = MinMaxValue,
                origin = origin,
                resolution = resolution,
                colorfulMode = colorfulMode
            }.ScheduleParallel(textureArray.Length, resolution, dependency);
    }
}