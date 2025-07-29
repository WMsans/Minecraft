using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;

namespace sapra.InfiniteLands{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct PointsToHeightJob : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> heightMap;
        [ReadOnly] NativeArray<float3> vertices;

        [ReadOnly]
        NativeArray<PointTransform> points;

        IndexAndResolution target;

        float Strength;
        float Radius;
        int PointsCount;
        int verticesResolution;
        float3 offset;

        bool afectedByScale;

        public void Execute(int i)
        {
            int pointIndex = MapTools.RemapIndex(i, target.Resolution, verticesResolution);
            var flatVertexPosition = vertices[pointIndex].xz+offset.xz;

            float totalWeight = 0;
            for(int p = 0; p < PointsCount; p++){
                float2 flat = points[p].Position.xz;
                float targetSize = afectedByScale ? Radius*points[p].Scale : Radius;
                float dis = math.distance(flat, flatVertexPosition);
                float weight = math.saturate(1.0f-math.smoothstep(targetSize, targetSize+Strength, dis));
                if(weight > 0){
                    totalWeight += weight;
                }
            }
            heightMap[target.StartIndex + i] = math.saturate(totalWeight);
        }

        public static JobHandle ScheduleParallel(NativeArray<float> globalMap,
            NativeArray<float3> vertices, int verticesResolution,
            NativeArray<PointTransform> points,
            IndexAndResolution target,
            int PointsCount, float Strength, float Distance, 
            bool afectedByScale, float3 offset, JobHandle dependency) => new PointsToHeightJob()
        {
            heightMap = globalMap,
            target = target,
            PointsCount = PointsCount,
            verticesResolution = verticesResolution,
            Strength = Strength,
            Radius = Distance/2.0f,
            vertices = vertices,
            points = points,
            offset = offset,
            afectedByScale = afectedByScale,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }

    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct PointsToHeightTextureJob : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> textureMap;
        IndexAndResolution textureIndex;

        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> heightMap;
        [ReadOnly] NativeArray<float3> vertices;

        [ReadOnly]
        NativeArray<PointTransform> points;

        IndexAndResolution target;

        float Size;
        int PointsCount;
        int verticesResolution;
        float3 offset;

        bool affectedByScale;
        bool affectedByRotation;
        bool textureHeightAffectedByScale;
        float2 textureMinMax;

        public void Execute(int i)
        {
            int pointIndex = MapTools.RemapIndex(i, target.Resolution, verticesResolution);
            var flatVertexPosition = vertices[pointIndex].xz+offset.xz;

            float totalWeight = 0;
            for(int p = 0; p < PointsCount; p++)
            {
                PointTransform point = points[p];
                float2 flat = point.Position.xz;
                float dist = affectedByScale ? Size*point.Scale : Size;
                float2 uv = (flatVertexPosition-flat)/dist+0.5f;            
                uv = affectedByRotation ? Rotate(uv, point.YRotation) : uv;  
                                
                if(uv.x >= 0.0f && uv.y >= 0.0f && uv.x <= 1.0f && uv.y <= 1.0f)
                {
                    // Apply smooth falloff and texture value
                    float textureValue = MapTools.biplanarSampling(uv, textureIndex, textureMap);
                    float weight = textureValue;
                    if(textureHeightAffectedByScale){
                        float normalized = JobExtensions.invLerp(textureMinMax.x, textureMinMax.y, textureValue);
                        normalized *= point.Scale;
                        weight = math.lerp(textureMinMax.x, textureMinMax.y, normalized);    
                    }

                    if(weight > 0)
                    {
                        totalWeight = math.max(totalWeight,weight);
                    }
                }
            }
            heightMap[target.StartIndex + i] = totalWeight;
        }

        public float2 Rotate(float2 uv, float rotation)
        {
            float rad = rotation * Mathf.Deg2Rad;
            float cosR = math.cos(rad);
            float sinR = math.sin(rad);
            // Apply rotation to UV coordinates around center (0.5, 0.5)
            float2 centeredUV = uv - 0.5f; // Move to center
            float2 rotatedUV;
            rotatedUV.x = centeredUV.x * cosR - centeredUV.y * sinR;
            rotatedUV.y = centeredUV.x * sinR + centeredUV.y * cosR;
            return rotatedUV + 0.5f; // Move back from center
        }

        public static JobHandle ScheduleParallel(NativeArray<float> globalMap, NativeArray<float> textureMap,
            NativeArray<float3> vertices, int verticesResolution,
            NativeArray<PointTransform> points, 
            IndexAndResolution target, IndexAndResolution textureIndex,
            int PointsCount, float Distance, 
            bool affectedByScale, bool affectedByRotation, bool textureHeightAffectedByScale, float2 textureMinMax,
            float3 offset, JobHandle dependency) => new PointsToHeightTextureJob()
        {
            heightMap = globalMap,
            textureMap = textureMap,
            target = target,
            textureIndex= textureIndex,
            PointsCount = PointsCount,
            verticesResolution = verticesResolution,
            Size = Distance,
            vertices = vertices,
            points = points,
            offset = offset,
            affectedByScale = affectedByScale, 
            affectedByRotation = affectedByRotation,
            textureHeightAffectedByScale = textureHeightAffectedByScale,
            textureMinMax = textureMinMax,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }
}