using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public struct AssetDataCompact
    {
        public NativeArray<float> Map;
        public int AssetCount;
        public int MapLenght;
        public IEnumerable<IAsset> ProcessingAssets;
        public JobHandle jobHandle;

        public ChunkData chunkData;
        public ReturnablePack returnablePack;

        public AssetDataCompact(NativeArray<float> Map, int AssetCount, int MapLenght, IEnumerable<IAsset> ProcessingAssets,
            ChunkData chunkData, ReturnablePack returnablePack, JobHandle jobHandle)
        {
            chunkData.AddProcessor(returnablePack);
            this.Map = Map;
            this.AssetCount = AssetCount;
            this.MapLenght = MapLenght;
            this.ProcessingAssets = ProcessingAssets;
            this.chunkData = chunkData;
            this.returnablePack = returnablePack;
            this.jobHandle = jobHandle;
        }

        public void CleanUp()
        {
            if (!jobHandle.IsCompleted)
                Debug.LogError("Asset wasn't completed before cleaning it up");

            chunkData.RemoveProcessor(returnablePack);
            returnablePack.Release();
        }
    }
}