using System;
using System.Collections.Generic;
using System.Linq;

using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class ExpandedExporter : IExportTextures
    {
        public string description => "Export the textures individually as a greyscale";

        public IBurstTexturePool texturePool;
        public void SetExporterResolution(int resolution)
        {
            texturePool = new BurstTexturePool(resolution);
        }

        public ExportedMultiResult GenerateHeightTexture(NativeArray<Vertex> vertices, Vector2 globalMinMax)
        {
            string[] names = new string[]{
                "Normal Map",
                string.Format("Height Map (Min{0}Max{1})",globalMinMax.x, globalMinMax.y),
            };

            List<BurstTexture> maps = texturePool.GetTexture(names, FilterMode.Bilinear, TextureFormat.RGBAFloat);

            NativeArray<Vertex4> reinterpreted = vertices.Reinterpret<Vertex4>(Vertex.size);
            JobHandle finalTextureJob;

            finalTextureJob = MTJHeightSeparated.ScheduleParallel(reinterpreted,
                maps[0].GetRawData<Color>(), maps[1].GetRawData<Color>(), globalMinMax, texturePool.GetTextureResolution(), default);
            
            return new ExportedMultiResult(maps, texturePool, finalTextureJob);
        }

        public ExportedMultiResult GenerateDensityTextures(AssetDataCompact assetResult)
        {
            string[] names = assetResult.ProcessingAssets.Select(a => a.name).ToArray();

            //Generate density textures
            List<BurstTexture> masks = texturePool.GetTexture(names, FilterMode.Bilinear);
            NativeArray<JobHandle> TextureCreationJob = new NativeArray<JobHandle>(names.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < names.Length; i++)
            {
                NativeArray<Color32> rawTexture = masks[i].GetRawData<Color32>();
                TextureCreationJob[i] = MTJTextureSingleChannel.ScheduleParallel(assetResult.Map, rawTexture,
                    i, assetResult.MapLenght, texturePool.GetTextureResolution(), assetResult.jobHandle);
            }

            JobHandle textureCreated = JobHandle.CombineDependencies(TextureCreationJob);
            TextureCreationJob.Dispose();
            return new ExportedMultiResult(masks,texturePool, textureCreated);
        }

        public void DestroyTextures(Action<UnityEngine.Object> Destroy) => texturePool.DestroyBurstTextures(Destroy);
        public int GetTextureResolution() => texturePool.GetTextureResolution();
        public void ResetExporter(){}

    }
}