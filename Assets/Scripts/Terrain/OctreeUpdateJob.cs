using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct OctreeUpdateJob : IJob
{
    [ReadOnly] public float3 playerPos;
    [ReadOnly] public FrustumCulling.FrustumPlanes frustumPlanes;
    [ReadOnly] public int maxDepth;

    [ReadOnly] public NativeList<OctreeNode> nodes; // Changed to ReadOnly
    public NativeList<int> toSubdivide;
    public NativeList<int> toMerge;
    public NativeList<int> toGenerate;
    public NativeList<int> toDestroy;

    public void Execute()
    {
        // Use a stack for a depth-first traversal of the octree
        var stack = new NativeList<int>(Allocator.Temp);
        if (nodes.Length > 0)
        {
            stack.Add(0); // Start with the root node (index 0)
        }

        while (stack.Length > 0)
        {
            // Pop a node from the stack
            int nodeIndex = stack[stack.Length - 1];
            stack.RemoveAt(stack.Length - 1);

            var node = nodes[nodeIndex];
            float distance = math.distance(node.bounds.center, playerPos);
            bool shouldSubdivide = distance < node.bounds.size.x * 1.5f && node.depth < maxDepth;

            if (node.isLeaf)
            {
                if (shouldSubdivide)
                {
                    toSubdivide.Add(nodeIndex);
                }
                else
                {
                    if (FrustumCulling.TestAABB(frustumPlanes, node.bounds))
                    {
                        toGenerate.Add(nodeIndex);
                    }
                    else
                    {
                        toDestroy.Add(nodeIndex);
                    }
                }
            }
            else // It's a parent node
            {
                if (shouldSubdivide)
                {
                    // This parent is close, so process its children
                    for (int i = 0; i < 8; i++)
                    {
                        stack.Add(node.childrenIndex + i);
                    }
                }
                else
                {
                    // This parent is far, so it should be merged.
                    // Its children are NOT added to the stack, so they won't be processed.
                    toMerge.Add(nodeIndex);
                }
            }
        }
        
        stack.Dispose();
    }
}