using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;
using System.Threading.Tasks;

public class OctreeTerrainManager : MonoBehaviour
{
    public static OctreeTerrainManager Instance;

    [Header("Terrain Settings")]
    public Material terrainMaterial;
    public Transform player;
    public Camera mainCamera;
    public int maxDepth = 2;
    public float nodeSize = 64;

    private NativeList<BurstOctreeNode> nodes;
    private Dictionary<int, Chunk> activeChunks;
    private Pool<Chunk> chunkPool;

    private TerrainGenerator terrainGenerator;

    // The dictionary now stores a tuple of the task and the chunk.
    private Dictionary<int, (Task<Chunk.MeshData> task, Chunk chunk)> generationTasks;

    public NativeArray<int> triangulationTable;
    public NativeArray<int3> cornerOffsets;
    public NativeArray<int> cornerIndexAFromEdge;
    public NativeArray<int> cornerIndexBFromEdge;

    void Awake()
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
            chunk.DisposeChunkResources();
            chunk.gameObject.SetActive(false);
        });
    }

    void Start()
    {
        nodes = new NativeList<BurstOctreeNode>(Allocator.Persistent);
        activeChunks = new Dictionary<int, Chunk>();
        // Initialize the new dictionary type.
        generationTasks = new Dictionary<int, (Task<Chunk.MeshData> task, Chunk chunk)>();
        nodes.Add(new BurstOctreeNode(new Bounds(Vector3.zero, Vector3.one * nodeSize), 0));

        terrainGenerator = new TerrainGenerator();
    }

    private void InitializeMarchingCubesTables()
    {
        triangulationTable = new NativeArray<int>(MarchingCubesTables.triangulation, Allocator.Persistent);
        cornerOffsets = new NativeArray<int3>(MarchingCubesTables.cornerOffsets, Allocator.Persistent);
        cornerIndexAFromEdge = new NativeArray<int>(MarchingCubesTables.cornerIndexAFromEdge, Allocator.Persistent);
        cornerIndexBFromEdge = new NativeArray<int>(MarchingCubesTables.cornerIndexBFromEdge, Allocator.Persistent);
    }

    void OnDestroy()
    {
        if (nodes.IsCreated) nodes.Dispose();
        if (triangulationTable.IsCreated) triangulationTable.Dispose();
        if (cornerOffsets.IsCreated) cornerOffsets.Dispose();
        if (cornerIndexAFromEdge.IsCreated) cornerIndexAFromEdge.Dispose();
        if (cornerIndexBFromEdge.IsCreated) cornerIndexBFromEdge.Dispose();
    }

    void Update()
    {
        var frustumPlanes = FrustumCulling.GetFrustumPlanes(mainCamera);

        var toSubdivide = new NativeList<int>(Allocator.TempJob);
        var toMerge = new NativeList<int>(Allocator.TempJob);
        var toGenerate = new NativeList<int>(Allocator.TempJob);
        var toDestroy = new NativeList<int>(Allocator.TempJob);

        var job = new OctreeUpdateJob
        {
            playerPos = player.position,
            frustumPlanes = frustumPlanes,
            maxDepth = this.maxDepth,
            nodes = this.nodes,
            toSubdivide = toSubdivide,
            toMerge = toMerge,
            toGenerate = toGenerate,
            toDestroy = toDestroy
        };

        job.Schedule().Complete();

        foreach (var index in toSubdivide) Subdivide(index);
        foreach (var index in toMerge) Merge(index);
        foreach (var index in toGenerate) GenerateChunk(index);
        foreach (var index in toDestroy) DestroyChunk(index);

        ProcessCompletedTasks();

        toSubdivide.Dispose();
        toMerge.Dispose();
        toGenerate.Dispose();
        toDestroy.Dispose();
    }

    private void ProcessCompletedTasks()
    {
        var completedTaskIndices = new List<int>();
        foreach (var entry in generationTasks)
        {
            if (entry.Value.task.IsCompleted)
            {
                completedTaskIndices.Add(entry.Key);
            }
        }

        foreach (var index in completedTaskIndices)
        {
            var (task, chunk) = generationTasks[index];
            generationTasks.Remove(index);

            var meshData = task.Result;

            if (activeChunks.ContainsKey(index) && activeChunks[index] == chunk)
            {
                // Chunk is still active, apply the mesh
                chunk.ApplyGeneratedMesh(meshData);
            }
            else
            {
                // Chunk was destroyed, dispose the mesh data and return the chunk to the pool
                if (meshData.IsCreated) meshData.Dispose();
                chunkPool.Return(chunk);
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
            nodes.Add(new BurstOctreeNode(new Bounds(node.bounds.center + centerOffset, new Vector3(childSize, childSize, childSize)), node.depth + 1));
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
        if (!activeChunks.ContainsKey(nodeIndex) && !generationTasks.ContainsKey(nodeIndex))
        {
            var chunk = chunkPool.Get();
            chunk.gameObject.SetActive(true);
            activeChunks[nodeIndex] = chunk;

            var task = Task.Run(() =>
            {
                return chunk.GenerateTerrain(nodes[nodeIndex]);
            });
            generationTasks[nodeIndex] = (task, chunk);
        }
    }

    private void DestroyChunk(int nodeIndex)
    {
        if (activeChunks.TryGetValue(nodeIndex, out Chunk chunk))
        {
            if (!generationTasks.ContainsKey(nodeIndex))
            {
                // No generation task, so it's safe to return to the pool immediately.
                chunkPool.Return(chunk);
            }
            // If there is a generation task, we just remove the chunk from activeChunks.
            // ProcessCompletedTasks will handle returning it to the pool later.
            activeChunks.Remove(nodeIndex);
        }
    }

    public void ModifyTerrain(Vector3 worldPos, float strength, float radius)
    {
        Bounds modificationBounds = new Bounds(worldPos, new Vector3(radius, radius, radius) * 2);

        for (int i = 0; i < nodes.Length; i++)
        {
            var node = nodes[i];
            if (node.isLeaf && node.bounds.Intersects(modificationBounds))
            {
                if (activeChunks.ContainsKey(i))
                {
                    activeChunks[i].ModifyDensity(worldPos, strength, radius);
                }
            }
        }
    }

    void OnDrawGizmos()
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