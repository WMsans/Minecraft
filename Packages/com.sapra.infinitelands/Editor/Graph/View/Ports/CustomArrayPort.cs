using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor{
    [EditorForClass(typeof(Array))]
    public class CustomArrayPort : PortView
    {
        public override void GeneratePorts(List<VisualElement> targetPorts, NodeView viewer, InfiniteLandsNode node, FieldInfo fieldInfo, Direction direction, IGraph graph, PropertyAttribute attribute)
        {
            var capacity = direction == Direction.Output ? Port.Capacity.Multi : Port.Capacity.Single;
            var lookingForConnection = attribute is IMatchList matchesList ? matchesList.matchingList : fieldInfo.Name;
            Type enumerableType = fieldInfo.FieldType.GetElementType();
            PortFieldData portFieldData = new PortFieldData(viewer, node, fieldInfo, direction, capacity, attribute);

            var amountOfInputs = node.GetCountOfNodesInInput(lookingForConnection);
            for (int i = 0; i < amountOfInputs; i++)
            {
                PortData data = new PortData(node.guid, fieldInfo.Name)
                {
                    listIndex = i,
                    itemIsRearrangable = false
                };
                var port = AutoCreatePort(portFieldData, fieldInfo.Name, enumerableType, data, graph);
                port.portName = string.Format("{0} {1}", fieldInfo.Name, i);
                targetPorts.Add(port);
            }
        }
    }
}