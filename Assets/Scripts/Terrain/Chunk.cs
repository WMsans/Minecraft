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
        // The chunk's GameObject will now sit at the world origin,
        // as the mesh vertices are in world space.
        transform.position = Vector3.zero;
        transform.localScale = Vector3.one;

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
            triangles = triangles,
            // Pass the node's bounds for world space conversion
            nodeMin = node.bounds.min,
            nodeSize = node.bounds.size.x
        };

        // Run jobs and apply mesh immediately
        noiseJob.Schedule(densityMap.Length, 64).Complete();
        marchingCubesJob.Schedule().Complete();

        ApplyMesh();
    }

    public void ModifyDensity(Vector3 worldPos, float strength, float radius)
    {
        var densityJob = new DensityModificationJob
        {
            worldPos = worldPos,
            strength = strength,
            radius = radius,
            nodeBounds = node.bounds,
            densityMap = densityMap
        };
        densityJob.Schedule().Complete();
    
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
            triangles = triangles,
            // Pass the node's bounds for world space conversion
            nodeMin = node.bounds.min,
            nodeSize = node.bounds.size.x
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
            
            // The bounds need to be recalculated in world space
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