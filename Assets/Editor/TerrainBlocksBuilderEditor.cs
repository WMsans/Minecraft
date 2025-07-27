using UnityEngine;
using UnityEditor;
using TextureArrayEssentials.GUIUtilities;

[CustomEditor(typeof(TerrainBlocksBuilder))]
public class TerrainBlocksBuilderEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        TerrainBlocksBuilder builder = (TerrainBlocksBuilder)target;

        GUILayout.Space(20);

        if (GUILayout.Button("Generate Texture Arrays", GUILayout.Height(GUIUtilities.LINE_HEIGHT * 2)))
        {
            builder.GenerateTextureArrays();
        }
    }
}