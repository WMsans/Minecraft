using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public struct WorldData
    {
        public NativeArray<float> ChunkMinMax;
        public NativeArray<Vertex> FinalPositions;
        public Vector2 GlobalMinMax;
        public JobHandle jobHandle;

        public WorldData(NativeArray<float> chunkMinMax, NativeArray<Vertex> finalPositions, Vector2 minMaxHeight, JobHandle job){
            GlobalMinMax = minMaxHeight;
            ChunkMinMax = chunkMinMax;
            FinalPositions = finalPositions;
            jobHandle = job;
        }
    }
}