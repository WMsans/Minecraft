using System.Runtime.CompilerServices;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands
{
    internal static partial class Noise
    {
        public struct VoronoiDataDiff
        {
            public float4 a;

            public float4 mx;
            public float4 my;

            public static VoronoiDataDiff Default => new VoronoiDataDiff
            {
                a = 8f
            };
        }

        public struct F2MinusF1 : INoise
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, float frequency, float smoothness)
            {
                LatticeSpan4
                    x = GetLatticeSpan4(positions.c0, frequency),
                    z = GetLatticeSpan4(positions.c2, frequency);

                SmallXXHash4 copy = hash;
                VoronoiDataDiff data = VoronoiDataDiff.Default;
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
                        ), h.Floats01A + xOffset, h.Floats01B + zOffset);

                        data = UpdateVoronoiData(data, GetDistance(
                            h.Floats01C + xOffset, h.Floats01D + zOffset
                        ), h.Floats01C + xOffset, h.Floats01D + zOffset);

                        data = UpdateVoronoiData(data, GetDistance(
                            h.Floats01B + xOffset, h.Floats01C + zOffset
                        ), h.Floats01B + xOffset, h.Floats01C + zOffset);
                    }
                }

                float4 final = 8.0f;
                for (int u = -1; u <= 1; u++)
                {
                    SmallXXHash4 hx = copy.Eat(x.p0 + u);
                    float4 xOffset = u - x.g0;

                    for (int v = -1; v <= 1; v++)
                    {
                        SmallXXHash4 h = hx.Eat(z.p0 + v);
                        float4 zOffset = v - z.g0;

                        float4 rX = h.Floats01A + xOffset;
                        float4 rY = h.Floats01B + zOffset;

                        float4x2 matrice = float4x2(rX - data.mx, rY - data.my);
                        float2x4 tran = transpose(matrice);
                        tran.c0 = normalize(tran.c0);
                        tran.c1 = normalize(tran.c1);
                        tran.c2 = normalize(tran.c2);
                        tran.c3 = normalize(tran.c3);

                        bool4 valid = DotFloat4x2(matrice, matrice) > 0.00001f;
                        final = select(final,
                            min(final, DotFloat4x2(.5f * float4x2(rX + data.mx, rY + data.my), transpose(tran))),
                            valid);

                        rX = h.Floats01C + xOffset;
                        rY = h.Floats01D + zOffset;
                        matrice = float4x2(rX - data.mx, rY - data.my);
                        tran = transpose(matrice);
                        tran.c0 = normalize(tran.c0);
                        tran.c1 = normalize(tran.c1);
                        tran.c2 = normalize(tran.c2);
                        tran.c3 = normalize(tran.c3);

                        valid = DotFloat4x2(matrice, matrice) > 0.00001f;
                        final = select(final,
                            min(final, DotFloat4x2(.5f * float4x2(rX + data.mx, rY + data.my), transpose(tran))),
                            valid);

                        rX = h.Floats01B + xOffset;
                        rY = h.Floats01C + zOffset;
                        matrice = float4x2(rX - data.mx, rY - data.my);
                        tran = transpose(matrice);
                        tran.c0 = normalize(tran.c0);
                        tran.c1 = normalize(tran.c1);
                        tran.c2 = normalize(tran.c2);
                        tran.c3 = normalize(tran.c3);

                        valid = DotFloat4x2(matrice, matrice) > 0.00001f;
                        final = select(final,
                            min(final, DotFloat4x2(.5f * float4x2(rX + data.mx, rY + data.my), transpose(tran))),
                            valid);
                    }
                }

                return final;
            }

            public float4 GetDistance(float4 x, float4 y) => (x * x + y * y);

            public float4 DotFloat4x2(float4x2 a, float4x2 b)
            {
                return (a.c0 * b.c0 + a.c1 * b.c1);
            }


            public VoronoiDataDiff UpdateVoronoiData(VoronoiDataDiff data, float4 sample, float4 x, float4 y)
            {
                bool4 newMinimum = sample < data.a;
                data.a = select(data.a, sample, newMinimum);
                data.mx = select(data.mx, x, newMinimum);
                data.my = select(data.my, y, newMinimum);
                return data;
            }
        }
    }
}