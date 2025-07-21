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
    public int renderDistance = 30; // In chunks
    public float lodLevelDistance = 20f; // Base distance for LOD 0


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
        foreach (var chunk in activeChunks.Values)
        {
            if (chunk != null && chunk.gameObject != null)
            {
                chunk.WaitForJob();
                Destroy(chunk.gameObject);
            }
        }
        activeChunks.Clear();
        foreach (var chunk in chunkPool)
        {
            if (chunk != null && chunk.gameObject != null)
            {
                Destroy(chunk.gameObject);
            }
        }
        chunkPool.Clear();
    }

    void Update()
    {
        UpdateChunks();
    }

    void UpdateChunks()
    {
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
        Vector3Int playerChunkPos = GetChunkCoordinatesFromPosition(player.position);

        var requiredChunks = new Dictionary<Vector3Int, int>();
        var processedChunkPositions = new HashSet<Vector3Int>();

        // 1. Determine all chunks that should be visible based on LOD distances
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

                    requiredChunks[snappedChunkPos] = lod;

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
                }
            }
        }

        // 2. Recycle chunks that are not in the required list or have the wrong LOD
        var chunksToRecycle = new List<Vector3Int>();
        foreach (var chunk in activeChunks)
        {
            if (!requiredChunks.ContainsKey(chunk.Key) || requiredChunks[chunk.Key] != chunk.Value.lod)
            {
                chunksToRecycle.Add(chunk.Key);
            }
        }
        foreach (var chunkPos in chunksToRecycle)
        {
            RecycleChunk(chunkPos);
        }

        // 3. Create new chunks or activate/update existing ones
        foreach (var required in requiredChunks)
        {
            Vector3Int chunkPos = required.Key;
            int lod = required.Value;

            var bounds = new Bounds((chunkPos * chunkSize) + (Vector3.one * (chunkSize * (1 << lod)) / 2f), Vector3.one * (chunkSize * (1 << lod)));
            bool inFrustum = GeometryUtility.TestPlanesAABB(frustumPlanes, bounds);

            if (activeChunks.TryGetValue(chunkPos, out Chunk chunk))
            {
                // Chunk exists, just handle its active state based on frustum
                if (chunk.gameObject.activeSelf != inFrustum)
                {
                    chunk.gameObject.SetActive(inFrustum);
                }
            }
            else if (inFrustum)
            {
                // Chunk doesn't exist and it's in the frustum, so create it
                CreateChunk(chunkPos, lod);
            }
        }
    }


    void RecycleChunk(Vector3Int chunkPos)
    {
        if (activeChunks.TryGetValue(chunkPos, out Chunk chunkToDeactivate))
        {
            chunkToDeactivate.WaitForJob();
            chunkToDeactivate.gameObject.SetActive(false);
            chunkPool.Enqueue(chunkToDeactivate);
            activeChunks.Remove(chunkPos);
        }
    }

    void CreateChunk(Vector3Int chunkPos, int lod)
    {
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
        if (distance < lodLevelDistance) return 0; // Highest resolution
        if (distance < lodLevelDistance * 2) return 1;
        if (distance < lodLevelDistance * 4) return 2;
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