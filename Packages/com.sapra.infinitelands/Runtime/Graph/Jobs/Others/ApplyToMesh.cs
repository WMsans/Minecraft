using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;
using System.Threading;

namespace sapra.InfiniteLands
{
    public static class GenericApplyMethods{
        public static float3 calculatePlaneNormalFromIndices(int2 A, int2 B, int2 C, int MeshResolution, int IncreasedResolution, NativeArray<Vertex> points)
        {
            var PA = position(A.x, A.y, MeshResolution, IncreasedResolution, points);
            var PB = position(B.x, B.y, MeshResolution, IncreasedResolution, points);
            var PC = position(C.x, C.y, MeshResolution, IncreasedResolution, points);
            return cross(PB - PA, PC - PA);
        }


        public static float3 position(int x, int y, int MeshResolution, int IncreasedResolution, NativeArray<Vertex> points)
        {
            int index = MapTools.GetFlatIndex(int2(x,y), MeshResolution, IncreasedResolution);
            Vertex v = points[index];     
            return v.position;
        }

        public static float3 calculatePlaneNormalFromIndices(int2 A, int2 B, int2 C, int MeshResolution, NativeArray<Vertex> points)
        {
            var PA = position(A.x, A.y, MeshResolution, points);
            var PB = position(B.x, B.y, MeshResolution, points);
            var PC = position(C.x, C.y, MeshResolution, points);
            return cross(PB - PA, PC - PA);
        }


        public static float3 position(int x, int y, int MeshResolution, NativeArray<Vertex> points)
        {
            int index = MapTools.GetFlatIndex(int2(x,y), MeshResolution-1, MeshResolution);
            Vertex v = points[index];     
            return v.position;
        }
    }
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    public struct ApplyToMeshDecimated : IJobFor
    {
        [ReadOnly]
        NativeArray<Vertex> vertices;
        public int MeshResolution;

        [NativeDisableContainerSafetyRestriction]
        NativeArray<Vertex> increasedVertices;
        int IncreasedResolution;

        [WriteOnly] NativeList<Vertex>.ParallelWriter clearedVertices;
        [WriteOnly] NativeParallelHashMap<int, int>.ParallelWriter pointToVertexReferences;

        int CoreGridSpacing;
        float NormalReduceThreshold;


        public unsafe void Execute(int i)
        {
            Vertex v = vertices[i];
            int2 index = MapTools.IndexToVector(i, MeshResolution);
            
            float3 right = GenericApplyMethods.calculatePlaneNormalFromIndices(int2(index.x, index.y), int2(index.x, index.y + 1), int2(index.x + 1, index.y), MeshResolution, IncreasedResolution, increasedVertices);
            float3 forward = GenericApplyMethods.calculatePlaneNormalFromIndices(int2(index.x, index.y), int2(index.x, index.y - 1), int2(index.x - 1, index.y), MeshResolution, IncreasedResolution, increasedVertices);

            var isEdge = index.x == 0 || index.x == MeshResolution || index.y == 0 || index.y == MeshResolution;
            var angle = acos(dot(normalize(forward), normalize(right)));
            var isCoreGridPoint = index.x % CoreGridSpacing == 0 && index.y % CoreGridSpacing == 0;
            var skipPoint = !isEdge && !isCoreGridPoint && angle < NormalReduceThreshold;

            if (skipPoint) return;
            int newIndex = AddWithIndex(ref clearedVertices, v);
            pointToVertexReferences.TryAdd(i, newIndex);
        }


        public static JobHandle ScheduleParallel(NativeArray<Vertex> vertices, int MeshResolution,
            NativeArray<Vertex> increasedVertices, int IncreasedResolution,
            NativeList<Vertex> clearedVertices, NativeParallelHashMap<int, int> pointToVertexReferences,
            int CoreGridSpacing, float NormalReduceThreshold,
            JobHandle dependency
        ) => new ApplyToMeshDecimated()
        {
            vertices = vertices,
            IncreasedResolution = IncreasedResolution,
            increasedVertices = increasedVertices,
            clearedVertices = clearedVertices.AsParallelWriter(),
            pointToVertexReferences = pointToVertexReferences.AsParallelWriter(),
            MeshResolution = MeshResolution,
            CoreGridSpacing = CoreGridSpacing,
            NormalReduceThreshold = NormalReduceThreshold,
        }.ScheduleParallel(vertices.Length, MeshResolution, dependency);

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