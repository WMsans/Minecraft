using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct ChunkData : IDisposable
{
    public NativeArray<float> densityMap;
    public NativeArray<byte> voxelTypes;
    public NativeList<float3> vertices;
    public NativeList<int> triangles;

    public void Allocate()
    {
        densityMap = new NativeArray<float>((TerrainSettings.MIN_NODE_SIZE + 1) * (TerrainSettings.MIN_NODE_SIZE + 1) * (TerrainSettings.MIN_NODE_SIZE + 1), Allocator.Persistent);
        voxelTypes = new NativeArray<byte>((TerrainSettings.MIN_NODE_SIZE + 1) * (TerrainSettings.MIN_NODE_SIZE + 1) * (TerrainSettings.MIN_NODE_SIZE + 1), Allocator.Persistent);
        vertices = new NativeList<float3>(Allocator.Persistent);
        triangles = new NativeList<int>(Allocator.Persistent);
    }

    public void Dispose()
    {
        if (densityMap.IsCreated) densityMap.Dispose();
        if (voxelTypes.IsCreated) voxelTypes.Dispose();
        if (vertices.IsCreated) vertices.Dispose();
        if (triangles.IsCreated) triangles.Dispose();
    }
}