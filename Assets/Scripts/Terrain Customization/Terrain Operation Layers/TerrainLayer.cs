using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

[BurstCompile]
public unsafe struct TerrainLayer
{
    public delegate void ApplyDelegate(ref TerrainLayer layer, int seed, float* density, byte* voxelTypes, int densityLength, int chunkSize, in float3 offset, float scale, float* heightmap, int heightmapLength);

    public FunctionPointer<ApplyDelegate> ApplyFunction;

    [MarshalAs(UnmanagedType.U1)]
    public bool enabled;

    public fixed float properties[16];

    public void Apply(int seed, NativeArray<float> density, NativeArray<byte> voxelTypes, int chunkSize, in float3 offset, float scale, NativeArray<float> heightmap)
    {
        ApplyFunction.Invoke(ref this, seed, (float*)density.GetUnsafePtr(), (byte*)voxelTypes.GetUnsafePtr(), density.Length, chunkSize, in offset, scale, (float*)heightmap.GetUnsafeReadOnlyPtr(), heightmap.Length);
    }
}