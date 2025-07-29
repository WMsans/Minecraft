using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands{  
    [AssetNode(typeof(AssetOutputNode))]
    [CreateAssetMenu(menuName = "Infinite Lands/Assets/Texture")]
    public class TextureAsset : InfiniteLandsAsset, IHoldTextures, IHaveAssetPreview
    {
        [SerializeField] private List<TextureData> textures = new();
        public DefaultSettings settings = DefaultSettings.Default;

        public ITextureSettings GetSettings() => settings;
        public List<TextureData> GetTextures() => textures;
        public VisualElement Preview(bool BigPreview)
        {
            var imagePreview = new Image();
            var texture = textures.Where(a => a.type == TextureType.Albedo).FirstOrDefault();
            if(texture != null){
                imagePreview.image = texture.GetTexture();
                return imagePreview;
            }
            return null;
        }
    }
}