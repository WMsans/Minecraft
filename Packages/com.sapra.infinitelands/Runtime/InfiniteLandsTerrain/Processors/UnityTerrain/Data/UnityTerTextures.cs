using UnityEngine;

namespace sapra.InfiniteLands
{
    public struct UnityTerTextures
    {
        public int alphamapResolution;
        public float[,,] details;
        public TerrainLayer[] layers;
        public Material GroundMaterial;
        public MeshSettings meshSettings;
        public Vector2 globalMinMax;
        public Vector3Int ID;

        public UnityTerTextures(Vector3Int ID, int alphamapResolution, float[,,] details, TerrainLayer[] layers, Material groundMaterial,
            MeshSettings meshSettings, Vector2 globalMinMax)
        {
            this.ID = ID;
            this.alphamapResolution = alphamapResolution;
            this.details = details;
            this.layers = layers;
            this.GroundMaterial = groundMaterial;
            this.meshSettings = meshSettings;
            this.globalMinMax = globalMinMax;
        }

    }
}