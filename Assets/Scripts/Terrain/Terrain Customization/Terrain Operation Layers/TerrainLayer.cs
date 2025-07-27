using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

[BurstCompile]
public unsafe struct TerrainLayer
{
    // The delegate now uses pointers for density and voxel type maps
    public delegate void ApplyDelegate(ref TerrainLayer layer, int seed, float* density, byte* voxelTypes, int densityLength, int chunkSize, in float3 offset, float scale);

    public FunctionPointer<ApplyDelegate> ApplyFunction;

    [MarshalAs(UnmanagedType.U1)]
    public bool enabled;
    public float noiseScale;
    public float noiseStrength;

    // This method converts the NativeArrays to pointers before calling the function pointer
    public void Apply(int seed, NativeArray<float> density, NativeArray<byte> voxelTypes, int chunkSize, in float3 offset, float scale)
    {
        ApplyFunction.Invoke(ref this, seed, (float*)density.GetUnsafePtr(), (byte*)voxelTypes.GetUnsafePtr(), density.Length, chunkSize, in offset, scale);
    }
}