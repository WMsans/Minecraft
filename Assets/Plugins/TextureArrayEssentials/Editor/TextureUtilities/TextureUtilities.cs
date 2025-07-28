using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace TextureArrayEssentials
{
    public static class TextureUtilities
    {
        // List of the texture formats that are compressed
        public static readonly TextureFormat[] COMPRESSED_TEXTURE_FORMATS = {
            TextureFormat.DXT1,
            TextureFormat.DXT5,
            TextureFormat.DXT1Crunched,
            TextureFormat.DXT5Crunched,
            TextureFormat.BC4,
            TextureFormat.BC5,
            TextureFormat.BC6H,
            TextureFormat.BC7,
            TextureFormat.PVRTC_RGB2,
            TextureFormat.PVRTC_RGB4,
            TextureFormat.PVRTC_RGBA2,
            TextureFormat.PVRTC_RGBA4,
            TextureFormat.ETC_RGB4,
            TextureFormat.EAC_R,
            TextureFormat.EAC_RG,
            TextureFormat.EAC_R_SIGNED,
            TextureFormat.EAC_RG_SIGNED,
            TextureFormat.ETC2_RGB,
            TextureFormat.ETC2_RGBA1,
            TextureFormat.ETC2_RGBA8,
            TextureFormat.ETC_RGB4Crunched,
            TextureFormat.ETC2_RGBA8Crunched,
            TextureFormat.ASTC_4x4,
            TextureFormat.ASTC_5x5,
            TextureFormat.ASTC_6x6,
            TextureFormat.ASTC_8x8,
            TextureFormat.ASTC_10x10,
            TextureFormat.ASTC_12x12,
            TextureFormat.ASTC_HDR_4x4,
            TextureFormat.ASTC_HDR_5x5,
            TextureFormat.ASTC_HDR_6x6,
            TextureFormat.ASTC_HDR_8x8,
            TextureFormat.ASTC_HDR_10x10,
            TextureFormat.ASTC_HDR_12x12,
        };

        /// <summary>
        /// Scales the input texture to the desired resolution<br />
        /// Does not modify the original texture, returns a new resized one
        /// </summary>
        /// <param name="texture">
        /// The texture that will be resized
        /// </param>
        /// <param name="newWidth">
        /// The width of the output texture
        /// </param>
        /// <param name="newHeight">
        /// The height of the output texture
        /// </param>
        /// <param name="filterMode">
        /// The filter mode used when resizing the texture
        /// </param>
        /// <returns>
        /// The texture resized to the input resolution
        /// </returns>
        public static Texture2D ResizeTexture(Texture2D texture, int newWidth, int newHeight, FilterMode filterMode = FilterMode.Bilinear)
        {
            RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight);
            rt.filterMode = filterMode;

            RenderTexture previousRT = RenderTexture.active;
            RenderTexture.active = rt;

            Graphics.Blit(texture, rt);
            Texture2D result = new Texture2D(newWidth, newHeight);

            result.ReadPixels(new Rect(0, 0, newWidth, newHeight), 0, 0);
            result.Apply();

            RenderTexture.active = previousRT;
            RenderTexture.ReleaseTemporary(rt);

            return result;
        }

        /// <summary>
        /// Re-Imports the input texture as readable if necessary
        /// </summary>
        /// <param name="texture">
        /// The texture that will set to readable
        /// </param>
        /// <param name="logChanges">
        /// Debugs if the texture is set to readable if it is not already
        /// </param>
        public static void SetReadable(Texture2D texture, bool logChanges = false)
        {
            string texturePath = AssetDatabase.GetAssetPath(texture);
            if (texturePath == "") return;

            TextureImporter ti = (TextureImporter)AssetImporter.GetAtPath(texturePath);
            if (ti != null && !ti.isReadable) {
                if(logChanges) Debug.LogWarning($"{texture.name} is not readable, setting read to true...");
                
                ti.isReadable = true;
                ti.SaveAndReimport();
            }
        }

        /// <summary>
        /// Re-Imports the input textures as readable if necessary
        /// </summary>
        /// <param name="textures">
        /// The array of textures that will set to readable
        /// </param>
        /// <param name="logChanges">
        /// Debugs if the texture is set to readable if it is not already
        /// </param>
        public static void SetReadable(Texture2D[] textures, bool logChanges = false)
        {
            for (int i = 0; i < textures.Length; i++) {
                SetReadable(textures[i], logChanges);
            }
        }

        /// <summary>
        /// Converts a unity compressed normal map back to an uncompressed one<br />
        /// From red to blue essentially<br />
        /// Does not modify the original texture, returns a new converted one
        /// </summary>
        /// <param name="texture">
        /// The texture that will be converted
        /// </param>
        /// <returns>
        /// The texture converted to an uncompressed normal
        /// </returns>
        public static Texture2D ConvertFromCompressedNormal(Texture2D texture)
        {
            if (texture == null) return null;

            Color[] pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++) {
                Color pixel = pixels[i];
                pixel.b = pixel.r;
                pixel.r = pixel.a;
                pixel.a = 1.0f;
                pixels[i] = pixel;
            }

            Texture2D normalTexture = new Texture2D(texture.width, texture.height);
            normalTexture.SetPixels(pixels);
            normalTexture.Apply();

            return normalTexture;
        }
    }
}
#endif