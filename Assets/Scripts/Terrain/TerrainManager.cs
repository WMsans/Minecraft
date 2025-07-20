using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using System.Collections.Generic;
using Unity.Mathematics;

public class TerrainManager : MonoBehaviour
{
    public static TerrainManager Instance;

    [Header("Terrain Settings")]
    public Material terrainMaterial;
    public Transform player;
    public Camera mainCamera;
    public const int chunkSize = 16;
    public const int renderDistance = 20;

    private Dictionary<Vector3Int, Chunk> activeChunks = new Dictionary<Vector3Int, Chunk>();
    private Queue<Chunk> chunkPool = new Queue<Chunk>();

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
    }

    void OnDestroy()
    {
        DisposeMarchingCubesTables();
        foreach(var chunk in activeChunks.Values)
        {
            if (chunk != null && chunk.gameObject != null)
            {
                Destroy(chunk.gameObject);
            }
        }
        foreach(var chunk in chunkPool)
        {
            if (chunk != null && chunk.gameObject != null)
            {
                Destroy(chunk.gameObject);
            }
        }
    }

    void Update()
    {
        UpdateChunks();
    }

    void UpdateChunks()
    {
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
        Vector3Int playerChunkPos = GetChunkCoordinatesFromPosition(player.position);

        List<Vector3Int> chunksToDeactivate = new List<Vector3Int>();
        foreach (var chunkPair in activeChunks)
        {
            float distance = Vector3.Distance(playerChunkPos, chunkPair.Key);
            if (distance > renderDistance)
            {
                chunksToDeactivate.Add(chunkPair.Key);
            }
        }

        foreach (var chunkPos in chunksToDeactivate)
        {
            Chunk chunkToDeactivate = activeChunks[chunkPos];
            chunkToDeactivate.gameObject.SetActive(false);
            chunkPool.Enqueue(chunkToDeactivate);
            activeChunks.Remove(chunkPos);
        }

        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int y = -renderDistance; y <= renderDistance; y++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    Vector3Int chunkPos = new Vector3Int(playerChunkPos.x + x, playerChunkPos.y + y, playerChunkPos.z + z);
                    float distance = Vector3.Distance(playerChunkPos, chunkPos);

                    if (distance <= renderDistance)
                    {
                        var bounds = new Bounds(chunkPos * chunkSize + Vector3.one * chunkSize / 2f, Vector3.one * chunkSize);
                        if (GeometryUtility.TestPlanesAABB(frustumPlanes, bounds))
                        {
                            int lod = GetLODFromDistance(distance);

                            if (!activeChunks.ContainsKey(chunkPos))
                            {
                                GetOrCreateChunk(chunkPos, lod);
                            }
                            else
                            {
                                Chunk chunk = activeChunks[chunkPos];
                                if (chunk.lod != lod)
                                {
                                    chunk.lod = lod;
                                    chunk.GenerateTerrain();
                                }
                                else if (!chunk.gameObject.activeSelf)
                                {
                                    chunk.gameObject.SetActive(true);
                                }
                            }
                        }
                    }
                }
            }
        }
    }

    void GetOrCreateChunk(Vector3Int chunkPos, int lod)
    {
        Chunk newChunk;
        if (chunkPool.Count > 0)
        {
            newChunk = chunkPool.Dequeue();
            newChunk.gameObject.transform.position = chunkPos * chunkSize;
            newChunk.gameObject.SetActive(true);
            newChunk.Initialize(chunkPos, terrainMaterial);
        }
        else
        {
            GameObject chunkObject = new GameObject($"Chunk {chunkPos.x}, {chunkPos.y}, {chunkPos.z}");
            chunkObject.transform.position = chunkPos * chunkSize;
            chunkObject.transform.parent = this.transform;
            newChunk = chunkObject.AddComponent<Chunk>();
            newChunk.Initialize(chunkPos, terrainMaterial);
        }

        activeChunks.Add(chunkPos, newChunk);
        newChunk.lod = lod;
        newChunk.GenerateTerrain();
    }

    public void ModifyTerrain(Vector3 worldPos, float strength, float radius)
    {
        int buildRadius = Mathf.CeilToInt(radius);
        // Use a HashSet to keep track of which chunks have been modified.
        HashSet<Vector3Int> chunksToRegenerate = new HashSet<Vector3Int>();

        // Iterate through all points within the modification radius.
        for (int x = -buildRadius; x <= buildRadius; x++)
        {
            for (int y = -buildRadius; y <= buildRadius; y++)
            {
                for (int z = -buildRadius; z <= buildRadius; z++)
                {
                    Vector3Int offset = new Vector3Int(x, y, z);
                    if (offset.magnitude > radius) continue;

                    Vector3Int modifiedPos = Vector3Int.FloorToInt(worldPos) + offset;

                    float falloff = 1 - (offset.magnitude / radius);
                    float modifiedStrength = strength * falloff;

                    // A single density point can be part of the data for up to 8 chunks.
                    // We need to identify all of them to apply the modification.
                    Vector3Int anchorChunkPos = GetChunkCoordinatesFromPosition(modifiedPos);

                    for (int i = 0; i < 8; i++)
                    {
                        // Check the anchor chunk and its 7 neighbors in the negative directions.
                        Vector3Int chunkOffset = new Vector3Int(-(i & 1), -((i & 2) >> 1), -((i & 4) >> 2));
                        Vector3Int chunkToModifyPos = anchorChunkPos + chunkOffset;

                        if (activeChunks.TryGetValue(chunkToModifyPos, out Chunk chunk))
                        {
                            // ModifyDensity has internal checks, so it will only apply the change
                            // if the point is within this chunk's density map.
                            chunk.ModifyDensity(modifiedPos, modifiedStrength);
                            chunksToRegenerate.Add(chunkToModifyPos);
                        }
                    }
                }
            }
        }

        // After all density modifications are done, regenerate the affected chunks.
        foreach (Vector3Int chunkPos in chunksToRegenerate)
        {
            if (activeChunks.TryGetValue(chunkPos, out Chunk chunk))
            {
                chunk.GenerateTerrain();
            }
        }
    }

    int GetLODFromDistance(float distance)
    {
        if (distance < renderDistance * 0.25f) return 0;
        if (distance < renderDistance * 0.5f) return 1;
        if (distance < renderDistance * 0.75f) return 2;
        return 3;
    }

    public static Vector3Int GetChunkCoordinatesFromPosition(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / chunkSize);
        int y = Mathf.FloorToInt(position.y / chunkSize);
        int z = Mathf.FloorToInt(position.z / chunkSize);
        return new Vector3Int(x, y, z);
    }

    private void InitializeMarchingCubesTables()
    {
        triangulationTable = new NativeArray<int>(MarchingCubesTables.triangulation, Allocator.Persistent);
        cornerOffsets = new NativeArray<int3>(MarchingCubesTables.cornerOffsets, Allocator.Persistent);
        cornerIndexAFromEdge = new NativeArray<int>(MarchingCubesTables.cornerIndexAFromEdge, Allocator.Persistent);
        cornerIndexBFromEdge = new NativeArray<int>(MarchingCubesTables.cornerIndexBFromEdge, Allocator.Persistent);
    }

    private void DisposeMarchingCubesTables()
    {
        if (triangulationTable.IsCreated) triangulationTable.Dispose();
        if (cornerOffsets.IsCreated) cornerOffsets.Dispose();
        if (cornerIndexAFromEdge.IsCreated) cornerIndexAFromEdge.Dispose();
        if (cornerIndexBFromEdge.IsCreated) cornerIndexBFromEdge.Dispose();
    }
}