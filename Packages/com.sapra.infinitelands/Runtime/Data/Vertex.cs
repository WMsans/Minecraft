using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace sapra.InfiniteLands
{
    [StructLayout(LayoutKind.Sequential)]
    public struct Vertex
    {
        public float3 position, normal;
        public float2 texCoord0;
        public static int size = (3 + 3 + 2) * sizeof(float);
    }

    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Vertex4
    {
        public readonly Vertex v0, v1, v2, v3;
    }
}