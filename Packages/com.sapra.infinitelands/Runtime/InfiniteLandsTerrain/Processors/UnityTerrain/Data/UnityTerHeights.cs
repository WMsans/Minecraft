using UnityEngine;

namespace sapra.InfiniteLands
{
    public struct UnityTerHeights
    {
        public Vector3Int ID;
        public Vector3 Size;
        public int HeightmapResolution;
        public float[,] Heights;

        public MeshSettings meshSettings;
        public Vector2 globalMinMax;
        public UnityTerHeights(ChunkData chunkData, Vector3 Size, int HeightmapResolution, float[,] Heights)
        {
            this.Size = Size;
            this.ID = chunkData.ID;
            this.meshSettings = chunkData.meshSettings;
            this.globalMinMax = chunkData.GlobalMinMax;
            this.HeightmapResolution = HeightmapResolution;
            this.Heights = Heights;
        }
    }
}