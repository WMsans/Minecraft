using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using System.Threading.Tasks;
using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;

public class OctreeTerrainManager : MonoBehaviour
{
    public static OctreeTerrainManager Instance;

    [Header("Terrain Settings")]
    public Transform player;
    public Camera mainCamera;
    public int maxDepth = 2;
    public float nodeSize = 64;
    public TerrainGraph terrainGraph;
    [Header("Chunk Settings")]
    public Chunk chunkPrefab;
    public int entityProcessingDepth = 2;
    public string worldName = "Za Warudo";

    private NativeList<OctreeNode> nodes;
    private Dictionary<int, Chunk> activeChunks;
    private Pool<Chunk> chunkPool;

    private TerrainGenerator terrainGenerator;
    private ChunkDataManager chunkDataManager;

    private Dictionary<int, (JobHandle jobHandle, Chunk chunk, Chunk.MeshData meshData)> generationJobs;
    private JobHandle applyModificationsHandle;

    public NativeArray<int> triangulationTable;
    public NativeArray<int3> cornerOffsets;
    public NativeArray<int> cornerIndexAFromEdge;
    public NativeArray<int> cornerIndexBFromEdge;

    public struct TerrainModification
    {
        public float3 worldPos;
        public float strength;
        public float radius;
        public byte newVoxelType;
    }
    private NativeList<TerrainModification> terrainModifications;

    // Dictionary to track children that need to be destroyed after a parent merge is complete.
    // Key: parent node index, Value: children start index
    private Dictionary<int, int> pendingMergeCleanup = new Dictionary<int, int>();

    // Dictionary to track the parent of new children from a subdivision.
    // Key: child node index, Value: parent node index
    private Dictionary<int, int> subdivisionParentMap = new Dictionary<int, int>();
    private Dictionary<int, int> subdivisionCompletionCounter = new Dictionary<int, int>();
    
