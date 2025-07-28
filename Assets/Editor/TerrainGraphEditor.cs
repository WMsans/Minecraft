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

        var openButton = new Button(() =>
        {
            var window = EditorWindow.GetWindow<TerrainGraphWindow>();
            window.titleContent = new GUIContent("Terrain Graph");
            window.Initialize(graph);
        });
        openButton.text = "Open Terrain Graph";
        container.Add(openButton);
        
        var registerButton = new Button(() =>
        {
            graph.FindAndRegisterLayers();
        });
        registerButton.text = "Register Layers";
        container.Add(registerButton);

        return container;
    }
}