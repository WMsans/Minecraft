using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands{
    public readonly struct VegetationResult{        
        public readonly TerrainConfiguration TerrainConfiguration;
        public readonly MeshSettings MeshSettings;
        public readonly ExportedMultiResult VegetationSplatMap;
        public readonly ExportedMultiResult HeightMap;
        public readonly Texture2D NormalAndHeight;
        public VegetationResult(TerrainConfiguration terrainConfig, MeshSettings meshSettings, ExportedMultiResult splatMap, ExportedMultiResult heightMap){
            VegetationSplatMap = splatMap;
            HeightMap = heightMap;
            TerrainConfiguration = terrainConfig;
            MeshSettings = meshSettings;
            NormalAndHeight = HeightMap.textures[0].ApplyTexture();
        }

        public Texture2D GetTextureOf(int index){ 
            return VegetationSplatMap.textures[index].ApplyTexture();

        }
    }
}