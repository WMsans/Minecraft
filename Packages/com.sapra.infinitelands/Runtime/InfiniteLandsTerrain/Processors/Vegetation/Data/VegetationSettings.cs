using System;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class VegetationSettings 
    {
        private static uint maxInstances => 512*65500;
        public readonly int ChunkInstances;
        public readonly int ChunkInstancesRow;
        public readonly float ChunkSize;
        public readonly int ItemIndex;
        public readonly Vector2 GridOffset;
        public readonly float Ratio;

        public readonly float DistanceBetweenItems;
        
        public float ViewDistance;
        public int ChunksVisible;
        public int TotalChunksInBuffer;

        private float AssetViewDistance;
        public readonly bool GlobalRendering;
        public readonly bool Culling;
        public bool Render{get; private set;}

        public readonly int TextureIndex;
        public readonly int SubTextureIndex;
        public int Layers;

        public VegetationSettings(float meshScale, int itemIndex, float densityPerSize, Vector2 localGridOffset, float distanceBetweenItems, float viewDistance, 
                bool globalRendering, bool culling, int textureIndex, int subTextureIndex, int layer){
            var MaxAvailableSize = densityPerSize * distanceBetweenItems;
            var targetMaxSize = Mathf.Min(viewDistance*2, Mathf.Min(MaxAvailableSize, meshScale));
            var times = meshScale/targetMaxSize;
            
            ChunkSize = meshScale/Mathf.Ceil(times);
            Ratio = Mathf.Ceil(times);

            ChunkInstancesRow = Mathf.CeilToInt(ChunkSize/distanceBetweenItems);
            ChunkInstances = ChunkInstancesRow*ChunkInstancesRow;
            
            ItemIndex = itemIndex;
            DistanceBetweenItems = ChunkSize/ChunkInstancesRow;
            GlobalRendering = globalRendering;
            Culling = culling;
            Layers = layer;
      
            GridOffset = MapTools.GetOffsetInGrid(localGridOffset, ChunkSize);
            if((meshScale/ChunkSize) % 2 == 0){
                GridOffset += Vector2.one*ChunkSize/2.0f;
            }

            AssetViewDistance = viewDistance;
            UpdateRenderingDistance(viewDistance);
            
            var ChunksRow = ChunksVisible+ChunksVisible+1;
            long totalMaximumAmountOfInstances = SystemInfo.maxGraphicsBufferSize/InstanceData.size;

            int chunksThatFit = (int)Math.Floor((double)totalMaximumAmountOfInstances/ChunkInstances); //What actually fits memory wise
            int chunksThatWillRender = ChunksRow*ChunksRow; //What we would need for one single draw call
            int chunksPerProcessableInstances = (int)Math.Floor((double)maxInstances/ChunkInstances); //calculate this, so that its limiting the max amount of threads

            TotalChunksInBuffer = Mathf.Min(Mathf.Min(chunksThatFit, chunksThatWillRender), chunksPerProcessableInstances);

            TextureIndex = textureIndex;
            SubTextureIndex = subTextureIndex;
        }
        public void Reset(){
            UpdateRenderingDistance(AssetViewDistance);
        }
        public void UpdateRenderingDistance(float viewDistance)
        {
            Render = viewDistance > 0;
            ViewDistance = Mathf.Min(AssetViewDistance, viewDistance);
            ChunksVisible = Mathf.CeilToInt(ViewDistance / ChunkSize);
        }
    }
}