using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using System.Threading;

namespace sapra.InfiniteLands.MeshProcess
{
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct FindValidDecimatedPoints : IJobFor
    {
        [ReadOnly]
        NativeArray<Vertex> vertices;
        public int MeshResolution;

        [WriteOnly] NativeList<Vertex>.ParallelWriter validPoints;
        [WriteOnly] NativeParallelHashMap<int, ushort>.ParallelWriter validHashMap;

        int CoreGridSpacing;
        float NormalReduceThreshold;

        public unsafe void Execute(int i)
        {
            Vertex v = vertices[i];
            int2 index = MapTools.IndexToVector(i, MeshResolution);
            var isEdge = index.x == 0 || index.x == MeshResolution || index.y == 0 || index.y == MeshResolution;
            ushort newIndex;
            if(isEdge){
                newIndex = (ushort)AddWithIndex(ref validPoints, v);
                validHashMap.TryAdd(i, newIndex);
                return;
            }
            
            float3 right = GenericApplyMethods.calculatePlaneNormalFromIndices(int2(index.x, index.y), int2(index.x, index.y + 1), int2(index.x + 1, index.y), MeshResolution, vertices);
            float3 forward = GenericApplyMethods.calculatePlaneNormalFromIndices(int2(index.x, index.y), int2(index.x, index.y - 1), int2(index.x - 1, index.y), MeshResolution, vertices);

            var angle = acos(dot(normalize(forward), normalize(right)));
            var isCoreGridPoint = index.x % CoreGridSpacing == 0 && index.y % CoreGridSpacing == 0;
            var skipPoint = !isCoreGridPoint && angle < NormalReduceThreshold;

            if (skipPoint) return;

            newIndex = (ushort)AddWithIndex(ref validPoints, v);
            validHashMap.TryAdd(i, newIndex);
        }


        public static JobHandle ScheduleParallel(NativeList<Vertex> validPoints, NativeParallelHashMap<int, ushort> validHashMap, int length,
            MeshSettings meshSettings,int CoreGridSpacing, float NormalReduceThreshold, NativeArray<Vertex> originalPositions) => new FindValidDecimatedPoints()
        {

            vertices = originalPositions,
            validPoints = validPoints.AsParallelWriter(),
            validHashMap = validHashMap.AsParallelWriter(),
            MeshResolution = meshSettings.Resolution,
            CoreGridSpacing = CoreGridSpacing,
            NormalReduceThreshold = NormalReduceThreshold,
        }.ScheduleParallel(length, meshSettings.Resolution, default);

        public static unsafe int AddWithIndex<T>(ref NativeList<T>.ParallelWriter list, in T element)
            where T : unmanaged
        {
            var listData = list.ListData;
            var idx = Interlocked.Increment(ref listData->m_length) - 1;
            UnsafeUtility.WriteArrayElement(listData->Ptr, idx, element);

            return idx;
        }
    }
}