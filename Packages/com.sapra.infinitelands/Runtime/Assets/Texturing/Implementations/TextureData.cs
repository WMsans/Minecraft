using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace sapra.InfiniteLands{
    public enum TextureDefault{White, Black, Gray, NormalMap, Red}
    public enum TextureType{Albedo, HeightMap, NormalMap, Occlusion, Other}

    [System.Serializable]
    public class TextureData{
        [SerializeField] public TextureType type = TextureType.Albedo;
        [SerializeField] private TextureDefault defaultColor = TextureDefault.White;
        [SerializeField] private Texture2D texture;
        [SerializeField] private string textureName;

        public TextureData(Texture2D texture, string textureName){
            this.texture = texture;
            this.textureName = textureName;
        }

        public Texture2D GetTexture(){
            return texture != null ? texture : GetDefaultTextureFromAsset();
        }

        public Texture2D GetDefaultTextureFromAsset(){
            if(type == TextureType.Other)
                return TextureDataExtensions.GetDefaultTexture(defaultColor);
            else
                return TextureDataExtensions.DefaultTextureFromType(type);
        }

        public string GetTextureName(){
            return TextureDataExtensions.DefaultNameFromType(type, textureName);
        }
    }

    public static class TextureDataExtensions{

        public static Dictionary<string, Texture2D> GetUniqueDefaultTextures(IEnumerable<TextureData> textures, IEnumerable<string> arraysInShader){
            return arraysInShader
                .Distinct() // Ensure unique texture names
                .Select(name =>
                {
                    var type = TypeFromDefaultName(name);
                    if (type == TextureType.Other)
                    {
                        // Find the first texture that matches the name and return its default texture
                        var matchingTexture = textures.FirstOrDefault(t => t.GetTextureName() == name);
                        return (name, matchingTexture?.GetDefaultTextureFromAsset());
                    }
                    else
                        return (name, DefaultTextureFromType(type));
                })
                .Where(pair => pair.Item2 != null)
                .ToDictionary(pair => pair.name, pair => pair.Item2);

        }

        public static Texture2D ExtractTextureWithType(IHoldTextures data, TextureType type){
            var targetTexture = data.GetTextures().FirstOrDefault(a => a.type == type);
            if(type.Equals(TextureType.Other))
                return targetTexture?.GetTexture();
            else
                return targetTexture != null ? targetTexture.GetTexture() : DefaultTextureFromType(type);
        }

        public static Texture2D GetDefaultTexture(TextureDefault type){
            switch(type){
                case TextureDefault.White:
                    return Texture2D.whiteTexture;
                case TextureDefault.Black:
                    return Texture2D.blackTexture;
                case TextureDefault.Gray:
                    return Texture2D.grayTexture;             
                case TextureDefault.NormalMap:
                    return Texture2D.normalTexture;
                case TextureDefault.Red:
                    return Texture2D.redTexture;
                default:
                    return Texture2D.redTexture;
            }
        }

        public static Texture2D DefaultTextureFromType(TextureType type){
            return GetDefaultTexture(DefaultTextureNameFromType(type));
        }

        public static TextureDefault DefaultTextureNameFromType(TextureType type){
            switch(type){
                case TextureType.Albedo:
                    return TextureDefault.Red;
                case TextureType.HeightMap:
                    return TextureDefault.Gray;
                case TextureType.NormalMap:
                    return TextureDefault.NormalMap;          
                case TextureType.Occlusion:
                    return TextureDefault.White;
                default:
                    return TextureDefault.Black;
            }
        }
        
        private static readonly Dictionary<TextureType, string> typeToNameMap = new Dictionary<TextureType, string>
        {
            { TextureType.Albedo, "_albedo_textures" },
            { TextureType.HeightMap, "_height_textures" },
            { TextureType.NormalMap, "_normal_textures" },
            { TextureType.Occlusion, "_occlusion_textures" }
        };

        private static readonly Dictionary<string, TextureType> nameToTypeMap = new Dictionary<string, TextureType>();

        static TextureDataExtensions()
        {
            // Automatically populate nameToTypeMap from typeToNameMap
            foreach (var pair in typeToNameMap)
            {
                nameToTypeMap[pair.Value] = pair.Key;
            }
        }

        public static string DefaultNameFromType(TextureType type, string targetValue)
        {
            return typeToNameMap.TryGetValue(type, out string name) ? name : targetValue;
        }

        public static TextureType TypeFromDefaultName(string name)
        {
            return nameToTypeMap.TryGetValue(name, out TextureType type) ? type : TextureType.Other; // Handle unknown case
        }
    }
}