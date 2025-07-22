using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;

public class OctreeTerrainManager : MonoBehaviour
{
    public static OctreeTerrainManager Instance;

    [Header("Terrain Settings")]
    public Material terrainMaterial;
    public Transform player;
    public Camera mainCamera;
    public int maxDepth = 2;
    public float nodeSize = 64;

    private OctreeNode rootNode;
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

        chunkPool = new Pool<Chunk>(() => {
            GameObject chunkObject = new GameObject("Chunk");
            chunkObject.transform.parent = transform;
            Chunk chunk = chunkObject.AddComponent<Chunk>();
            chunk.Initialize(terrainMaterial);
            return chunk;
        }, (chunk) => {
            chunk.gameObject.SetActive(true);
        }, (chunk) => {
            chunk.DisposeChunkResources(); // Dispose resources before returning to pool
            chunk.gameObject.SetActive(false);
        });
    }

    void Start()
    {
        rootNode = new OctreeNode(Vector3.zero, nodeSize, 0);
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
        if (triangulationTable.IsCreated) triangulationTable.Dispose();
        if (cornerOffsets.IsCreated) cornerOffsets.Dispose();
        if (cornerIndexAFromEdge.IsCreated) cornerIndexAFromEdge.Dispose();
        if (cornerIndexBFromEdge.IsCreated) cornerIndexBFromEdge.Dispose();
    }

    void Update()
    {
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
        rootNode.Update(player.position, frustumPlanes, chunkPool);
    }

    public void ModifyTerrain(Vector3 worldPos, float strength, float radius)
    {
        rootNode.ModifyTerrain(worldPos, strength, radius, chunkPool);
    }

    void OnDrawGizmos()
    {
        if (rootNode != null)
        {
            rootNode.DrawGizmos();
        }
    }
}