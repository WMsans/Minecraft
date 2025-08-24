using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

[BurstCompile]
public unsafe struct HeightmapLayer
{
    public delegate void ApplyDelegate(ref HeightmapLayer layer, int seed, ref Heightmap heightmap, in float3 offset, float scale);

    public FunctionPointer<ApplyDelegate> ApplyFunction;

    [MarshalAs(UnmanagedType.U1)]
    public bool enabled;

    public fixed float properties[16];

    public void Apply(int seed, ref Heightmap heightmap, in float3 offset, float scale)
    {
        ApplyFunction.Invoke(ref this, seed, ref heightmap, in offset, scale);
    }
}