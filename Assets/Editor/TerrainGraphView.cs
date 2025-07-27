using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class TerrainGraphView : GraphView
{
    private readonly TerrainGraph _graph;

    public TerrainGraphView(TerrainGraph graph)
    {
        _graph = graph;

        this.AddManipulator(new ContentZoomer());
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        var grid = new GridBackground();
        Insert(0, grid);
        grid.StretchToParentSize();

        LoadGraph();

        graphViewChanged += OnGraphViewChanged;
    }

    private void LoadGraph()
    {
        // Create node views
        foreach (var nodeData in _graph.nodes)
        {
            CreateNodeView(nodeData);
        }

        // Create edge views
        foreach (var edgeData in _graph.edges)
        {
            var outputNodeView = GetNodeByGuid(edgeData.outputNodeGuid) as GraphNode;
            var inputNodeView = GetNodeByGuid(edgeData.inputNodeGuid) as GraphNode;

            if (outputNodeView != null && inputNodeView != null)
            {
                var edge = outputNodeView.outputPort.ConnectTo(inputNodeView.inputPort);
                AddElement(edge);
            }
        }
        
        // Update visuals after loading
        UpdateRootNodeVisuals();
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
    {
        // Handle element deletion
        if (graphViewChange.elementsToRemove != null)
        {
            foreach (var element in graphViewChange.elementsToRemove)
            {
                if (element is GraphNode nodeView)
                {
                    // If the root node is deleted, set it to null
                    if (_graph.rootNode == nodeView.Data)
                    {
                        _graph.rootNode = null;
                    }
                    _graph.nodes.Remove(nodeView.Data);
                }

                if (element is Edge edge)
                {
                    var outputNode = edge.output.node as GraphNode;
                    var inputNode = edge.input.node as GraphNode;
                    _graph.edges.RemoveAll(x => x.outputNodeGuid == outputNode.Data.guid && x.inputNodeGuid == inputNode.Data.guid);
                }
            }
        }

        // Handle edge creation
        if (graphViewChange.edgesToCreate != null)
        {
            foreach (var edge in graphViewChange.edgesToCreate)
            {
                var outputNode = edge.output.node as GraphNode;
                var inputNode = edge.input.node as GraphNode;
                _graph.edges.Add(new EdgeData { outputNodeGuid = outputNode.Data.guid, inputNodeGuid = inputNode.Data.guid });
            }
        }
        
        // Mark the asset as dirty to ensure changes are saved
        EditorUtility.SetDirty(_graph);
        return graphViewChange;
    }

    // This creates the right-click context menu
    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        base.BuildContextualMenu(evt);
        var menu = evt.menu;

        // Add "Set as Root" option when right-clicking a node
        if (evt.target is GraphNode node)
        {
            menu.AppendAction("Set as Root", (action) =>
            {
                _graph.rootNode = node.Data;
                UpdateRootNodeVisuals();
                EditorUtility.SetDirty(_graph);
            });
        }

        // Use reflection to find all static classes ending in "Layer"
        var layerTypes = TypeCache.GetTypesDerivedFrom<object>()
            .Where(t => !t.IsAbstract && t.Name.EndsWith("TerrainLayer") && t.Name != "TerrainLayer");

        foreach (var type in layerTypes)
        {
            menu.AppendAction($"Create/{type.Name.Replace("Layer", "")}", (action) =>
            {
                // Assumes a 'Create' method exists to get default properties
                var createMethod = type.GetMethod("Create", BindingFlags.Public | BindingFlags.Static);
                var layerInstance = (TerrainLayer)createMethod.Invoke(null, new object[] { default, default });

                var nodeData = new NodeData(
                    Guid.NewGuid().ToString(),
                    type.Name,
                    contentViewContainer.WorldToLocal(action.eventInfo.mousePosition),
                    16 // Default property count from TerrainLayer struct
                );
                
                // Copy default properties from the temporary layer instance
                for(int i = 0; i < 2; i++) // Example for 2 properties
                {
                    unsafe
                    {
                        nodeData.properties[i] = layerInstance.properties[i];
                    }
                }

                _graph.nodes.Add(nodeData);
                CreateNodeView(nodeData);
            });
        }
    }

    private void CreateNodeView(NodeData nodeData)
    {
        var nodeView = new GraphNode(nodeData);
        AddElement(nodeView);
    }

    private void UpdateRootNodeVisuals()
    {
        foreach (var n in nodes)
        {
            if (n is GraphNode graphNode)
            {
                graphNode.SetAsRoot(graphNode.Data == _graph.rootNode);
            }
        }
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        return ports.Where(endPort =>
            endPort.direction != startPort.direction &&
            endPort.node != startPort.node).ToList();
    }
}