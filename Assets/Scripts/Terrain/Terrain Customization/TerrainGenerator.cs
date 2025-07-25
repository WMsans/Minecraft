using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public class TerrainGenerator
{
    public List<TerrainLayer> layers = new(){ PerlinNoiseLayer.Create() };

    public void ApplyLayers(NativeArray<float> density, NativeArray<byte> voxelTypes, int chunkSize, float3 offset, float scale)
    {
        var layersArray = new NativeArray<TerrainLayer>(layers.ToArray(), Allocator.TempJob);

        var job = new ApplyLayersJob
        {
            layers = layersArray,
            density = density,
            voxelTypes = voxelTypes, 
            chunkSize = chunkSize,
            offset = offset,
            scale = scale
        };
        job.Schedule().Complete();

        layersArray.Dispose();
    }
}