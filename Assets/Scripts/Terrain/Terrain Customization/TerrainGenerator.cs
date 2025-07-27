using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

public class TerrainGenerator
{
    public List<TerrainLayer> layers = new() { PerlinNoiseLayer.Create(), BlockTypeLayer.Create() };
    private NativeArray<TerrainLayer> layersArray;

    public void Initialize()
    {
        layersArray = new NativeArray<TerrainLayer>(layers.ToArray(), Allocator.Persistent);
    }

    public void Dispose()
    {
        if (layersArray.IsCreated)
            layersArray.Dispose();
    }

    public JobHandle ScheduleApplyLayers(NativeArray<float> density, NativeArray<byte> voxelTypes, int chunkSize, float3 offset, float scale, JobHandle dependency)
    {
        var job = new ApplyLayersJob
        {
            seed = SeedController.Seed,
            layers = layersArray,
            density = density,
            voxelTypes = voxelTypes,
            chunkSize = chunkSize,
            offset = offset,
            scale = scale
        };
        return job.Schedule(dependency);
    }
}