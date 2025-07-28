using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using System.Collections.Generic;

[BurstCompile]
public static class BurstRaycast
{
    public struct RaycastHit
    {
        public float3 point;
        public float distance;
        public float3 normal;
    }

    public static bool Raycast(Ray ray, NativeList<OctreeNode> nodes, Dictionary<int, Chunk.MeshData> activeMeshData, out RaycastHit hit)
    {
        var intersectingNodeIndices = new List<int>();
        var stack = new Stack<int>();
        if (nodes.Length > 0)
        {
            stack.Push(0);
        }

        // Broad-phase: Traverse the octree to find leaf nodes that intersect with the ray.
        while (stack.Count > 0)
        {
            var nodeIndex = stack.Pop();
            var node = nodes[nodeIndex];
            var bounds = node.bounds;
            if (!bounds.IntersectRay(ray))
            {
                continue;
            }

            if (node.isLeaf)
            {
                if (activeMeshData.ContainsKey(nodeIndex))
                {
                    intersectingNodeIndices.Add(nodeIndex);
                }
            }
            else
            {
                if (node.childrenIndex != -1)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        stack.Push(node.childrenIndex + i);
                    }
                }
            }
        }

        if (intersectingNodeIndices.Count == 0)
        {
            hit = default;
            return false;
        }
        
        // Calculate the total number of triangles to allocate the correct capacity for the hits list.
        int totalTriangleCount = 0;
        foreach (var nodeIndex in intersectingNodeIndices)
        {
            if (activeMeshData.TryGetValue(nodeIndex, out var meshData))
            {
                totalTriangleCount += meshData.triangles.Length / 3;
            }
        }
        
        if (totalTriangleCount == 0)
        {
            hit = default;
            return false;
        }

        // Narrow-phase: For each intersected node, run a Burst job to check for ray-triangle intersections.
        var hits = new NativeList<RaycastHit>(totalTriangleCount, Allocator.TempJob);
        JobHandle jobHandle = default;

        foreach (var nodeIndex in intersectingNodeIndices)
        {
            var meshData = activeMeshData[nodeIndex];

            if (meshData.triangles.Length > 0)
            {
                var job = new RaycastJob
                {
                    rayOrigin = ray.origin,
                    rayDirection = ray.direction,
                    vertices = meshData.vertices.AsDeferredJobArray(),
                    triangles = meshData.triangles.AsDeferredJobArray(),
                    hits = hits.AsParallelWriter()
                };
                jobHandle = job.Schedule(meshData.triangles.Length / 3, 32, jobHandle);
            }
        }

        jobHandle.Complete();

        if (hits.Length == 0)
        {
            hit = default;
            hits.Dispose();
            return false;
        }

        // Find the closest hit point among all intersections.
        hit = hits[0];
        for (int i = 1; i < hits.Length; i++)
        {
            if (hits[i].distance < hit.distance)
            {
                hit = hits[i];
            }
        }

        hits.Dispose();
        return true;
    }
}