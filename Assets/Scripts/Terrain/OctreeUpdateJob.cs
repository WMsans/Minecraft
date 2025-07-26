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

    [ReadOnly] public NativeList<OctreeNode> nodes;
    public NativeList<int> toSubdivide;
    public NativeList<int> toMerge;
    public NativeList<int> toGenerate;
    public NativeList<int> toHide; 

    public void Execute()
    {
        var stack = new NativeList<int>(Allocator.Temp);
        if (nodes.Length > 0)
        {
            stack.Add(0); 
        }

        while (stack.Length > 0)
        {
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
                        toHide.Add(nodeIndex);
                    }
                }
            }
            else 
            {
                if (shouldSubdivide)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        stack.Add(node.childrenIndex + i);
                    }
                }
                else
                {
                    toMerge.Add(nodeIndex);
                }
            }
        }
        
        stack.Dispose();
    }
}