using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct ApplyLayersJob : IJob
{
    public NativeArray<TerrainLayer> layers;
    public NativeArray<float> density;
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
                // Pass offset by "in" reference
                layer.Apply(density, chunkSize, in offset, scale);
            }
        }
    }
}