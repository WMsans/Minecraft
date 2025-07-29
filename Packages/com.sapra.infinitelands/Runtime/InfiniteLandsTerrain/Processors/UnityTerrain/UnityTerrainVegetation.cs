using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

namespace sapra.InfiniteLands.UnityTerrain{
    [ExecuteAlways]
    public class UnityTerrainVegetation : ChunkProcessor<ChunkData>, IGenerate<UnityTerVegetation>
    {
        public bool UseGPUInstancingInDetails;
        public float DetailTreshold = 1;
        private GameObject PrefabHolder;
        private readonly string HolderName = "Unity Prefabs Holder";

        private List<AssetWithLoaders<IHoldVegetation>> vegetationAssets;

        public Action<UnityTerVegetation> onProcessDone { get; set; }
        public Action<UnityTerVegetation> onProcessRemoved { get; set; }
        private SimpleChunkProcesor<UnityTerVegetationProcess> VegetationProcess;

        protected override void InitializeProcessor()
        {
            GetPrefabHolder();
            vegetationAssets = AssetDataHelper.GetAssetsWithHolder<IHoldVegetation>(infiniteLands.graph);
            if (VegetationProcess == null)
                VegetationProcess = new SimpleChunkProcesor<UnityTerVegetationProcess>(infiniteLands, Complete, CreateProcess);
        }

        protected override void DisableProcessor()
        {
            VegetationProcess?.DisableProcessor();
            AdaptiveDestroy(PrefabHolder);
        }
        
        public override void Update()
        {
            VegetationProcess?.UpdateProcesses();
        }
        
        void GetPrefabHolder(){
            if(PrefabHolder != null)
                return;
            
            PrefabHolder = RuntimeTools.FindOrCreateObject(HolderName, transform);
            PrefabHolder.SetActive(false);
        }

        public void Complete(UnityTerVegetationProcess process, bool cancel)
        {
            process.job.Complete();
            if (!cancel)
            {
                Finalize(vegetationAssets, process.meshSettings, process);
            }
            process.assetData.CleanUp();
        }

        public (UnityTerVegetationProcess process, bool completed) CreateProcess(ChunkData chunkData)
        {
            AssetDataCompact result = AssetDataHelper.GetCompactAssetData(chunkData, vegetationAssets);
            return (new UnityTerVegetationProcess(chunkData.meshSettings, chunkData.ID, chunkData.GlobalMinMax, result), true);
        }

        private void Finalize(List<AssetWithLoaders<IHoldVegetation>> assets, MeshSettings meshSettings, UnityTerVegetationProcess process) {
            GetPrefabHolder();

            var assetResult = process.assetData;

            List<TreePrototype> prototypes = new List<TreePrototype>();
            List<DetailPrototype> details = new List<DetailPrototype>();
            List<int[,]> detailMaps = new List<int[,]>();

            List<TreeInstance> instances = new List<TreeInstance>();

            var rnd = new System.Random(meshSettings.Seed);
            if (assets.Count > 0)
            {
                for (int i = 0; i < assets.Count; i++)
                {
                    var assetPack = assets[i];
                    IHoldVegetation asset = assetPack.casted;
                    var positionData = asset.GetPositionData();
                    var spawnMode = asset.GetSpawningMode();
                    var objectData = asset.GetObjectData();

                    float distanceBetweenItems = positionData.distanceBetweenItems;
                    if (distanceBetweenItems < DetailTreshold)
                    {
                        //Debug.LogWarningFormat("{0} has a distance between items of {1} units and is not supported. Minimum distance allowed is {2} unit", asset.name, distanceBetweenItems, 1);
                        DetailPrototype detail = new DetailPrototype();
                        if (spawnMode == IHoldVegetation.SpawnMode.GPUInstancing)
                        {
                            detail.prototype = FindOrLoadAssetLOD(asset, false);
                            detail.usePrototypeMesh = true;
                            detail.renderMode = DetailRenderMode.VertexLit;
                            detail.minHeight = positionData.minimumMaximumScale.x;
                            detail.maxHeight = positionData.minimumMaximumScale.y;
                            detail.minWidth = 1;
                            detail.maxWidth = 1;
                            detail.useInstancing = UseGPUInstancingInDetails;
                            details.Add(detail);
                            var detailMap = GenerateDetailMap(meshSettings.TextureResolution, assetResult.Map, meshSettings, assetResult.MapLenght * i, positionData.distanceBetweenItems);
                            detailMaps.Add(detailMap);
                        }
                        continue;
                    }

                    int rowInstances = Mathf.CeilToInt(meshSettings.MeshScale / distanceBetweenItems);
                    TreePrototype prototype = new TreePrototype();

                    if (spawnMode == IHoldVegetation.SpawnMode.GPUInstancing)
                        prototype.prefab = FindOrLoadAssetLOD(asset, true);
                    else
                        prototype.prefab = objectData.gameObject;

                    for (int x = 0; x < rowInstances; x++)
                    {
                        for (int y = 0; y < rowInstances; y++)
                        {
                            Vector2 position = new Vector2(y, x) / rowInstances;
                            Vector2 rndm = new Vector2(rnd.Next(100) / 100f, rnd.Next(100) / 100f);
                            rndm = positionData.positionRandomness * (rndm - Vector2.one * 0.5f) * 2f;
                            rndm *= (distanceBetweenItems / meshSettings.MeshScale) / 1.5f;
                            position += rndm;
                            float height = sampleSplatMap(assetResult.Map, position, meshSettings, assetResult.MapLenght * i);
                            if (height > 0.1f)
                            {
                                TreeInstance instance = new TreeInstance();
                                instance.position = new Vector3(position.x, 0, position.y);

                                float sizeRandom = Mathf.Lerp(positionData.minimumMaximumScale.x, positionData.minimumMaximumScale.y, rnd.Next(100) / 100f);
                                instance.widthScale = sizeRandom;
                                instance.heightScale = sizeRandom;
                                instance.rotation = 2 * Mathf.PI * rnd.Next(100) / 100f;
                                instance.prototypeIndex = prototypes.Count;
                                instances.Add(instance);
                            }
                        }
                    }
                    prototypes.Add(prototype);
                }
                onProcessDone?.Invoke(new UnityTerVegetation(prototypes.ToArray(),
                    details.ToArray(),
                    instances.ToArray(),
                    meshSettings.TextureResolution,
                    detailMaps,
                    meshSettings, process.GlobalMinMax, process.ID));
            }
        }

