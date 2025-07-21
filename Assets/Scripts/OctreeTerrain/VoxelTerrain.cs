using OctreeTerrain;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

public class VoxelTerrain : MonoBehaviour
{
    [Header("Terrain Dimensions")]
    public int Size = 64; // The overall size of the terrain volume.
    public int MaxDepth = 5; // Determines the resolution. Resolution = Size / 2^MaxDepth.

    [Header("Terrain Generation")]
    public float NoiseFrequency = 0.05f;
    public float NoiseAmplitude = 10f;

    [Header("References")]
    public Material VoxelMaterial;

    // This list holds all the nodes of our octree.
    private NativeList<VoxelOctreeNode> _nodes;
    private GameObject _chunkGameObject;

    void Start()
    {
        // Initialize the list for the octree nodes.
        _nodes = new NativeList<VoxelOctreeNode>(Allocator.Persistent);

        // Generate the entire terrain structure and mesh.
        GenerateTerrain();
        CreateChunk();
    }

    void OnDestroy()
    {
        // Clean up the native list when the object is destroyed to prevent memory leaks.
        if (_nodes.IsCreated)
        {
            _nodes.Dispose();
        }
    }

    /// <summary>
    /// Builds the octree structure and then populates it with terrain data using a job.
    /// </summary>
    void GenerateTerrain()
    {
        // 1. Create the root node of the octree.
        var rootNode = new VoxelOctreeNode(-1, float3.zero, Size);
        _nodes.Add(rootNode);

        // 2. Recursively build the octree structure down to the max depth.
        BuildOctreeRecursive(0, 0);

        // 3. Schedule the terrain generation job to calculate the SDF for all leaf nodes.
        var generationJob = new TerrainGenerationJob
        {
            Nodes = _nodes.AsArray(),
            NoiseFrequency = NoiseFrequency,
            NoiseAmplitude = NoiseAmplitude
        };

        // Execute the job and wait for it to complete.
        JobHandle handle = generationJob.Schedule(_nodes.Length, 32);
        handle.Complete();
    }

    /// <summary>
    /// Recursively subdivides nodes to build a uniform octree up to the specified MaxDepth.
    /// </summary>
    private void BuildOctreeRecursive(int nodeIndex, int currentDepth)
    {
        if (currentDepth >= MaxDepth)
        {
            // We've reached the desired resolution, so stop subdividing.
            return;
        }

        // Subdivide the current node into 8 children.
        Subdivide(nodeIndex);

        // Get the node again after subdivision, as its FirstChildIndex has changed.
        var node = _nodes[nodeIndex];

        // Recursively call this function for all the new children.
        for (int i = 0; i < 8; i++)
        {
            BuildOctreeRecursive(node.FirstChildIndex + i, currentDepth + 1);
        }
    }

    /// <summary>
    /// Creates 8 child nodes for a given parent node.
    /// </summary>
    private void Subdivide(int nodeIndex)
    {
        var parentNode = _nodes[nodeIndex];
        if (!parentNode.IsLeaf) return; // Avoid subdividing a node that already has children.

        // The index of the first child will be the current end of the list.
        int firstChildIndex = _nodes.Length;
        parentNode.FirstChildIndex = firstChildIndex;
        _nodes[nodeIndex] = parentNode; // Update the parent node in the list with the new child index.

        int childSize = parentNode.Size / 2;
        float childOffset = childSize * 0.5f;

        // Create and add the 8 child nodes.
        for (int i = 0; i < 8; i++)
        {
            float3 childCenter = parentNode.Center + new float3(
                (i & 1) == 0 ? -childOffset : childOffset,
                (i & 2) == 0 ? -childOffset : childOffset,
                (i & 4) == 0 ? -childOffset : childOffset
            );
            _nodes.Add(new VoxelOctreeNode(nodeIndex, childCenter, childSize));
        }
    }

    /// <summary>
    /// Gathers all the leaf nodes and schedules the meshing job to create the terrain geometry.
    /// </summary>
    void CreateChunk()
    {
        // Collect all the leaf nodes from the octree. These are the highest-resolution voxels.
        var leafNodes = new NativeList<VoxelOctreeNode>(Allocator.TempJob);
        CollectLeafNodes(0, leafNodes);

        if (leafNodes.Length == 0)
        {
            Debug.LogWarning("No leaf nodes found to mesh.");
            leafNodes.Dispose();
            return;
        }
        
        // The size of our leaf voxels is determined by the max depth.
        float leafNodeSize = Size / Mathf.Pow(2, MaxDepth);

        var vertices = new NativeList<Vector3>(Allocator.TempJob);
        var indices = new NativeList<int>(Allocator.TempJob);
        var colors = new NativeList<Color>(Allocator.TempJob);

        var job = new StitchedSurfaceNetsJob
        {
            Nodes = leafNodes.AsArray(),
            LeafNodeSize = leafNodeSize,
            Vertices = vertices,
            Indices = indices,
            Colors = colors
        };

        JobHandle handle = job.Schedule();
        handle.Complete();

        // Create the Unity Mesh object from the job's results.
        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices.AsArray());
        mesh.SetIndices(indices.AsArray(), MeshTopology.Triangles, 0);
        mesh.SetColors(colors.AsArray());
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();

        // Create a GameObject to display the mesh.
        _chunkGameObject = new GameObject("VoxelChunk");
        _chunkGameObject.transform.parent = transform;
        var meshFilter = _chunkGameObject.AddComponent<MeshFilter>();
        var meshRenderer = _chunkGameObject.AddComponent<MeshRenderer>();

        meshFilter.mesh = mesh;
        meshRenderer.material = VoxelMaterial;

        // Dispose of all temporary native collections.
        leafNodes.Dispose();
        vertices.Dispose();
        indices.Dispose();
        colors.Dispose();
    }

    /// <summary>
    /// Recursively traverses the octree to find and collect all leaf nodes.
    /// </summary>
    void CollectLeafNodes(int nodeIndex, NativeList<VoxelOctreeNode> leafNodes)
    {
        var node = _nodes[nodeIndex];
        if (node.IsLeaf)
        {
            leafNodes.Add(node);
        }
        else
        {
            for (int i = 0; i < 8; i++)
            {
                CollectLeafNodes(node.FirstChildIndex + i, leafNodes);
            }
        }
    }
}
