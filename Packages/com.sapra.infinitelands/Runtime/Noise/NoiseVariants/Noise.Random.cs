using System.Runtime.CompilerServices;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands
{
    internal static partial class Noise
    {
        public struct Random<G> : INoise where G : struct, IGradient
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public float4 GetNoise4(float4x3 positions, SmallXXHash4 hash, float frequency, float smoothness)
            {
                LatticeSpan4
                    x = GetLatticeSpan4(positions.c0, frequency),
                    z = GetLatticeSpan4(positions.c2, frequency);

                SmallXXHash4 h0 = hash.Eat(x.p0);
                SmallXXHash4 h1 = h0.Eat(z.p1);

                var g = default(G);
                float4 a = g.Evaluate(h1.Eat(z.p0).Eat(x.p1), x.g0, z.g0);

                return (a+1.0f)/2.0f;
            }
        }
    }
}