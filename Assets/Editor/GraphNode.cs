using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using System.Linq;
using System.Collections.Generic;

public class GraphNode : Node
{
    public NodeData Data { get; }
    public List<Port> inputPorts = new List<Port>();
    public List<Port> outputPorts = new List<Port>();
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

        CreatePorts();
        AddPropertyFields();

        RefreshExpandedState();
        RefreshPorts();
    }

    private void CreatePorts()
    {
        var inputPortNames = _graph.GetInputPortNames(Data.layerType);
        foreach (var portName in inputPortNames)
        {
            var port = Port.Create<Edge>(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(float));
            port.portName = portName;
            inputContainer.Add(port);
            inputPorts.Add(port);
        }

        var outputPortNames = _graph.GetOutputPortNames(Data.layerType);
        foreach (var portName in outputPortNames)
        {
            var port = Port.Create<Edge>(Orientation.Horizontal, Direction.Output, Port.Capacity.Multi, typeof(float));
            port.portName = portName;
            outputContainer.Add(port);
            outputPorts.Add(port);
        }
    }

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