using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace sapra.InfiniteLands.Editor
{
    [CustomEditor(typeof(TerrainGenerator), true)]
    [CanEditMultipleObjects]
    internal class TerrainGeneratorEditor : UnityEditor.Editor
    {
        Dictionary<string, bool> textureFoldouts = new Dictionary<string, bool>();
        bool textureFoldoutFinal;
        bool validated;

        [InitializeOnLoadMethod]
        static void OnLoad()
        {
            DragAndDrop.AddDropHandler(OnSceneDrop);
            DragAndDrop.AddDropHandler(OnHierarchyDrop);
        }

        private static DragAndDropVisualMode OnSceneDrop(Object dropUpon, Vector3 worldPosition, Vector2 viewportPosition, Transform parentForDraggedObjects, bool perform)
        {
            // Early exit if not performing (preview phase)
            if (!perform)
            {
                return CanDrop() 
                    ? DragAndDropVisualMode.Move 
                    : DragAndDropVisualMode.None;
            }

            // Perform phase: validate and act
            if (CanDrop())
            {
                CreateVisualizer(dropUpon as GameObject, worldPosition, parentForDraggedObjects);
                return DragAndDropVisualMode.Move; // Accept the drop
            }

            return DragAndDropVisualMode.None; // Let other handlers try
        }

        private static DragAndDropVisualMode OnHierarchyDrop(int dropTargetInstanceID, HierarchyDropFlags dropMode, Transform parentForDraggedObjects, bool perform)
        {
            // Early exit if not performing (preview phase)
            if (!perform)
            {
                return CanDrop() 
                    ? DragAndDropVisualMode.Move 
                    : DragAndDropVisualMode.None;
            }

            // Perform phase: validate and act
            if (CanDrop())
            {
                CreateVisualizer(null, Vector3.zero, parentForDraggedObjects);
                return DragAndDropVisualMode.Move; // Accept the drop
            }

            return DragAndDropVisualMode.None; // Let other handlers try
        }

        private static bool CanDrop()
        {
            return DragAndDrop.objectReferences.OfType<TerrainGenerator>().Any(); 
        }
        public static void CreateVisualizer(GameObject droppedUpon, Vector3 worldPosition, Transform parentForDraggedObjects){
            var terrains = DragAndDrop.objectReferences.OfType<TerrainGenerator>();
            InfiniteLandsTerrain dhcomponent = droppedUpon?.GetComponent<InfiniteLandsTerrain>();
            if(dhcomponent != null){
                var first = terrains.FirstOrDefault();
                if(first != null)
                    dhcomponent.ChangeGenerator(first);
            }
            else{
                foreach(var targetTerrain in DragAndDrop.objectReferences.OfType<TerrainGenerator>()){
                    if(targetTerrain == null)
                        return;
                    InfiniteLandsTerrain createdTerrain = CreateInstance(targetTerrain, worldPosition, parentForDraggedObjects);
                    createdTerrain.ChangeGenerator(targetTerrain);
                }
            }
        }
        
        public static InfiniteLandsTerrain CreateInstance(Object targetTerrain, Vector3 worldPosition, Transform parent){
            var go = new GameObject(targetTerrain.name);
            var dhcomponent = go.AddComponent<InfiniteLandsTerrain>();

            go.transform.SetParent(parent);
            worldPosition.y = 0;
            go.transform.position = worldPosition;
            return dhcomponent;
        }

        private void DeleteMissingNodes()
        {
            TerrainGenerator generator = target as TerrainGenerator;
            InfiniteLandsWindow.CloseWindow(generator);
            generator.nodes.RemoveAll(a => a == null);
            generator.nodes.RemoveAll(a => a.GetType().Equals(typeof(MissingNode)));
        }

        private void DeleteMissingConnections()
        {
            TerrainGenerator generator = target as TerrainGenerator;
            InfiniteLandsWindow.CloseWindow(generator);
            generator.ValidationCheck();
            DeleteMissingNodes();
            generator.edges.RemoveAll(a => a == null);
            generator.edges.RemoveAll(a => generator.GetNodeFromGUID(a.inputPort.nodeGuid) == null || generator.GetNodeFromGUID(a.outputPort.nodeGuid) == null);
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("Open Editor"))
            {
                InfiniteLandsWindow.OpenGraphAsset(target as TerrainGenerator);
            }

            if (GUILayout.Button("Delete Missing Nodes"))
            {
                DeleteMissingNodes();
            }

            if (GUILayout.Button("Delete Missing Connections"))
            {
                DeleteMissingConnections();
            }

            EditorGUI.BeginDisabledGroup(true);
            serializedObject.Update();
            base.OnInspectorGUI();
            serializedObject.ApplyModifiedProperties();
            EditorGUI.EndDisabledGroup();

            textureFoldoutFinal = EditorGUILayout.Foldout(textureFoldoutFinal, "Textures Used", true); // The foldout for the main section
            if (textureFoldoutFinal)
            {
                TerrainGenerator graph = target as TerrainGenerator;
                if (!validated)
                {
                    graph.ValidationCheck();
                    validated = true;
                }
                var textures = AssetDataHelper.GetAssets<IHoldTextures>(graph)
                    .SelectMany(a => a.GetTextures(), (asset, texture) => new { asset, texture }) // Access textures via IHoldTextures
                    .GroupBy(x => x.texture.GetTextureName()) // Group by texture name
                    .ToList(); // Materialize the result
                EditorGUI.indentLevel++;
                foreach (var texture in textures)
                {
                    // Add a foldout to toggle visibility for each texture group
                    textureFoldouts[texture.Key] = EditorGUILayout.Foldout(textureFoldouts.GetValueOrDefault(texture.Key, false), texture.Key);
                    if (textureFoldouts[texture.Key])
                    {
                        // Indentation for assets inside the texture group
                        EditorGUI.indentLevel++;

                        EditorGUI.BeginDisabledGroup(true);
                        foreach (var refre in texture)
                        {
                            EditorGUILayout.ObjectField(refre.asset as InfiniteLandsAsset, typeof(InfiniteLandsAsset), false);
                        }
                        EditorGUI.EndDisabledGroup();


                        // Reset indentation
                        EditorGUI.indentLevel--;
                    }

                    // Add some spacing between each group for clarity
                    EditorGUILayout.Space();
                }
                EditorGUI.indentLevel--;
            }
        }
    }
}