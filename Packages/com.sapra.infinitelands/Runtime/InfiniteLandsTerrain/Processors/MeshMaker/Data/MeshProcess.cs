using UnityEngine;

namespace sapra.InfiniteLands.MeshProcess{
    public readonly struct MeshProcess{
        public readonly MeshSettings meshSettings;
        public readonly MeshMaker.MeshType meshType;
        public readonly int CoreGridSpacing;
        public readonly float NormalReduceThreshold;
        public readonly TerrainConfiguration terrainConfiguration;
        public readonly Bounds ObjectBounds;
        public readonly ChunkData chunkData;
        public MeshProcess(ChunkData chunk,
            MeshMaker.MeshType meshType, int CoreGridSpacing, float NormalReduceThreshold)
        {
            this.meshSettings = chunk.meshSettings;
            this.terrainConfiguration = chunk.terrainConfig;
            this.ObjectBounds = chunk.ObjectSpaceBounds;
            this.meshType = meshType;
            this.CoreGridSpacing = CoreGridSpacing;
            this.NormalReduceThreshold = NormalReduceThreshold;
            this.chunkData = chunk;

        }
    }
}