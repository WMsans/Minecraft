using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace sapra.InfiniteLands
{
    [CustomNode("Color Output", docs = "https://ensapra.com/packages/infinite_lands/nodes/heightmap/output/coloroutput")]
    public class ColorOutputNode : InfiniteLandsNode, ILoadAsset
    {
        public Color color = Color.red;
        [field: SerializeField] public ILoadAsset.Operation action{get; private set;}
        private IAsset Asset{
            get{
                if(Cache == null || Cache.color != color){
                    Cache?.Remove();
                    Cache = new ColorData(color, guid);
                }
                return Cache;
            }
        }

        public string OutputVariableName => nameof(Density);

        private ColorData Cache;
        [Input] public HeightData Density;
        public IEnumerable<IAsset> GetAssets()
        {
            if (Asset is IHoldManyAssets manyAssets)
            {
                return manyAssets.GetAssets();
            }
            else
            {
                return new[] { Asset };
            }
        }
        public override void Restart(IGraph graph)
        {
            base.Restart(graph);
            Cache?.Remove();
            Cache = new ColorData(color, guid);
        }
        protected override bool SetInputValues(BranchData branch)
        {
            return TryGetInputData(branch, out Density, nameof(Density));
        }
        protected override bool Process(BranchData branch)
        {
            if (state.SubState == 0)
            {
                if (!branch.ForcedOrFinished(Density.jobHandle)) return false;
                state.IncreaseSubState();
            }
            return state.SubState == 1;
        }
        protected override void CacheOutputValues()
        {
            CacheOutputValue(Density, nameof(Density));
        }
        public bool AssetExists(IAsset asset) => GetAssets().Contains(asset);
    }
    
    public class ColorData : IAsset, IHoldTextures
    {
        public string name { get; private set; }
        public Color color;
        private DefaultSettings Settings;
        private List<TextureData> Textures;
        private Texture2D SimpleColorTexture;
        public ColorData(Color color, string name)
        {
            this.name = name;
            this.color = color;
            SimpleColorTexture = new Texture2D(2, 2);
            Color[] pixels = SimpleColorTexture.GetPixels();

            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            SimpleColorTexture.SetPixels(pixels);
            SimpleColorTexture.Apply();

            Textures = new List<TextureData>() { new TextureData(SimpleColorTexture, name) };
            Settings = DefaultSettings.Default;
        }

        public List<TextureData> GetTextures() => Textures;
        public void Remove()
        {
            if (SimpleColorTexture == null)
                return;
            if (Application.isPlaying)
                GameObject.Destroy(SimpleColorTexture);
            else
                GameObject.DestroyImmediate(SimpleColorTexture);

        }

        public ITextureSettings GetSettings() => Settings;
    }
}