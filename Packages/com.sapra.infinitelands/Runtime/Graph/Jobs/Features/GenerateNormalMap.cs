using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct NormalMapCalculator
    {
        [ReadOnly] NativeArray<float3> points;
        int verticesResolution;

        [ReadOnly] NativeArray<float> globalMap;

        IndexAndResolution normalIndex;
        IndexAndResolution current;

        public NormalMapCalculator(NativeArray<float3> points, int verticesResolution, NativeArray<float> globalMap,
            IndexAndResolution normalIndex, IndexAndResolution current)
        {
            this.points = points;
            this.verticesResolution = verticesResolution;
            this.globalMap = globalMap;
            this.normalIndex = normalIndex;
            this.current = current;
        }

        public float3 calculateNormalAtIndex(int x, int y)
        {
            float3 xyPos = position(x, y);
            float3 xmyPos = position(x-1, y);
            float3 xmypPos = position(x-1, y+1);
            float3 xypPos = position(x, y+1);
            float3 xpyPos = position(x+1, y);
            float3 xpymPos = position(x+1, y-1);
            float3 xymPos = position(x, y-1);

            float3 normal1 = calculatePlaneNormalFromIndices(xyPos, xmyPos, xmypPos);
            float3 normal2 = calculatePlaneNormalFromIndices(xyPos, xmypPos, xypPos);
            float3 normal3 = calculatePlaneNormalFromIndices(xyPos, xypPos, xpyPos);
            float3 normal4 = calculatePlaneNormalFromIndices(xyPos, xpyPos, xpymPos);
            float3 normal5 = calculatePlaneNormalFromIndices(xyPos, xpymPos, xymPos);
            float3 normal6 = calculatePlaneNormalFromIndices(xyPos, xymPos, xmyPos);

            float3 result = normalize(normal1 + normal2 + normal3 + normal4 + normal5 + normal6);

            return result;
        }

        public float3 surfaceNormal(int i, int j)
        {
            float ijHeight = extractHeight(i, j);
            float ipjHeight = extractHeight(i+1, j);
            float ipjpHeight = extractHeight(i+1, j+1);
            float ipjmHeight = extractHeight(i+1, j-1);
            float ijpHeight = extractHeight(i, j+1);
            float ijmHeight = extractHeight(i, j-1);

            float3 n = float3(0.15) * normalize(float3(ijHeight - ipjHeight, 1.0f, 0.0f));  //Positive X
            n += float3(0.15) * normalize(float3(ijHeight - ijHeight, 1.0f, 0.0f));  //Negative X
            n += float3(0.15) * normalize(float3(0.0f, 1.0f, ijHeight - ijpHeight));    //Positive Y
            n += float3(0.15) * normalize(float3(0.0f, 1.0f, ijmHeight - ijHeight));  //Negative Y

            //Diagonals! (This removes the last spatial artifacts)
/*             n += float3(0.1) * normalize(float3(ijHeight - ipjpHeight / sqrt(2), sqrt(2), ijHeight - ipjpHeight / sqrt(2)));    //Positive Y
            n += float3(0.1) * normalize(float3(ijHeight - ipjmHeight / sqrt(2), sqrt(2), ijHeight - ipjmHeight / sqrt(2)));    //Positive Y
            n += float3(0.1) * normalize(float3(ijHeight - ijpHeight / sqrt(2), sqrt(2), ijHeight - ijpHeight / sqrt(2)));    //Positive Y
            n += float3(0.1) * normalize(float3(ijHeight - ijmHeight / sqrt(2), sqrt(2), ijHeight - ijmHeight / sqrt(2)));    //Positive Y
 */
            return n;
        }

        public float3 calculateNormalAtIndexFast(float2 pos)
        {
            int2 coord = int2(pos);

            // Calculate droplet's offset inside the cell (0,0) = at NW node, (1,1) = at SE node
            float2 ver = pos - coord;
            float x = ver.x;
            float y = ver.y;

            // Calculate heights of the four nodes of the droplet's cell
            int index = MapTools.VectorToIndex(coord, current.Resolution);

            float heightNW = globalMap[index + current.StartIndex];
            float heightNE = globalMap[index + current.StartIndex + 1];
            float heightSW = globalMap[index + current.StartIndex + current.Resolution + 1];
            float heightSE = globalMap[index + current.StartIndex + current.Resolution + 1 + 1];

            // Calculate droplet's direction of flow with bilinear interpolation of height difference along the edges
            float gradientX = (heightNE - heightNW) * (1 - y) + (heightSE - heightSW) * y;
            float gradientY = (heightSW - heightNW) * (1 - x) + (heightSE - heightNE) * x;

            // Calculate height with bilinear interpolation of the heights of the nodes of the cell
            float height = heightNW * (1 - x) * (1 - y) + heightNE * x * (1 - y) + heightSW * (1 - x) * y + heightSE * x * y;
            return -float3(gradientX, height, gradientY);
        }

        float3 calculatePlaneNormalFromIndices(float3 PA, float3 PB, float3 PC)
        {
            float3 re1 = normalize(PB - PA);
            if (float.IsNaN(length(re1)) || length(re1) < 1e-1)
                re1 = float3(0, 0, 1);

            float3 re2 = normalize(PC - PA);
            if (float.IsNaN(length(re2)) || length(re2) < 1e-1)
                re2 = float3(1, 0, 0);

            float3 result = cross(re1, re2);
            if (result.y < 0)
                result = -result;

            return normalize(result);
        }


        public float3 position(int x, int y)
        {
            int index = MapTools.GetFlatIndex(int2(x, y), normalIndex.Resolution, current.Resolution);
            int vertexIndex = MapTools.GetFlatIndex(int2(x, y), normalIndex.Resolution, verticesResolution);
            float3 ps = points[vertexIndex];
            //float2 sition = Meshoat2(x,y)/(verticesResolution); //fake fix for strong warping, should it be used?
            float3 position = float3(ps.x, globalMap[index + current.StartIndex], ps.z);
            return position;
        }

        public float extractHeight(int x, int y)
        {
            int index = MapTools.GetFlatIndex(int2(x, y), normalIndex.Resolution, current.Resolution);
            return globalMap[index + current.StartIndex];
        }
    }
    
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct GenerateNormalMap : IJobFor
    {
        [WriteOnly] NativeArray<float3> normalMap;
        IndexAndResolution normalIndex;

        NormalMapCalculator calculator;

        public void Execute(int index)
        {
            int2 vector = MapTools.IndexToVector(index, normalIndex.Resolution);

            float3 normal = calculator.calculateNormalAtIndex(vector.x, vector.y);
            normalMap[index] = normal;

        }
        
        public static JobHandle ScheduleParallel(NativeArray<float3> vertices, int verticesResolution, NativeArray<float3> normalMap,
            NativeArray<float> globalMap, IndexAndResolution normalIndex, IndexAndResolution original,
            JobHandle dependency) => new GenerateNormalMap()
        {
            calculator = new NormalMapCalculator(vertices, verticesResolution, globalMap, normalIndex, original),
            normalIndex = normalIndex,
            normalMap = normalMap,
        }.ScheduleParallel(normalIndex.Length, normalIndex.Resolution, dependency);

    }
}