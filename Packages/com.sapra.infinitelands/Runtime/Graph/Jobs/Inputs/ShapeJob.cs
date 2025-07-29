using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using static Unity.Mathematics.math;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using System;
using System.Runtime.CompilerServices;

namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    internal struct ShapeJob<T> : IJobFor where T : struct, IShapeJob
    {
        [ReadOnly] NativeArray<float3x4> vertices;

        [NativeDisableContainerSafetyRestriction] [WriteOnly]
        NativeSlice<float4> globalMap;
                
        float2 FromTo;
        float3 offset;
        float YRotation;
        float Size;

        public void Execute(int i)
        {
            float4x3 pt = transpose(vertices[i]);
            pt.c0 += offset.x;  // Add x offset
            pt.c2 += offset.z;  // Add z offset

            // Apply rotation
            float4x3 position = RotateY(pt, math.radians(YRotation));
            
            // Calculate shape value and lerp
            globalMap[i] = lerp(FromTo.x, FromTo.y, 
                default(T).GetValue(position, YRotation, Size));
        }

        // Helper method to rotate points around Y axis
        private float4x3 RotateY(float4x3 points, float angle)
        {
            float cosA = math.cos(angle);
            float sinA = math.sin(angle);

            float4 newX = cosA * points.c0 - sinA * points.c2;
            float4 newY = points.c1;
            float4 newZ = sinA * points.c0 + cosA * points.c2;

            return float4x3(newX, newY, newZ);
        }

        public static JobHandle ScheduleParallel(NativeArray<float3x4> vertices, NativeSlice<float4> globalMap,
            float2 FromTo, float3 offset, float YRotation, float Size, 
            IndexAndResolution target, JobHandle dependency) => new ShapeJob<T>()
        {
            globalMap = globalMap,
            FromTo = FromTo,
            vertices = vertices,
            offset = offset,
            YRotation = YRotation,
            Size = Size,
        }.ScheduleParallel(vertices.Length, target.Resolution, dependency);
    }


    public interface IShapeJob
    {
        float4 GetValue(float4x3 position, float YRotation, float Size);
    }

    public static class ShapeMath
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float4 SquareDistXZ(float4 x, float4 z) => x * x + z * z;
    }

    public struct SSquare : IShapeJob
    {
        public float4 GetValue(float4x3 position, float YRotation, float Size)
        {
            float4 x = position.c0;
            float4 z = position.c2;
            float halfSize = Size * 0.5f;
            
            // Check if points are within square bounds (returns 1 or 0)
            float4 within = step(abs(x), halfSize) * step(abs(z), halfSize);
            return within; // Normalized [0,1], height is either full or none
        }
    }

    public struct SPyramid : IShapeJob
    {
        public float4 GetValue(float4x3 position, float YRotation, float Size)
        {
            float4 x = position.c0;
            float4 z = position.c2;
            float halfSize = Size * 0.5f;
            
            float4 maxDist = max(abs(x), abs(z));
            return max(0, 1 - maxDist / halfSize); // Normalized [0,1]
        }
    }

    public struct SCone : IShapeJob
    {
        public float4 GetValue(float4x3 position, float YRotation, float Size)
        {
            float4 x = position.c0;
            float4 z = position.c2;
            float radius = Size * 0.5f;
            
            float4 dist = sqrt(ShapeMath.SquareDistXZ(x, z));
            return 1 - saturate(dist / radius); // Normalized [0,1]
        }
    }

    public struct SBump : IShapeJob
    {
        public float4 GetValue(float4x3 position, float YRotation, float Size)
        {
            float4 x = position.c0;
            float4 z = position.c2;
            float radius = Size * 0.5f;
            
            float4 distSq = ShapeMath.SquareDistXZ(x, z);
            return 1 - saturate(distSq / (radius * radius)); // Normalized [0,1]
        }
    }

    public struct SCylinder : IShapeJob
    {
        public float4 GetValue(float4x3 position, float YRotation, float Size)
        {
            float4 x = position.c0;
            float4 y = position.c1;
            float4 z = position.c2;
            float halfSize = Size * 0.5f;
            
            float4 dist = sqrt(ShapeMath.SquareDistXZ(x, z));
            return step(dist, halfSize) * step(-halfSize, y) * step(y, halfSize);
        }
    }

    public struct SHalfSphere : IShapeJob
    {
        public float4 GetValue(float4x3 position, float YRotation, float Size)
        {
            float4 x = position.c0;
            float4 z = position.c2;
            float radius = Size * 0.5f;
            
            float4 distSq = ShapeMath.SquareDistXZ(x, z);
            return sqrt(max(0, radius * radius - distSq)) / radius;
        }
    }

    public struct SHalfTorus : IShapeJob
    {
        public float4 GetValue(float4x3 position, float YRotation, float Size)
        {
            float4 x = position.c0;
            float4 z = position.c2;
            float rMajor = Size * 0.25f;
            float rMinor = Size * 0.25f;
            
            float4 distXZ = sqrt(ShapeMath.SquareDistXZ(x, z));
            float4 q = abs(distXZ - rMajor);
            return sqrt(max(0, rMinor * rMinor - q * q)) / rMinor;
        }
    }
}