using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public struct UnityTerVegetationProcess : IProcessChunk
    {
        public MeshSettings meshSettings;
        public Vector3Int ID;
        public Vector2 GlobalMinMax;
        public AssetDataCompact assetData;
        public UnityTerVegetationProcess(MeshSettings meshSettings, Vector3Int ID, Vector2 globalMinMax, AssetDataCompact assetData)
        {
            this.meshSettings = meshSettings;
            this.ID = ID;
            this.GlobalMinMax = globalMinMax;
            this.assetData = assetData;
        }
        public Vector3Int GetID() => ID;
        public JobHandle job => assetData.jobHandle;
        public bool IsCompleted() => job.IsCompleted;
    }
}