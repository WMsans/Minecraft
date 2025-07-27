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
        if (Data.layerType == "PerlinNoiseTerrainLayer")
        {
            var scaleField = new FloatField("Noise Scale") { value = Data.properties[0] };
            var strengthField = new FloatField("Noise Strength") { value = Data.properties[1] };
            scaleField.RegisterValueChangedCallback(evt => Data.properties[0] = evt.newValue);
            strengthField.RegisterValueChangedCallback(evt => Data.properties[1] = evt.newValue);
            extensionContainer.Add(scaleField);
            extensionContainer.Add(strengthField);
        }
        else if (Data.layerType == "BlockTypeTerrainLayer")
        {
            var stoneField = new FloatField("Stone Level") { value = Data.properties[0] };
            var dirtField = new FloatField("Dirt Level") { value = Data.properties[1] };
            stoneField.RegisterValueChangedCallback(evt => Data.properties[0] = evt.newValue);
            dirtField.RegisterValueChangedCallback(evt => Data.properties[1] = evt.newValue);
            extensionContainer.Add(stoneField);
            extensionContainer.Add(dirtField);
        }
    }
}