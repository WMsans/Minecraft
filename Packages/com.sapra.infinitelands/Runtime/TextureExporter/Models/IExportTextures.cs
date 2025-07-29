using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands{
    public interface IExportTextures{
        public string description{get;}
        public int GetTextureResolution();
        public void SetExporterResolution(int resolution);
        public ExportedMultiResult GenerateHeightTexture(NativeArray<Vertex> vertices, Vector2 globalMinMax);
        public ExportedMultiResult GenerateDensityTextures(AssetDataCompact assetResult);
        
        public void DestroyTextures(Action<UnityEngine.Object> Destroy);
    }
}
