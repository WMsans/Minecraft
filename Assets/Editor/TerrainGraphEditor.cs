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

        var button = new Button(() =>
        {
            var window = EditorWindow.GetWindow<TerrainGraphWindow>();
            window.titleContent = new GUIContent("Terrain Graph");
            window.Initialize(graph);
        });

        button.text = "Open Terrain Graph";
        container.Add(button);

        return container;
    }
}