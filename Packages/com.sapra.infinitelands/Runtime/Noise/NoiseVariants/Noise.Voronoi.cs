using System.Runtime.CompilerServices;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands
{
    internal static partial class Noise
    {
        public interface IVoronoiFunction
        {
            float4 Evaluate(VoronoiData data);
        }

        public struct F1 : IVoronoiFunction
        {
            public float4 Evaluate(VoronoiData data) => data.a;
        }

        public struct F2 : IVoronoiFunction
        {
            public float4 Evaluate(VoronoiData data) => data.b;
        }

        public struct VoronoiData
        {
            public float4 a, b;

            public static VoronoiData Default => new VoronoiData
            {
                a = 8f,
                b = 8f
            };
        }

        public struct Voronoi2D<F> : INoise
            where F : struct, IVoronoiFunction
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, float frequency, float smoothness)
            {
                LatticeSpan4
                    x = GetLatticeSpan4(positions.c0, frequency),
                    z = GetLatticeSpan4(positions.c2, frequency);

                VoronoiData data = VoronoiData.Default;
                for (int u = -1; u <= 1; u++)
                {
                    SmallXXHash4 hx = hash.Eat(x.p0 + u);
                    float4 xOffset = u - x.g0;
                    for (int v = -1; v <= 1; v++)
                    {
                        SmallXXHash4 h = hx.Eat(z.p0 + v);
                        float4 zOffset = v - z.g0;
                        data = UpdateVoronoiData(data, GetDistance(
                            h.Floats01A + xOffset, h.Floats01B + zOffset
                        ), smoothness);
                        data = UpdateVoronoiData(data, GetDistance(
                            h.Floats01C + xOffset, h.Floats01D + zOffset
                        ), smoothness);
                        data = UpdateVoronoiData(data, GetDistance(
                            h.Floats01B + xOffset, h.Floats01C + zOffset
                        ), smoothness);
                    }
                }

                float4 s = default(F).Evaluate(data);
                return s;
            }

            public float4 GetDistance(float4 x, float4 y) => x * x + y * y;

            public VoronoiData UpdateVoronoiData(VoronoiData data, float4 sample, float smoothness)
            {
                float4 valA = UpdateDistance(data.a, sample, smoothness);
                float4 valB = UpdateDistance(data.b, sample, smoothness);

                bool4 newMinimum = valA < data.a;
                data.b = select(select(data.b, valB, valB < data.b), data.a, newMinimum);
                data.a = select(data.a, valA, newMinimum);
                return data;
            }

            public static float4 UpdateDistance(float4 originalValue, float4 distance, float smoothness)
            {
                float4 h = smoothstep(0f, 1f, .5f + (originalValue - distance) / smoothness);
                return lerp(originalValue, distance, h) - smoothness * h * (1f - h) * .5f;
            }
        }
    }
}