using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands{
    public readonly struct VegetationProcess : IProcessChunk
    {
        public readonly JobHandle job;

        public readonly ExportedMultiResult VegetationSplatMap;
        public readonly ExportedMultiResult HeightMap;
        public readonly TerrainConfiguration TerrainConfiguration;
        public readonly MeshSettings MeshSettings;
        public readonly AssetDataCompact assetData;
        public VegetationProcess(AssetDataCompact assetData, ExportedMultiResult VegetationSplatMap, ExportedMultiResult HeightMap,
            TerrainConfiguration TerrainConfiguration, MeshSettings MeshSettings)
        {
            this.assetData = assetData;
            this.TerrainConfiguration = TerrainConfiguration;
            this.MeshSettings = MeshSettings;
            this.VegetationSplatMap = VegetationSplatMap;
            this.HeightMap = HeightMap;
            job = JobHandle.CombineDependencies(VegetationSplatMap.job, HeightMap.job);
        }
        
        public Vector3Int GetID() => TerrainConfiguration.ID;
        public bool IsCompleted() => job.IsCompleted;
    }
}