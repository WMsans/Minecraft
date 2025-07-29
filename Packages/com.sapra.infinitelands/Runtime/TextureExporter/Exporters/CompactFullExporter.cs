using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;

using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class CompactFullExporter : IExportTextures
    {
        private IBurstTexturePool texturePool;
        private IBurstTexturePool heightPool;

        public void SetExporterResolution(int resolution)
        {
            texturePool = new BurstTexturePool(resolution);
            heightPool = new BurstTexturePool(resolution);
        }
        private string[] vectorizedNames;
        private bool namesInitialized = false;
        public bool HasItems => vectorizedNames.Length > 0;
        private string heightName = null;

        public CompactFullExporter(){}
        public CompactFullExporter(int resolution, int meshRes)
        {
            texturePool = new BurstTexturePool(resolution);
            heightPool = new BurstTexturePool(meshRes);
        }

        public void InitializeNames(IEnumerable<IAsset> assets)
        {
            string[] ogNames = assets.Select(a => a.name).ToArray();
            vectorizedNames = VectorizeNames(ogNames);
            namesInitialized = true;
        }
        public string description => "Export the textures compacted, keeping the height map at full range";
        public ExportedMultiResult GenerateHeightTexture(NativeArray<Vertex> vertices, Vector2 globalMinMax)
        {
            if(heightName == null){
                heightName = string.Format("Normal and Height Map (Min{0}Max{1})", globalMinMax.x, globalMinMax.y);
            }
            List<BurstTexture> burstTexture = heightPool.GetTexture(heightName, FilterMode.Bilinear, TextureFormat.RGBAFloat);
            NativeArray<Vertex4> reinterpreted = vertices.Reinterpret<Vertex4>(Vertex.size);
            JobHandle finalTextureJob;

            finalTextureJob = MTJHeightJob.ScheduleParallel(reinterpreted,
                burstTexture[0].GetRawData<Color>(), heightPool.GetTextureResolution(), default);
            
            return new ExportedMultiResult(burstTexture, heightPool, finalTextureJob);
        }
        
        private string[] VectorizeNames(string[] originals)
        {
            int nameCount = (originals.Length + 3) / 4; // Equivalent to Mathf.CeilToInt(originals.Length / 4f)
            string[] newNames = new string[nameCount];

            for (int i = 0; i < nameCount; i++)
            {
                int startIndex = i * 4;
                string a = startIndex < originals.Length ? originals[startIndex] : "";
                string b = startIndex + 1 < originals.Length ? originals[startIndex + 1] : "";
                string c = startIndex + 2 < originals.Length ? originals[startIndex + 2] : "";
                string d = startIndex + 3 < originals.Length ? originals[startIndex + 3] : "";

                newNames[i] = $"{a} - {b} - {c} - {d}";
            }

            return newNames;
        }

        public ExportedMultiResult GenerateDensityTextures(AssetDataCompact assetResult)
        {
            if (!namesInitialized)
            {
                InitializeNames(assetResult.ProcessingAssets);
                Debug.Log("Manual initialization");
            }

            List<BurstTexture> masks = texturePool.GetTexture(vectorizedNames, FilterMode.Bilinear);
            var map = assetResult.Map;
            var mapLenght = assetResult.MapLenght;
            var jobHandle = assetResult.jobHandle;
            var assetCount = assetResult.AssetCount;
            //Generate density textures
            NativeArray<JobHandle> TextureCreationJob = new NativeArray<JobHandle>(vectorizedNames.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < vectorizedNames.Length; i++)
            {
                int offset = i*4;
                NativeArray<Color32> rawTexture = masks[i].GetRawData<Color32>();
                TextureCreationJob[i] = MTJVegetationJobFlat.ScheduleParallel(map, rawTexture,
                    AssetDataHelper.MaxOrInvalid(offset, assetCount),
                    AssetDataHelper.MaxOrInvalid(offset+1, assetCount),
                    AssetDataHelper.MaxOrInvalid(offset+2, assetCount),
                    AssetDataHelper.MaxOrInvalid(offset+3, assetCount),
                    mapLenght, texturePool.GetTextureResolution(), jobHandle);
            }
            JobHandle textureCreated = JobHandle.CombineDependencies(TextureCreationJob);
            TextureCreationJob.Dispose();
            return new ExportedMultiResult(masks,texturePool, textureCreated);
        }

        public ExportedMultiResult GenerateSingleTexture(HeightData heightData, BranchData settings)
        {
            List<BurstTexture> mask = texturePool.GetTexture("preview", FilterMode.Bilinear);
            var map = settings.GetData<HeightMapBranch>().GetMap();
            var jobHandle = heightData.jobHandle;
            //Generate density textures
            var normalizationRange = new NativeArray<float>(new[] { heightData.minMaxValue.x, heightData.minMaxValue.y }, Allocator.TempJob);

            NativeArray<Color32> rawTexture = mask[0].GetRawData<Color32>();
            JobHandle job = MTJGeneral.ScheduleParallel(rawTexture, normalizationRange, map, heightData.indexData, texturePool.GetTextureResolution(),
                jobHandle, true);
            normalizationRange.Dispose(job);
            return new ExportedMultiResult(mask,texturePool, job);
        }
        public void DestroyTextures(Action<UnityEngine.Object> Destroy) => texturePool.DestroyBurstTextures(Destroy);
        public int GetTextureResolution() => texturePool.GetTextureResolution();
    }
}