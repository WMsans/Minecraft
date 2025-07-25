using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

public class TerrainGenerator
{
    public List<TerrainLayer> layers = new(){new PerlinNoiseLayer()};

    public void ApplyLayers(NativeArray<float> density, int chunkSize, float3 offset, float scale)
    {
        foreach (var layer in layers)
        {
            if (layer.enabled)
            {
                layer.Apply(density, chunkSize, offset, scale);
            }
        }
    }
}