using UnityEngine;
using UnityEditor;
using System.Linq;

namespace sapra.InfiniteLands.Editor{
    [CustomEditor(typeof(TextureAsset), true)]
    [CanEditMultipleObjects]
    public class TextureAssetEditor : UnityEditor.Editor
    {
        public override Texture2D RenderStaticPreview(string assetPath, Object[] subAssets, int width, int height)
        {   
            TextureAsset example = (TextureAsset)target;
            if(example == null)
                return null;

            var albedo = TextureDataExtensions.ExtractTextureWithType(example, TextureType.Albedo);
            if(albedo == null)
                return null;

            Texture2D previewTexture = null;
            int attempts = 0;
            while (previewTexture == null && attempts <= 10)
            {
                previewTexture = AssetPreview.GetAssetPreview(albedo);
                attempts++;
            }
            if(previewTexture == null)
                return null;
                
            Texture2D tex = new Texture2D (width, height);
            EditorUtility.CopySerialized(previewTexture, tex);
            return tex;
        }
    }
}