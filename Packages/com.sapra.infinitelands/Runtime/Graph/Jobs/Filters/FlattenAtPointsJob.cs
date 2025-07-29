using UnityEngine;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;

namespace sapra.InfiniteLands{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct FlattenAtPointsJob : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> heightMap;
        [ReadOnly] NativeArray<float3> vertices;

        [ReadOnly]
        NativeArray<PointTransform> points;

        IndexAndResolution current;
        IndexAndResolution target;

        float FallofDistance;
        float Radius;
        int PointsCount;
        int verticesResolution;
        float3 offset;

        bool afectedByScale;

        public void Execute(int i)
        {
            int currentIndex = MapTools.RemapIndex(i, target.Resolution, current.Resolution);
            int pointIndex = MapTools.RemapIndex(i, target.Resolution, verticesResolution);
            var flatVertexPosition = vertices[pointIndex].xz+offset.xz;

            float value = heightMap[current.StartIndex + currentIndex];
            float height = 0;
            float totalWeight = 0;
            for(int p = 0; p < PointsCount; p++){
                float2 flat = points[p].Position.xz;
                float dis = math.distance(flat, flatVertexPosition);
                float targetSize = afectedByScale ? Radius*points[p].Scale : Radius;
                float weight = math.saturate(1.0f - math.smoothstep(targetSize, targetSize+FallofDistance, dis));
                if(weight > 0){
                    totalWeight += weight;
                    height += weight*points[p].Position.y;
                }
            }

            var averageHeight = value;
            if(totalWeight > 0)
                averageHeight = height/totalWeight;


            heightMap[target.StartIndex + i] = math.lerp(value, averageHeight, math.saturate(totalWeight));
        }

        public static JobHandle ScheduleParallel(NativeArray<float> globalMap, NativeArray<float3> vertices, int verticesResolution, NativeArray<PointTransform> points,
            IndexAndResolution current, IndexAndResolution target,
            int PointsCount, float Strength, float Distance, bool afectedByScale,
            float3 offset, JobHandle dependency) => new FlattenAtPointsJob()
        {
            heightMap = globalMap,
            current = current,
            afectedByScale = afectedByScale,
            target = target,
            PointsCount = PointsCount,
            verticesResolution = verticesResolution,
            FallofDistance = Strength,
            Radius = Distance/2.0f,
            vertices = vertices,
            points = points,
            offset = offset,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct FlattenAtPointsTextureJob : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> heightMap;
        [ReadOnly] NativeArray<float3> vertices;
        
        [NativeDisableContainerSafetyRestriction]
        NativeArray<float> textureMap;
        IndexAndResolution textureIndex;

        [ReadOnly]
        NativeArray<PointTransform> points;

        IndexAndResolution current;
        IndexAndResolution target;

        float Distance;
        int PointsCount;
        int verticesResolution;
        float3 offset;

        bool affectedByScale;
        bool affectedByRotation;

        public void Execute(int i)
        {
            int currentIndex = MapTools.RemapIndex(i, target.Resolution, current.Resolution);
            int pointIndex = MapTools.RemapIndex(i, target.Resolution, verticesResolution);
            var flatVertexPosition = vertices[pointIndex].xz+offset.xz;

            float value = heightMap[current.StartIndex + currentIndex];
            float height = 0;
            float totalWeight = 0;
            for(int p = 0; p < PointsCount; p++){
                PointTransform point = points[p];
                float2 flat = point.Position.xz;
                float dist = affectedByScale ? Distance*point.Scale : Distance;
                float2 uv = (flatVertexPosition-flat)/dist+0.5f;            
                uv = affectedByRotation ? Rotate(uv, point.YRotation) : uv;  
                                 
                if(uv.x >= 0.0f && uv.y >= 0.0f && uv.x <= 1.0f && uv.y <= 1.0f)
                {
                    // Apply smooth falloff and texture value
                    float textureValue = MapTools.biplanarSampling(uv, textureIndex, textureMap);
                    var weight = textureValue;
                    
                    if(weight > 0)
                    {
                        totalWeight += weight;
                        height += weight * points[p].Position.y;
                    }
                }
            }

            var averageHeight = value;
            if(totalWeight > 0)
                averageHeight = height/totalWeight;

            heightMap[target.StartIndex + i] = math.lerp(value, averageHeight, math.saturate(totalWeight));
        }
        public float2 Rotate(float2 uv, float rotation){
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

        public static JobHandle ScheduleParallel(NativeArray<float> globalMap, NativeArray<float> textureMap, NativeArray<float3> vertices, int VerticesResolution,
            NativeArray<PointTransform> points, IndexAndResolution target, IndexAndResolution current, IndexAndResolution textureIndex,
            int PointsCount,float Distance, 
            bool affectedByScale, bool affectedByRotation, float3 offset, JobHandle dependency) => new FlattenAtPointsTextureJob()
        {
            heightMap = globalMap,
            textureMap = textureMap,
            target = target,
            textureIndex= textureIndex,
            PointsCount = PointsCount,
            current = current,
            verticesResolution = VerticesResolution,
            Distance = Distance,
            vertices = vertices,
            points = points,
            offset = offset,
            affectedByScale = affectedByScale,
            affectedByRotation = affectedByRotation,
        }.ScheduleParallel(target.Length, target.Resolution, dependency);
    }
}