using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;
using EditorAttributes;
using TextureArrayEssentials;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(fileName = "TerrainBlocksBuilder", menuName = "Terrain/Terrain Blocks Builder")]
public class TerrainBlocksBuilder : ScriptableObject
{
    [Header("Target Material")]
    [Tooltip("The material to which the generated texture arrays will be applied.")]
    public Material TargetMaterial;

    [Header("Source Textures")]
    [Tooltip("Define all block types to be included in the texture arrays.")]
    public List<BlockAttributes> Blocks = new List<BlockAttributes>();

    [Serializable]
    public struct BlockAttributes
    {
        [Tooltip("A descriptive name for the block (e.g., Grass, Stone, Dirt).")]
        public string Name;

        [Header("Side Textures")]
        public Texture2D Albedo;
        public Texture2D Normal;
        public Texture2D Smoothness;
        [Range(0f, 1f)]
        public float Metallic; // Metallic value for side faces
        public Texture2D AmbientOcclusion;
        
        [Header("Top Textures (Optional)")]
        [Tooltip("Assign all four top textures to use them. Otherwise, the side textures will be used for the top face.")]
        public Texture2D TopAlbedo;
        public Texture2D TopNormal;
        public Texture2D TopSmoothness;
        [Range(0f, 1f)]
        public float TopMetallic; // Metallic value for top faces
        public Texture2D TopAmbientOcclusion;
        
        /// <summary>
        /// Determines if dedicated top textures should be used for this block.
        /// </summary>
        /// <returns>True if all top texture fields are assigned, false otherwise.</returns>
        public bool HasSeparateTopTextures() => TopAlbedo != null && TopNormal != null && TopSmoothness != null && TopAmbientOcclusion != null;
    }

    [Header("Generated Side Texture Arrays")]
    public Texture2DArray SideAlbedoArray;
    public Texture2DArray SideNormalArray;
    public Texture2DArray SideSmoothnessArray;
    public Texture2DArray SideAmbientOcclusionArray;
    
    [Header("Generated Top Texture Arrays")]
    [Tooltip("Top arrays are generated for all blocks. If a block lacks dedicated top textures, its side textures are used as a fallback.")]
    public Texture2DArray TopAlbedoArray;
    public Texture2DArray TopNormalArray;
    public Texture2DArray TopSmoothnessArray;
    public Texture2DArray TopAmbientOcclusionArray;

    /// <summary>
    /// Generates separate Texture2DArrays for side and top textures and applies them to the Target Material.
    /// </summary>
    [VInspector.Button("Generate and Apply Texture Arrays")]
    public void GenerateAndApplyTextureArrays()
    {
#if UNITY_EDITOR
        if (Blocks.Count == 0)
        {
            Debug.LogWarning("No blocks defined. Cannot generate texture arrays.");
            return;
        }

        string assetPath = AssetDatabase.GetAssetPath(this);
        string assetDirectory = Path.GetDirectoryName(assetPath);
        string folderName = $"{name}_GeneratedArrays";
        string folderPath = Path.Combine(assetDirectory, folderName);

        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder(assetDirectory, folderName);
        }

        // --- Build Side Texture Lists ---
        var sideAlbedoTextures = Blocks.Select(b => b.Albedo).ToList();
        var sideNormalTextures = Blocks.Select(b => b.Normal).ToList();
        var sideAoTextures = Blocks.Select(b => b.AmbientOcclusion).ToList();
        var sideSmoothnessTextures = Blocks.Select(b => b.Smoothness).ToList();
        var sideMetallicValues = Blocks.Select(b => b.Metallic).ToList();

        // --- Build Top Texture Lists (with fallbacks to side textures) ---
        var topAlbedoTextures = Blocks.Select(b => b.HasSeparateTopTextures() ? b.TopAlbedo : b.Albedo).ToList();
        var topNormalTextures = Blocks.Select(b => b.HasSeparateTopTextures() ? b.TopNormal : b.Normal).ToList();
        var topAoTextures = Blocks.Select(b => b.HasSeparateTopTextures() ? b.TopAmbientOcclusion : b.AmbientOcclusion).ToList();
        var topSmoothnessTextures = Blocks.Select(b => b.HasSeparateTopTextures() ? b.TopSmoothness : b.Smoothness).ToList();
        var topMetallicValues = Blocks.Select(b => b.HasSeparateTopTextures() ? b.TopMetallic : b.Metallic).ToList();
        
        // --- Create and Save SIDE Arrays ---
        SideAlbedoArray = CreateAndSaveArray("SideAlbedo", folderPath, sideAlbedoTextures, TextureFormat.DXT1, false);
        SideNormalArray = CreateAndSaveNormalArray("SideNormal", folderPath, sideNormalTextures);
        SideSmoothnessArray = CreateAndSaveMetallicSmoothnessArray("SideSmoothness", folderPath, sideSmoothnessTextures, sideMetallicValues);
        SideAmbientOcclusionArray = CreateAndSaveArray("SideAmbientOcclusion", folderPath, sideAoTextures, TextureFormat.DXT1, false);
        
        // --- Create and Save TOP Arrays ---
        TopAlbedoArray = CreateAndSaveArray("TopAlbedo", folderPath, topAlbedoTextures, TextureFormat.DXT1, false);
        TopNormalArray = CreateAndSaveNormalArray("TopNormal", folderPath, topNormalTextures);
        TopSmoothnessArray = CreateAndSaveMetallicSmoothnessArray("TopSmoothness", folderPath, topSmoothnessTextures, topMetallicValues);
        TopAmbientOcclusionArray = CreateAndSaveArray("TopAmbientOcclusion", folderPath, topAoTextures, TextureFormat.DXT1, false);
        
