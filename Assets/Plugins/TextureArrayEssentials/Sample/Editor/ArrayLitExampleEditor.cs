using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

// Using the namespace to avoid potential conflicts, your script does not need a namespace
namespace TextureArrayEssentials.Samples
{
    using GUIUtilities;

    public class ArrayLitExampleEditor : ShaderGUI
    {
        // Editor
        MaterialEditor _editor;
        MaterialProperty[] _properties;

        // GUI
        TextureArrayGUIDrawer _arrayDrawer;

        private static GUIStyle _headerStyle = null;
        private static GUIStyle _subtitleStyle = null;

        // ShaderGUI doesnt have an OnEnable function, using this instead
        private bool _firstSetup = true;

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

            _arrayDrawer = new TextureArrayGUIDrawer(FindProperty("_Textures"), FindProperty("_TexturesAssigned"), 5);
        }

        public override void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
        {
            // OnEnable if first call
            if (_firstSetup) {
                OnEnable(materialEditor, properties);
                _firstSetup = false;
            }

            // Get material properties
            MaterialProperty colorProperty = FindProperty("_Color");
            MaterialProperty metalicProperty = FindProperty("_Metallic");
            MaterialProperty smoothnessProperty = FindProperty("_Smoothness");
            MaterialProperty normalMapProperty = FindProperty("_Normal");
            MaterialProperty normalStrengthProperty = FindProperty("_NormalStrength");
            MaterialProperty occlussionProperty = FindProperty("_Occlussion");
            MaterialProperty emissionEnabledProperty = FindProperty("_EmissionEnabled");
            MaterialProperty emissionProperty = FindProperty("_Emission");

            // Header
            GUIUtilities.BeginBackgroundVertical();
            EditorGUILayout.LabelField("Lit Example", _headerStyle, GUILayout.Height(20));
            EditorGUILayout.LabelField("All the textures are stored in a Texture2DArray saving multiple Texture properties in the shader. This example also shows some of the different accompanying variables that can be drawn with a texture through the TextureArrayGUIDrawer without any extra work.", _subtitleStyle);
            GUILayout.Space(10);
            EditorGUILayout.LabelField("Note that the normal map is saved into its own texture as storing it in a texture array can produce unwanted results", _subtitleStyle);
            GUILayout.Space(10);
            EditorGUILayout.LabelField("The normals may also appear different to the regular lit example on the right due to how normals are calculated differently in the shader graph", _subtitleStyle);

            GUILayout.Space(20);

            // Albedo
            colorProperty.colorValue = (_arrayDrawer.DrawTextureWithColor(0, colorProperty.colorValue, false, new GUIContent("Albedo"))).Item2;
            
            // Metallic
            if (_arrayDrawer.TextureAssignedAt(1))
                _arrayDrawer.DrawTexture(1, new GUIContent("Metallic"));
            else
                metalicProperty.floatValue = (_arrayDrawer.DrawTextureWithSlider(1, metalicProperty.floatValue, 0, 1, new GUIContent("Metallic"))).Item2;
            
            // Smoothness
            if (_arrayDrawer.TextureAssignedAt(2))
                _arrayDrawer.DrawTexture(2, new GUIContent("Smoothness"));
            else
                smoothnessProperty.floatValue = (_arrayDrawer.DrawTextureWithSlider(2, smoothnessProperty.floatValue, 0, 1, new GUIContent("Smoothness"))).Item2;

            // Normal
            Texture2D normal = (Texture2D)normalMapProperty.textureValue;
            float normalStrength = normalStrengthProperty.floatValue;
            if (normal == null)
                normal = GUIUtilities.DrawTexture(normal, new GUIContent("Normal"));
            else {
                (Texture2D, float) input = GUIUtilities.DrawTextureWithFloat(normal, normalStrength, new GUIContent("Normal"));
                normal = input.Item1;
                normalStrength = input.Item2;
            }

            normalMapProperty.textureValue = normal;
            normalStrengthProperty.floatValue = normalStrength;
            
            // Occlussion
            if (_arrayDrawer.TextureAssignedAt(3))
                occlussionProperty.floatValue = (_arrayDrawer.DrawTextureWithSlider(3, occlussionProperty.floatValue, 0, 1, new GUIContent("Occlussion"))).Item2;
            else
                _arrayDrawer.DrawTexture(3, new GUIContent("Occlussion"));

            // Emission
            emissionEnabledProperty.floatValue = EditorGUILayout.Toggle("Emission", emissionEnabledProperty.floatValue != 0) ? 1.0f : 0.0f;
            if (emissionEnabledProperty.floatValue != 0)
                emissionProperty.colorValue = (_arrayDrawer.DrawTextureWithColor(4, emissionProperty.colorValue, true, new GUIContent("Color"))).Item2;
            
            GUIUtilities.EndBackgroundVertical();
        }
    }
}

#endif