using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TextureArrayEssentials;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "TerrainBlocksBuilder", menuName = "Terrain/Terrain Blocks Builder")]
public class TerrainBlocksBuilder : ScriptableObject
{
    [System.Serializable]
    public struct BlockAttributes
    {
        [Tooltip("A descriptive name for the block (e.g., Grass, Stone, Dirt).")]
        public string Name;
        public Texture2D Albedo;
        public Texture2D Normal;
        public Texture2D Smoothness;
        public Texture2D AmbientOcclusion;
        [Tooltip("Height map for displacement or parallax effects. Grayscale data is read from the Red channel.")]
        public Texture2D Height;
    }

    [Header("Source Textures")]
    [Tooltip("Define all block types to be included in the texture arrays.")]
    public List<BlockAttributes> Blocks = new List<BlockAttributes>();

    [Header("Generated Texture Arrays")]
    public Texture2DArray AlbedoArray;
    public Texture2DArray NormalArray;
    public Texture2DArray SmoothnessArray;
    public Texture2DArray AmbientOcclusionArray;
    public Texture2DArray HeightArray;

    /// <summary>
    /// Generates the Texture2DArrays for all defined map types.
    /// </summary>
    public void GenerateTextureArrays()
    {
#if UNITY_EDITOR
        if (Blocks.Count == 0)
        {
            Debug.LogWarning("No blocks defined. Cannot generate texture arrays.");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(this);
        string assetDirectory = Path.GetDirectoryName(assetPath);
        string folderName = $"{this.name}_GeneratedArrays";
        string folderPath = Path.Combine(assetDirectory, folderName);

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(assetDirectory, folderName);
        }
        
        // --- UPDATED ---
        // The Height array is now generated using DXT5 compression after being converted to a visual grayscale format.
        AlbedoArray = CreateAndSaveArray("Albedo", folderPath, Blocks.Select(b => b.Albedo).ToList(), TextureFormat.DXT5, false);
        NormalArray = CreateAndSaveNormalArray("Normal", folderPath, Blocks.Select(b => b.Normal).ToList());
        SmoothnessArray = CreateAndSaveArray("Smoothness", folderPath, Blocks.Select(b => b.Smoothness).ToList(), TextureFormat.DXT5, false);
        AmbientOcclusionArray = CreateAndSaveArray("AmbientOcclusion", folderPath, Blocks.Select(b => b.AmbientOcclusion).ToList(), TextureFormat.DXT5, false);
        HeightArray = CreateAndSaveArray("Height", folderPath, Blocks.Select(b => b.Height).ToList(), TextureFormat.DXT5, true);

        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Texture arrays generated successfully!");
#endif
    }

#if UNITY_EDITOR
    /// <summary>
    /// Specialized function to create and save a Normal Map Texture2DArray.
    /// </summary>
    private Texture2DArray CreateAndSaveNormalArray(string arrayName, string folderPath, List<Texture2D> textures)
    {
        Texture2DArray normalArray = CreateAndSaveArray(arrayName, folderPath, textures, TextureFormat.RGBA32, true);

        if (normalArray != null)
        {
            string arrayPath = AssetDatabase.GetAssetPath(normalArray);
            var importer = AssetImporter.GetAtPath(arrayPath) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.SaveAndReimport();
                return AssetDatabase.LoadAssetAtPath<Texture2DArray>(arrayPath);
            }
        }
        return normalArray;
    }

    /// <summary>
    /// Creates a Texture2DArray from a list of textures, saves it as an asset, and returns it.
    /// </summary>
    private Texture2DArray CreateAndSaveArray(string arrayName, string folderPath, List<Texture2D> textures, TextureFormat format, bool linear = false)
    {
        if (textures.Any(t => t == null))
        {
            Debug.LogWarning($"Cannot create {arrayName} array, one or more textures are missing.");
            return null;
        }

        int minWidth = textures.Min(t => t.width);
        int minHeight = textures.Min(t => t.height);

        var processedTextures = new List<Texture2D>();
        foreach (var texture in textures)
        {
            TextureUtilities.SetReadable(texture);
            Texture2D processedTex = texture;

            if (texture.width != minWidth || texture.height != minHeight)
            {
                processedTex = TextureUtilities.ResizeTexture(texture, minWidth, minHeight);
            }

            // --- FIX ---
            // If creating the "Height" array, this logic converts the source texture into a visually grayscale
            // RGBA texture. This makes it easier to preview in the editor.
            if (arrayName == "Height")
            {
                // Create a temporary RGBA32 texture to store the new pixel data.
                var grayscaleTexture = new Texture2D(processedTex.width, processedTex.height, TextureFormat.RGBA32, false, linear);
                
                Color32[] sourcePixels = processedTex.GetPixels32();
                var newPixels = new Color32[sourcePixels.Length];
                
                // Copy the value from the red channel of the source into the R, G, and B channels of the destination.
                for (int i = 0; i < sourcePixels.Length; i++)
                {
                    byte grayValue = sourcePixels[i].r;
                    newPixels[i] = new Color32(grayValue, grayValue, grayValue, 255);
                }
                
                grayscaleTexture.SetPixels32(newPixels);
                grayscaleTexture.Apply();
                
                // If the source was a temporary resized texture, destroy it now to free memory.
                if(processedTex != texture) DestroyImmediate(processedTex);
                
                processedTex = grayscaleTexture;
            }
            // --- END FIX ---

            processedTextures.Add(processedTex);
        }

        // Create the final texture array using the processed textures.
        Texture2DArray textureArray = Texture2DArrayUtilities.CreateArray(processedTextures.ToArray(), format, true, linear);

        // Clean up temporary textures that were created during processing.
        foreach (var tex in processedTextures)
        {
            if (!textures.Contains(tex))
            {
                DestroyImmediate(tex);
            }
        }

        if (textureArray != null)
        {
            string arrayPath = Path.Combine(folderPath, $"{this.name}_{arrayName}Array.asset");
            AssetDatabase.CreateAsset(textureArray, arrayPath);
            return AssetDatabase.LoadAssetAtPath<Texture2DArray>(arrayPath);
        }

        return null;
    }
#endif
}