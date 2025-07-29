using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands
{
    internal static partial class Noise
    {
        public struct LatticeSpan4
        {
            public int4 p0, p1;
            public float4 g0, g1;
            public float4 t, dt;
        }

        static LatticeSpan4 GetLatticeSpan4(float4 coordinates, float frequency)
        {
            coordinates *= frequency;
            float4 points = floor(coordinates);
            LatticeSpan4 span;
            span.p0 = (int4)points;
            span.p1 = span.p0 + 1;
            span.g0 = coordinates - span.p0;
            span.g1 = span.g0 - 1f;

            float4 t = coordinates - points;
            span.t = t * t * t * (t * (t * 6f - 15f) + 10f);
            span.dt = t * t * (t * (t * 30f - 60f) + 30f);
            return span;
        }
    }
}