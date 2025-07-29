using System.Threading;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands
{
    internal static class JobExtensions
    {
        public static float4 invLerp(float4 from, float4 to, float4 value)
        {
            return saturate((value - from) / (to - from));
        }

        public static float invLerp(float from, float to, float value)
        {
            return saturate((value - from) / (to - from));
        }

        public static float4x3 TransformVectors(
            this float3x4 trs, float4x3 p, float w = 1f
        ) => float4x3(
            trs.c0.x * p.c0 + trs.c1.x * p.c1 + trs.c2.x * p.c2 + trs.c3.x * w,
            trs.c0.y * p.c0 + trs.c1.y * p.c1 + trs.c2.y * p.c2 + trs.c3.y * w,
            trs.c0.z * p.c0 + trs.c1.z * p.c1 + trs.c2.z * p.c2 + trs.c3.z * w
        );

        public static void normalizeArray(NativeArray<float> globalArray, NativeArray<IndexAndResolution> masksIndices,
            NativeArray<IndexAndResolution> masksTargets, IndexAndResolution target, int arrayCount, int indexOffset)
        {
            float totalSum = 0;
            //Normalize the masks
            for (int m = 0; m < arrayCount; m++)
            {
                float currentValue = globalArray[masksIndices[m].StartIndex + indexOffset];
                totalSum += currentValue;

                int targetIndex = MapTools.RemapIndex(indexOffset, target.Resolution, masksTargets[m].Resolution);
                globalArray[masksTargets[m].StartIndex + targetIndex] = currentValue;
            }

            for (int m = 0; m < arrayCount; m++)
            {
                int maskIndex = MapTools.RemapIndex(indexOffset, target.Resolution, masksTargets[m].Resolution);
                globalArray[masksTargets[m].StartIndex + maskIndex] /= totalSum;
            }
        }


        public static void insertSorted(ref int4x4 indices, ref float4x4 weight, int4 newIndice, float4 newWeight)
        {
            float4 fourthPlace = step(weight.c3, newWeight);
            float4 thirdPlace = step(weight.c2, newWeight);
            float4 secondPlace = step(weight.c1, newWeight);
            float4 firstPlace = step(weight.c0, newWeight);

            weight.c3 = lerp(lerp(weight.c3, newWeight, fourthPlace), weight.c2,
                saturate(thirdPlace + secondPlace + firstPlace));
            weight.c2 = lerp(lerp(weight.c2, newWeight, thirdPlace), weight.c1, saturate(secondPlace + firstPlace));
            weight.c1 = lerp(lerp(weight.c1, newWeight, secondPlace), weight.c0, firstPlace);
            weight.c0 = lerp(weight.c0, newWeight, firstPlace);

            indices.c3 = (int4)lerp(lerp(indices.c3, newIndice, fourthPlace), indices.c2,
                saturate(thirdPlace + secondPlace + firstPlace));
            indices.c2 = (int4)lerp(lerp(indices.c2, newIndice, thirdPlace), indices.c1,
                saturate(secondPlace + firstPlace));
            indices.c1 = (int4)lerp(lerp(indices.c1, newIndice, secondPlace), indices.c0, firstPlace);
            indices.c0 = (int4)lerp(indices.c0, newIndice, firstPlace);
        }

        public static void insertSorted(ref int4 indices, ref float4 weight, int newIndice, float newWeight)
        {
            float fourthPlace = step(weight.w, newWeight);
            float thirdPlace = step(weight.z, newWeight);
            float secondPlace = step(weight.y, newWeight);
            float firstPlace = step(weight.x, newWeight);

            weight.w = lerp(lerp(weight.w, newWeight, fourthPlace), weight.z,
                saturate(thirdPlace + secondPlace + firstPlace));
            weight.z = lerp(lerp(weight.z, newWeight, thirdPlace), weight.y, saturate(secondPlace + firstPlace));
            weight.y = lerp(lerp(weight.y, newWeight, secondPlace), weight.x, firstPlace);
            weight.x = lerp(weight.x, newWeight, firstPlace);

            indices.w = (int)lerp(lerp(indices.w, newIndice, fourthPlace), indices.z,
                saturate(thirdPlace + secondPlace + firstPlace));
            indices.z = (int)lerp(lerp(indices.z, newIndice, thirdPlace), indices.y,
                saturate(secondPlace + firstPlace));
            indices.y = (int)lerp(lerp(indices.y, newIndice, secondPlace), indices.x, firstPlace);
            indices.x = (int)lerp(indices.x, newIndice, firstPlace);
        }


        public static void ApplyFoundValue(int i, float sum, int arrayCount, NativeArray<HeightData> indices, NativeArray<float> globalArray, NativeArray<IndexAndResolution> targetWeights, IndexAndResolution target)
        {
            bool MaxFound = false;
            for (int x = arrayCount - 1; x >= 0; x--)
            {
                var indexAndRe = indices[x].indexData;
                int locator = MapTools.RemapIndex(i, target.Resolution, indexAndRe.Resolution);
                var value = globalArray[indexAndRe.StartIndex + locator];

                var targetWeight = targetWeights[x];
                int weightLocator = MapTools.RemapIndex(i, target.Resolution, targetWeight.Resolution);
                bool IsValue = sum == value && !MaxFound;
                globalArray[targetWeight.StartIndex + weightLocator] = IsValue ? 1 : 0;
                if (IsValue) MaxFound = true;
            }
        }

        public static Color toColor(float4 value)
        {
            return new Color(value.x, value.y, value.z, value.w);
        }

        public static Color32 toColor32(float4 value)
        {
            return (Color32)new Color(value.x, value.y, value.z, value.w);
        }

        public static bool InterlockedMax(ref float target, float newValue)
        {
            float snapshot;
            bool stillMore;
            do
            {
                snapshot = target;
                stillMore = newValue > snapshot;
            } while (stillMore && Interlocked.CompareExchange(ref target, newValue, snapshot) != snapshot);

            return stillMore;
        }

        public static bool InterlockedMin(ref float target, float newValue)
        {
            float snapshot;
            bool stillMore;
            do
            {
                snapshot = target;
                stillMore = newValue < snapshot;
            } while (stillMore && Interlocked.CompareExchange(ref target, newValue, snapshot) != snapshot);

            return stillMore;
        }
        
        public static bool InterlockedAdd(ref float target, float newValue)
        {
            float initialValue, computedValue;
            do
            {
                initialValue = target;
                computedValue = initialValue + newValue;
            } while (Interlocked.CompareExchange(ref target, computedValue, initialValue) != initialValue);
            return true;
        }
    }
}