        EditorUtility.SetDirty(this);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Texture arrays generated successfully! ✨ Attempting to apply to material...");
        
        // --- Apply Generated Arrays to Target Material ---
        ApplyArraysToMaterial();
#endif
    }
    
#if UNITY_EDITOR
    /// <summary>
    /// Applies the generated texture arrays to the TargetMaterial.
    /// </summary>
    private void ApplyArraysToMaterial()
    {
        if (TargetMaterial == null)
        {
            Debug.LogWarning("Target Material not set. Skipping auto-apply.");
            return;
        }

        // Based on your shader graph image, we map the generated arrays to the material's properties.
        // The property names in Shader Graph often start with an underscore.
        TargetMaterial?.SetTexture("_MainTexArray", SideAlbedoArray);
        TargetMaterial?.SetTexture("_NormalArray", SideNormalArray);
        TargetMaterial?.SetTexture("_AOArray", SideAmbientOcclusionArray);
        TargetMaterial?.SetTexture("_SmoothnessArray", SideSmoothnessArray);
        
        TargetMaterial?.SetTexture("_TopMainTexArray", TopAlbedoArray);
        TargetMaterial?.SetTexture("_TopNormalArray", TopNormalArray);
        TargetMaterial?.SetTexture("_TopAOArray", TopAmbientOcclusionArray);
        TargetMaterial?.SetTexture("_TopSmoothnessArray", TopSmoothnessArray);
        
        EditorUtility.SetDirty(TargetMaterial);
        Debug.Log($"Successfully applied texture arrays to '{TargetMaterial.name}'! ✅");
    }

    /// <summary>
    /// Specialized function to create and save a Normal Map Texture2DArray.
    /// </summary>
    private Texture2DArray CreateAndSaveNormalArray(string arrayName, string folderPath, List<Texture2D> textures)
    {
        // Use DXT5 for normals as it's better for gradients.
        Texture2DArray normalArray = CreateAndSaveArray(arrayName, folderPath, textures, TextureFormat.DXT5, true);

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
    /// Creates a Texture2DArray from a list of smoothness textures and metallic values, saves it, and returns it.
    /// The smoothness texture (grayscale) is stored in the RGB channels and the metallic value is stored in the Alpha channel.
    /// </summary>
    private Texture2DArray CreateAndSaveMetallicSmoothnessArray(string arrayName, string folderPath, List<Texture2D> textures, List<float> metallicValues)
    {
        if (textures.Any(t => t == null))
        {
            Debug.LogWarning($"Cannot create {arrayName} array, one or more smoothness textures are missing. Please check all block attributes.");
            return null;
        }

        int minWidth = textures.Min(t => t.width);
        int minHeight = textures.Min(t => t.height);

        var processedTextures = new List<Texture2D>();
        for (int i = 0; i < textures.Count; i++)
        {
            var sourceTex = textures[i];
            var metallic = metallicValues[i];

            TextureUtilities.SetReadable(sourceTex);

            var combinedTex = new Texture2D(sourceTex.width, sourceTex.height, TextureFormat.RGBA32, false, false);
            Color[] pixels = sourceTex.GetPixels();
            var newPixels = new Color[pixels.Length];
            
            if(arrayName == "TopSmoothness") Debug.Log(metallic);

            for (int j = 0; j < pixels.Length; j++)
            {
                // Smoothness is grayscale, so we use its 'r' channel for the new RGB values.
                // The metallic value is stored in the alpha channel.
                float smoothness = pixels[j].r;
                newPixels[j] = new Color(smoothness, metallic, smoothness, metallic);
            }
            
            combinedTex.SetPixels(newPixels);
            combinedTex.Apply();

            Texture2D finalTex = combinedTex;
            if (combinedTex.width != minWidth || combinedTex.height != minHeight)
            {
                finalTex = TextureUtilities.ResizeTexture(combinedTex, minWidth, minHeight);
                DestroyImmediate(combinedTex);
            }
            
            processedTextures.Add(finalTex);
        }

        Texture2DArray textureArray = Texture2DArrayUtilities.CreateArray(processedTextures.ToArray(), TextureFormat.DXT5, true, false);
        
        foreach (var tex in processedTextures)
        {
            DestroyImmediate(tex);
        }

        if (textureArray != null)
        {
            string arrayPath = Path.Combine(folderPath, $"{name}_{arrayName}Array.asset");
            AssetDatabase.CreateAsset(textureArray, arrayPath);
            return AssetDatabase.LoadAssetAtPath<Texture2DArray>(arrayPath);
        }

        return null;
    }

    /// <summary>
    /// Creates a Texture2DArray from a list of textures, saves it as an asset, and returns it.
    /// </summary>
    private Texture2DArray CreateAndSaveArray(string arrayName, string folderPath, List<Texture2D> textures, TextureFormat format, bool linear = false)
    {
        if (textures.Any(t => t == null))
        {
            Debug.LogWarning($"Cannot create {arrayName} array, one or more textures are missing. Please check all block attributes.");
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

            processedTextures.Add(processedTex);
        }

        Texture2DArray textureArray = Texture2DArrayUtilities.CreateArray(processedTextures.ToArray(), format, true, linear);
        
        foreach (var tex in processedTextures)
        {
            if (!textures.Contains(tex))
            {
                DestroyImmediate(tex);
            }
        }

        if (textureArray != null)
        {
            string arrayPath = Path.Combine(folderPath, $"{name}_{arrayName}Array.asset");
            AssetDatabase.CreateAsset(textureArray, arrayPath);
            return AssetDatabase.LoadAssetAtPath<Texture2DArray>(arrayPath);
        }

        return null;
    }
#endif
}