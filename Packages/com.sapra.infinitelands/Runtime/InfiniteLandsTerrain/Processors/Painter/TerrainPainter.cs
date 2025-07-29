using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Rendering;

namespace sapra.InfiniteLands{
    [ExecuteAlways]
    [RequireComponent(typeof(PointStore))]
    public class TerrainPainter : ChunkProcessor<ChunkData>, IGenerate<TextureResult>
    {
        public bool UseMaximumTextureResolution = true;
        [HideIf(nameof(UseMaximumTextureResolution))][Min(8)] public int Width = 256;
        [HideIf(nameof(UseMaximumTextureResolution))][Min(8)] public int Height = 256;
        [HideIf(nameof(UseMaximumTextureResolution))][Min(8)] public int MipCount = 10;
        
        public Material TerrainMaterial;
        private SimpleChunkProcesor<TextureProcess> TextureProcessor;
        private TextureArrayPool textureArrayPool;
        private TerrainTextures textureArrays;
        private UnityEngine.Pool.ObjectPool<Material> _materialPool;
        private CompactFullExporter exporter;

        public Action<TextureResult> onProcessDone { get; set; }
        public Action<TextureResult> onProcessRemoved { get; set; }

        private Dictionary<Vector3Int, TextureResult> ReloadableRequests = new();
        private List<Material> Orphans = new();

        private List<AssetWithLoaders<IHoldTextures>> TextureAssetLoader = null;
        public bool ContainsTextures => TextureAssetLoader.Count > 0;

        public override void OnValidate()
        {
            ReassignMaterials();
        }

        protected override void OnProcessRemoved(ChunkData chunk)
        {
            TextureProcessor?.OnProcessRemoved(chunk);

            if (ReloadableRequests.TryGetValue(chunk.ID, out TextureResult result))
            {
                result.SplatMaps.Return();
                result.HeightMap.Return();

                _materialPool.Release(result.groundMaterial);
                textureArrayPool?.Release(result.TextureMasksArray);
                onProcessRemoved?.Invoke(result);
            }
            ReloadableRequests.Remove(chunk.ID);
        
        }

        protected override void OnProcessAdded(ChunkData chunk)
        {
            TextureProcessor?.OnProcessAdded(chunk);
        }
        public override void Update()
        {
            TextureProcessor?.UpdateProcesses();
        }
        protected override void InitializeProcessor()
        {
#if UNITY_EDITOR
            TextureReloader.OnSaveAnyAsset -= ReassignMaterials;
            TextureReloader.OnSaveAnyAsset += ReassignMaterials;
#endif
            ReloadableRequests.Clear();
            Orphans.Clear();

            if (TerrainMaterial == null)
            {
                Debug.LogWarningFormat("No material has been set in {0}. Creating a temporal one", nameof(TerrainMaterial));
                TerrainMaterial = new Material(Resources.Load<Material>("Materials/InfiniteLandsDefault"));
            }

            if (_materialPool == null)
                _materialPool = new UnityEngine.Pool.ObjectPool<Material>(CreateMaterial, actionOnDestroy: AdaptiveDestroy);

            if (TextureProcessor == null)
                TextureProcessor = new SimpleChunkProcesor<TextureProcess>(infiniteLands, Complete, TryCreateProcess);

            InitializeComponents();
            UpdateMaterialsOfAssets();
        }

        private void UpdateMaterialsOfAssets()
        {
            var graph = infiniteLands.graph;
            if (graph == null)
                return;
            
            var materialsRequired = AssetDataHelper.GetAssets<IHoldMaterials>(graph).SelectMany(a => a.GetMaterials()).Distinct();
            foreach (var material in materialsRequired)
            {
                AssignTexturesToMaterials(material);
            }
        }

