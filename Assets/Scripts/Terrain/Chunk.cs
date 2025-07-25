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
    private BurstOctreeNode node;

    // A new struct to hold the generated mesh data
    public struct MeshData
    {
        public NativeList<float3> vertices;
        public NativeList<int> triangles;
        public bool IsCreated => vertices.IsCreated && triangles.IsCreated;
        public void Dispose()
        {
            if (vertices.IsCreated) vertices.Dispose();
            if (triangles.IsCreated) triangles.Dispose();
        }
    }

    public void Initialize(Material mat)
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        meshRenderer.material = mat;
    }

    public MeshData GenerateTerrain(BurstOctreeNode node)
    {
        this.node = node;

        densityMap = new NativeArray<float>((TerrainSettings.MIN_NODE_SIZE + 1) * (TerrainSettings.MIN_NODE_SIZE + 1) * (TerrainSettings.MIN_NODE_SIZE + 1), Allocator.Persistent);
        var vertices = new NativeList<float3>(Allocator.Persistent);
        var triangles = new NativeList<int>(Allocator.Persistent);

        var noiseJob = new NoiseJob
        {
            density = densityMap,
            chunkSize = TerrainSettings.MIN_NODE_SIZE,
            offset = new float3(node.bounds.center.x, node.bounds.center.y, node.bounds.center.z),
            scale = node.bounds.size.x
        };

        var marchingCubesJob = new MarchingCubesJob
        {
            density = densityMap,
            chunkSize = TerrainSettings.MIN_NODE_SIZE,
            lod = 0,
            triangulationTable = OctreeTerrainManager.Instance.triangulationTable,
            cornerOffsets = OctreeTerrainManager.Instance.cornerOffsets,
            cornerIndexAFromEdge = OctreeTerrainManager.Instance.cornerIndexAFromEdge,
            cornerIndexBFromEdge = OctreeTerrainManager.Instance.cornerIndexBFromEdge,
            vertices = vertices,
            triangles = triangles,
            nodeMin = node.bounds.min,
            nodeSize = node.bounds.size.x
        };

        noiseJob.Schedule(densityMap.Length, 64).Complete();
        marchingCubesJob.Schedule().Complete();

        if (densityMap.IsCreated) densityMap.Dispose();

        return new MeshData { vertices = vertices, triangles = triangles };
    }

    public void ApplyGeneratedMesh(MeshData meshData)
    {
        if (meshData.vertices.Length > 3)
        {
            Mesh mesh = new Mesh
            {
                indexFormat = IndexFormat.UInt32
            };

            mesh.SetVertices(meshData.vertices.AsArray());
            mesh.SetIndexBufferParams(meshData.triangles.Length, IndexFormat.UInt32);
            mesh.SetIndexBufferData(meshData.triangles.AsArray(), 0, 0, meshData.triangles.Length);
            SubMeshDescriptor subMesh = new SubMeshDescriptor(0, meshData.triangles.Length, MeshTopology.Triangles);
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

        if(meshData.IsCreated) meshData.Dispose();
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
    
        var vertices = new NativeList<float3>(Allocator.Persistent);
        var triangles = new NativeList<int>(Allocator.Persistent);
    
        var marchingCubesJob = new MarchingCubesJob
        {
            density = densityMap,
            chunkSize = TerrainSettings.MIN_NODE_SIZE, 
            lod = 0,
            triangulationTable = OctreeTerrainManager.Instance.triangulationTable,
            cornerOffsets = OctreeTerrainManager.Instance.cornerOffsets,
            cornerIndexAFromEdge = OctreeTerrainManager.Instance.cornerIndexAFromEdge,
            cornerIndexBFromEdge = OctreeTerrainManager.Instance.cornerIndexBFromEdge,
            vertices = vertices,
            triangles = triangles,
            nodeMin = node.bounds.min,
            nodeSize = node.bounds.size.x
        };

        marchingCubesJob.Schedule().Complete();
        ApplyGeneratedMesh(new MeshData { vertices = vertices, triangles = triangles});
    }

    public void DisposeChunkResources()
    {
        if (densityMap.IsCreated) densityMap.Dispose();
    }

    void OnDestroy()
    {
        DisposeChunkResources();
    }
}