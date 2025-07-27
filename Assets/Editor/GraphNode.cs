using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;

public class GraphNode : Node
{
    public NodeData Data { get; }
    public Port inputPort;
    public Port outputPort;

    public GraphNode(NodeData data) : base()
    {
        this.Data = data;
        this.title = data.layerType.Replace("Layer", " ");
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
        title = Data.layerType.Replace("Layer", " ");
        if (isRoot)
        {
            title += " (Root)";
        }
    }

    private void AddPropertyFields()
    {
        // Add fields based on layer type
        var fields = TerrainLayerRegistry.Instance.GetPropertiesName(Data.layerType);
        for (var i = 0; i < fields.Length;i++)
        {
            var field = fields[i];
            var floatField = new FloatField(field) { value = Data.properties[i] };
            floatField.RegisterValueChangedCallback(evt => Data.properties[i] = evt.newValue);
            extensionContainer.Add(floatField);
        }
    }
}