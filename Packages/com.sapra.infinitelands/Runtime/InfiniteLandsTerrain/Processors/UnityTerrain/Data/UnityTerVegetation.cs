using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands
{
    public struct UnityTerVegetation
    {
        public TreePrototype[] prototypes;
        public DetailPrototype[] details;
        public TreeInstance[] instances;
        public int TextureResolution;
        public List<int[,]> DetailMaps;

        public MeshSettings meshSettings;
        public Vector2 globalMinMax;
        public Vector3Int ID;
        public UnityTerVegetation(TreePrototype[] prototypes,
            DetailPrototype[] details,
            TreeInstance[] instances,
            int TextureResolution,
            List<int[,]> DetailMaps,
            MeshSettings meshSettings, Vector2 globalMinMax, Vector3Int ID)
        {
            this.ID = ID;
            this.prototypes = prototypes;
            this.details = details;
            this.instances = instances;
            this.TextureResolution = TextureResolution;
            this.DetailMaps = DetailMaps;
            this.meshSettings = meshSettings;
            this.globalMinMax = globalMinMax;
        }                
    }
}