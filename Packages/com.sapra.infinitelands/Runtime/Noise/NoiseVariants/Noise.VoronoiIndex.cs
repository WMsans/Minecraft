using System.Runtime.CompilerServices;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands
{
    internal static partial class Noise
    {
        public struct VoronoiIndex : INoise
        {
            public struct VoronoiIndexData
            {
                public float4 d, xpos, ypos;

                public static VoronoiIndexData Default => new VoronoiIndexData
                {
                    d = 90f,
                    xpos = 0,
                    ypos = 0,
                };
            }
            
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, float frequency, float smoothness)
            {
                LatticeSpan4
                    x = GetLatticeSpan4(positions.c0, frequency),
                    z = GetLatticeSpan4(positions.c2, frequency);

                VoronoiIndexData data = VoronoiIndexData.Default;
                for (int u = -1; u <= 1; u++)
                {
                    SmallXXHash4 hx = hash.Eat(x.p0 + u);
                    float4 xOffset = u - x.g0;
                    for (int v = -1; v <= 1; v++)
                    {
                        SmallXXHash4 h = hx.Eat(z.p0 + v);
                        float4 zOffset = v - z.g0;
                        data = UpdateNodes(data, 
                            h.Floats01A + xOffset, h.Floats01B + zOffset, 
                            h.Floats01A + u+x.p0, h.Floats01B + v+z.p0);
                        data = UpdateNodes(data, 
                            h.Floats01C + xOffset, h.Floats01D + zOffset, 
                            h.Floats01C + u+x.p0, h.Floats01D + v+z.p0);
                        data = UpdateNodes(data, 
                            h.Floats01B + xOffset, h.Floats01C + zOffset,
                            h.Floats01B + u+x.p0,h.Floats01C + v+z.p0);
                    }
                }
                return rand(rand(data.xpos)+rand(data.ypos));
            }

            public static VoronoiIndexData UpdateNodes(VoronoiIndexData data, float4 x, float4 y, float4 px, float4 py){
                float4 d = UpdateDistance(data.d, x * x + y * y, 0.001f);
                bool4 smaller = d < data.d;
                data.xpos = select(data.xpos, px, smaller);
                data.ypos = select(data.ypos, py, smaller);
                data.d = min(d, data.d);
                return data;
            }

            public static float4 UpdateDistance(float4 originalValue, float4 distance, float smoothness)
            {
                float4 h = smoothstep(.0f, 1f, .5f + (originalValue - distance) / smoothness);
                return lerp(originalValue, distance, h) - smoothness * h * (1f - h) * .5f;
            }
            
            public static float4 rand(float4 n)
            {
                float4 flattened = (sin(n)+1f)/2f;
                return frac(flattened*43758.5453f);//frac(sin(n) * 43758.5453f);
            }
        }
    }
}