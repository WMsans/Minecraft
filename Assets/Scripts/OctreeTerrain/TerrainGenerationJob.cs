using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace OctreeTerrain
{
    /// <summary>
    /// A Burst-compiled job to calculate the Signed Distance Function (SDF) for a terrain
    /// using Simplex noise. This job runs in parallel for high performance.
    /// </summary>
    [BurstCompile]
    public struct TerrainGenerationJob : IJobParallelFor
    {
        // The list of all nodes in the octree. The job will modify the 'Value' of each node.
        public NativeArray<VoxelOctreeNode> Nodes;

        // Parameters to control the shape of the noise-based terrain.
        [ReadOnly] public float NoiseFrequency;
        [ReadOnly] public float NoiseAmplitude;

        /// <summary>
        /// The Execute method is called for each node in the 'Nodes' array.
        /// </summary>
        /// <param name="index">The index of the current node to process.</param>
        public void Execute(int index)
        {
            // Get the node we are working on.
            var node = Nodes[index];

            // We only need to generate the final, high-resolution SDF for the leaf nodes.
            if (node.IsLeaf)
            {
                // Position for the 2D noise calculation (we use the X and Z axes for the terrain plane).
                float2 position = node.Center.xz;

                // Calculate the terrain height at this position using Simplex noise.
                // The result is a smooth, natural-looking value.
                float height = noise.snoise(position * NoiseFrequency) * NoiseAmplitude;

                // The Signed Distance Function (SDF) is the node's vertical distance from the terrain surface.
                // A negative value means the point is "underground" (solid).
                // A positive value means the point is "in the air" (empty).
                // A value of zero means the point is exactly on the surface.
                node.Value = node.Center.y - height;

                // Write the updated node back to the array.
                Nodes[index] = node;
            }
        }
    }
}