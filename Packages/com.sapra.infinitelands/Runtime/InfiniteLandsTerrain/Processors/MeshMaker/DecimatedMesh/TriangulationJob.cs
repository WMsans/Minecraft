using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using static Unity.Mathematics.math;

namespace sapra.InfiniteLands.MeshProcess
{
    [BurstCompile]
    internal struct TriangulationJob : IJobFor
    {
        [ReadOnly] public MeshSettings settings;
        [ReadOnly] public NativeParallelHashMap<int, ushort> vertexReferences;
        [WriteOnly] public NativeList<ushort3>.ParallelWriter triangles;

        int patchCountPerLine;

        public void Execute(int patchIndex)
        {
            int2 patchPosition = ReverseLinearIndex(patchIndex, patchCountPerLine);

            int patchLineVertCount = (int)ceil(settings.Resolution / (float)patchCountPerLine);
            int2 startVertex = patchPosition * patchLineVertCount;

            NativeList<int2> patchVertices = new NativeList<int2>(patchLineVertCount * patchLineVertCount / 2, Allocator.Temp);

            float size = length(new int2(patchLineVertCount + 1));

            // get all non-skipped vertices
            for (int x = startVertex.x; x < startVertex.x + patchLineVertCount + 1; x++)
            {
                for (int y = startVertex.y; y < startVertex.y + patchLineVertCount + 1; y++)
                {
                    int candidatePos = x + y * (settings.Resolution + 1);
                    var isValid = vertexReferences.ContainsKey(candidatePos) && x <= settings.Resolution &&
                                  y <= settings.Resolution;
                    if (isValid) patchVertices.Add(new int2(x, y));
                }
            }

            var triangulation = SimpleBowyerWatson.Delaunay(ref patchVertices, (int)size);

            // add triangles to global triangle list, while getting the correct global indices. 
            for (var i = 0; i < triangulation.Length; i++)
            {
                var triangle = triangulation[i];
                var indices = triangle.Indices;

                var posA = patchVertices[indices.x].x + patchVertices[indices.x].y * (settings.Resolution + 1);
                var posB = patchVertices[indices.y].x + patchVertices[indices.y].y * (settings.Resolution + 1);
                var posC = patchVertices[indices.z].x + patchVertices[indices.z].y * (settings.Resolution + 1);


                var indexA = vertexReferences[posA];
                var indexB = vertexReferences[posB];
                var indexC = vertexReferences[posC];

                var globalIndices = new ushort3(indexA, indexB, indexC);

                triangles.AddNoResize(globalIndices);
            }
        }

        private int2 ReverseLinearIndex(int index, int count)
        {
            var y = index % count;
            var x = index / count;
            return new int2(x, y);
        }

        public static JobHandle ScheduleParallel(NativeParallelHashMap<int, ushort> vertexReferences,
            NativeList<ushort3> triangles,
            MeshSettings settings, int patchCount, JobHandle dependency
        ) => new TriangulationJob
        {
            settings = settings,
            vertexReferences = vertexReferences,
            patchCountPerLine = patchCount,
            triangles = triangles.AsParallelWriter(),
        }.ScheduleParallel(patchCount * patchCount, 1, dependency);
    }
}