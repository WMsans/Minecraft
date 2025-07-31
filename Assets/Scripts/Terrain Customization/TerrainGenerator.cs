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

        var sortedHeightmapNodes = SortNodesByDependency<IHeightmapLayer>(graph);
        var sortedHeightmapLayers = sortedHeightmapNodes.Select(n => _graph.CreateLayer<HeightmapLayer>(n.layerType, n.properties)).ToList();
        _heightmapLayersArray = new NativeArray<HeightmapLayer>(sortedHeightmapLayers.ToArray(), Allocator.Persistent);

        var sortedVoxelNodes = SortNodesByDependency<ITerrainLayer>(graph);
        var sortedVoxelLayers = sortedVoxelNodes.Select(n => _graph.CreateLayer<TerrainLayer>(n.layerType, n.properties)).ToList();
        _voxelLayersArray = new NativeArray<TerrainLayer>(sortedVoxelLayers.ToArray(), Allocator.Persistent);
    }

    private List<NodeData> SortNodesByDependency<TInterface>(TerrainGraph graph)
    {
        var sortedNodes = new List<NodeData>();
        var nodesToProcess = new Queue<NodeData>();
        var nodeGuids = new HashSet<string>(graph.nodes.Where(n => typeof(TInterface).IsAssignableFrom(Type.GetType(n.layerType))).Select(n => n.guid));
        var inDegree = new Dictionary<string, int>();

        foreach (var guid in nodeGuids)
        {
            inDegree[guid] = 0;
        }

        foreach (var edge in graph.edges.Where(e => nodeGuids.Contains(e.inputNodeGuid) && nodeGuids.Contains(e.outputNodeGuid)))
        {
            inDegree[edge.inputNodeGuid]++;
        }

        foreach (var guid in nodeGuids)
        {
            if (inDegree[guid] == 0)
            {
                nodesToProcess.Enqueue(graph.nodes.First(n => n.guid == guid));
            }
        }
        
        if (graph.rootNode != null && nodeGuids.Contains(graph.rootNode.guid))
        {
            nodesToProcess.Clear();
            nodesToProcess.Enqueue(graph.rootNode);
        }

        while (nodesToProcess.Count > 0)
        {
            var currentNode = nodesToProcess.Dequeue();
            if (sortedNodes.All(n => n.guid != currentNode.guid))
            {
                sortedNodes.Add(currentNode);

                var outgoingEdges = graph.edges.Where(e => e.outputNodeGuid == currentNode.guid && nodeGuids.Contains(e.inputNodeGuid));
                foreach (var edge in outgoingEdges)
                {
                    inDegree[edge.inputNodeGuid]--;
                    if (inDegree[edge.inputNodeGuid] == 0)
                    {
                        nodesToProcess.Enqueue(graph.nodes.First(n => n.guid == edge.inputNodeGuid));
                    }
                }
            }
        }

        return sortedNodes;
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

        heightmap.heights.Dispose(voxelHandle);

        return voxelHandle;
    }
}