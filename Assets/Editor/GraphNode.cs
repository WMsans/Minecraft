using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System.Linq;

public class GraphNode : Node
{
    public NodeData Data { get; }
    public Port inputPort;
    public Port outputPort;
    private readonly TerrainGraph _graph;

    public GraphNode(NodeData data, TerrainGraph graph) : base()
    {
        this.Data = data;
        this._graph = graph;
        var friendlyName = data.layerType.Split(',')[0].Split('.').Last();
        this.title = friendlyName.Replace("Layer", " ");
        this.viewDataKey = data.guid;

        style.left = data.position.x;
        style.top = data.position.y;

        inputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
        inputPort.portName = "In";
        inputContainer.Add(inputPort);

        outputPort = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
        outputPort.portName = "Out";
        outputContainer.Add(outputPort);

        AddPropertyFields();

        RefreshExpandedState();
        RefreshPorts();
    }

    // New method to update the node's title to indicate if it's the root
    public void SetAsRoot(bool isRoot)
    {
        var friendlyName = Data.layerType.Split(',')[0].Split('.').Last();
        title = friendlyName.Replace("Layer", " ");
        if (isRoot)
        {
            title += " (Root)";
        }
    }

    private void AddPropertyFields()
    {
        // Add fields based on layer type
        var fields = _graph.GetPropertyNames(Data.layerType);
        for (var i = 0; i < fields.Length;i++)
        {
            var field = fields[i];
            var floatField = new FloatField(field) { value = Data.properties[i] };
            int index = i;
            floatField.RegisterValueChangedCallback(evt => Data.properties[index] = evt.newValue);
            extensionContainer.Add(floatField);
        }
    }
}