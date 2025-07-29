using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public unsafe struct ApplyLayersJob : IJob
{
    [ReadOnly] public NativeArray<TerrainLayer> layers;
    public int seed;
    public NativeArray<float> density;
    public NativeArray<byte> voxelTypes; 
    public NativeList<EntityData> entities; // Changed from ParallelWriter to NativeList
    public int chunkSize;
    public float3 offset;
    public float scale;

    public void Execute()
    {
        // Get the ParallelWriter and its pointer here
        var entitiesWriter = entities.AsParallelWriter();
        void* entitiesPtr = UnsafeUtility.AddressOf(ref entitiesWriter);

        for (int i = 0; i < layers.Length; i++)
        {
            var layer = layers[i];
            if (layer.enabled)
            {
                // Pass the pointer to all layers
                layer.Apply(seed, density, voxelTypes, chunkSize, in offset, scale, entitiesPtr); 
            }
        }
    }
}