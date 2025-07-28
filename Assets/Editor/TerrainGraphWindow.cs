using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

public class TerrainGraphWindow : EditorWindow
{
    private TerrainGraphView _graphView;
    private TerrainGraph _graph;

    public void Initialize(TerrainGraph graph)
    {
        _graph = graph;
        titleContent = new GUIContent("Terrain Graph - " + _graph.name);

        // Each time a graph is opened, create a new view for it
        _graphView = new TerrainGraphView(graph)
        {
            name = "Terrain Graph View",
        };
        _graphView.StretchToParentSize();
        rootVisualElement.Clear();
        rootVisualElement.Add(_graphView);
    }

    private void OnEnable()
    {
        // When the window is enabled (e.g., after a recompile),
        // if we have a graph, reinitialize the view.
        if (_graph != null)
        {
            Initialize(_graph);
        }
    }
}