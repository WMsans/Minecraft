using System;
using System.Collections.Generic;
using System.Linq;

using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class CompactNormalizedExporter : IExportTextures
    {
        public string description => "Export the textures compacted,  normalizing the height map";
        public IBurstTexturePool texturePool;
        public void SetExporterResolution(int resolution)
        {
            texturePool = new BurstTexturePool(resolution);
        }

        private string[] VectorizeNames(string[] originals)
        {
            int nameCount = Mathf.CeilToInt(originals.Length / 4f);
            string[] newNames = new string[nameCount];
            for (int i = 0; i < newNames.Length; i++)
            {
                string a = i * 4 < originals.Length ? originals[i * 4] : "";
                string b = i * 4 + 1 < originals.Length ? originals[i * 4 + 1] : "";
                string c = i * 4 + 2 < originals.Length ? originals[i * 4 + 2] : "";
                string d = i * 4 + 3 < originals.Length ? originals[i * 4 + 3] : "";

                newNames[i] = a + " - " + b + " - " + c + " - " + d;
            }

            return newNames;
        }
        public ExportedMultiResult GenerateHeightTexture(NativeArray<Vertex> vertices, Vector2 globalMinMax)
        {
            List<BurstTexture> burstTexture = texturePool.GetTexture(string.Format("Normal and Height Map (Min{0}Max{1})", globalMinMax.x, globalMinMax.y),  FilterMode.Bilinear, TextureFormat.RGBAFloat);
            NativeArray<Vertex4> reinterpreted = vertices.Reinterpret<Vertex4>(Vertex.size);
            JobHandle finalTextureJob;

            finalTextureJob = MTJHeightNormalizedJob.ScheduleParallel(reinterpreted,
                burstTexture[0].GetRawData<Color>(), globalMinMax, texturePool.GetTextureResolution(), default);
            
            return new ExportedMultiResult(burstTexture, texturePool, finalTextureJob);

        }

        public ExportedMultiResult GenerateDensityTextures(AssetDataCompact assetResult)
        {
            string[] ogNames = assetResult.ProcessingAssets.Select(a => a.name).ToArray();
            string[] names = VectorizeNames(ogNames);
            List<BurstTexture> masks = texturePool.GetTexture(names, FilterMode.Bilinear);
            //Generate density textures
            NativeArray<JobHandle> TextureCreationJob = new NativeArray<JobHandle>(names.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            int count = assetResult.AssetCount;
            var map = assetResult.Map;
            var mapLenght = assetResult.MapLenght;
            var jobHandle = assetResult.jobHandle;
            for (int i = 0; i < names.Length; i++)
            {
                int offset = i * 4;
                NativeArray<Color32> rawTexture = masks[i].GetRawData<Color32>();
                TextureCreationJob[i] = MTJVegetationJobFlat.ScheduleParallel(map, rawTexture,
                    AssetDataHelper.MaxOrInvalid(offset, count),
                    AssetDataHelper.MaxOrInvalid(offset+1, count),
                    AssetDataHelper.MaxOrInvalid(offset+2, count),
                    AssetDataHelper.MaxOrInvalid(offset+3, count),
                    mapLenght, texturePool.GetTextureResolution(), jobHandle);
            }

            JobHandle textureCreated = JobHandle.CombineDependencies(TextureCreationJob);
            TextureCreationJob.Dispose();
            return new ExportedMultiResult(masks, texturePool, textureCreated);
        }

        public void DestroyTextures(Action<UnityEngine.Object> Destroy) => texturePool.DestroyBurstTextures(Destroy);
        public int GetTextureResolution() => texturePool.GetTextureResolution();
    }
}