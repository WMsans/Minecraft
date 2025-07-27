using UnityEditor;
using UnityEngine.UIElements;

public class TerrainGraphWindow : EditorWindow
{
    private TerrainGraphView _graphView;

    public void Initialize(TerrainGraph graph)
    {
        // Each time a graph is opened, create a new view for it
        _graphView = new TerrainGraphView(graph)
        {
            name = "Terrain Graph View",
        };
        _graphView.StretchToParentSize();
        rootVisualElement.Clear();
        rootVisualElement.Add(_graphView);
    }
}