using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

[BurstCompile]
public struct RaycastJob : IJobParallelFor
{
    [ReadOnly] public float3 rayOrigin;
    [ReadOnly] public float3 rayDirection;
    [ReadOnly] public NativeArray<float3> vertices;
    [ReadOnly] public NativeArray<int> triangles;

    public NativeList<BurstRaycast.RaycastHit>.ParallelWriter hits;

    public void Execute(int index)
    {
        int triIndex = index * 3;
        float3 v0 = vertices[triangles[triIndex]];
        float3 v1 = vertices[triangles[triIndex + 1]];
        float3 v2 = vertices[triangles[triIndex + 2]];

        if (RayTriangleIntersection(rayOrigin, rayDirection, v0, v1, v2, out float distance, out float3 normal))
        {
            hits.AddNoResize(new BurstRaycast.RaycastHit
            {
                distance = distance,
                point = rayOrigin + rayDirection * distance,
                normal = normal
            });
        }
    }

    // Möller–Trumbore intersection algorithm for ray-triangle intersection.
    private bool RayTriangleIntersection(float3 ro, float3 rd, float3 v0, float3 v1, float3 v2, out float distance, out float3 normal)
    {
        distance = 0;
        normal = float3.zero;

        float3 edge1 = v1 - v0;
        float3 edge2 = v2 - v0;
        float3 h = math.cross(rd, edge2);
        float a = math.dot(edge1, h);

        if (a > -1e-6 && a < 1e-6)
            return false; // The ray is parallel to the triangle.

        float f = 1.0f / a;
        float3 s = ro - v0;
        float u = f * math.dot(s, h);

        if (u < 0.0 || u > 1.0)
            return false;

        float3 q = math.cross(s, edge1);
        float v = f * math.dot(rd, q);

        if (v < 0.0 || u + v > 1.0)
            return false;

        float t = f * math.dot(edge2, q);

        if (t > 1e-6) // Ray intersection
        {
            distance = t;
            normal = math.normalize(math.cross(edge1, edge2));
            if (math.dot(normal, rd) > 0)
            {
                normal = -normal;
            }
            return true;
        }

        return false;
    }
}