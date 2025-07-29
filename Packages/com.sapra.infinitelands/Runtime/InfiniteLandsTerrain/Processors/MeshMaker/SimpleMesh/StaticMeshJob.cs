using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using static Unity.Mathematics.math;
using System.Runtime.InteropServices;
using Unity.Mathematics;
using Unity.Collections.LowLevel.Unsafe;

namespace sapra.InfiniteLands.MeshProcess
{
    [StructLayout(LayoutKind.Sequential)]
    public struct TriangleUInt16
    {
        public ushort a, b, c;

        public static implicit operator TriangleUInt16(int3 t) => new TriangleUInt16
        {
            a = (ushort)t.x,
            b = (ushort)t.y,
            c = (ushort)t.z
        };
    }
    //Actual responsability to generate a mesh
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    internal struct StaticMeshJob : IJobFor
    {
        [NativeDisableContainerSafetyRestriction]
        NativeArray<Vertex> vertex;

        [NativeDisableContainerSafetyRestriction]
        NativeArray<TriangleUInt16> triangles;

        [ReadOnly] NativeArray<Vertex> OriginalPositions;

        public int Resolution { get; set; }
        public float MeshScale { get; set; }
        public int JobLength => Resolution + 1;

        public void SetTriangle(int index, int3 triangle) =>
            triangles[index] = triangle;

        public void Execute(int i)
        {
            int vi = (Resolution + 1) * i, ti = 2 * Resolution * (i - 1);
            
            this.vertex[vi] = GetVertex(vi);
            vi += 1;
            for (int x = 1; x <= Resolution; x++, vi++)
            {      
                var foundVertex =  GetVertex(vi);         
                this.vertex[vi] = foundVertex;

                if (i > 0)
                {
                    SetTriangle(
                        ti + 0, vi + int3(-Resolution - 2, -1, -Resolution - 1)
                    );
                    SetTriangle(
                        ti + 1, vi + int3(-Resolution - 1, -1, 0)
                    );

                    ti += 2;
                }
            }
        }

        Vertex GetVertex(int index){
            Vertex vrt = OriginalPositions[index];
            if(float.IsNaN(vrt.position.y))
                vrt.position.y = 0;
            return vrt;
        }


        public static JobHandle ScheduleParallel(Mesh.MeshData meshData, MeshSettings meshSettings, NativeArray<Vertex> finalPositions, Bounds objectSpaceBounds)
        {
            int res = Mathf.Max(meshSettings.Resolution, 1);
            int IndexCount = 6 * res * res;
            int vertexCount = (res+1)*(res+1);
            MeshBuilder.Configure(meshData,vertexCount, IndexCount, objectSpaceBounds);

            var vertex = meshData.GetVertexData<Vertex>();
            var triangles = meshData.GetIndexData<ushort>().Reinterpret<TriangleUInt16>(2);
            
            var job = new StaticMeshJob
            {
                Resolution = res,
                MeshScale = meshSettings.MeshScale,
                OriginalPositions = finalPositions,
                vertex = vertex,
                triangles = triangles,
            };

            return job.ScheduleParallel(job.JobLength, res, default);
        }
    }
}