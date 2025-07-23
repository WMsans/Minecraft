using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private MeshCollider meshCollider;

    private NativeArray<float> densityMap;
    private NativeList<float3> vertices;
    private NativeList<int> triangles;
    private BurstOctreeNode node;

    public void Initialize(Material mat)
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        meshRenderer.material = mat;
    }

    public void GenerateTerrain(BurstOctreeNode node)
    {
        this.node = node;
        transform.position = node.bounds.min;
        transform.localScale = Vector3.one * node.bounds.size.x / 16;

        densityMap = new NativeArray<float>((16 + 1) * (16 + 1) * (16 + 1), Allocator.Persistent);
        vertices = new NativeList<float3>(Allocator.Persistent);
        triangles = new NativeList<int>(Allocator.Persistent);

        var noiseJob = new NoiseJob
        {
            density = densityMap,
            chunkSize = 16,
            offset = new float3(node.bounds.center.x, node.bounds.center.y, node.bounds.center.z),
            scale = node.bounds.size.x
        };

        var marchingCubesJob = new MarchingCubesJob
        {
            density = densityMap,
            chunkSize = 16,
            lod = 0,
            triangulationTable = OctreeTerrainManager.Instance.triangulationTable,
            cornerOffsets = OctreeTerrainManager.Instance.cornerOffsets,
            cornerIndexAFromEdge = OctreeTerrainManager.Instance.cornerIndexAFromEdge,
            cornerIndexBFromEdge = OctreeTerrainManager.Instance.cornerIndexBFromEdge,
            vertices = vertices,
            triangles = triangles
        };

        // Run jobs and apply mesh immediately
        noiseJob.Schedule(densityMap.Length, 64).Complete();
        marchingCubesJob.Schedule().Complete();

        ApplyMesh();
    }

    public void ModifyDensity(Vector3 worldPos, float strength, float radius)
    {
        int buildRadius = Mathf.CeilToInt(radius);
        for (int x = -buildRadius; x <= buildRadius; x++)
        {
            for (int y = -buildRadius; y <= buildRadius; y++)
            {
                for (int z = -buildRadius; z <= buildRadius; z++)
                {
                    Vector3Int offset = new Vector3Int(x, y, z);
                    if (offset.magnitude > radius) continue;

                    Vector3Int modifiedPos = Vector3Int.FloorToInt(worldPos) + offset;
                    
                    int densityX = (int)((modifiedPos.x - node.bounds.min.x) / node.bounds.size.x * 16);
                    int densityY = (int)((modifiedPos.y - node.bounds.min.y) / node.bounds.size.x * 16);
                    int densityZ = (int)((modifiedPos.z - node.bounds.min.z) / node.bounds.size.x * 16);

                    if (densityX >= 0 && densityX <= 16 &&
                        densityY >= 0 && densityY <= 16 &&
                        densityZ >= 0 && densityZ <= 16)
                    {
                        int index = densityX + (16 + 1) * (densityY + (16 + 1) * densityZ);
                        if (index >= 0 && index < densityMap.Length)
                        {
                            float falloff = 1 - (offset.magnitude / radius);
                            densityMap[index] += strength * falloff;
                        }
                    }
                }
            }
        }
        
        // Before creating new lists, dispose of the old ones
        if (vertices.IsCreated) vertices.Dispose();
        if (triangles.IsCreated) triangles.Dispose();

        vertices = new NativeList<float3>(Allocator.Persistent);
        triangles = new NativeList<int>(Allocator.Persistent);
        
        var marchingCubesJob = new MarchingCubesJob
        {
            density = densityMap,
            chunkSize = 16,
            lod = 0,
            triangulationTable = OctreeTerrainManager.Instance.triangulationTable,
            cornerOffsets = OctreeTerrainManager.Instance.cornerOffsets,
            cornerIndexAFromEdge = OctreeTerrainManager.Instance.cornerIndexAFromEdge,
            cornerIndexBFromEdge = OctreeTerrainManager.Instance.cornerIndexBFromEdge,
            vertices = vertices,
            triangles = triangles
        };

        marchingCubesJob.Schedule().Complete();
        ApplyMesh();
    }

    private void ApplyMesh()
    {
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
    }

    private void DisposeNativeContainers()
    {
        if (vertices.IsCreated) vertices.Dispose();
        if (triangles.IsCreated) triangles.Dispose();
    }

    public void DisposeChunkResources()
    {
        if (densityMap.IsCreated) densityMap.Dispose();
        DisposeNativeContainers();
    }

    void OnDestroy()
    {
        DisposeChunkResources();
    }
}