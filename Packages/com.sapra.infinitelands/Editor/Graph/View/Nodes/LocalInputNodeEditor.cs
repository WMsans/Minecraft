using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor{
    [EditorForClass(typeof(LocalInputNode))]
    public class LocalInputNodeEditor : NodeView
    {
        public override bool TriggerRedrawOnConnection => true;

        public LocalInputNodeEditor(InfiniteLandsGraphView view, InfiniteLandsNode node) : base(view, node){}
        protected override List<VisualElement> CreateOutputPorts()
        {
            var ports = new List<VisualElement>();
            LocalInputNode nd = node as LocalInputNode;

            Button btn = new Button() { text = "- select -" };
            var targetEdge = graph.GetBaseEdges().FirstOrDefault(a => a.inputPort.nodeGuid == node.guid);
            LocalOutputNode targetNode = null;
            if(targetEdge != null){
                targetNode = graph.GetNodeFromGUID(targetEdge.outputPort.nodeGuid) as LocalOutputNode;
                if (targetNode != null)
                {
                    btn.text = targetNode.InputName;
                    var targetNodeProperty = GetNodeProperty(targetNode);
                    var inputName = targetNodeProperty.FindPropertyRelative(nameof(targetNode.InputName));
                    btn.bindingPath = inputName.propertyPath;
                }
            }

            var thisNode = node as LocalInputNode;

            btn.clicked += () => CreateMenu(targetEdge);
            btn.style.minWidth = 80;
            btn.style.marginTop = 0;
            btn.style.marginBottom = 0;
            FieldInfo fieldInfo = node.GetType().GetField(nameof(thisNode.Output));
            OutputAttribute attribute = fieldInfo.GetCustomAttribute<OutputAttribute>();
            PortData outputPortData = new PortData(node.guid, nameof(thisNode.Output));        
            PortView.PortFieldData basicData = new PortView.PortFieldData(this, node, fieldInfo, Direction.Output, Port.Capacity.Multi, attribute);  
            Port outputPort = PortView.AutoCreatePort(basicData, nameof(thisNode.Output), typeof(object), outputPortData, graph);

            ports.Add(outputPort);

            var label = outputPort.contentContainer.Q<Label>("type");
            label.style.color = new StyleColor(new Color(0, 0, 0, 0));
            label.style.marginLeft = 0;
            label.style.marginRight = 0;
            label.Add(btn);
            return ports;
        }

        void CreateMenu(EdgeConnection currentConnection)
        {
            GenericMenu menu = new GenericMenu();
            IEnumerable<LocalOutputNode> asset = graph.GetBaseNodes().OfType<LocalOutputNode>();
            var thisNode = node as LocalInputNode;
            PortData inputPort = new PortData(node.guid, nameof(thisNode.Input));

            Dictionary<string, int> currentItems = new();
            foreach (LocalOutputNode newNode in asset)
            {
                if(!newNode.isValid)
                    continue;
                var outputEdge = graph.GetBaseEdges().FirstOrDefault(a => a.inputPort.nodeGuid == newNode.guid);
                var tp = typeof(object);
                if(outputEdge != null){
                    var outputNode = graph.GetNodeFromGUID(outputEdge.outputPort.nodeGuid);
                    tp = outputNode.GetType().GetField(outputEdge.outputPort.fieldName).GetSimpleField();
                }
                //string contentName = string.Format("{0} | type {1}",newNode.InputName, tp.Name);
                string contentName = string.Format("{1}/{0}",newNode.InputName, tp.Name);

                if(!currentItems.TryGetValue(contentName, out int index)){
                    currentItems[contentName] = 0;
                }
                
                if(index > 0)
                    contentName += " {"+index+"}";
                index++;
                currentItems[contentName] = index;
    
                bool active = currentConnection != null ? newNode.guid == currentConnection.outputPort.nodeGuid : false;
                menu.AddItem(new GUIContent(contentName), active, () =>
                {
                    graph.RemoveConnection(currentConnection);
                    PortData outputPort = new PortData(newNode.guid, nameof(newNode.Output));
                    if(currentConnection == null)
                        currentConnection = new EdgeConnection(outputPort, inputPort);
                    else
                        currentConnection.outputPort = outputPort;

                    graph.AddConnection(currentConnection);
                    treeView.RedrawNode(this);
                });
            }

            menu.ShowAsContext();
        }
    }
}