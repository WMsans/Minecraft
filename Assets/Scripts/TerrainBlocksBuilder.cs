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
    }

    [Header("Source Textures")]
    [Tooltip("Define all block types to be included in the texture arrays.")]
    public List<BlockAttributes> Blocks = new List<BlockAttributes>();

    [Header("Generated Texture Arrays")]
    public Texture2DArray AlbedoArray;
    public Texture2DArray NormalArray;
    public Texture2DArray SmoothnessArray;
    public Texture2DArray AmbientOcclusionArray;

    /// <summary>
    /// Generates the Texture2DArrays for Albedo, Normal, Roughness, and Ambient Occlusion.
    /// </summary>
    public void GenerateTextureArrays()
    {
#if UNITY_EDITOR
        if (Blocks.Count == 0)
        {
            Debug.LogWarning("No blocks defined. Cannot generate texture arrays.");
            return;
        }

        // Get the path of the ScriptableObject asset
        string assetPath = AssetDatabase.GetAssetPath(this);
        string assetDirectory = Path.GetDirectoryName(assetPath);
        string folderName = $"{this.name}_GeneratedArrays";
        string folderPath = Path.Combine(assetDirectory, folderName);

        // Create a subfolder for the generated arrays if it doesn't exist
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(assetDirectory, folderName);
        }

        // Generate each texture array
        AlbedoArray = CreateAndSaveArray("Albedo", folderPath, Blocks.Select(b => b.Albedo).ToList(), TextureFormat.DXT5);
        NormalArray = CreateAndSaveArray("Normal", folderPath, Blocks.Select(b => b.Normal).ToList(), TextureFormat.RGBA32, true);
        SmoothnessArray = CreateAndSaveArray("Smoothness", folderPath, Blocks.Select(b => b.Smoothness).ToList(), TextureFormat.BC4);
        AmbientOcclusionArray = CreateAndSaveArray("AmbientOcclusion", folderPath, Blocks.Select(b => b.AmbientOcclusion).ToList(), TextureFormat.BC4);

        // Mark the ScriptableObject as dirty to save the changes
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Texture arrays generated successfully!");
#endif
    }

#if UNITY_EDITOR
    private Texture2DArray CreateAndSaveArray(string arrayName, string folderPath, List<Texture2D> textures, TextureFormat format, bool linear = false)
    {
        if (textures.Any(t => t == null))
        {
            Debug.LogWarning($"Cannot create {arrayName} array, one or more textures are missing.");
            return null;
        }

        // Find the smallest resolution
        int minWidth = textures.Min(t => t.width);
        int minHeight = textures.Min(t => t.height);

        // Ensure all textures are readable and resize them to the smallest resolution
        List<Texture2D> resizedTextures = new List<Texture2D>();
        foreach (var texture in textures)
        {
            TextureUtilities.SetReadable(texture);
            if (texture.width != minWidth || texture.height != minHeight)
            {
                resizedTextures.Add(TextureUtilities.ResizeTexture(texture, minWidth, minHeight));
            }
            else
            {
                resizedTextures.Add(texture);
            }
        }

        Texture2DArray textureArray = Texture2DArrayUtilities.CreateArray(resizedTextures.ToArray(), format, true, linear);

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