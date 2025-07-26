using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using System.Threading.Tasks;
using Unity.Burst;

public class OctreeTerrainManager : MonoBehaviour
{
    public static OctreeTerrainManager Instance;

    [Header("Terrain Settings")]
    public Material terrainMaterial;
    public Transform player;
    public Camera mainCamera;
    public int maxDepth = 2;
    public float nodeSize = 64;

    private NativeList<OctreeNode> nodes;
    private Dictionary<int, Chunk> activeChunks;
    private Pool<Chunk> chunkPool;

    private TerrainGenerator terrainGenerator;

    private Dictionary<int, (JobHandle jobHandle, Chunk chunk, Chunk.MeshData meshData)> generationJobs;
    private JobHandle applyModificationsHandle; // Added to track modification jobs

    public NativeArray<int> triangulationTable;
    public NativeArray<int3> cornerOffsets;
    public NativeArray<int> cornerIndexAFromEdge;
    public NativeArray<int> cornerIndexBFromEdge;
    
    // Struct to hold modification data
    public struct TerrainModification
    {
        public float3 worldPos;
        public float strength;
        public float radius;
        public byte newVoxelType;
    }
    // List of all modifications made
    private NativeList<TerrainModification> terrainModifications;

    [BurstCompile]
    private struct ApplyModificationsJob : IJob
    {
        [ReadOnly] public NativeList<TerrainModification> modifications;
        [ReadOnly] public Bounds nodeBounds;
        public NativeArray<float> densityMap;
        public NativeArray<byte> voxelTypes;
        [ReadOnly] public int chunkSize;

