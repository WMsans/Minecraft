using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands
{
    internal static partial class Noise
    {
        public interface IGradient
        {
            float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y);
        }

        public struct Value : IGradient
        {
            public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y) =>
                hash.Floats01A * 2f - 1f;
        }

        public struct Perlin : IGradient
        {
            public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y) =>
                BaseGradients.Square(hash, x, y) * (2f / 0.53528f);
        }

        public struct Simplex : IGradient
        {
            public float4 Evaluate(SmallXXHash4 hash, float4 x, float4 y) =>
                BaseGradients.Circle(hash, x, y) * (5.832f / sqrt(2f));
        }

        public static class BaseGradients
        {
            public static float4 Line(SmallXXHash4 hash, float4 x)
            {
                float4 l =
                    (1f + hash.Floats01A) * select(-1f, 1f, ((uint4)hash & 1 << 8) == 0);
                return l * x;
            }

            public static float4 Square(SmallXXHash4 hash, float4 x, float4 y)
            {
                float4x2 v = SquareVectors(hash);
                return v.c0 * x + v.c1 * y;
            }

            public static float4 Circle(SmallXXHash4 hash, float4 x, float4 y)
            {
                float4x2 v = SquareVectors(hash);
                return (v.c0 * x + v.c1 * y) * rsqrt(v.c0 * v.c0 + v.c1 * v.c1);
            }

            static float4x2 SquareVectors(SmallXXHash4 hash)
            {
                float4x2 v;
                v.c0 = hash.Floats01A * 2f - 1f;
                v.c1 = 0.5f - abs(v.c0);
                v.c0 -= floor(v.c0 + 0.5f);
                return v;
            }
        }
    }
}