        void DisableTextureArrays()
        {
            if (textureArrays != null)
            {
                textureArrays.Release();
                textureArrays.OnTextureAssetModified -= OnGraphUpdated;
                textureArrays = null;
            }

            foreach (var request in ReloadableRequests)
            {
                var result = request.Value;
                textureArrayPool?.Release(result.TextureMasksArray);
            }

            if (textureArrayPool != null)
            {
                textureArrayPool.Dispose();
                textureArrayPool = null;
            }
            TextureAssetLoader = null;
        }
        protected override void DisableProcessor()
        {
            DisableTextureArrays();

            foreach (var request in ReloadableRequests)
            {
                var result = request.Value;
                result.SplatMaps.Return();
                result.HeightMap.Return();
                _materialPool.Release(result.groundMaterial);
            }

            ReloadableRequests.Clear();

            if (exporter != null)
                exporter.DestroyTextures(AdaptiveDestroy);

            if (_materialPool != null)
            {
                if (_materialPool.CountActive > 0)
                    Debug.LogErrorFormat("Not all materials have been released {0}", _materialPool.CountActive);
                _materialPool.Dispose();
                _materialPool = null;
            }

            if (TextureProcessor != null)
                TextureProcessor.DisableProcessor();

            #if UNITY_EDITOR
                TextureReloader.OnSaveAnyAsset -= ReassignMaterials;
            #endif
        }

        public override void OnGraphUpdated()
        {
            if (infiniteLands == null || infiniteLands.graph == null)
                return;

            if (textureArrays != null && textureArrays.AssetsAreDirty)
            {
                textureArrays.Release();
                textureArrays.OnTextureAssetModified -= OnGraphUpdated;
                textureArrays = null;
            }

            InitializeComponents();
            UpdateMaterialsOfAssets();
            ReassignMaterials();
        }

        protected (TextureProcess, bool) TryCreateProcess(ChunkData chunk)
        {
            if (!TerrainMaterial)
                return (default, false);

            ExportedMultiResult HeightMap = default;
            AssetDataCompact assetResult;
            ExportedMultiResult SplatMaps;

            var branch = chunk.GetVariantTree().GetTrunk();
            var isValidPreview = GraphSettingsController.TryGetPreviewData(infiniteLands.graph, chunk.GetMainTree().GetTrunk(), out HeightData result);
            if (isValidPreview)
            {
                assetResult = AssetDataHelper.GetCompactAssetData(chunk, new List<AssetWithLoaders<IHoldTextures>>());
                HeightMap = exporter.GenerateSingleTexture(result, branch);
                SplatMaps = new ExportedMultiResult(new List<BurstTexture>(), null, default);
            }
            else
            {
                assetResult = AssetDataHelper.GetCompactAssetData(chunk, TextureAssetLoader);
                SplatMaps = exporter.GenerateDensityTextures(assetResult);
                if (!exporter.HasItems)
                {
                    HeightMap = exporter.GenerateHeightTexture(chunk.DisplacedVertexPositions, chunk.GlobalMinMax);
                }
            }

            JobHandle job = JobHandle.CombineDependencies(SplatMaps.job, HeightMap.job);
            return (new TextureProcess(assetResult, chunk.meshSettings, chunk.terrainConfig, SplatMaps, HeightMap, job), true);
        }

        protected void Complete(TextureProcess process, bool cancel)
        {
            process.job.Complete();
            if (cancel)
            {
                process.SplatMaps.Return();
            }
            else
            {
                var id = process.GetID();
                Material mat = _materialPool?.Get();
                AssignTexturesToMaterials(mat);
                Texture2DArray texture2DArray = null;
                if (textureArrayPool != null && process.SplatMaps.textures.Count > 0)
                {
                    var texturesToArray = ListPoolLight<Texture2D>.Get();
                    for (int x = 0; x < process.SplatMaps.textures.Count; x++)
                    {
                        texturesToArray.Add(process.SplatMaps.textures[x].ApplyTexture());
                    }
                    texture2DArray = textureArrayPool?.GetConfiguredArray("Masks", texturesToArray);
                    ListPoolLight<Texture2D>.Release(texturesToArray);
                }
                TextureResult result = new TextureResult(process.meshSettings, process.terrainConfig,
                    process.SplatMaps, process.HeightMap, mat, texture2DArray);

                ReloadableRequests.TryAdd(id, result);
                onProcessDone?.Invoke(result);
            }

            process.assetData.CleanUp();
        }
    