        public void Execute()
        {
            for (int i = 0; i < modifications.Length; i++)
            {
                var mod = modifications[i];
                
                Bounds modBounds = new Bounds(mod.worldPos, new float3(mod.radius, mod.radius, mod.radius) * 2);
                if (!modBounds.Intersects(nodeBounds)) continue;

                int buildRadius = (int)math.ceil(mod.radius);
                for (int x = -buildRadius; x <= buildRadius; x++)
                {
                    for (int y = -buildRadius; y <= buildRadius; y++)
                    {
                        for (int z = -buildRadius; z <= buildRadius; z++)
                        {
                            float3 offset = new float3(x, y, z);
                            if (math.length(offset) > mod.radius) continue;

                            float3 modifiedPos = math.floor(mod.worldPos) + offset;

                            if (modifiedPos.x < nodeBounds.min.x || modifiedPos.x > nodeBounds.max.x ||
                                modifiedPos.y < nodeBounds.min.y || modifiedPos.y > nodeBounds.max.y ||
                                modifiedPos.z < nodeBounds.min.z || modifiedPos.z > nodeBounds.max.z)
                            {
                                continue;
                            }

                            int densityX = (int)((modifiedPos.x - nodeBounds.min.x) / nodeBounds.size.x * chunkSize);
                            int densityY = (int)((modifiedPos.y - nodeBounds.min.y) / nodeBounds.size.y * chunkSize);
                            int densityZ = (int)((modifiedPos.z - nodeBounds.min.z) / nodeBounds.size.z * chunkSize);

                            if (densityX >= 0 && densityX <= chunkSize &&
                                densityY >= 0 && densityY <= chunkSize &&
                                densityZ >= 0 && densityZ <= chunkSize)
                            {
                                int index = densityX + (chunkSize + 1) * (densityY + (chunkSize + 1) * densityZ);
                                if (index >= 0 && index < densityMap.Length)
                                {
                                    float falloff = 1 - (math.length(offset) / mod.radius);
                                    densityMap[index] += mod.strength * falloff;
                                    if(mod.strength < 0)
                                    {
                                        voxelTypes[index] = mod.newVoxelType;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }


    private struct ChunkData
    {
        public NativeArray<float> densityMap;
        public NativeArray<byte> voxelTypes;

        public bool IsCreated => densityMap.IsCreated && voxelTypes.IsCreated;

        public void Allocate()
        {
            densityMap = new NativeArray<float>((TerrainSettings.MIN_NODE_SIZE + 1) * (TerrainSettings.MIN_NODE_SIZE + 1) * (TerrainSettings.MIN_NODE_SIZE + 1), Allocator.Persistent);
            voxelTypes = new NativeArray<byte>((TerrainSettings.MIN_NODE_SIZE + 1) * (TerrainSettings.MIN_NODE_SIZE + 1) * (TerrainSettings.MIN_NODE_SIZE + 1), Allocator.Persistent);
        }

        public void Dispose()
        {
            if (densityMap.IsCreated) densityMap.Dispose();
            if (voxelTypes.IsCreated) voxelTypes.Dispose();
        }
    }
    
    private Dictionary<int, ChunkData> activeChunkData;

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

        chunkPool = new Pool<Chunk>(() =>
        {
            GameObject chunkObject = new GameObject("Chunk");
            chunkObject.transform.parent = transform;
            Chunk chunk = chunkObject.AddComponent<Chunk>();
            chunk.Initialize(terrainMaterial, terrainGenerator);
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
        activeChunkData = new Dictionary<int, ChunkData>(); 
        generationJobs = new Dictionary<int, (JobHandle, Chunk, Chunk.MeshData)>();
        nodes.Add(new OctreeNode(new Bounds(Vector3.zero, Vector3.one * nodeSize), 0));

        terrainGenerator = new TerrainGenerator();
        terrainGenerator.Initialize();
        
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
        applyModificationsHandle.Complete(); // Complete handle on destroy
        foreach (var genJob in generationJobs.Values)
        {
            genJob.jobHandle.Complete();
            genJob.meshData.Dispose();
        }
        generationJobs.Clear();

        foreach(var data in activeChunkData.Values)
        {
            data.Dispose();
        }
        activeChunkData.Clear();

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
            }
            else
            {
                if (meshData.IsCreated) meshData.Dispose();
                chunkPool.Return(chunk);
            }
            
            // The data is now on the GPU, we can dispose of it.
            if(activeChunkData.TryGetValue(index, out var chunkData))
            {
                chunkData.Dispose();
                activeChunkData.Remove(index);
            }
        }
    }


    private void Subdivide(int nodeIndex)
    {
        DestroyChunk(nodeIndex);

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
            nodes.Add(new OctreeNode(new Bounds(node.bounds.center + centerOffset, new Vector3(childSize, childSize, childSize)), node.depth + 1));
        }
    }

    private void Merge(int nodeIndex)
    {
        var node = nodes[nodeIndex];
        if (node.isLeaf || node.childrenIndex < 0) return;
        
        var stack = new Stack<int>();
        stack.Push(node.childrenIndex);

        while (stack.Count > 0)
        {
            int currentIndex = stack.Pop();
            for(int i = 0; i < 8; i++)
            {
                var childNode = nodes[currentIndex + i];
                if(!childNode.isLeaf)
                {
                    stack.Push(childNode.childrenIndex);
                }
                DestroyChunk(currentIndex + i);
            }
        }

        node.isLeaf = true;
        node.childrenIndex = -1;
        nodes[nodeIndex] = node;
        
        if (FrustumCulling.TestAABB(FrustumCulling.GetFrustumPlanes(mainCamera), node.bounds))
        {
            GenerateChunk(nodeIndex);
        }
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

            var chunkData = new ChunkData();
            chunkData.Allocate();
            activeChunkData[nodeIndex] = chunkData;

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

            // Combine the handle with the global modification handle
            applyModificationsHandle = JobHandle.CombineDependencies(applyModificationsHandle, applyModsHandle);

            var newChunk = chunkPool.Get();
            newChunk.gameObject.SetActive(true);
            activeChunks[nodeIndex] = newChunk;

            var jobHandle = newChunk.ScheduleTerrainGeneration(nodes[nodeIndex], chunkData.densityMap, chunkData.voxelTypes, applyModsHandle, out var meshData);
            generationJobs[nodeIndex] = (jobHandle, newChunk, meshData);
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
        if (generationJobs.TryGetValue(nodeIndex, out var job))
        {
            job.jobHandle.Complete();
            job.meshData.Dispose();
            generationJobs.Remove(nodeIndex);
        }

        if (activeChunks.TryGetValue(nodeIndex, out Chunk chunk))
        {
            chunkPool.Return(chunk);
            activeChunks.Remove(nodeIndex);
        }
        if (activeChunkData.TryGetValue(nodeIndex, out var data))
        {
            data.Dispose();
            activeChunkData.Remove(nodeIndex);
        }
    }

    public void ModifyTerrain(Vector3 worldPos, float strength, float radius, byte newVoxelType)
    {
        // Complete jobs before modifying the list
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

        for (int i = 0; i < nodes.Length; i++)
        {
            var node = nodes[i];
            if (node.isLeaf && node.bounds.Intersects(modificationBounds))
            {
                modifiedNodeIndices.Add(i);
            }
        }

        foreach(var index in modifiedNodeIndices)
        {
            DestroyChunk(index);
            GenerateChunk(index);
        }
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