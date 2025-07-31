using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(TerrainGraph))]
public class TerrainGraphEditor : Editor
{
    public override VisualElement CreateInspectorGUI()
    {
        var container = new VisualElement();
        var graph = (TerrainGraph)target;

        // --- Custom Buttons ---
        var openButton = new Button(() =>
        {
            var window = EditorWindow.GetWindow<TerrainGraphWindow>();
            window.titleContent = new GUIContent("Terrain Graph");
            window.Initialize(graph);
        })
        {
            text = "Open Terrain Graph"
        };
        container.Add(openButton);

        var registerButton = new Button(() =>
        {
            graph.FindAndRegisterLayers();
        })
        {
            text = "Register Layers"
        };
        container.Add(registerButton);
        
        // Add a visual separator
        var separator = new VisualElement()
        {
            style =
            {
                height = 2,
                backgroundColor = new StyleColor(new Color(0.3f, 0.3f, 0.3f)),
                marginTop = 10,
                marginBottom = 10
            }
        };
        container.Add(separator);

        // --- Read-only Default Inspector ---
        var defaultInspector = new IMGUIContainer(() =>
        {
            // By wrapping DrawDefaultInspector in a disabled group, the fields become read-only.
            EditorGUI.BeginDisabledGroup(true);
            DrawDefaultInspector();
            EditorGUI.EndDisabledGroup();
        });
        container.Add(defaultInspector);

        return container;
    }
}