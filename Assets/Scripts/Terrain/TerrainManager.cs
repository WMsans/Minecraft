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
    public const int renderDistance = 20; // In chunks

    private Dictionary<Vector3Int, Chunk> activeChunks = new Dictionary<Vector3Int, Chunk>();
    private Queue<Chunk> chunkPool = new Queue<Chunk>();
    private HashSet<Vector3Int> processedChunkPositions = new HashSet<Vector3Int>();

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
        foreach (var chunk in activeChunks.Values)
        {
            if (chunk != null && chunk.gameObject != null)
            {
                Destroy(chunk.gameObject);
            }
        }
        foreach (var chunk in chunkPool)
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
        processedChunkPositions.Clear();
        List<Vector3Int> chunksToDeactivate = new List<Vector3Int>(activeChunks.Keys);

        for (int x = -renderDistance; x <= renderDistance; x++)
        {
            for (int y = -renderDistance; y <= renderDistance; y++)
            {
                for (int z = -renderDistance; z <= renderDistance; z++)
                {
                    Vector3Int chunkCoord = playerChunkPos + new Vector3Int(x, y, z);
                    
                    if (processedChunkPositions.Contains(chunkCoord))
                    {
                        continue;
                    }

                    float distance = Vector3.Distance(playerChunkPos, chunkCoord);
                    if (distance > renderDistance)
                    {
                        continue;
                    }

                    int lod = GetLODFromDistance(distance);
                    int lodScale = 1 << lod;

                    Vector3Int snappedChunkPos = new Vector3Int(
                        Mathf.FloorToInt((float)chunkCoord.x / lodScale) * lodScale,
                        Mathf.FloorToInt((float)chunkCoord.y / lodScale) * lodScale,
                        Mathf.FloorToInt((float)chunkCoord.z / lodScale) * lodScale
                    );
                    
                    chunksToDeactivate.Remove(snappedChunkPos);
                    
                    for (int sx = 0; sx < lodScale; sx++)
                    {
                        for (int sy = 0; sy < lodScale; sy++)
                        {
                            for (int sz = 0; sz < lodScale; sz++)
                            {
                                processedChunkPositions.Add(snappedChunkPos + new Vector3Int(sx, sy, sz));
                            }
                        }
                    }

                    var bounds = new Bounds((snappedChunkPos * chunkSize) + (Vector3.one * (chunkSize * lodScale) / 2f), Vector3.one * (chunkSize * lodScale));
                    if (!GeometryUtility.TestPlanesAABB(frustumPlanes, bounds))
                    {
                        continue;
                    }

                    if (activeChunks.TryGetValue(snappedChunkPos, out Chunk chunk))
                    {
                        if (chunk.lod != lod)
                        {
                            RecycleChunk(snappedChunkPos);
                            GetOrCreateChunk(snappedChunkPos, lod);
                        }
                        else if (!chunk.gameObject.activeSelf)
                        {
                            chunk.gameObject.SetActive(true);
                        }
                    }
                    else
                    {
                        GetOrCreateChunk(snappedChunkPos, lod);
                    }
                }
            }
        }

        foreach (var chunkPos in chunksToDeactivate)
        {
            RecycleChunk(chunkPos);
        }
    }
    
    void RecycleChunk(Vector3Int chunkPos)
    {
        if (activeChunks.TryGetValue(chunkPos, out Chunk chunkToDeactivate))
        {
            chunkToDeactivate.gameObject.SetActive(false);
            chunkPool.Enqueue(chunkToDeactivate);
            activeChunks.Remove(chunkPos);
        }
    }

    void GetOrCreateChunk(Vector3Int chunkPos, int lod)
    {
        // Before creating a new chunk, remove any smaller chunks that would be overlapped by it.
        int lodScale = 1 << lod;
        for (int x = 0; x < lodScale; x++) {
            for (int y = 0; y < lodScale; y++) {
                for (int z = 0; z < lodScale; z++) {
                    Vector3Int smallerChunkPos = chunkPos + new Vector3Int(x, y, z);
                    if (smallerChunkPos != chunkPos && activeChunks.ContainsKey(smallerChunkPos)) {
                         RecycleChunk(smallerChunkPos);
                    }
                }
            }
        }
    
        Chunk newChunk;
        if (chunkPool.Count > 0)
        {
            newChunk = chunkPool.Dequeue();
            newChunk.gameObject.name = $"Chunk {chunkPos.x}, {chunkPos.y}, {chunkPos.z}";
            newChunk.transform.position = (Vector3)chunkPos * chunkSize;
            newChunk.gameObject.SetActive(true);
        }
        else
        {
            GameObject chunkObject = new GameObject($"Chunk {chunkPos.x}, {chunkPos.y}, {chunkPos.z}");
            chunkObject.transform.position = (Vector3)chunkPos * chunkSize;
            chunkObject.transform.parent = this.transform;
            newChunk = chunkObject.AddComponent<Chunk>();
        }
        
        newChunk.Initialize(chunkPos, terrainMaterial, lod);
        activeChunks.Add(chunkPos, newChunk);
        newChunk.GenerateTerrain();
    }


    public void ModifyTerrain(Vector3 worldPos, float strength, float radius)
    {
        int buildRadius = Mathf.CeilToInt(radius);
        HashSet<Vector3Int> chunksToRegenerate = new HashSet<Vector3Int>();

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

                    Vector3Int anchorChunkPos = GetChunkCoordinatesFromPosition(modifiedPos);

                    for (int i = 0; i < 8; i++)
                    {
                        Vector3Int chunkOffset = new Vector3Int(-(i & 1), -((i & 2) >> 1), -((i & 4) >> 2));
                        Vector3Int chunkToModifyPos = anchorChunkPos + chunkOffset;

                        if (activeChunks.TryGetValue(chunkToModifyPos, out Chunk chunk))
                        {
                            chunk.ModifyDensity(modifiedPos, modifiedStrength);
                            chunksToRegenerate.Add(chunkToModifyPos);
                        }
                    }
                }
            }
        }

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
        if (distance < renderDistance * 0.25f) return 0; // Highest resolution
        if (distance < renderDistance * 0.5f) return 1;
        if (distance < renderDistance * 0.75f) return 2;
        return 3; // Lowest resolution
    }

    public static Vector3Int GetChunkCoordinatesFromPosition(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / chunkSize);
        int y = Mathf.FloorToInt(position.y / chunkSize);
        int z = Mathf.FloorToInt(position.z / chunkSize);
        return new Vector3Int(x, y, z);
    }
    
    public int GetChunkLOD(Vector3Int chunkPos)
    {
        if (activeChunks.TryGetValue(chunkPos, out var chunk))
        {
            return chunk.lod;
        }
        // Return a default high LOD if neighbor doesn't exist to avoid seams with empty space
        return 0; 
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