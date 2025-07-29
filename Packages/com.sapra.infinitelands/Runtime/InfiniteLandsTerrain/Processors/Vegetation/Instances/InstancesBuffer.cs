using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace sapra.InfiniteLands{
    public class InstancesBuffer{
        private static readonly int 
            chunkInstancesRowID = Shader.PropertyToID("_ChunkInstancesRow"),
            perInstanceDataID = Shader.PropertyToID("_PerInstanceData");

        public ComputeBuffer PerInstanceData{get; private set;}
        public VegetationChunk[] ChunksForPosition{get; private set;}
        private List<int> FreeIndices;
        private int TotalChunksInBuffer;
        private float UnusedTime;
        int TotalInstances;

        public InstancesBuffer(IHoldVegetation asset, VegetationSettings vegetationSettings, 
            Action<Vector2Int, VegetationChunk> onChunkCreated, Action<Vector2Int, List<InstanceData>> onInstancesCreated,
            IControlTerrain infiniteLandsController, ComputeBuffer _textureMasksBuffer){
            
            ChunksForPosition = new VegetationChunk[vegetationSettings.TotalChunksInBuffer];
            FreeIndices = new();
            TotalChunksInBuffer = vegetationSettings.TotalChunksInBuffer;
            for(int i = 0; i < vegetationSettings.TotalChunksInBuffer; i++){
                var chunk = new VegetationChunk(asset, vegetationSettings, infiniteLandsController, _textureMasksBuffer);
                ChunksForPosition[i] = chunk;
                chunk.OnChunkCreated += onChunkCreated;
                chunk.OnInstancesCreated += onInstancesCreated;
                FreeIndices.Add(i);
            }

            TotalInstances = vegetationSettings.ChunkInstances*vegetationSettings.TotalChunksInBuffer; 
            PerInstanceData = new ComputeBuffer(TotalInstances, InstanceData.size, ComputeBufferType.Structured);
        }

        public void OriginShift(CommandBuffer bf,ComputeShader compute, int kernel){
            compute.GetKernelThreadGroupSizes(kernel, out uint x, out _, out _);
            bf.SetComputeBufferParam(compute, kernel, perInstanceDataID, PerInstanceData);
            bf.SetComputeIntParam(compute, chunkInstancesRowID, TotalInstances);
            int bladesInChunk = Mathf.CeilToInt(TotalInstances / (float)x);
            bf.DispatchCompute(compute, kernel, bladesInChunk, 1, 1);
        }

        public bool UpdateTimer(){
            if(FreeIndices.Count == TotalChunksInBuffer)
                UnusedTime += Time.deltaTime;
            else
                UnusedTime = 0;
            return UnusedTime > 10;
        }
        public int SetChunk(VegetationChunk chunk){
            if(FreeIndices.Count <= 0)
                return -1;
            
            int index = FreeIndices[0];
            ChunksForPosition[index] = chunk;
            FreeIndices.RemoveAt(0);
            return index;
        }

        
        public VegetationChunk GetEmptySlot(out int index){
            int target = FreeIndices.Count-1;
            if(target < 0){
                index = -1;
                return null;
            }
            
            index = FreeIndices[target];
            FreeIndices.RemoveAt(target);
            return ChunksForPosition[index];
        }

        public void Dispose(){
            if(PerInstanceData != null){
                PerInstanceData.Release();
                PerInstanceData = null;
            }     

            for(int i = 0; i < ChunksForPosition.Length; i++){
                var chunk = ChunksForPosition[i];
                if(chunk != null)
                    chunk.Dispose();
                ChunksForPosition[i] = null;
            }
        }


        public void DrawBounds(){
            foreach(VegetationChunk chunk in ChunksForPosition){
                if(chunk != null)
                    chunk.DrawGizmos();
            }
        }

        public void DisableChunk(int chunkIndex)
        {
            FreeIndices.Add(chunkIndex);
            if(ChunksForPosition[chunkIndex] != null)
                ChunksForPosition[chunkIndex].DisableChunk();
        }
    }
}