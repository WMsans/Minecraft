using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

[BurstCompile]
public unsafe struct TerrainLayer
{
    // The delegate now accepts a void pointer for entity data.
    public delegate void ApplyDelegate(ref TerrainLayer layer, int seed, float* density, byte* voxelTypes, int densityLength, int chunkSize, in float3 offset, float scale, void* entities);

    public FunctionPointer<ApplyDelegate> ApplyFunction;

    [MarshalAs(UnmanagedType.U1)]
    public bool enabled;

    public fixed float properties[16];

    // The Apply method is updated to pass the pointer.
    public void Apply(int seed, NativeArray<float> density, NativeArray<byte> voxelTypes, int chunkSize, in float3 offset, float scale, void* entities)
    {
        ApplyFunction.Invoke(ref this, seed, (float*)density.GetUnsafePtr(), (byte*)voxelTypes.GetUnsafePtr(), density.Length, chunkSize, in offset, scale, entities);
    }
}