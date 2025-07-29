using System;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace sapra.InfiniteLands{
    public static class MapTools
    {
        public static int MaxIncreaseSize => 30;
        public static int IncreaseResolution(int OriginalResolution, int Extra)
        {
            if (Extra > OriginalResolution)
                Debug.LogWarning("Too much resolution gain. Please conside reducing the amount of samples");
            return OriginalResolution + Math.Min(Extra, OriginalResolution) * 2;
        }
        private static bool IsEdge(int component, int resolution)
        {
            return component <= 0 || component >= resolution;
        }

        public static bool IsEdge(int2 index, int resolution)
        {
            return IsEdge(index.x, resolution) || IsEdge(index.y, resolution);
        }
        public static int2 IndexToVector(int index, int Resolution)
        {
            int x = index % (Resolution + 1);
            int y = index / (Resolution + 1);
            return new int2(x, y);
        }
        public static int VectorToIndex(int2 indices, int Resolution)
        {
            return indices.x + indices.y * (Resolution + 1);
        }

        public static int RemapIndex(int index, int FromResolution, int ToResolution)
        {
            if (FromResolution == ToResolution)
                return index;
            int2 remapIndex = GetVectorIndex(index, FromResolution, ToResolution);
            return VectorToIndex(remapIndex, ToResolution);
        }
        public static int2 RemapIndex(int2 index, int FromResolution, int ToResolution)
        {
            if (FromResolution == ToResolution)
                return index;
            int difference = ToResolution - FromResolution;
            int newX = math.clamp(index.x + difference / 2, 0, ToResolution);
            int newY = math.clamp(index.y + difference / 2, 0, ToResolution);
            return new int2(newX, newY);
        }
        public static int GetFlatIndex(int2 index, int FromResolution, int ToResolution)
        {
            int2 remaped = RemapIndex(index, FromResolution, ToResolution);
            return VectorToIndex(remaped, ToResolution);
        }
        public static int2 GetVectorIndex(int index, int FromResolution, int ToResolution)
        {
            int2 vectorized = IndexToVector(index, FromResolution);
            return RemapIndex(vectorized, FromResolution, ToResolution);
        }
        public static int LengthFromResolution(int resolution)
        {
            if (resolution <= 0)
                return 0;
            int vertexCount = (resolution + 1) * (resolution + 1);
            if ((vertexCount % 4) != 0)
            {
                var extra = 4 - vertexCount % 4;
                vertexCount += extra;
            }

            return vertexCount;
        }
        public static float2 GetOffsetInGrid(float2 position, float gridSize)
        {
            var value = position / gridSize;
            var fractional = value - math.floor(value);
            return -fractional * gridSize;
        }

        public static float biplanarSampling(float2 uv, IndexAndResolution target, NativeArray<float> map)
        {
            var scaledUv = math.clamp(uv * target.Resolution,0, target.Resolution-1);
            int2 floorIndices = (int2)math.floor(scaledUv);
            float2 middlePoints = math.frac(scaledUv - floorIndices);
            int indexA = VectorToIndex(floorIndices, target.Resolution);
            int indexB = VectorToIndex(new int2(floorIndices.x, floorIndices.y + 1), target.Resolution);
            int indexC = VectorToIndex(new int2(floorIndices.x + 1, floorIndices.y), target.Resolution);
            int indexD = VectorToIndex(new int2(floorIndices.x + 1, floorIndices.y + 1), target.Resolution);
            
            float valueA = map[indexA + target.StartIndex];
            if (math.all(middlePoints == 0))
                return valueA;
            
            float valueB = map[indexB + target.StartIndex];
            float valueC = map[indexC + target.StartIndex];
            float valueD = map[indexD + target.StartIndex];

            float bottom = Mathf.Lerp(valueA, valueC, middlePoints.x);
            float top = Mathf.Lerp(valueB, valueD, middlePoints.x);

            return Mathf.Lerp(bottom, top, middlePoints.y);

        }
    }
}