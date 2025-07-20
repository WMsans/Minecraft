using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    public Vector3Int chunkPosition;
    public int lod;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    private JobHandle noiseJobHandle;
    private JobHandle meshGenerationJobHandle;
    private bool isJobScheduled = false;

    private NativeArray<float> densityMap;
    private NativeList<float3> vertices;
    private NativeList<int> triangles;

    public void Initialize(Vector3Int pos, Material mat, int lod)
    {
        WaitForJob();

        this.chunkPosition = pos;
        this.lod = lod;
        int lodScale = 1 << lod;
        this.transform.localScale = Vector3.one * lodScale;

        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        meshRenderer.material = mat;

        if (!densityMap.IsCreated)
        {
            densityMap = new NativeArray<float>((TerrainManager.chunkSize + 1) * (TerrainManager.chunkSize + 1) * (TerrainManager.chunkSize + 1), Allocator.Persistent);
        }
        var noiseJob = new NoiseJob
        {
            density = densityMap,
            chunkSize = TerrainManager.chunkSize,
            offset = new float3(chunkPosition.x * TerrainManager.chunkSize, chunkPosition.y * TerrainManager.chunkSize, chunkPosition.z * TerrainManager.chunkSize),
            lod = this.lod
        };
        noiseJobHandle = noiseJob.Schedule(densityMap.Length, 64);
    }

    public void GenerateTerrain()
    {
        WaitForJob();

        vertices = new NativeList<float3>(Allocator.Persistent);
        triangles = new NativeList<int>(Allocator.Persistent);

        // Get neighbor LODs for seam stitching
        var neighborLODs = new NativeArray<int>(6, Allocator.TempJob);
        int lodScale = 1 << lod;
        neighborLODs[0] = TerrainManager.Instance.GetChunkLOD(chunkPosition + new Vector3Int(lodScale, 0, 0)); // X+
        neighborLODs[1] = TerrainManager.Instance.GetChunkLOD(chunkPosition - new Vector3Int(lodScale, 0, 0)); // X-
        neighborLODs[2] = TerrainManager.Instance.GetChunkLOD(chunkPosition + new Vector3Int(0, lodScale, 0)); // Y+
        neighborLODs[3] = TerrainManager.Instance.GetChunkLOD(chunkPosition - new Vector3Int(0, lodScale, 0)); // Y-
        neighborLODs[4] = TerrainManager.Instance.GetChunkLOD(chunkPosition + new Vector3Int(0, 0, lodScale)); // Z+
        neighborLODs[5] = TerrainManager.Instance.GetChunkLOD(chunkPosition - new Vector3Int(0, 0, lodScale)); // Z-


        var marchingCubesJob = new MarchingCubesJob
        {
            density = densityMap,
            chunkSize = TerrainManager.chunkSize,
            lod = lod,
            triangulationTable = TerrainManager.Instance.triangulationTable,
            cornerOffsets = TerrainManager.Instance.cornerOffsets,
            cornerIndexAFromEdge = TerrainManager.Instance.cornerIndexAFromEdge,
            cornerIndexBFromEdge = TerrainManager.Instance.cornerIndexBFromEdge,
            vertices = vertices,
            triangles = triangles,
            neighborLODs = neighborLODs
        };

        meshGenerationJobHandle = marchingCubesJob.Schedule(noiseJobHandle);
        isJobScheduled = true;
    }

    public void ModifyDensity(Vector3Int worldPos, float strength)
    {
        int scale = 1 << lod;
        int x = (worldPos.x / scale) - chunkPosition.x * TerrainManager.chunkSize;
        int y = (worldPos.y / scale) - chunkPosition.y * TerrainManager.chunkSize;
        int z = (worldPos.z / scale) - chunkPosition.z * TerrainManager.chunkSize;

        if (x >= 0 && x <= TerrainManager.chunkSize &&
            y >= 0 && y <= TerrainManager.chunkSize &&
            z >= 0 && z <= TerrainManager.chunkSize)
        {
            int index = x + (TerrainManager.chunkSize + 1) * (y + (TerrainManager.chunkSize + 1) * z);
            if (index >= 0 && index < densityMap.Length)
            {
                densityMap[index] += strength;
            }
        }
    }


    public void WaitForJob()
    {
        if (isJobScheduled)
        {
            meshGenerationJobHandle.Complete();
            DisposeNativeContainers();
            isJobScheduled = false;
        }
    }

    void LateUpdate()
    {
        if (isJobScheduled && meshGenerationJobHandle.IsCompleted)
        {
            meshGenerationJobHandle.Complete();

            if (vertices.Length > 3)
            {
                Mesh mesh = new Mesh
                {
                    indexFormat = IndexFormat.UInt32
                };

                mesh.SetVertices(vertices.AsArray());
                mesh.SetIndexBufferParams(triangles.Length, IndexFormat.UInt32);
                mesh.SetIndexBufferData(triangles.AsArray(), 0, 0, triangles.Length);
                SubMeshDescriptor subMesh = new SubMeshDescriptor(0, triangles.Length, MeshTopology.Triangles);
                mesh.SetSubMesh(0, subMesh);

                mesh.RecalculateBounds();
                mesh.RecalculateNormals();

                meshFilter.mesh = mesh;
                meshCollider.sharedMesh = mesh;
            }
            else
            {
                meshFilter.mesh = null;
                meshCollider.sharedMesh = null;
            }

            DisposeNativeContainers();
            isJobScheduled = false;
        }
    }

    private void DisposeNativeContainers()
    {
        if(vertices.IsCreated) vertices.Dispose();
        if(triangles.IsCreated) triangles.Dispose();
    }

    void OnDestroy()
    {
        if (isJobScheduled)
        {
            meshGenerationJobHandle.Complete();
            DisposeNativeContainers();
        }
        if (densityMap.IsCreated)
        {
            densityMap.Dispose();
        }
    }
}