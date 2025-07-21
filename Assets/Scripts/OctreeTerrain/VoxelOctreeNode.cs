using Unity.Mathematics;

namespace OctreeTerrain
{
    public struct VoxelOctreeNode
    {
        // Use indices instead of direct references to avoid circular dependencies
        // and to make the struct blittable.
        public int ParentIndex;
        public int FirstChildIndex; // Index of the first child in the main nodes array

        public float3 Center;
        public int Size;
        public float Value;
        public int Lod;

        // A property to check if the node is a leaf (has no children).
        public bool IsLeaf => FirstChildIndex == -1;

        public VoxelOctreeNode(int parentIndex, float3 center, int size)
        {
            ParentIndex = parentIndex;
            Center = center;
            Size = size;
            FirstChildIndex = -1; // Initialize as a leaf node
            Value = 0;
            Lod = 0;
        }
    }
}
