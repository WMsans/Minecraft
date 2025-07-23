using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Jobs;

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
            chunk.Initialize(terrainMaterial);
            chunk.gameObject.SetActive(false); // Ensure chunks start as inactive
            return chunk;
        }, (chunk) =>
        {
            // No longer need to set active here
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
        nodes.Add(new BurstOctreeNode(new Bounds(Vector3.zero, Vector3.one * nodeSize), 0));
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

        // Process results
        foreach (var index in toSubdivide) Subdivide(index);
        foreach (var index in toMerge) Merge(index);
        foreach (var index in toGenerate) GenerateChunk(index);
        foreach (var index in toDestroy) DestroyChunk(index);

        toSubdivide.Dispose();
        toMerge.Dispose();
        toGenerate.Dispose();
        toDestroy.Dispose();
    }

    private void Subdivide(int nodeIndex)
    {
        var node = nodes[nodeIndex];
        if (activeChunks.ContainsKey(nodeIndex))
        {
            chunkPool.Return(activeChunks[nodeIndex]);
            activeChunks.Remove(nodeIndex);
        }

        node.isLeaf = false;
        node.childrenIndex = nodes.Length;
        nodes[nodeIndex] = node;

        float childSize = node.bounds.size.x / 2f;
        float offset = childSize / 2f;

        nodes.Add(new BurstOctreeNode(new Bounds(node.bounds.center + new Vector3(-offset, -offset, -offset), new Vector3(childSize, childSize, childSize)), node.depth + 1));
        nodes.Add(new BurstOctreeNode(new Bounds(node.bounds.center + new Vector3(offset, -offset, -offset), new Vector3(childSize, childSize, childSize)), node.depth + 1));
        nodes.Add(new BurstOctreeNode(new Bounds(node.bounds.center + new Vector3(offset, -offset, offset), new Vector3(childSize, childSize, childSize)), node.depth + 1));
        nodes.Add(new BurstOctreeNode(new Bounds(node.bounds.center + new Vector3(-offset, -offset, offset), new Vector3(childSize, childSize, childSize)), node.depth + 1));
        nodes.Add(new BurstOctreeNode(new Bounds(node.bounds.center + new Vector3(-offset, offset, -offset), new Vector3(childSize, childSize, childSize)), node.depth + 1));
        nodes.Add(new BurstOctreeNode(new Bounds(node.bounds.center + new Vector3(offset, offset, -offset), new Vector3(childSize, childSize, childSize)), node.depth + 1));
        nodes.Add(new BurstOctreeNode(new Bounds(node.bounds.center + new Vector3(offset, offset, offset), new Vector3(childSize, childSize, childSize)), node.depth + 1));
        nodes.Add(new BurstOctreeNode(new Bounds(node.bounds.center + new Vector3(-offset, offset, offset), new Vector3(childSize, childSize, childSize)), node.depth + 1));
    }

    private void Merge(int nodeIndex)
    {
        var node = nodes[nodeIndex];

        // If it's already a leaf or has no children, there's nothing to merge.
        if (node.isLeaf || node.childrenIndex < 0)
        {
            return;
        }

        // Use a stack to find and destroy all descendant chunks.
        var stack = new Stack<int>();
        for (int i = 0; i < 8; i++)
        {
            stack.Push(node.childrenIndex + i);
        }

        while (stack.Count > 0)
        {
            int currentIndex = stack.Pop();
            if (currentIndex < 0 || currentIndex >= nodes.Length) continue;

            var currentNode = nodes[currentIndex];

            // If this node is a parent, add its children to the stack for cleanup.
            if (!currentNode.isLeaf && currentNode.childrenIndex != -1)
            {
                for (int i = 0; i < 8; i++)
                {
                    stack.Push(currentNode.childrenIndex + i);
                }
            }

            // Destroy the chunk associated with this descendant node.
            if (activeChunks.ContainsKey(currentIndex))
            {
                chunkPool.Return(activeChunks[currentIndex]);
                activeChunks.Remove(currentIndex);
            }
        }

        // Now that children's chunks are gone, make this node a leaf.
        node.isLeaf = true;
        node.childrenIndex = -1;
        nodes[nodeIndex] = node;

        // The job didn't add this new leaf to `toGenerate` because it saw it as a parent.
        // We need to generate its chunk now if it's visible.
        if (FrustumCulling.TestAABB(FrustumCulling.GetFrustumPlanes(mainCamera), node.bounds))
        {
            GenerateChunk(nodeIndex);
        }
    }

    private void GenerateChunk(int nodeIndex)
    {
        if (!activeChunks.ContainsKey(nodeIndex))
        {
            var chunk = chunkPool.Get();
            chunk.GenerateTerrain(nodes[nodeIndex]);
            chunk.gameObject.SetActive(true);
            activeChunks[nodeIndex] = chunk;
        }
    }

    private void DestroyChunk(int nodeIndex)
    {
        if (activeChunks.ContainsKey(nodeIndex))
        {
            chunkPool.Return(activeChunks[nodeIndex]);
            activeChunks.Remove(nodeIndex);
        }
    }

    public void ModifyTerrain(Vector3 worldPos, float strength, float radius)
    {
        // To be implemented with a Burst job
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