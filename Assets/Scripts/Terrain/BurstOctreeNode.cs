// Terrain/BurstOctreeNode.cs
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public struct BurstOctreeNode
{
    public Bounds bounds;
    public int depth;
    public int childrenIndex; // Index into a flat array of children
    public bool isLeaf;

    public BurstOctreeNode(Bounds bounds, int depth)
    {
        this.bounds = bounds;
        this.depth = depth;
        this.childrenIndex = -1;
        this.isLeaf = true;
    }
}