    private EntityManager entityManager;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            InitializeMarchingCubesTables();
        }
        else
        {
            Destroy(this);
        }

        terrainGenerator = new TerrainGenerator(terrainGraph);
        entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        chunkDataManager = new ChunkDataManager(worldName);

        chunkPool = new Pool<Chunk>(() =>
        {
            Chunk chunk = Instantiate(chunkPrefab, transform, true);
            chunk.Initialize(terrainGenerator);
            chunk.gameObject.SetActive(false);
            return chunk;
        }, (chunk) =>
        {
        }, (chunk) =>
        {
            chunk.gameObject.SetActive(false);
        });
    }

    private void Start()
    {
        nodes = new NativeList<OctreeNode>(Allocator.Persistent);
        activeChunks = new Dictionary<int, Chunk>();
        generationJobs = new Dictionary<int, (JobHandle, Chunk, Chunk.MeshData)>();
        nodes.Add(new OctreeNode(new Bounds(Vector3.zero, Vector3.one * nodeSize), 0));

        terrainModifications = new NativeList<TerrainModification>(Allocator.Persistent);
    }

    private void InitializeMarchingCubesTables()
    {
        triangulationTable = new NativeArray<int>(MarchingCubesTables.triangulation, Allocator.Persistent);
        cornerOffsets = new NativeArray<int3>(MarchingCubesTables.cornerOffsets, Allocator.Persistent);
        cornerIndexAFromEdge = new NativeArray<int>(MarchingCubesTables.cornerIndexAFromEdge, Allocator.Persistent);
        cornerIndexBFromEdge = new NativeArray<int>(MarchingCubesTables.cornerIndexBFromEdge, Allocator.Persistent);
    }

    private void OnDestroy()
    {
        applyModificationsHandle.Complete();
        foreach (var genJob in generationJobs.Values)
        {
            genJob.jobHandle.Complete();
            genJob.meshData.Dispose();
        }
        generationJobs.Clear();

        if (nodes.IsCreated) nodes.Dispose();
        if (triangulationTable.IsCreated) triangulationTable.Dispose();
        if (cornerOffsets.IsCreated) cornerOffsets.Dispose();
        if (cornerIndexAFromEdge.IsCreated) cornerIndexAFromEdge.Dispose();
        if (cornerIndexBFromEdge.IsCreated) cornerIndexBFromEdge.Dispose();
        if (terrainModifications.IsCreated) terrainModifications.Dispose();

        terrainGenerator.Dispose();
    }

    private void Update()
    {
        var frustumPlanes = FrustumCulling.GetFrustumPlanes(mainCamera);

        var toSubdivide = new NativeList<int>(Allocator.TempJob);
        var toMerge = new NativeList<int>(Allocator.TempJob);
        var toGenerate = new NativeList<int>(Allocator.TempJob);
        var toHide = new NativeList<int>(Allocator.TempJob);

        var job = new OctreeUpdateJob
        {
            playerPos = player.position,
            frustumPlanes = frustumPlanes,
            maxDepth = this.maxDepth,
            nodes = this.nodes,
            toSubdivide = toSubdivide,
            toMerge = toMerge,
            toGenerate = toGenerate,
            toHide = toHide
        };

        job.Schedule().Complete();

        foreach (var index in toSubdivide) Subdivide(index);
        foreach (var index in toMerge) Merge(index);
        foreach (var index in toGenerate) GenerateChunk(index);
        foreach (var index in toHide) HideChunk(index);

        ProcessCompletedJobs();

        toSubdivide.Dispose();
        toMerge.Dispose();
        toGenerate.Dispose();
        toHide.Dispose();
    }

    private void ProcessCompletedJobs()
    {
        var completedJobIndices = new List<int>();
        foreach (var entry in generationJobs)
        {
            if (entry.Value.jobHandle.IsCompleted)
            {
                completedJobIndices.Add(entry.Key);
            }
        }

        foreach (var index in completedJobIndices)
        {
            var (jobHandle, chunk, meshData) = generationJobs[index];
            generationJobs.Remove(index);

            jobHandle.Complete();

            if (activeChunks.ContainsKey(index) && activeChunks[index] == chunk)
            {
                chunk.ApplyGeneratedMesh(meshData);

                var chunkData = chunkDataManager.GetChunkData(index);
                chunkData.vertices.Clear();
                chunkData.vertices.AddRange(meshData.vertices.AsArray());
                chunkData.triangles.Clear();
                chunkData.triangles.AddRange(meshData.triangles.AsArray());
            }
            
            if (meshData.IsCreated) meshData.Dispose();
            
            if (!activeChunks.ContainsKey(index))
            {
                chunkPool.Return(chunk);
            }

            // Cleanup Case 1: A child of a subdivision finished generating.
            if (subdivisionParentMap.Remove(index, out int parentNodeIndex)) // Use .Remove to ensure we process each child only once
            {
                HandleChildCompletion(parentNodeIndex);
            }

            // Cleanup Case 2: A parent of a merge finished generating.
            if (pendingMergeCleanup.TryGetValue(index, out int childrenIndexToDestroy))
            {
                // The parent chunk is now visible. Destroy the obsolete children.
                var stack = new Stack<int>();
                if (childrenIndexToDestroy != -1)
                {
                    stack.Push(childrenIndexToDestroy);
                }

                while (stack.Count > 0)
                {
                    int currentIndex = stack.Pop();
                    for (int i = 0; i < 8; i++)
                    {
                        var childNode = nodes[currentIndex + i];
                        if (!childNode.isLeaf)
                        {
                            stack.Push(childNode.childrenIndex);
                        }
                        DestroyChunk(currentIndex + i);
                    }
                }

                // Finalize the node state.
                var node = nodes[index];
                node.childrenIndex = -1;
                nodes[index] = node;

                pendingMergeCleanup.Remove(index);
            }
        }
    }


    private void Subdivide(int nodeIndex)
    {
        // We DO NOT destroy the parent chunk here. It will be destroyed in
        // ProcessCompletedJobs once its children are ready.
        // DestroyChunk(nodeIndex);

        var node = nodes[nodeIndex];
        node.isLeaf = false;
        node.childrenIndex = nodes.Length;
        nodes[nodeIndex] = node;

        float childSize = node.bounds.size.x / 2f;
        float offset = childSize / 2f;

        for (int i = 0; i < 8; i++)
        {
            Vector3 centerOffset = new Vector3(
                ((i & 1) == 0 ? -offset : offset),
                ((i & 4) == 0 ? -offset : offset),
                ((i & 2) == 0 ? -offset : offset)
            );

            int childNodeIndex = nodes.Length;
            // Map the new child to its parent for cleanup after generation.
            subdivisionParentMap[childNodeIndex] = nodeIndex;

            nodes.Add(new OctreeNode(new Bounds(node.bounds.center + centerOffset, new Vector3(childSize, childSize, childSize)), node.depth + 1));
        }
    }

    private void Merge(int nodeIndex)
    {
        var node = nodes[nodeIndex];
        if (node.isLeaf || node.childrenIndex < 0) return;

        // The children are NOT destroyed here. They remain visible.
        // First, we generate the new parent chunk.
        GenerateChunk(nodeIndex);

        // Defer the destruction of the children until the parent is ready.
        // We store the parent's index and its children's start index for cleanup.
        pendingMergeCleanup[nodeIndex] = node.childrenIndex;

        // Mark the node as a leaf, but don't clear childrenIndex yet.
        node.isLeaf = true;
        nodes[nodeIndex] = node;
    }

    private void GenerateChunk(int nodeIndex)
    {
        if (generationJobs.ContainsKey(nodeIndex)) return;

        if (activeChunks.TryGetValue(nodeIndex, out Chunk chunk))
        {
            chunk.gameObject.SetActive(true);
        }
        else
        {
            var node = nodes[nodeIndex];

            var chunkData = chunkDataManager.GetChunkData(nodeIndex);

            var applyLayersHandle = terrainGenerator.ScheduleApplyLayers(chunkData.densityMap, chunkData.voxelTypes, TerrainSettings.MIN_NODE_SIZE, new float3(node.bounds.center.x, node.bounds.center.y, node.bounds.center.z), node.bounds.size.x, default);

            var applyModsJob = new ApplyModificationsJob
            {
                modifications = this.terrainModifications,
                nodeBounds = node.bounds,
                densityMap = chunkData.densityMap,
                voxelTypes = chunkData.voxelTypes,
                chunkSize = TerrainSettings.MIN_NODE_SIZE
            };
            var applyModsHandle = applyModsJob.Schedule(applyLayersHandle);

            applyModificationsHandle = JobHandle.CombineDependencies(applyModificationsHandle, applyModsHandle);

            var newChunk = chunkPool.Get();
            newChunk.gameObject.SetActive(true);
            activeChunks[nodeIndex] = newChunk;

            var jobHandle = newChunk.ScheduleTerrainGeneration(nodes[nodeIndex], chunkData.densityMap, chunkData.voxelTypes, applyModsHandle, out var meshData);
            generationJobs[nodeIndex] = (jobHandle, newChunk, meshData);
            SpawnEntitiesForChunk(nodeIndex, chunkData);
        }
    }

    private void HideChunk(int nodeIndex)
    {
        if (activeChunks.TryGetValue(nodeIndex, out Chunk chunk))
        {
            chunk.gameObject.SetActive(false);
        }
    }

    private void DestroyChunk(int nodeIndex)
    {
        DestroyEntitiesForChunk(nodeIndex);
        if (generationJobs.TryGetValue(nodeIndex, out var job))
        {
            job.jobHandle.Complete();
            job.meshData.Dispose();
            generationJobs.Remove(nodeIndex);
        }
    
        if (activeChunks.TryGetValue(nodeIndex, out Chunk chunk))
        {
            chunk.ClearMesh();
            chunkPool.Return(chunk);
            activeChunks.Remove(nodeIndex);
        }
        
        chunkDataManager.UnloadChunkData(nodeIndex);
        
        if (subdivisionParentMap.Remove(nodeIndex, out int parentNodeIndex))
        {
            HandleChildCompletion(parentNodeIndex);
        }
    }
    
    private void SpawnEntitiesForChunk(int nodeIndex, ChunkData chunkData)
    {
        var entitiesToSpawn = LoadEntityDataForChunk(nodeIndex, chunkData);

        foreach (var data in entitiesToSpawn)
        {
            Entity newEntity = entityManager.CreateEntity();
            entityManager.AddComponentData(newEntity, new EntityType { Value = data.entityType });
            entityManager.AddComponentData(newEntity, new LocalTransform { Position = data.position, Scale = 1f, Rotation = quaternion.identity });
            entityManager.AddComponentData(newEntity, new Velocity { Value = data.velocity });
            entityManager.AddComponentData(newEntity, new Health { Value = data.health, MaxValue = 100 });
            entityManager.AddSharedComponent(newEntity, new ChunkOwner { NodeIndex = nodeIndex });
        }
    }

    public List<int> GetEntityProcessingChunks()
    {
        var processingChunks = new List<int>();
        var stack = new Stack<int>();

        if (nodes.IsCreated && nodes.Length > 0)
        {
            stack.Push(0); 
        }

        while (stack.Count > 0)
        {
            int nodeIndex = stack.Pop();
            var node = nodes[nodeIndex];

            
            if (node.depth >= entityProcessingDepth)
            {
                processingChunks.Add(nodeIndex);
                continue; 
            }
            
            if (!node.isLeaf)
            {
                for (int i = 0; i < 8; i++)
                {
                    stack.Push(node.childrenIndex + i);
                }
            }
            else 
            {
                processingChunks.Add(nodeIndex);
            }
        }

        return processingChunks;
    }

    private void DestroyEntitiesForChunk(int nodeIndex)
    {
        EntityQuery query = entityManager.CreateEntityQuery(typeof(ChunkOwner));
        query.SetSharedComponentFilter(new ChunkOwner { NodeIndex = nodeIndex });
        entityManager.DestroyEntity(query);
    }

    private NativeList<EntityData> LoadEntityDataForChunk(int nodeIndex, ChunkData chunkData)
    {
        return chunkData.entities;
    }
    
    /// <summary>
    /// Handles the completion of a child chunk from a subdivision.
    /// Once all 8 children are accounted for, it triggers the destruction of the parent chunk.
    /// </summary>
    /// <param name="parentNodeIndex">The index of the parent node whose child has completed.</param>
    private void HandleChildCompletion(int parentNodeIndex)
    {
        // Initialize or increment the counter for the parent.
        if (!subdivisionCompletionCounter.TryGetValue(parentNodeIndex, out int count))
        {
            count = 0;
        }
        subdivisionCompletionCounter[parentNodeIndex] = count + 1;

        // If all 8 children are accounted for, the parent is now fully obsolete.
        if (subdivisionCompletionCounter[parentNodeIndex] >= 8)
        {
            // Destroy the parent chunk and clean up the counter.
            DestroyChunk(parentNodeIndex);
            subdivisionCompletionCounter.Remove(parentNodeIndex);
        }
    }

    public void ModifyTerrain(Vector3 worldPos, float strength, float radius, byte newVoxelType)
    {
        applyModificationsHandle.Complete();

        terrainModifications.Add(new TerrainModification
        {
            worldPos = worldPos,
            strength = strength,
            radius = radius,
            newVoxelType = newVoxelType
        });

        Bounds modificationBounds = new Bounds(worldPos, new Vector3(radius, radius, radius) * 2);
        List<int> modifiedNodeIndices = new List<int>();

        var stack = new Stack<int>();
        if (nodes.Length > 0)
        {
            stack.Push(0);
        }

        while (stack.Count > 0)
        {
            int nodeIndex = stack.Pop();
            var node = nodes[nodeIndex];

            if (!node.bounds.Intersects(modificationBounds))
            {
                continue;
            }

            if (node.isLeaf)
            {
                modifiedNodeIndices.Add(nodeIndex);
            }
            else if (node.childrenIndex != -1)
            {
                for (int i = 0; i < 8; i++)
                {
                    stack.Push(node.childrenIndex + i);
                }
            }
        }


        foreach (var index in modifiedNodeIndices)
        {
            RegenerateChunk(index);
        }
    }

    private void RegenerateChunk(int nodeIndex)
    {
        if (generationJobs.ContainsKey(nodeIndex))
        {
            return;
        }

        if (!activeChunks.TryGetValue(nodeIndex, out Chunk chunkToUpdate))
        {
            GenerateChunk(nodeIndex);
            return;
        }
        
        var node = nodes[nodeIndex];

        var chunkData = chunkDataManager.GetChunkData(nodeIndex);

        var applyLayersHandle = terrainGenerator.ScheduleApplyLayers(chunkData.densityMap, chunkData.voxelTypes, TerrainSettings.MIN_NODE_SIZE, new float3(node.bounds.center.x, node.bounds.center.y, node.bounds.center.z), node.bounds.size.x, default);

        var applyModsJob = new ApplyModificationsJob
        {
            modifications = this.terrainModifications,
            nodeBounds = node.bounds,
            densityMap = chunkData.densityMap,
            voxelTypes = chunkData.voxelTypes,
            chunkSize = TerrainSettings.MIN_NODE_SIZE
        };
        var applyModsHandle = applyModsJob.Schedule(applyLayersHandle);

        applyModificationsHandle = JobHandle.CombineDependencies(applyModificationsHandle, applyModsHandle);

        var jobHandle = chunkToUpdate.ScheduleTerrainGeneration(nodes[nodeIndex], chunkData.densityMap, chunkData.voxelTypes, applyModsHandle, out var meshData);

        generationJobs[nodeIndex] = (jobHandle, chunkToUpdate, meshData);
    }
    
    public bool Raycast(Ray ray, out BurstRaycast.RaycastHit hit)
    {
        var chunkDataDict = new Dictionary<int, ChunkData>();
        foreach (var chunk in activeChunks)
        {
            chunkDataDict.Add(chunk.Key, chunkDataManager.GetChunkData(chunk.Key));
        }
        return BurstRaycast.Raycast(ray, nodes, chunkDataDict, out hit);
    }

    private void OnDrawGizmos()
    {
        if (nodes.IsCreated)
        {
            foreach (var node in nodes)
            {
                Gizmos.color = Color.Lerp(Color.green, Color.red, node.depth / (float)maxDepth);
                Gizmos.DrawWireCube(node.bounds.center, node.bounds.size);
            }
        }
    }
}