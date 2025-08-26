using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct ApplyLayersJob : IJob
{
    [ReadOnly] public NativeArray<TerrainLayer> layers;
    [ReadOnly] public NativeArray<float> heightmap;
    public int seed;
    public NativeArray<float> density;
    public NativeArray<byte> voxelTypes;
    public int chunkSize;
    public float3 offset;
    public float scale;

    public void Execute()
    {
        for (int i = 0; i < layers.Length; i++)
        {
            var layer = layers[i];
            if (layer.enabled)
            {
                layer.Apply(seed, density, voxelTypes, chunkSize, in offset, scale, heightmap);
            }
        }
    }
}