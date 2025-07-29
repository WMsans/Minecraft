using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands{
    public readonly struct TextureProcess : IProcessChunk{
        public readonly AssetDataCompact assetData;
        public readonly MeshSettings meshSettings;
        public readonly TerrainConfiguration terrainConfig;
        public readonly ExportedMultiResult SplatMaps;
        public readonly ExportedMultiResult HeightMap;
        public readonly JobHandle job;

        public TextureProcess(AssetDataCompact assetData,
            MeshSettings meshSettings, TerrainConfiguration terrainConfig, 
            ExportedMultiResult SplatMaps, ExportedMultiResult HeightMap, JobHandle job)
        {
            this.assetData = assetData;
            this.meshSettings = meshSettings;
            this.terrainConfig = terrainConfig;
            this.SplatMaps = SplatMaps;
            this.HeightMap = HeightMap;
            this.job = job;
        }

        public Vector3Int GetID() => terrainConfig.ID;
        public bool IsCompleted() => job.IsCompleted;
    }
}