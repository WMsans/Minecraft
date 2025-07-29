using System.Runtime.CompilerServices;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands
{
    internal static partial class Noise
    {
        public struct TrueVoronoiIndex : INoise
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, float frequency, float smoothness)
            {
                LatticeSpan4
                    x = GetLatticeSpan4(positions.c0, frequency),
                    z = GetLatticeSpan4(positions.c2, frequency);

                float4 res = 90;
                float4 xpos = 0;
                float4 ypos = 0;
                for (int u = -1; u <= 1; u++)
                {
                    SmallXXHash4 hx = hash.Eat(x.p0 + u);
                    float4 xOffset = u - x.g0;
                    for (int v = -1; v <= 1; v++)
                    {
                        SmallXXHash4 h = hx.Eat(z.p0 + v);
                        float4 zOffset = v - z.g0;

                        float4 x2 = h.Floats01A + xOffset;
                        float4 y2 = h.Floats01B + zOffset;

                        float4 d = (x2 * x2 + y2 * y2);
                        bool4 smaller = d < res;
                        float4 newX = x.p0 + u;
                        float4 newY = z.p0 + v;

                        xpos = select(xpos, newX, smaller);
                        ypos = select(ypos, newY, smaller);

                        res = min(res, d);
                    }
                }

                float4 dotProds;
                dotProds.x = dot(xpos.x, ypos.x);
                dotProds.y = dot(xpos.y, ypos.y);
                dotProds.z = dot(xpos.z, ypos.z);
                dotProds.w = dot(xpos.w, ypos.w);

                return .5f + .5f * sin(dotProds);
            }
        }
    }
}