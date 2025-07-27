using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    public MeshFilter meshFilter;
    public MeshRenderer meshRenderer;
    public MeshCollider meshCollider;

    public struct MeshData
    {
        public NativeList<float3> vertices;
        public NativeList<int> triangles;
        public NativeList<float> vertexTypes;
        public NativeList<float3> normals; // Add this line
        public bool IsCreated => vertices.IsCreated && triangles.IsCreated && vertexTypes.IsCreated && normals.IsCreated;

        public void Dispose()
        {
            if (vertices.IsCreated) vertices.Dispose();
            if (triangles.IsCreated) triangles.Dispose();
            if (vertexTypes.IsCreated) vertexTypes.Dispose();
            if (normals.IsCreated) normals.Dispose(); // And this one
        }
    }

    public void Initialize(TerrainGenerator terrainGenerator)
    {
    }
    
    public void ClearMesh()
    {
        meshFilter.mesh = null;
        meshCollider.sharedMesh = null;
    }

    public JobHandle ScheduleTerrainGeneration(OctreeNode node, NativeArray<float> densityMap, NativeArray<byte> voxelTypes, JobHandle dependency, out MeshData meshData)
    {
        var vertices = new NativeList<float3>(Allocator.Persistent);
        var triangles = new NativeList<int>(Allocator.Persistent);
        var vertexTypesOut = new NativeList<float>(Allocator.Persistent);
        var normals = new NativeList<float3>(Allocator.Persistent); // Allocate the normals list here

        var marchingCubesJob = new MarchingCubesJob
        {
            density = densityMap,
            voxelTypes = voxelTypes, 
            chunkSize = TerrainSettings.MIN_NODE_SIZE,
            lod = 0,
            triangulationTable = OctreeTerrainManager.Instance.triangulationTable,
            cornerOffsets = OctreeTerrainManager.Instance.cornerOffsets,
            cornerIndexAFromEdge = OctreeTerrainManager.Instance.cornerIndexAFromEdge,
            cornerIndexBFromEdge = OctreeTerrainManager.Instance.cornerIndexBFromEdge,
            vertices = vertices,
            triangles = triangles,
            vertexTypes = vertexTypesOut,
            normals = normals, // Pass the allocated list to the job
            nodeMin = node.bounds.min,
            nodeSize = node.bounds.size.x
        };

        var marchingCubesHandle = marchingCubesJob.Schedule(dependency);
        meshData = new MeshData { vertices = vertices, triangles = triangles, vertexTypes = vertexTypesOut, normals = normals };
        return marchingCubesHandle;
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
            
            mesh.SetNormals(meshData.normals.AsArray()); // Set the calculated normals

            mesh.RecalculateBounds();
            // We no longer need to recalculate normals here!
            // mesh.RecalculateNormals();

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
}