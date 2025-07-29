using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Pool;

namespace sapra.InfiniteLands.UnityTerrain{
    [ExecuteAlways]
    public class UnityTerrainPainter : ChunkProcessor<ChunkData>, IGenerate<UnityTerTextures>
    {
        public Material terrainMaterial;
        private ObjectPool<Material> _materialPool;

        public Action<UnityTerTextures> onProcessDone { get; set; }
        public Action<UnityTerTextures> onProcessRemoved { get; set; }
        public HeightmapResolution TextureResolution = HeightmapResolution._257x257;
        private List<AssetWithLoaders<IHoldTextures>> textureAssets;
        private SimpleChunkProcesor<UnityTerTextureProcess> TextureProcessor;

        protected override void DisableProcessor()
        {
            TextureProcessor?.DisableProcessor();
            if (_materialPool != null)
                _materialPool.Dispose();
        }

        private Material CreateMaterial(){
            return new(terrainMaterial);
        }
        protected override void InitializeProcessor()
        {
            if (terrainMaterial == null)
            {
                Debug.LogWarningFormat("No material has been set in {0}. Creating a temporal one", nameof(terrainMaterial));
                terrainMaterial = new Material(Shader.Find("Nature/Terrain/Diffuse"));
            }
            if (_materialPool == null)
                _materialPool = new UnityEngine.Pool.ObjectPool<Material>(CreateMaterial, actionOnDestroy: AdaptiveDestroy);

            if (TextureProcessor == null)
                TextureProcessor = new SimpleChunkProcesor<UnityTerTextureProcess>(infiniteLands, Complete, CreateProcess);
                
            textureAssets = AssetDataHelper.GetAssetsWithHolder<IHoldTextures>(infiniteLands.graph);
        }

        public override void Update()
        {
            TextureProcessor.UpdateProcesses();
        }

        
        protected override void OnProcessRemoved(ChunkData chunk)
        {
            TextureProcessor.OnProcessRemoved(chunk);
        }

        protected override void OnProcessAdded(ChunkData chunk)
        {
            TextureProcessor.OnProcessAdded(chunk);
        }

        private void Complete(UnityTerTextureProcess process, bool cancel)
        {
            process.job.Complete();
            if (!cancel)
            {
                var assetResult = process.assetResult;
                var meshSettings = process.meshSettings;
                int resolution = (int)TextureResolution;

                var length = textureAssets.Count;
                int count = 0;

                if (length > 0)
                {
                    int totalCount = length;
                    TerrainLayer[] layers = new TerrainLayer[totalCount];
                    float[,,] details = new float[resolution, resolution, totalCount];
                    for (int i = 0; i < length; i++)
                    {
                        var holder = textureAssets[i];
                        var actualAsset = holder.casted;
                        TerrainLayer terrainLayer = new TerrainLayer();
                        for (int x = 0; x < resolution; x++)
                        {
                            for (int y = 0; y < resolution; y++)
                            {
                                float height = Remap(meshSettings.Resolution + 1, resolution, y, x, assetResult.Map,
                                    (meshSettings.Resolution + 1) * (meshSettings.Resolution + 1) * i, assetResult.Map.Length - 1);

                                details[x, y, count] = height;
                            }
                        }
                        var albedoTexture = TextureDataExtensions.ExtractTextureWithType(actualAsset, TextureType.Albedo);
                        var normalTexture = TextureDataExtensions.ExtractTextureWithType(actualAsset, TextureType.NormalMap);
                        terrainLayer.diffuseTexture = albedoTexture;
                        terrainLayer.normalMapTexture = normalTexture;
                        terrainLayer.tileSize = Vector2.one * actualAsset.GetSettings().GetTextureSize(meshSettings.MeshScale);
                        terrainLayer.name = actualAsset.name;
                        layers[count] = terrainLayer;
                        count++;
                    }
                    for (int x = 0; x < resolution; x++)
                    {
                        for (int y = 0; y < resolution; y++)
                        {
                            float total = 0;
                            for (int c = 0; c < count; c++)
                            {
                                total += details[x, y, c];
                            }
                            for (int c = 0; c < count; c++)
                            {
                                details[x, y, c] /= total;
                            }
                            if (total <= 0.1f)
                                details[x, y, 0] = 1;
                        }
                    }

                    var result = new UnityTerTextures(process.GetID(), resolution, details, layers, _materialPool.Get(), process.meshSettings, process.GlobalMinMax);
                    onProcessDone?.Invoke(result);
                }
            }

            process.assetResult.CleanUp();
        }

        protected (UnityTerTextureProcess, bool) CreateProcess(ChunkData chunkData)
        {
            AssetDataCompact assetResult = AssetDataHelper.GetCompactAssetData(chunkData, textureAssets);
            return (new UnityTerTextureProcess(chunkData.meshSettings, chunkData.ID, chunkData.GlobalMinMax, assetResult), true);
        }

        private float Remap(int currentResolution, int targetResolution, int xTarget, int yTarget, NativeArray<float> map, int offset, int maxLength){
            float factor = (float)currentResolution/(float)targetResolution;
            float ogX = xTarget*factor;
            float ogY = yTarget*factor;

            if(xTarget == 0)
                ogX = 0;
            else if(xTarget == targetResolution-1)
                ogX = currentResolution-1;
            if(yTarget == 0)
                ogY = 0;
            else if(yTarget == targetResolution-1)
                ogY = currentResolution-1;

            int btmX = Mathf.Min(Mathf.FloorToInt(ogX), currentResolution);
            int btmY = Mathf.Min(Mathf.FloorToInt(ogY), currentResolution);
            int topX = Mathf.Min(Mathf.CeilToInt(ogX), currentResolution);
            int topY = Mathf.Min(Mathf.CeilToInt(ogY), currentResolution);
            
            int maxSingleLength = MapTools.LengthFromResolution(currentResolution-1)-1;
            int indBtmX = Mathf.Min(maxSingleLength, btmX+btmY*(currentResolution));
            int indBtmY = Mathf.Min(maxSingleLength, btmX+topY*(currentResolution));
            int indTopX = Mathf.Min(maxSingleLength, topX+btmY*(currentResolution));
            int indTopY = Mathf.Min(maxSingleLength, topX+topY*(currentResolution));


            int leftDownIndex =  Mathf.Min(indBtmX+offset,maxLength);
            int leftUpIndex =  Mathf.Min(indBtmY+offset,maxLength);
            int rightDownIndex =  Mathf.Min(indTopX+offset,maxLength);
            int righTUpIndex =  Mathf.Min(indTopY+offset,maxLength);

            float leftDown = map[leftDownIndex];
            float leftUp = map[leftUpIndex];
            float rightDown = map[rightDownIndex];
            float righTUp = map[righTUpIndex];

            float fcX = ogX-btmX;
            float fcy = ogY-btmY;
            float interBotm = Mathf.Lerp(leftDown, rightDown, fcX);
            float interUp = Mathf.Lerp(leftUp, righTUp, fcX);
            return Mathf.Lerp(interBotm, interUp, fcy);
        }
    }
}