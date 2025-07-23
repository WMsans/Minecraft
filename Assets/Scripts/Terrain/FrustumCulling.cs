using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

[BurstCompile]
public static class FrustumCulling
{
    public struct FrustumPlanes
    {
        public float4 left, right, bottom, top, near, far;
    }

    public static FrustumPlanes GetFrustumPlanes(Camera camera)
    {
        Plane[] planes = GeometryUtility.CalculateFrustumPlanes(camera);
        return new FrustumPlanes
        {
            left = new float4(planes[0].normal, planes[0].distance),
            right = new float4(planes[1].normal, planes[1].distance),
            bottom = new float4(planes[2].normal, planes[2].distance),
            top = new float4(planes[3].normal, planes[3].distance),
            near = new float4(planes[4].normal, planes[4].distance),
            far = new float4(planes[5].normal, planes[5].distance)
        };
    }

    public static bool TestAABB(FrustumPlanes planes, Bounds bounds)
    {
        float3 center = bounds.center;
        float3 extents = bounds.extents;

        if (math.dot(planes.left.xyz, center) + extents.x * math.abs(planes.left.x) + extents.y * math.abs(planes.left.y) + extents.z * math.abs(planes.left.z) + planes.left.w < 0) return false;
        if (math.dot(planes.right.xyz, center) + extents.x * math.abs(planes.right.x) + extents.y * math.abs(planes.right.y) + extents.z * math.abs(planes.right.z) + planes.right.w < 0) return false;
        if (math.dot(planes.bottom.xyz, center) + extents.x * math.abs(planes.bottom.x) + extents.y * math.abs(planes.bottom.y) + extents.z * math.abs(planes.bottom.z) + planes.bottom.w < 0) return false;
        if (math.dot(planes.top.xyz, center) + extents.x * math.abs(planes.top.x) + extents.y * math.abs(planes.top.y) + extents.z * math.abs(planes.top.z) + planes.top.w < 0) return false;
        if (math.dot(planes.near.xyz, center) + extents.x * math.abs(planes.near.x) + extents.y * math.abs(planes.near.y) + extents.z * math.abs(planes.near.z) + planes.near.w < 0) return false;
        if (math.dot(planes.far.xyz, center) + extents.x * math.abs(planes.far.x) + extents.y * math.abs(planes.far.y) + extents.z * math.abs(planes.far.z) + planes.far.w < 0) return false;

        return true;
    }
}