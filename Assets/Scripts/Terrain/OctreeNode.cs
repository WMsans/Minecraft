using UnityEngine;
using System.Collections.Generic;

public class OctreeNode
{
    public Bounds bounds;
    public int depth;
    public OctreeNode[] children;
    public bool isLeaf = true;
    public Chunk chunk;

    public OctreeNode(Vector3 position, float size, int depth)
    {
        this.bounds = new Bounds(position, Vector3.one * size);
        this.depth = depth;
    }

    public void Update(Vector3 playerPos, Plane[] frustumPlanes, Pool<Chunk> chunkPool)
    {
        float distance = Vector3.Distance(bounds.center, playerPos);
        bool shouldSubdivide = distance < bounds.size.x * 1.5f && depth < OctreeTerrainManager.Instance.maxDepth;

        if (isLeaf)
        {
            if (shouldSubdivide)
            {
                Subdivide(chunkPool);
            }
            else
            {
                if (chunk == null)
                {
                    if (GeometryUtility.TestPlanesAABB(frustumPlanes, bounds))
                    {
                        chunk = chunkPool.Get();
                        chunk.GenerateTerrain(this);
                    }
                }
                else
                {
                    if (!GeometryUtility.TestPlanesAABB(frustumPlanes, bounds))
                    {
                        chunkPool.Return(chunk);
                        chunk = null;
                    }
                }
            }
        }
        else
        {
            if (!shouldSubdivide)
            {
                Merge(chunkPool);
            }
            else
            {
                foreach (var child in children)
                {
                    child.Update(playerPos, frustumPlanes, chunkPool);
                }
            }
        }
    }

    private void Subdivide(Pool<Chunk> chunkPool)
    {
        isLeaf = false;
        if (chunk != null)
        {
            chunkPool.Return(chunk);
            chunk = null;
        }

        children = new OctreeNode[8];
        float childSize = bounds.size.x / 2f;
        float offset = childSize / 2f;
        children[0] = new OctreeNode(bounds.center + new Vector3(-offset, -offset, -offset), childSize, depth + 1);
        children[1] = new OctreeNode(bounds.center + new Vector3(offset, -offset, -offset), childSize, depth + 1);
        children[2] = new OctreeNode(bounds.center + new Vector3(offset, -offset, offset), childSize, depth + 1);
        children[3] = new OctreeNode(bounds.center + new Vector3(-offset, -offset, offset), childSize, depth + 1);
        children[4] = new OctreeNode(bounds.center + new Vector3(-offset, offset, -offset), childSize, depth + 1);
        children[5] = new OctreeNode(bounds.center + new Vector3(offset, offset, -offset), childSize, depth + 1);
        children[6] = new OctreeNode(bounds.center + new Vector3(offset, offset, offset), childSize, depth + 1);
        children[7] = new OctreeNode(bounds.center + new Vector3(-offset, offset, offset), childSize, depth + 1);
    }

    private void Merge(Pool<Chunk> chunkPool)
    {
        isLeaf = true;
        if (children != null)
        {
            foreach (var child in children)
            {
                child.Merge(chunkPool);
                if (child.chunk != null)
                {
                    chunkPool.Return(child.chunk);
                }
            }
            children = null;
        }
    }

    public void ModifyTerrain(Vector3 worldPos, float strength, float radius, Pool<Chunk> chunkPool)
    {
        if (!bounds.Intersects(new Bounds(worldPos, Vector3.one * radius * 2)))
        {
            return;
        }

        if (isLeaf)
        {
            if (depth < OctreeTerrainManager.Instance.maxDepth)
            {
                float distanceToEdge = float.MaxValue;
                distanceToEdge = Mathf.Min(distanceToEdge, worldPos.x - bounds.min.x);
                distanceToEdge = Mathf.Min(distanceToEdge, bounds.max.x - worldPos.x);
                distanceToEdge = Mathf.Min(distanceToEdge, worldPos.y - bounds.min.y);
                distanceToEdge = Mathf.Min(distanceToEdge, bounds.max.y - worldPos.y);
                distanceToEdge = Mathf.Min(distanceToEdge, worldPos.z - bounds.min.z);
                distanceToEdge = Mathf.Min(distanceToEdge, bounds.max.z - worldPos.z);

                if (distanceToEdge < radius)
                {
                    Subdivide(chunkPool);
                    if (children != null)
                    {
                        foreach (var child in children)
                        {
                            child.ModifyTerrain(worldPos, strength, radius, chunkPool);
                        }
                    }
                    return;
                }
            }

            if (chunk != null)
            {
                chunk.ModifyDensity(worldPos, strength, radius);
            }
        }
        else
        {
            if (children != null)
            {
                foreach (var child in children)
                {
                    child.ModifyTerrain(worldPos, strength, radius, chunkPool);
                }
            }
        }
    }

    public void DrawGizmos()
    {
        Gizmos.color = Color.Lerp(Color.green, Color.red, depth / (float)OctreeTerrainManager.Instance.maxDepth);
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        if (!isLeaf)
        {
            foreach (var child in children)
            {
                child.DrawGizmos();
            }
        }
    }
}