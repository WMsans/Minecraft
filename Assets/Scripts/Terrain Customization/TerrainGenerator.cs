using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class TerrainGenerator
{
    private NativeArray<HeightmapLayer> _heightmapLayersArray;
    private NativeArray<TerrainLayer> _voxelLayersArray;
    private readonly TerrainGraph _graph;

    public TerrainGenerator(TerrainGraph graph)
    {
        _graph = graph;
        Initialize(graph);
    }

    private void Initialize(TerrainGraph graph)
    {
        if (graph == null)
        {
            Debug.LogError("Cannot initialize TerrainGenerator: TerrainGraph is null.");
            return;
        }

        var sortedHeightmapLayers = SortLayersFromGraph<IHeightmapLayer, HeightmapLayer>(graph);
        _heightmapLayersArray = new NativeArray<HeightmapLayer>(sortedHeightmapLayers.ToArray(), Allocator.Persistent);
        
        var sortedVoxelLayers = SortLayersFromGraph<ITerrainLayer, TerrainLayer>(graph);
        _voxelLayersArray = new NativeArray<TerrainLayer>(sortedVoxelLayers.ToArray(), Allocator.Persistent);
    }

    private List<TLayer> SortLayersFromGraph<TInterface, TLayer>(TerrainGraph graph) where TLayer : struct
    {
        var layers = new List<TLayer>();
        if (graph.rootNode == null && graph.nodes.Any())
        {
            graph.rootNode = graph.nodes[0];
            Debug.LogWarning("TerrainGraph has no root node. Falling back to the first node in the list.");
        }
        
        if (graph.rootNode == null) return layers;

        var sortedNodes = new List<NodeData>();
        var nodesToProcess = new Queue<NodeData>();
        
        nodesToProcess.Enqueue(graph.rootNode);

        while (nodesToProcess.Count > 0)
        {
            var currentNode = nodesToProcess.Dequeue();
            if (sortedNodes.All(n => n.guid != currentNode.guid))
            {
                Type layerType = Type.GetType(currentNode.layerType);
                if (layerType != null && typeof(TInterface).IsAssignableFrom(layerType))
                {
                    sortedNodes.Add(currentNode);
                }

                var outgoingEdges = graph.edges.Where(e => e.outputNodeGuid == currentNode.guid);
                foreach (var edge in outgoingEdges)
                {
                    var nextNode = graph.nodes.First(n => n.guid == edge.inputNodeGuid);
                    if (nextNode != null && nodesToProcess.All(n => n.guid != nextNode.guid))
                    {
                       nodesToProcess.Enqueue(nextNode);
                    }
                }
            }
        }
        
        // Create layer instances from the sorted node data
        foreach (var nodeData in sortedNodes)
        {
            layers.Add(_graph.CreateLayer<TLayer>(nodeData.layerType, nodeData.properties));
        }

        return layers;
    }

    public void Dispose()
    {
        if (_heightmapLayersArray.IsCreated)
            _heightmapLayersArray.Dispose();
        if (_voxelLayersArray.IsCreated)
            _voxelLayersArray.Dispose();
    }

    public JobHandle ScheduleApplyLayers(NativeArray<float> density, NativeArray<byte> voxelTypes, NativeList<EntityData> entities, int chunkSize, float3 offset, float scale, JobHandle dependency)
    {
        var heightmapSize = new int2(chunkSize + 1, chunkSize + 1);
        var heightmap = new Heightmap(heightmapSize, Allocator.TempJob);

        var generateHeightmapJob = new GenerateHeightmapJob
        {
            seed = SeedController.Seed,
            layers = _heightmapLayersArray,
            heightmap = heightmap,
            offset = offset,
            scale = scale
        };
        var heightmapHandle = generateHeightmapJob.Schedule(dependency);

        var applyVoxelLayersJob = new ApplyLayersJob
        {
            seed = SeedController.Seed,
            layers = _voxelLayersArray,
            density = density,
            voxelTypes = voxelTypes,
            entities = entities,
            chunkSize = chunkSize,
            offset = offset,
            scale = scale,
            heightmap = heightmap.heights
        };
        var voxelHandle = applyVoxelLayersJob.Schedule(heightmapHandle);

        // Dispose the heightmap after the jobs are done with it.
        heightmap.heights.Dispose(voxelHandle);

        return voxelHandle;
    }
}