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
    private OctreeNode node;
    private TerrainGenerator terrainGenerator;
    
    private NativeArray<byte> voxelTypes;

    // hold the generated mesh data
    public struct MeshData
    {
        public NativeList<float3> vertices;
        public NativeList<int> triangles;
        public NativeList<float> vertexTypes; 
        public bool IsCreated => vertices.IsCreated && triangles.IsCreated && vertexTypes.IsCreated;
        public void Dispose()
        {
            if (vertices.IsCreated) vertices.Dispose();
            if (triangles.IsCreated) triangles.Dispose();
            if (vertexTypes.IsCreated) vertexTypes.Dispose(); 
        }
    }

    public void Initialize(Material mat, TerrainGenerator terrainGenerator)
    {
        meshFilter = GetComponent<MeshFilter>();
        meshRenderer = GetComponent<MeshRenderer>();
        meshCollider = GetComponent<MeshCollider>();
        meshRenderer.material = mat;
        this.terrainGenerator = terrainGenerator;
    }

    public MeshData GenerateTerrain(OctreeNode node)
    {
        this.node = node;

        if (!densityMap.IsCreated)
        {
            densityMap = new NativeArray<float>((TerrainSettings.MIN_NODE_SIZE + 1) * (TerrainSettings.MIN_NODE_SIZE + 1) * (TerrainSettings.MIN_NODE_SIZE + 1), Allocator.Persistent);
        }
        if (!voxelTypes.IsCreated)
        {
            voxelTypes = new NativeArray<byte>((TerrainSettings.MIN_NODE_SIZE + 1) * (TerrainSettings.MIN_NODE_SIZE + 1) * (TerrainSettings.MIN_NODE_SIZE + 1), Allocator.Persistent);
        }

        // Pass voxelTypes to ApplyLayers
        terrainGenerator.ApplyLayers(densityMap, voxelTypes, TerrainSettings.MIN_NODE_SIZE, new float3(node.bounds.center.x, node.bounds.center.y, node.bounds.center.z), node.bounds.size.x);

        var vertices = new NativeList<float3>(Allocator.Persistent);
        var triangles = new NativeList<int>(Allocator.Persistent);
        var vertexTypesOut = new NativeList<float>(Allocator.Persistent);  

        var marchingCubesJob = new MarchingCubesJob
        {
            density = densityMap,
            voxelTypes = this.voxelTypes,  
            chunkSize = TerrainSettings.MIN_NODE_SIZE,
            lod = 0,
            triangulationTable = OctreeTerrainManager.Instance.triangulationTable,
            cornerOffsets = OctreeTerrainManager.Instance.cornerOffsets,
            cornerIndexAFromEdge = OctreeTerrainManager.Instance.cornerIndexAFromEdge,
            cornerIndexBFromEdge = OctreeTerrainManager.Instance.cornerIndexBFromEdge,
            vertices = vertices,
            triangles = triangles,
            vertexTypes = vertexTypesOut,  
            nodeMin = node.bounds.min,
            nodeSize = node.bounds.size.x
        };

        marchingCubesJob.Schedule().Complete();

        return new MeshData { vertices = vertices, triangles = triangles, vertexTypes = vertexTypesOut }; // Modified
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

            mesh.SetUVs(1, meshData.vertexTypes.AsArray());

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
            densityMap = densityMap,
            voxelTypes = this.voxelTypes,  
            newVoxelType = 1, // Example type, you can change this
        };
        densityJob.Schedule().Complete();
    
        var vertices = new NativeList<float3>(Allocator.Persistent);
        var triangles = new NativeList<int>(Allocator.Persistent);
        var vertexTypesOut = new NativeList<float>(Allocator.Persistent); //New
    
        var marchingCubesJob = new MarchingCubesJob
        {
            density = densityMap,
            voxelTypes = this.voxelTypes,  
            chunkSize = TerrainSettings.MIN_NODE_SIZE, 
            lod = 0,
            triangulationTable = OctreeTerrainManager.Instance.triangulationTable,
            cornerOffsets = OctreeTerrainManager.Instance.cornerOffsets,
            cornerIndexAFromEdge = OctreeTerrainManager.Instance.cornerIndexAFromEdge,
            cornerIndexBFromEdge = OctreeTerrainManager.Instance.cornerIndexBFromEdge,
            vertices = vertices,
            triangles = triangles,
            vertexTypes = vertexTypesOut, 
            nodeMin = node.bounds.min,
            nodeSize = node.bounds.size.x
        };

        marchingCubesJob.Schedule().Complete();
        ApplyGeneratedMesh(new MeshData { vertices = vertices, triangles = triangles, vertexTypes = vertexTypesOut}); // Modified
    }

    public void DisposeChunkResources()
    {
        if (densityMap.IsCreated) densityMap.Dispose();
        if (voxelTypes.IsCreated) voxelTypes.Dispose();  
    }

    void OnDestroy()
    {
        DisposeChunkResources();
    }
}