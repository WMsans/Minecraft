using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace sapra.InfiniteLands
{
    public readonly struct TextureResult{
        private static readonly int 
            splatMapsID = Shader.PropertyToID("_splatMap"),
            offsetID = Shader.PropertyToID("_MeshOffset"),
            resolutionID = Shader.PropertyToID("_Resolution"),
            textureMaskID = Shader.PropertyToID("_TextureMask"),
            mainTextureID = Shader.PropertyToID("_MainTex"),
            meshScaleID = Shader.PropertyToID("_MeshScale");
        
        public readonly ExportedMultiResult HeightMap;
        public readonly ExportedMultiResult SplatMaps;

        public readonly MeshSettings MeshSettings;
        public readonly TerrainConfiguration TerrainConfiguration;

        public readonly Material groundMaterial;
        public readonly Texture2DArray TextureMasksArray;
        public TextureResult(MeshSettings settings, TerrainConfiguration terrainConfig, 
            ExportedMultiResult splatMaps, ExportedMultiResult heightMap, 
            Material material, Texture2DArray _textureMasksArray)
        {
            MeshSettings = settings;
            TerrainConfiguration = terrainConfig;
            HeightMap = heightMap;
            SplatMaps = splatMaps;
            groundMaterial = material;
            TextureMasksArray = _textureMasksArray;
            ReloadMaterial();
        }        

        public void ReloadMaterial(){
            groundMaterial.SetInt(resolutionID, MeshSettings.TextureResolution);
            groundMaterial.SetFloat(meshScaleID, MeshSettings.MeshScale);

            if (TextureMasksArray != null && TextureMasksArray.depth > 0)
            {
                TextureMasksArray.wrapMode = TextureWrapMode.Clamp;
                groundMaterial.SetTexture(splatMapsID, TextureMasksArray);
                groundMaterial.EnableKeyword("_PROCEDURALTEXTURING");
            }
            else
            {
                Texture2D texture2D = HeightMap.textures?[0].ApplyTexture();
                if (texture2D != null)
                {
                    groundMaterial.SetTexture(mainTextureID, texture2D);
                }
                groundMaterial.SetVector(textureMaskID, new Vector4(1, 1, 1, 0));
                groundMaterial.DisableKeyword("_PROCEDURALTEXTURING");
            }
        }

        public void DynamicMeshResultApply(CommandBuffer bf, ComputeShader compute, int kernelIndex){
            if (TextureMasksArray != null)
            {
                bf.SetComputeTextureParam(compute, kernelIndex, splatMapsID, TextureMasksArray);
            }

            bf.SetComputeFloatParam(compute, meshScaleID, MeshSettings.MeshScale);
            bf.SetComputeIntParam(compute, resolutionID,  MeshSettings.TextureResolution);
            bf.SetComputeVectorParam(compute, offsetID, TerrainConfiguration.Position);
        }
    }
}