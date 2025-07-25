using System.Reflection;
using System.Linq;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

namespace TextureArrayEssentials.CustomWindows
{
    using GUIUtilities;

    public class CreateArrayWindow : Texture2DArrayWindowBase
    {
        private string _fileName = "TextureArray";

        protected static void ShowWindow()
        {
            CreateArrayWindow window = GetWindow<CreateArrayWindow>(false, "Create Array");
            window.Show();
        }

#if UNITY_2022_3_OR_NEWER
        [MenuItem("Assets/Texture Array Utilities/Create Array", priority = 40, secondaryPriority = 0)]
#else
    [MenuItem("Assets/Texture Array Utilities/Create Array", priority = 40)]
#endif
        static void CreateArrayAssets(MenuCommand command)
        {
            ShowWindow();
        }

#if UNITY_2022_3_OR_NEWER
        [MenuItem("Tools/Texture Array Essentials/Create Array", secondaryPriority = 0)]
#else
        [MenuItem("Tools/Texture Array Essentials/Create Array")]
#endif
        static void CreateArray()
        {
            ShowWindow();
        }

        protected override void CreateGUI()
        {
            base.CreateGUI();

            // Get current selection, if any are textures automatically prep them for array creation
            Object[] selectedTextures = Selection.objects.Where(x => x is Texture2D).ToArray();
            if (selectedTextures.Length > 0) {
                _textures.Clear();
                _texturesResizing.Clear();
                _textureErrors.Clear();

                for (int i = 0; i < selectedTextures.Length; i++) {
                    _textures.Add((Texture2D)selectedTextures[i]);
                    _texturesResizing.Add(false);
                    _textureErrors.Add(false);
                }

                _arrayResolution = new Vector2Int(_textures[0].width, _textures[0].height);
            }
        }

        protected override void OnGUIUpdate()
        {
            base.OnGUIUpdate();

            GUILayout.Space(10);

            // Dont allow array creation if errors exist
            if (_textureErrors.Contains(true)) {
                GUI.enabled = false;
            }

            // Dont allow array creation if no textures are assigned
            if (!ArrayTexturesExist()) {
                GUI.enabled = false;
            }

            // Create array button
            if (GUILayout.Button("Create Array", GUILayout.Height(GUIUtilities.LINE_HEIGHT * 2))) {
                CreateArrayWithPath();
            }

            GUI.enabled = true;
        }

        protected override void DrawFinalOutputFields()
        {
            base.DrawFinalOutputFields();

            _fileName = EditorGUILayout.TextField(new GUIContent("File Name"), _fileName);
        }

        protected override void DisplayWarnings()
        {
            base.DisplayWarnings();

            // Display error if errors exist
            if (_textureErrors.Contains(true)) {
                EditorGUILayout.HelpBox($"Cannot create texture array, {(_textureErrors.Count(x => x == true) == 1 ? "a texture" : "textures")} still need resolving", MessageType.Error);
            }

            // Display error if no textures are assigned
            if (!ArrayTexturesExist()) {
                EditorGUILayout.HelpBox("Cannot create texture array, no textures are assigned", MessageType.Error);
                GUI.enabled = false;
            }

        }

        private void CreateArrayWithPath()
        {
            // Get current folder that user is viewing in project files
            MethodInfo tryGetActiveFolderPath = typeof(ProjectWindowUtil).GetMethod("TryGetActiveFolderPath", BindingFlags.Static | BindingFlags.NonPublic);

            object[] args = new object[] { null };
            bool found = (bool)tryGetActiveFolderPath.Invoke(null, args);
            string path = (string)args[0];

            // If cannot get folder resort to project root
            if (!found)
                path = "Assets";

            // Add filename to path
            path += $"/{_fileName}.asset";

            // Check if file already exists at target path
            string fullPath = $"{Application.dataPath.Substring(0, Application.dataPath.Length - 6)}{path}"; // Remove ending "Assets" from Application.dataPath
            if (File.Exists(fullPath)) {
                // Let user determine if overwriting
                bool overwrite = EditorUtility.DisplayDialog("File Exists", $"A file at \"{path}\" already exists. Would you like to overwrite the file?", "Overwrite", "Cancel");
                if (!overwrite)
                    return;

                // Delete file if overwriting
                AssetDatabase.DeleteAsset(path);
            }

            // Create array and if successful close window
            if (CreateArray(path))
                Close();
        }
    }
}
#endif