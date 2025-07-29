using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
namespace sapra.InfiniteLands
{
    public class ExporterGenerator 
    {
        IGraph generator;
        public ExporterGenerator(IGraph generator){
            this.generator = generator;
        }
        public List<Texture2D> GenerateAndExportWorld(int EditorResolution, float MeshScale, int Seed, Vector2 WorldOffset, IExportTextures exporter)
        {
            TerrainConfiguration config = new TerrainConfiguration
            {
                Position = WorldOffset,
                TerrainNormal = Vector3.up,
            };
            
            MeshSettings meshSettings = new MeshSettings
            {
                Resolution = EditorResolution,
                MeshScale = MeshScale,
                Seed = Seed,
            };

            //DeepRestart();
            generator.ValidationCheck();
            
            WorldGenerator worldGenerator = new WorldGenerator(generator, false);
            WorldGenerationData worldGeneratorData = GenericPoolLight<WorldGenerationData>.Get();
            worldGeneratorData.Reuse(worldGenerator, config, meshSettings);
            worldGeneratorData.ForceComplete();

            var chunkData = worldGeneratorData.Result;

            List<Texture2D> texturesToExport = new List<Texture2D>();

            var assetHolders = AssetDataHelper.GetAssetsWithHolder<IAsset>(generator);
            AssetDataCompact assetData = AssetDataHelper.GetCompactAssetData(chunkData, assetHolders);

            var heightResult = exporter.GenerateHeightTexture(chunkData.DisplacedVertexPositions, chunkData.GlobalMinMax);
            heightResult.job.Complete();
            texturesToExport.AddRange(heightResult.textures.Select(a => a.ApplyTexture()));
            
            var result = exporter.GenerateDensityTextures(assetData);
            result.job.Complete();
            texturesToExport.AddRange(result.textures.Select(a => a.ApplyTexture()));
            GenericPoolLight.Release(worldGeneratorData);
            
            chunkData.CompletedInvocations();
            worldGenerator.Dispose(default);
            assetData.CleanUp();
            return texturesToExport;
        }
    }
}