        // Generate detail placement map
        private int[,] GenerateDetailMap(int resolution, NativeArray<float> ogMap, MeshSettings settings, int offset, float dist)
        {
            int[,] map = new int[resolution, resolution];
            float terrainSize = settings.MeshScale; // Terrain width/length in Unity units
            float cellSize = terrainSize / resolution; // Size of one detail map cell
            int resolutionPerPatch = 8; // Match the value in SetDetailResolution
            float patchSize = cellSize * resolutionPerPatch;
            for (int x = 0; x < resolution; x++)
            {
                for (int y = 0; y < resolution; y++)
                {
                    float2 uv = new float2(y,x)/resolution;
                    float heightAtPoint = sampleSplatMap(ogMap, uv, settings, offset);
                    float instancesPerPatch = Mathf.Pow(patchSize / dist, 2) * heightAtPoint;
                    map[x, y] = Mathf.RoundToInt(instancesPerPatch);
                }
            }
            return map;
            
        }

        public float sampleSplatMap(NativeArray<float> map, Vector2 uv, MeshSettings settings, int offset){
            Vector2Int closest = Vector2Int.FloorToInt(math.saturate(uv)*settings.Resolution);
            int index = MapTools.VectorToIndex(new int2(closest.x, closest.y), settings.Resolution);
            return map[offset + index];

        }

        private GameObject FindOrLoadAssetLOD(IHoldVegetation asset, bool withGroup){
            var temp = RuntimeTools.FindOrCreateObject(asset.name, PrefabHolder.transform, out bool justCreated);
            if(justCreated){
                ArgumentsData argumentsData = asset.InitializeMeshes();
                if(withGroup){
                    var group = temp.AddComponent<LODGroup>();
                    MeshLOD[] CurrentLodS = argumentsData.Lods;
                    LOD[] lods = new LOD[CurrentLodS.Length];
                    for(int i = 0; i<lods.Length; i++){
                        var render = FindOrLoadAssetOne(CurrentLodS[i], temp.transform, i);
                        lods[i] = new LOD(1.0F / (i + 2),new Renderer[]{render});
                    }
                    group.SetLODs(lods);
                    group.RecalculateBounds();
                }
                else{
                    var lod = argumentsData.Lods[0];
                    var filter = temp.AddComponent<MeshFilter>();
                    var render = temp.AddComponent<MeshRenderer>();
                    filter.sharedMesh = lod.mesh;
                    render.materials = lod.materials;
                }
            }
            return temp;
        }

        

        private MeshRenderer FindOrLoadAssetOne(MeshLOD lod, Transform parent, int i){
            var lodValue = RuntimeTools.FindOrCreateObject(string.Format("LOD {0}", i), parent, out bool generated);
            if(generated){
                var filter = lodValue.AddComponent<MeshFilter>();
                var render = lodValue.AddComponent<MeshRenderer>();
                filter.sharedMesh = lod.mesh;
                render.materials = lod.materials;
                return render;
            }
            else
                return lodValue.GetComponent<MeshRenderer>();
                
        }
        

        protected override void OnProcessRemoved(ChunkData chunk)
        {
            VegetationProcess.OnProcessRemoved(chunk);
        }

        protected override void OnProcessAdded(ChunkData chunk)
        {
            VegetationProcess.OnProcessAdded(chunk);
        }
    }
}