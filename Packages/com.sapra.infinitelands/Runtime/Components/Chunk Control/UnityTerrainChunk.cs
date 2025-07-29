using UnityEngine;

namespace sapra.InfiniteLands.UnityTerrain{
    public class UnityTerrainChunk : ChunkControl
    {
        public Terrain terrain;
        public TerrainCollider terrainCollider;
        public string GetGraphName() => infiniteLands.graph.name;

        private IGenerate<UnityTerHeights> heightGenerator;
        private IGenerate<UnityTerTextures> textureGenerator;
        private IGenerate<UnityTerVegetation> vegetationGenerator;

        public override void UnsubscribeEvents()
        {
            if (heightGenerator != null)
                heightGenerator.onProcessDone -= OnHeightsGenerated;

            if (textureGenerator != null)
                textureGenerator.onProcessDone -= OnTextureGenerated;
                
            if(vegetationGenerator != null)    
                vegetationGenerator.onProcessDone -= OnVegetationGenerated;
        }

        protected override void DisableIt()
        {
            if(terrain != null)
                terrain.terrainData = null;
        }
        public void OnVegetationGenerated(UnityTerVegetation unityVegetation)
        {
            if (!unityVegetation.ID.Equals(config.ID))
                return;

            if (terrain == null)
                return;
            var terrainData = GetTerrainData(unityVegetation.meshSettings, unityVegetation.globalMinMax);
            terrainData.treePrototypes = unityVegetation.prototypes;
            terrainData.detailPrototypes = unityVegetation.details;
            terrainData.SetTreeInstances(unityVegetation.instances, true);
            terrainData.SetDetailResolution(unityVegetation.TextureResolution, 8);
            for (int i = 0; i < unityVegetation.DetailMaps.Count; i++)
            {
                var detailMap = unityVegetation.DetailMaps[i];
                terrainData.SetDetailLayer(0, 0, i, detailMap);
            }
        }
        public void OnHeightsGenerated(UnityTerHeights unityHeights)
        {
            if (!unityHeights.ID.Equals(config.ID))
                return;

            if (terrain == null)
                return;
            var terrainData = GetTerrainData(unityHeights.meshSettings, unityHeights.globalMinMax);
            terrainData.name = unityHeights.ID.ToString();
            terrainData.heightmapResolution = unityHeights.HeightmapResolution;
            terrainData.size = unityHeights.Size;
            terrainData.SetHeights(0, 0, unityHeights.Heights);
            terrainCollider.terrainData = terrainData;
        }
        public void OnTextureGenerated(UnityTerTextures unityTextures)
        {
            if (!unityTextures.ID.Equals(config.ID))
                return;

            if (terrain == null)
                return;
            var terrainData = GetTerrainData(unityTextures.meshSettings, unityTextures.globalMinMax);
            terrainData.alphamapResolution = unityTextures.alphamapResolution;
            terrainData.terrainLayers = unityTextures.layers;
            terrainData.SetAlphamaps(0, 0, unityTextures.details);
            terrain.materialTemplate = unityTextures.GroundMaterial;
        }

/*         terrainData.treePrototypes = prototypes.ToArray();
                terrainData.detailPrototypes = details.ToArray();
                terrainData.SetTreeInstances(instances.ToArray(), true);
                terrainData.SetDetailResolution(meshSettings.TextureResolution, 8);
                for (int i = 0; i < detailMaps.Count; i++)
                {
                    var detailMap = detailMaps[i];
                    terrainData.SetDetailLayer(0, 0, i, detailMap);
                } */
        private TerrainData GetTerrainData(MeshSettings meshSettings, Vector2 globalMinMax)
        {
            if (terrain.terrainData == null)
            {
                terrain.terrainData = new TerrainData();
                float meshScale = meshSettings.MeshScale;
                float vertical = globalMinMax.x;
                Vector3 ps = terrain.transform.localPosition;
                ps.y = vertical;
                ps.x = -meshScale / 2f;
                ps.z = -meshScale / 2f;
                terrain.transform.localPosition = ps;
            }
            return terrain.terrainData;
        }
        
        protected override void CleanVisuals()
        {
            if(terrain != null)
                terrain.terrainData = null;
        }

        public override void UpdateVisuals(bool enabled)
        {
            terrain.enabled = enabled;
        }

        public override bool VisualsDone() => terrain.terrainData != null;

        protected override void InitializeChunk()
        {
            terrain = GetComponentInChildren<Terrain>();
            GameObject sub;
            if (terrain == null)
                sub = RuntimeTools.FindOrCreateObject("Terrain", transform);
            else
                sub = terrain.gameObject;

            terrain = GetOrAddComponent(ref terrain, sub);
            terrainCollider = GetOrAddComponent(ref terrainCollider, sub);
            heightGenerator = infiniteLands.GetInternalComponent<IGenerate<UnityTerHeights>>();
            textureGenerator = infiniteLands.GetInternalComponent<IGenerate<UnityTerTextures>>();
            vegetationGenerator = infiniteLands.GetInternalComponent<IGenerate<UnityTerVegetation>>();
            
            if (heightGenerator != null)
                heightGenerator.onProcessDone += OnHeightsGenerated;

            if (textureGenerator != null)
                textureGenerator.onProcessDone += OnTextureGenerated;
                
            if(vegetationGenerator != null)    
                vegetationGenerator.onProcessDone += OnVegetationGenerated;
        }
    }
}