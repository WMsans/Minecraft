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

    public void Initialize(Vector3Int pos, Material mat)
    {
        chunkPosition = pos;
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
            offset = new float3(chunkPosition.x * TerrainManager.chunkSize, chunkPosition.y * TerrainManager.chunkSize, chunkPosition.z * TerrainManager.chunkSize)
        };
        noiseJobHandle = noiseJob.Schedule(densityMap.Length, 64);
    }

    public void GenerateTerrain()
    {
        if (isJobScheduled)
        {
            return;
        }

        vertices = new NativeList<float3>(Allocator.Persistent);
        triangles = new NativeList<int>(Allocator.Persistent);

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
            triangles = triangles
        };

        meshGenerationJobHandle = marchingCubesJob.Schedule(noiseJobHandle);
        isJobScheduled = true;
    }

    public void ModifyDensity(Vector3Int worldPos, float strength)
    {
        int x = worldPos.x - chunkPosition.x * TerrainManager.chunkSize;
        int y = worldPos.y - chunkPosition.y * TerrainManager.chunkSize;
        int z = worldPos.z - chunkPosition.z * TerrainManager.chunkSize;

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