using System.Runtime.InteropServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;

[BurstCompile]
public unsafe struct TerrainLayer
{
    // The delegate now uses a pointer for the density map
    public delegate void ApplyDelegate(ref TerrainLayer layer, float* density, int densityLength, int chunkSize, in float3 offset, float scale);

    public FunctionPointer<ApplyDelegate> ApplyFunction;

    [MarshalAs(UnmanagedType.U1)]
    public bool enabled;
    public float noiseScale;
    public float noiseStrength;

    // This method converts the NativeArray to a pointer before calling the function pointer
    public void Apply(NativeArray<float> density, int chunkSize, in float3 offset, float scale)
    {
        ApplyFunction.Invoke(ref this, (float*)density.GetUnsafePtr(), density.Length, chunkSize, in offset, scale);
    }
}