        private void ReassignMaterials()
        {
            foreach (KeyValuePair<Vector3Int, TextureResult> reassignable in ReloadableRequests)
            {
                TextureResult reques = reassignable.Value;
                reques.ReloadMaterial();
            }
                 
            if (textureArrays != null)
            {
                foreach (Material material in Orphans)
                {
                    textureArrays.ApplyTextureArrays(material);
                }
            }
        }

        private Material CreateMaterial()
        {
            return new(TerrainMaterial);
        }

        public void AssignTexturesToMaterials(Material material)
        {
            if (material == null)
                return;

            if (textureArrays != null)
            {
                textureArrays.ApplyTextureArrays(material);
            }

            Orphans.Add(material);
        }

        public uint[] ExtractTexturesMask(List<TextureAsset> textures)
        {
            int length = TextureAssetLoader.Count;
            if (length == 0)
                return new uint[1] { 0 };

            uint[] mask = new uint[(length + 31) / 32];
            if (textures == null || textures.Count == 0)
                return mask;

            foreach (TextureAsset texture in textures)
            {
                if (texture == null)
                    continue;

                uint index = GetTextureIndex(TextureAssetLoader, texture, out bool matched);
                if (matched)
                    mask[index / 32] |= 1u << (int)(index % 32);
            }
            return mask;
        }

        private uint GetTextureIndex(List<AssetWithLoaders<IHoldTextures>> previousTextures, TextureAsset texture, out bool match)
        {
            for (uint i = 0; i < previousTextures.Count; i++)
            {
                var current = previousTextures[(int)i];
                if (texture.Equals(current.casted))
                {
                    match = true;
                    return i;
                }
            }
            match = false;
            return 0;
        }

        public void AssignTexturesToMaterials(CommandBuffer bf, ComputeShader compute, int kernelIndex, IHoldVegetation.ColorSamplingMode colorSamplingMode)
        {
            if (textureArrays != null)
            {
                textureArrays.ApplyTextureArrays(bf, compute, kernelIndex, colorSamplingMode);
            }
        }

        public bool TryGetDataAt(Vector2 position, out TextureResult data)
        {
            return infiniteLands.TryGetChunkDataAtGridPosition(position, ReloadableRequests, out data);
        }

        private void InitializeComponents()
        {
            var graph = infiniteLands.graph;
            if (graph == null)
                return;

            var textures = AssetDataHelper.GetAssetsWithHolder<IHoldTextures>(graph);
            var arraysMatch = AssetWithLoaders<IHoldTextures>.SequenceEqual(TextureAssetLoader, textures);
            var settings = infiniteLands.meshSettings;
            if (!arraysMatch || textureArrays == null || textureArrayPool == null)
            {
                DisableTextureArrays();
                if (textures.Count() > 0)
                {
                    if (textureArrays == null)
                    {
                        TextureResolution TextureResolution = new TextureResolution()
                        {
                            MipCount = MipCount,
                            UseMaximumResolution = UseMaximumTextureResolution,
                            Width = Width,
                            Height = Height,
                        };

                        textureArrays = new TerrainTextures(textures, AdaptiveDestroy, TerrainMaterial?.shader, TextureResolution, settings.MeshScale);
                        textureArrays.OnTextureAssetModified += OnGraphUpdated;
                    }
                    if (textureArrayPool == null)
                    {
                        int size = settings.TextureResolution + 1;
                        int count = (textures.Count() + 3) / 4; // Equivalent to Mathf.CeilToInt(originals.Length / 4f)
                        textureArrayPool = new TextureArrayPool(size, size, 1, count, false, AdaptiveDestroy, true);
                    }
                }

            }

            if (exporter == null || (exporter != null && exporter.GetTextureResolution() != settings.TextureResolution) || !arraysMatch)
            {
                exporter?.DestroyTextures(AdaptiveDestroy);
                exporter = new CompactFullExporter(settings.TextureResolution, settings.Resolution);
                exporter.InitializeNames(textures.Select(a => a.asset));
            }
            TextureAssetLoader = textures;
        }
    }
}