using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct GenerateHeightmapJob : IJob
{
    [ReadOnly] public NativeArray<HeightmapLayer> layers;
    public int seed;
    public Heightmap heightmap;
    public float3 offset;
    public float scale;

    public void Execute()
    {
        for (int i = 0; i < layers.Length; i++)
        {
            var layer = layers[i];
            if (layer.enabled)
            {
                layer.Apply(seed, ref heightmap, in offset, scale);
            }
        }
    }
}