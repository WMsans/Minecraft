using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands.MeshProcess
{
    //Actual responsability to generate a mesh
    [BurstCompile(FloatPrecision.Standard, FloatMode.Default, CompileSynchronously = true)]
    internal struct DecimatedMeshJob : IJob
    {
        [WriteOnly] private UnityEngine.Mesh.MeshData _meshData;
        private NativeArray<Vertex> _vertices;
        private NativeArray<ushort3> _triangles;
        private Bounds _bounds;
        public void Execute()
        {
            MeshBuilder.Configure(_meshData, _vertices.Length, _triangles.Length*3, _bounds);
            var meshVerts = _meshData.GetVertexData<Vertex>();
            meshVerts.CopyFrom(_vertices);

            var meshTris = _meshData.GetIndexData<ushort>();

            for (var i = 0; i < _triangles.Length; i++)
            {
                var triangle = _triangles[i];
                var startIndex = i * 3;
                meshTris[startIndex] = triangle.x;
                meshTris[startIndex + 1] = triangle.y;
                meshTris[startIndex + 2] = triangle.z;
            }
        }

        public static JobHandle ScheduleParallel(UnityEngine.Mesh.MeshData meshData, Bounds bounds,
            NativeList<Vertex> vertices, NativeList<ushort3> triangles, JobHandle dependency
        ){
            
            return new DecimatedMeshJob
            {
                _meshData = meshData,
                _triangles = triangles.AsDeferredJobArray(),
                _vertices = vertices.AsDeferredJobArray(),
                _bounds = bounds,
            }.Schedule(dependency);
        }
    }

    internal struct ushort3{
        public ushort x;
        public ushort y;
        public ushort z;
        public ushort3(ushort x, ushort y, ushort z){
            this.x = x;
            this.y = y;
            this.z = z;
        }
    }
}