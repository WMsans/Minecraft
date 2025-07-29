using Unity.Collections;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Jobs;
using Unity.Collections.LowLevel.Unsafe;

namespace sapra.InfiniteLands
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct CoordinateDataJob : IJobFor
    {
        [ReadOnly] NativeArray<Vertex> points;

/*         [ReadOnly] NativeArray<float> vegetationMap;
        int vegetationCount;
        [ReadOnly] NativeArray<float> texturesMap;
        int textureCount;
 */
        [NativeDisableContainerSafetyRestriction]
        NativeArray<CoordinateData> coordinateData;

        //int verticesLenght;
        float3 offset;
        int3 id;

        public void Execute(int i)
        {
            Vertex v = points[i];
            float3 np = v.position+offset;

/*             float4 vegWeights = 0;
            int4 vegIndices = -1;
            for (int l = 0; l < vegetationCount; l++)
            {
                JobExtensions.insertSorted(ref vegIndices, ref vegWeights, l, vegetationMap[l * verticesLenght + i]);
            }

            float4 texWeights = 0;
            int4 texIndices = -1;
            for (int l = 0; l < textureCount; l++)
            {
                JobExtensions.insertSorted(ref texIndices, ref texWeights, l, texturesMap[l * verticesLenght + i]);
            } */

            CoordinateData point = new CoordinateData(np, v.normal, id);

            coordinateData[i] = point;
            
        }
        public static JobHandle ScheduleParallel(NativeArray<Vertex> vertices,
            //NativeArray<float> vegetationMap, int vegetationCount, NativeArray<float> texturesMap, int textureCount,
            NativeArray<CoordinateData> coordinateData, float3 offset, int3 id, int resolution,
            JobHandle dependency) => new CoordinateDataJob()
        {
/*             vegetationMap = vegetationMap,
            vegetationCount = vegetationCount, */
            points = vertices,
            offset = offset,
            id = id,
            //verticesLenght = coordinateData.Length,
/*             texturesMap = texturesMap,
            textureCount = textureCount, */
            coordinateData = coordinateData,
        }.ScheduleParallel(coordinateData.Length, resolution, dependency);
    }
}