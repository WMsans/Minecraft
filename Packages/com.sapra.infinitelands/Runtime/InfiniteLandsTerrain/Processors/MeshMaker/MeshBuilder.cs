using UnityEngine;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace sapra.InfiniteLands.MeshProcess
{
    public static class MeshBuilder
    {
        public static MeshGenerationData ScheduleParallel(List<MeshProcess> data)
        {
            int count = data.Count;
            var allocatedData = UnityEngine.Mesh.AllocateWritableMeshData(count);
            NativeArray<JobHandle> handles = new NativeArray<JobHandle>(count, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < count; i++)
            {
                MeshProcess process = data[i];
                JobHandle jobHandle;
                switch (process.meshType)
                {
                    case MeshMaker.MeshType.Decimated:
                        jobHandle = DecimatedMesh(process, allocatedData[i]);
                        break;
                    default:
                        jobHandle = StaticMesh(process, allocatedData[i]);
                        break;
                }

                handles[i] = jobHandle;
            }

            var result = new MeshGenerationData(data, allocatedData, JobHandle.CombineDependencies(handles));
            handles.Dispose();
            return result;
        }

        private static JobHandle DecimatedMesh(MeshProcess meshProcess, UnityEngine.Mesh.MeshData meshData)
        {
            MeshSettings settings = meshProcess.meshSettings;
            int vertexCount = (settings.Resolution+1) * (settings.Resolution+1);
            int triangleCountHalf = settings.Resolution * settings.Resolution;
            int coreGridSpacing = Mathf.CeilToInt(settings.Resolution / (float)Mathf.CeilToInt(settings.Resolution / (float)meshProcess.CoreGridSpacing));
            int patchCountPerLine = Mathf.CeilToInt(settings.Resolution / (float)coreGridSpacing);

            NativeList<Vertex> validPoints = new NativeList<Vertex>(vertexCount, Allocator.Persistent);
            NativeParallelHashMap<int, ushort> validHashMap = new NativeParallelHashMap<int, ushort>(vertexCount, Allocator.Persistent);
            NativeList<ushort3> triangles = new NativeList<ushort3>(triangleCountHalf * 2, Allocator.Persistent);

            JobHandle applyToTheMesh = FindValidDecimatedPoints.ScheduleParallel(validPoints, validHashMap, vertexCount, settings,  coreGridSpacing, meshProcess.NormalReduceThreshold, meshProcess.chunkData.DisplacedVertexPositions);
            JobHandle triangulationHandle = TriangulationJob.ScheduleParallel(validHashMap, triangles, settings, patchCountPerLine, applyToTheMesh);
            JobHandle meshingHandle = DecimatedMeshJob.ScheduleParallel(meshData, meshProcess.ObjectBounds, validPoints, triangles, triangulationHandle);

            triangles.Dispose(meshingHandle);
            validPoints.Dispose(meshingHandle);
            validHashMap.Dispose(meshingHandle);
            return meshingHandle;
        }

        private static JobHandle StaticMesh(MeshProcess meshProcess, Mesh.MeshData meshData)
        {
            return StaticMeshJob.ScheduleParallel(meshData, meshProcess.meshSettings, meshProcess.chunkData.DisplacedVertexPositions, meshProcess.ObjectBounds);
        }

        public static Mesh CreateMesh(){
            var current = new UnityEngine.Mesh();
            ReuseMesh(current);
            return current;
        }

        public static void ReuseMesh(Mesh current){
            current.name = "Infinite Lands Mesh";
        }

        public static void Consolidate(MeshGenerationData data, List<Mesh> selection)
        {
            for(int i = 0; i < data.meshDataArray.Length; i++){
                selection[i].bounds = data.generatedChunks[i].ObjectBounds;
            }
            Mesh.ApplyAndDisposeWritableMeshData(data.meshDataArray, selection, NoCalculations());
        }

        public static MeshUpdateFlags NoCalculations()
        {
            return MeshUpdateFlags.DontRecalculateBounds |
                   MeshUpdateFlags.DontValidateIndices |
                   MeshUpdateFlags.DontNotifyMeshUsers |
                   MeshUpdateFlags.DontResetBoneBounds;
        }

        public static void Configure(Mesh.MeshData _meshData, int verticesLength, int indicesLength, Bounds _bounds){
            var descriptor = new NativeArray<VertexAttributeDescriptor>(
                3, Allocator.Temp, NativeArrayOptions.UninitializedMemory
            );
            descriptor[0] = new VertexAttributeDescriptor(dimension: 3);
            descriptor[1] = new VertexAttributeDescriptor(VertexAttribute.Normal,
                dimension: 3);

            descriptor[2] = new VertexAttributeDescriptor(
                VertexAttribute.TexCoord0, dimension: 2
            );
            _meshData.SetVertexBufferParams(verticesLength, descriptor);
            descriptor.Dispose();

            _meshData.SetIndexBufferParams(indicesLength, IndexFormat.UInt16);

            _meshData.subMeshCount = 1;
            _meshData.SetSubMesh(0, new SubMeshDescriptor(0, indicesLength)
            {
                bounds = _bounds,
                vertexCount = verticesLength,
            }, MeshBuilder.NoCalculations());
        }
    }
}