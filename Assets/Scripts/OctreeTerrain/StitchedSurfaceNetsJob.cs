using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace OctreeTerrain
{
    [BurstCompile]
    public struct StitchedSurfaceNetsJob : IJob
    {
        [ReadOnly] public NativeArray<VoxelOctreeNode> Nodes;
        [ReadOnly] public float LeafNodeSize; // The size of the leaf nodes we are meshing.

        // Output mesh data
        public NativeList<Vector3> Vertices;
        public NativeList<int> Indices;
        public NativeList<Color> Colors;

        public void Execute()
        {
            if (Nodes.Length == 0) return;

            // A hash map to quickly look up a vertex index using its grid position.
            // This is the key to connecting the mesh vertices correctly.
            var centerToVertexIndex = new NativeHashMap<int3, int>(Nodes.Length, Allocator.Temp);

            // Pass 1: Create vertices.
            // A vertex is created at the center of any voxel that is "solid" (SDF value <= 0).
            for (int i = 0; i < Nodes.Length; i++)
            {
                if (Nodes[i].Value <= 0)
                {
                    // Convert the float center position to an integer grid position to use as a key.
                    // This is a robust way to identify each voxel's location.
                    int3 gridPos = (int3)math.round(Nodes[i].Center / LeafNodeSize);
                
                    // Make sure we don't add a vertex for the same position twice.
                    if (!centerToVertexIndex.ContainsKey(gridPos))
                    {
                        Vertices.Add(Nodes[i].Center);
                        Colors.Add(new Color(0.7f, 0.7f, 0.7f)); // Default color
                        centerToVertexIndex.Add(gridPos, Vertices.Length - 1);
                    }
                }
            }

            // Pass 2: Create triangles (quads).
            // We iterate through the solid voxels again and connect them to their neighbors.
            for (int i = 0; i < Nodes.Length; i++)
            {
                if (Nodes[i].Value > 0) continue; // Skip non-solid nodes.

                int3 p0 = (int3)math.round(Nodes[i].Center / LeafNodeSize);

                // Check for neighbors on the X, Y, and Z axes and create quads between them.
                // This creates the visible surface of the terrain.
                CreateQuad(p0, p0 + new int3(1, 0, 0), p0 + new int3(0, 1, 0), p0 + new int3(1, 1, 0), centerToVertexIndex); // XY plane
                CreateQuad(p0, p0 + new int3(0, 1, 0), p0 + new int3(0, 0, 1), p0 + new int3(0, 1, 1), centerToVertexIndex); // YZ plane
                CreateQuad(p0, p0 + new int3(0, 0, 1), p0 + new int3(1, 0, 0), p0 + new int3(1, 0, 1), centerToVertexIndex); // XZ plane
            }

            centerToVertexIndex.Dispose();
        }

        /// <summary>
        /// Checks if four vertices exist to form a quad and, if so, creates two triangles.
        /// </summary>
        private void CreateQuad(int3 p0, int3 p1, int3 p2, int3 p3, NativeHashMap<int3, int> centerToVertexIndex)
        {
            // We need to check if all four corners of the quad have a corresponding solid voxel.
            if (centerToVertexIndex.TryGetValue(p0, out int v0) &&
                centerToVertexIndex.TryGetValue(p1, out int v1) &&
                centerToVertexIndex.TryGetValue(p2, out int v2) &&
                centerToVertexIndex.TryGetValue(p3, out int v3))
            {
                // Create two triangles to form the quad. The order of vertices (winding order)
                // determines which side of the triangle is visible.
                Indices.Add(v0);
                Indices.Add(v2);
                Indices.Add(v1);

                Indices.Add(v1);
                Indices.Add(v2);
                Indices.Add(v3);
            }
        }
    }
}