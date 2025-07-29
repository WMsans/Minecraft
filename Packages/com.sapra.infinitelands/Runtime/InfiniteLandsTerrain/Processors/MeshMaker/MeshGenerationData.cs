using System.Collections.Generic;
using Unity.Jobs;

namespace sapra.InfiniteLands.MeshProcess
{
    public readonly struct MeshGenerationData
    {
        public readonly JobHandle handle;
        public readonly UnityEngine.Mesh.MeshDataArray meshDataArray;
        public readonly List<MeshProcess> generatedChunks;

        public MeshGenerationData(List<MeshProcess> generatedChunks, UnityEngine.Mesh.MeshDataArray meshDataArray, JobHandle handle){
            this.handle = handle;
            this.generatedChunks = generatedChunks;
            this.meshDataArray = meshDataArray;
        }
    }
}