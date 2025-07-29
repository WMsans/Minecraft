using System;
using UnityEngine;
using Unity.Mathematics;
using Unity.Burst;

namespace sapra.InfiniteLands
{
    [Serializable]
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct NoiseSettings
    {
        public enum NoiseBurstType
        {
            PerlinValue,
            SimplexValue,
            Perlin,
            Simplex,
            VoronoiF1,
            VoronoiF2,
            VoronoiF1F2,
            VoronoiIndex
        };

        public Vector2 minmaxValue;
        [Min(0.001f)] public float scale;
        [Min(1)] public int octaves;
        public Vector3 rotation;
        public int lacunarity;
        public float persistence;
        public bool ridgeMode;

        public float SmoothVoronoi;

        public static NoiseSettings Default => new NoiseSettings
        {
            minmaxValue = new float2(0, 1),
            octaves = 1,
            lacunarity = 2,
            persistence = 0.5f,
            scale = 1,
            SmoothVoronoi = .001f,
            ridgeMode = false
        };

        public float3x4 Matrix
        {
            get
            {
                float3x3 m = math.mul(
                    float3x3.Scale(1f / scale), float3x3.EulerZXY(math.radians(rotation))
                );
                return math.float3x4(m.c0, m.c1, m.c2, 0);
            }
        }
    }
}