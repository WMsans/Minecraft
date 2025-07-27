using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class TerrainGenerator
{
    private NativeArray<TerrainLayer> _layersArray;

    public TerrainGenerator(TerrainGraph graph)
    {
        Initialize(graph);
    }

    private void Initialize(TerrainGraph graph)
    {
        if (graph == null)
        {
            Debug.LogError("Cannot initialize TerrainGenerator: TerrainGraph is null.");
            return;
        }

        var sortedLayers = SortLayersFromGraph(graph);
        _layersArray = new NativeArray<TerrainLayer>(sortedLayers.ToArray(), Allocator.Persistent);
    }

    private List<TerrainLayer> SortLayersFromGraph(TerrainGraph graph)
    {
        var layers = new List<TerrainLayer>();
        if (graph.rootNode == null && graph.nodes.Any())
        {
             // Fallback to the first node if no root is explicitly set.
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
                sortedNodes.Add(currentNode);
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
            layers.Add(CreateLayerFromNodeData(nodeData));
        }

        return layers;
    }
    
    private TerrainLayer CreateLayerFromNodeData(NodeData data)
    {
        return TerrainLayerRegistry.CreateLayer(data.layerType, data.properties);
    }

    public void Dispose()
    {
        if (_layersArray.IsCreated)
            _layersArray.Dispose();
    }

    public JobHandle ScheduleApplyLayers(NativeArray<float> density, NativeArray<byte> voxelTypes, int chunkSize, float3 offset, float scale, JobHandle dependency)
    {
        var job = new ApplyLayersJob
        {
            seed = 1234, // Replace with your seed logic
            layers = _layersArray,
            density = density,
            voxelTypes = voxelTypes,
            chunkSize = chunkSize,
            offset = offset,
            scale = scale
        };
        return job.Schedule(dependency);
    }
}