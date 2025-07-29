using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace sapra.InfiniteLands{
    public class VegetationChunkManager
    {
        public readonly IHoldVegetation Asset;
        private readonly VegetationSettings Settings;
        private readonly ComputeBuffer TextureMasksBuffer;
        IControlTerrain infiniteLandsController;

        
        private Dictionary<Vector2Int, BufferIndex> ChunksWithIndex = new();
        private Dictionary<Vector2Int, VegetationChunk> InProcess = new();
        private HashSet<Vector2Int> InstancesRequested = new();
        private List<InstancesBuffer> InstanceBufferHolder = new List<InstancesBuffer>();

        public Action<Vector2Int, List<InstanceData>> OnInstancesCreated;
        public Action<Vector2Int, VegetationChunk> OnCreateChunk;
        public Action<Vector2Int, VegetationChunk> OnDisableChunk;

        public VegetationChunkManager(IHoldVegetation asset, VegetationSettings vegetationSettings, IControlTerrain infiniteLandsController){
            Settings = vegetationSettings;
            Asset = asset;
            this.infiniteLandsController = infiniteLandsController;

            var colorData = asset.GetColorData();
            var terrainPainter = infiniteLandsController.GetInternalComponent<TerrainPainter>();
            if (terrainPainter != null)
            {
                uint[] masks = terrainPainter.ExtractTexturesMask(colorData.removeAtTextures);
                TextureMasksBuffer = new ComputeBuffer(masks.Length, sizeof(uint));
                TextureMasksBuffer.SetData(masks);
            }
        }

        public InstancesBuffer GetInstanceBuffer(int index){
            return InstanceBufferHolder[index];
        }
        public void Dispose(){
            for(int i = 0; i < InstanceBufferHolder.Count; i++){
                InstanceBufferHolder[i].Dispose();
            }
            
            TextureMasksBuffer?.Release();
        }

        public void UpdateReleaseOfCompactors(){
            for(int i = InstanceBufferHolder.Count-1; i >= 0; i--){
                var bufferHolder = InstanceBufferHolder[i];
                if(bufferHolder.UpdateTimer()){
                    bufferHolder.Dispose();
                    InstanceBufferHolder.RemoveAt(i);
                }
            }
        }

        public void OnOriginShift(CommandBuffer bf, ComputeShader compute, int kernel, Vector3 offset){
            foreach(InstancesBuffer compact in InstanceBufferHolder){
                compact.OriginShift(bf, compute, kernel);
            }
            foreach(BufferIndex buffer in ChunksWithIndex.Values){
                var chunk = buffer.instanceBuffer.ChunksForPosition[buffer.chunkIndex];
                chunk.OriginShift(offset);
            }
        }

        public VegetationChunk GetChunk(Vector2Int chunkID, out BufferIndex ind){
            if(ChunksWithIndex.TryGetValue(chunkID, out ind)){
                return ind.instanceBuffer.ChunksForPosition[ind.chunkIndex];
            }
            return null;
        }

        public bool IsChunkPrepared(Vector2Int chunkID){
            return InProcess.ContainsKey(chunkID);
        }

        public VegetationChunk EnableChunk(Vector2Int id){
            VegetationChunk chunk = GetEmptySlot(out BufferIndex bufferIndex);
            Vector2 chunkPosition = new Vector2(id.x, id.y) * Settings.ChunkSize + Settings.GridOffset;
            chunk.EnableChunk(bufferIndex, id, chunkPosition);
            InProcess.Add(id, chunk);
            return chunk;
        }

        public void DisableChunk(Vector2Int ID, bool forced){ 
            if(InProcess.TryGetValue(ID, out VegetationChunk foundChunk)){
                if(DisableChunk(foundChunk, forced)){
                    InProcess.Remove(ID);
                    return;
                }
            }

            if(ChunksWithIndex.TryGetValue(ID, out BufferIndex ind)){
                var compactor = ind.instanceBuffer;
                var chunk = compactor.ChunksForPosition[ind.chunkIndex];
                if(DisableChunk(chunk, forced)){
                    OnDisableChunk?.Invoke(chunk.ID, chunk);
                    ChunksWithIndex.Remove(chunk.ID);
                }
            }
        }

        private bool DisableChunk(VegetationChunk chunk, bool forced){
            BufferIndex ind = chunk.bufferData;
            var compactor = ind.instanceBuffer;

            if(forced || chunk.DecreaseUses()){
                compactor.DisableChunk(ind.chunkIndex);
                return true;
            }
            return false;
        }
        private VegetationChunk GetEmptySlot(out BufferIndex bufferIndex){
            VegetationChunk chunk;
            int subIndex;
            for(int i = 0; i < InstanceBufferHolder.Count; i++){
                chunk = InstanceBufferHolder[i].GetEmptySlot(out subIndex);
                if(chunk != null){
                    bufferIndex = new BufferIndex(){
                        instanceBuffer = InstanceBufferHolder[i],
                        instanceBufferIndex = i,
                        chunkIndex = subIndex
                    };
                    return chunk;
                }
            }

            //In case we couldn't find any
            InstancesBuffer instancesBufferData = new InstancesBuffer(Asset, Settings, OnDataRecieved, OnInstancesRecived, infiniteLandsController, TextureMasksBuffer);
            InstanceBufferHolder.Add(instancesBufferData);
            chunk = instancesBufferData.GetEmptySlot(out subIndex);
            bufferIndex = new BufferIndex(){
                instanceBuffer = instancesBufferData,
                instanceBufferIndex = InstanceBufferHolder.Count-1,
                chunkIndex = subIndex
            };
            return chunk;
        }

        void OnDataRecieved(Vector2Int id, VegetationChunk chunk){
            InProcess.Remove(id);
            ChunksWithIndex.Add(id, chunk.bufferData);
            OnCreateChunk?.Invoke(id, chunk); // The chunk was fully created, inform to subscribers about it
            if(InstancesRequested.Contains(id)){
                chunk.GetInstances();
                InstancesRequested.Remove(id);
            }
        }

        void OnInstancesRecived(Vector2Int id, List<InstanceData> instances){
            OnInstancesCreated?.Invoke(id, instances);
        }

        public void WaitForInstances(Vector2Int id){
            InstancesRequested.Add(id);
        }

        public void OnDrawGizmos(){
            foreach(InstancesBuffer compact in InstanceBufferHolder){
                compact.DrawBounds();
            }
        }
    }
}