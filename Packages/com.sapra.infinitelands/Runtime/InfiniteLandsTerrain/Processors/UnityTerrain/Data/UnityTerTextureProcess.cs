using Unity.Jobs;
using UnityEngine;
namespace sapra.InfiniteLands
{
    public struct UnityTerTextureProcess : IProcessChunk
    {
        public AssetDataCompact assetResult;
        public MeshSettings meshSettings;
        public Vector3Int ID;
        public Vector2 GlobalMinMax;
        public JobHandle job => assetResult.jobHandle;
        public Vector3Int GetID() => ID;
        public bool IsCompleted() => job.IsCompleted;

        public UnityTerTextureProcess(MeshSettings meshSettings, Vector3Int ID, Vector2 GlobalMinMax, AssetDataCompact data)
        {
            this.assetResult = data;
            this.meshSettings = meshSettings;
            this.ID = ID;
            this.GlobalMinMax = GlobalMinMax;
        }
    }
}