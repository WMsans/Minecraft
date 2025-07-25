using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

// Using the namespace to avoid potential conflicts, your script does not need a namespace
namespace TextureArrayEssentials.Samples
{
    using GUIUtilities;

    public class ArrayComparisonExampleEditor : ShaderGUI
    {
        MaterialEditor _editor;
        MaterialProperty[] _properties;

        TextureArrayGUIDrawer _arrayDrawer;

        // ShaderGUI doesnt have an OnEnable function, using this instead
        private bool _firstSetup = true;

        private static GUIStyle _headerStyle = null;
        private static GUIStyle _subtitleStyle = null;

        private MaterialProperty FindProperty(string propertyName)
        {
            return FindProperty(propertyName, _properties);
        }

        private void OnEnable(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            _editor = materialEditor;
            _properties = properties;

            _headerStyle = new GUIStyle("label");
            _headerStyle.fontStyle = FontStyle.Bold;
            _headerStyle.fontSize = 16;
            _headerStyle.alignment = TextAnchor.MiddleCenter;

            _subtitleStyle = new GUIStyle("label");
            _subtitleStyle.alignment = TextAnchor.MiddleCenter;
            _subtitleStyle.wordWrap = true;

            _arrayDrawer = new TextureArrayGUIDrawer(FindProperty("_Array"), FindProperty("_ArrayTexturesAssigned"), 4);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            // OnEnable if first call
            if (_firstSetup) {
                OnEnable(materialEditor, properties);
                _firstSetup = false;
            }

            // Top Textures Config
            GUIUtilities.BeginBackgroundVertical();
            EditorGUILayout.LabelField("Regular Textures", _headerStyle, GUILayout.Height(20));
            EditorGUILayout.LabelField("These textures are assigned individually into the shader, using 4 different properties, one for each texture", _subtitleStyle);

            GUILayout.Space(10);

            materialEditor.TexturePropertySingleLine(new GUIContent("Texture 1"), FindProperty("_1"));
            materialEditor.TexturePropertySingleLine(new GUIContent("Texture 2"), FindProperty("_2"));
            materialEditor.TexturePropertySingleLine(new GUIContent("Texture 3"), FindProperty("_3"));
            materialEditor.TexturePropertySingleLine(new GUIContent("Texture 4"), FindProperty("_4"));

            GUIUtilities.EndBackgroundVertical();
            GUILayout.Space(20);

            // Texture Array Config
            GUIUtilities.BeginBackgroundVertical();
            EditorGUILayout.LabelField("Texture Array", _headerStyle, GUILayout.Height(20));
            EditorGUILayout.LabelField("Changing these textures will automatically update the texture array and the material accordingly, using only two properties in the shader for each Array in which this example only uses one.", _subtitleStyle);

            GUILayout.Space(10);

            // Clear array button
            if (GUILayout.Button(new GUIContent("Clear Texture Array")))
                _arrayDrawer.DeleteArray();

            // Draw textures
            _arrayDrawer.DrawTextures();

            // Draw array property
            GUILayout.Space(10);
            materialEditor.TextureProperty(FindProperty("_Array"), "Array");

            GUIUtilities.EndBackgroundVertical();
        }
    }
}

#endif