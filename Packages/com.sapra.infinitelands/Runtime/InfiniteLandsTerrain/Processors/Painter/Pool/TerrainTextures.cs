using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;
using static sapra.InfiniteLands.AssetDataHelper;

namespace sapra.InfiniteLands
{
    public class TerrainTextures
    {
        private LocalKeyword heightMapBlending;
        private List<(Texture2DArray, string)> TexturesAvailable;

        private ComputeBuffer TextureSettings;
        private ComputeBuffer TextureSizes;
        private TextureArrayPool pool;

        public List<AssetWithLoaders<IHoldTextures>> AllTextures{ get; private set; }
        public Action OnTextureAssetModified;
        public bool AssetsAreDirty{ get; private set; }

        public TerrainTextures(List<AssetWithLoaders<IHoldTextures>> allTextures, Action<Texture2DArray> DestroyTextures, Shader forShader,
            TextureResolution textureResolution, float MeshScale)
        {
            var textureArrays = GetTextureArrays(forShader);

            AllTextures = allTextures;
            var allTextureData = AllTextures.SelectMany(a => a.casted.GetTextures());
            Dictionary<string, Texture2D> defaultData = TextureDataExtensions.GetUniqueDefaultTextures(allTextureData, textureArrays);
            var possibleNames = defaultData.Keys;

            Dictionary<string, List<Texture2D>> texturePacks = new();
            foreach (var holder in AllTextures)
            {
                if (holder.asset is UpdateableSO updateable)
                {
                    updateable.OnValuesUpdated += TextureHasBeenModified;
                }

                var text = holder.casted;
                var textures = text.GetTextures();
                foreach (var desiredName in possibleNames)
                {
                    var exists = textures.FirstOrDefault(a => a.GetTextureName().Equals(desiredName));
                    Texture2D targetTexture = exists != null ? exists.GetTexture() : defaultData[desiredName];
                    if (!texturePacks.TryGetValue(desiredName, out var list))
                    {
                        list = new List<Texture2D>();
                        texturePacks[desiredName] = list;
                    }
                    list.Add(targetTexture);
                }
            }

            GenerateTexturePool(texturePacks, AllTextures.Count, DestroyTextures, textureResolution);

            TexturesAvailable = new List<(Texture2DArray, string)>();
            foreach (KeyValuePair<string, List<Texture2D>> nameAndTexture in texturePacks)
            {
                Texture2DArray textureArray = pool.GetConfiguredArray(nameAndTexture.Key, nameAndTexture.Value);
                textureArray.wrapMode = TextureWrapMode.Repeat;
                TexturesAvailable.Add((textureArray, nameAndTexture.Key));
            }

            IEnumerable<ITextureSettings> textureSettings = AllTextures.Select(a => a.casted.GetSettings());
            if (textureSettings.Select(a => a.GetObjectByteSize()).Distinct().Count() >= 2)
                Debug.LogError("Differences in the size of the texture settings! There will be dragons!");


            ComputeBuffer assetSettingsBuffer = textureSettings.First().CreateTextureCompute(textureSettings, MeshScale);
            ComputeBuffer sizesOnly = new ComputeBuffer(allTextures.Count(),
                sizeof(float), ComputeBufferType.Default);
            sizesOnly.SetData(textureSettings.Select(a => a.GetTextureSize(MeshScale)).ToArray());

            this.TextureSettings = assetSettingsBuffer;
            this.TextureSizes = sizesOnly;
        }

        void TextureHasBeenModified()
        {
            AssetsAreDirty = true;
            OnTextureAssetModified?.Invoke();
        }

        void GenerateTexturePool(Dictionary<string, List<Texture2D>> _texturePacks, int defaultCount, 
            Action<Texture2DArray> _destroyTextures, TextureResolution _textureResolution)
        {
            int width = _textureResolution.Width;
            int height = _textureResolution.Height;
            int mipCount = _textureResolution.MipCount;

            if(_textureResolution.UseMaximumResolution){
                Texture2D defaultTexture = _texturePacks.SelectMany(a => a.Value).OrderByDescending(a => a.width*a.height).First();
                width = defaultTexture.width;
                height = defaultTexture.height;
                mipCount = defaultTexture.mipmapCount;
            }
            pool = new TextureArrayPool(height, width, mipCount, defaultCount, true, _destroyTextures, false);
        }
        
        HashSet<string> GetTextureArrays(Shader shader)
        {
            HashSet<string> textureArrayNames = new HashSet<string>();
            if (shader == null)
            {
                Debug.LogError("Shader is null!");
                return textureArrayNames;
            }

            int propertyCount = shader.GetPropertyCount();
            for (int i = 0; i < propertyCount; i++)
            {
                var propType = shader.GetPropertyType(i);
                if (propType == ShaderPropertyType.Texture)
                {
                    string propName = shader.GetPropertyName(i);
                    if (shader.GetPropertyTextureDimension(i) == TextureDimension.Tex2DArray)
                    {
                        textureArrayNames.Add(propName);
                    }
                }
            }

            return textureArrayNames;
        }

        public void ApplyTextureArrays(Material material){
            for(int i = 0; i < TexturesAvailable.Count; i++){
                (Texture2DArray array, string name) arrayTouple = TexturesAvailable[i];
                material.SetTexture(arrayTouple.name, arrayTouple.array);
            }
            material.SetBuffer("_texture_settings", TextureSettings);
        }

        public void ApplyTextureArrays(CommandBuffer bf, ComputeShader compute, int kernelIndex, IHoldVegetation.ColorSamplingMode colorSamplingMode){
            if(heightMapBlending == default)
                heightMapBlending = new LocalKeyword(compute, "HEIGHTMAP_ENABLED");

            for(int i = 0; i < TexturesAvailable.Count; i++){
                (Texture2DArray array, string name) arrayTouple = TexturesAvailable[i];
                bf.SetComputeTextureParam(compute, kernelIndex, arrayTouple.name, arrayTouple.array);
                switch(colorSamplingMode){
                    case IHoldVegetation.ColorSamplingMode.WeightBlend:
                        bf.SetKeyword(compute, heightMapBlending, false);
                        break;
                    case IHoldVegetation.ColorSamplingMode.HeightMapBlend:
                        bf.SetKeyword(compute, heightMapBlending, true);
                        break;
                }
            }
            bf.SetComputeBufferParam(compute, kernelIndex,"_textureSize", TextureSizes);
        }

        public void Release()
        {
            foreach (var texture in AllTextures)
            {
                if (texture.asset is UpdateableSO updateable)
                {
                    updateable.OnValuesUpdated -= TextureHasBeenModified;
                }
            }

            if (TextureSettings != null)
            {
                TextureSettings.Release();
                TextureSettings = null;
            }
            if (TextureSizes != null)
            {
                TextureSizes.Release();
                TextureSizes = null;
            }

            foreach((Texture2DArray array, string name) arrayTouple in TexturesAvailable){
                pool.Release(arrayTouple.array);
            }
            pool.Dispose();
        }
    }
}