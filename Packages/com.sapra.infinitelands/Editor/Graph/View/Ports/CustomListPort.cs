using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor{
    [EditorForClass(typeof(IList))]
    public class CustomListPort : PortView
    {
        public override void GeneratePorts(List<VisualElement> targetPorts, NodeView viewer, InfiniteLandsNode node, FieldInfo fieldInfo, Direction direction, IGraph graph, PropertyAttribute attribute)
        {
            var capacity = direction == Direction.Output ? Port.Capacity.Multi : Port.Capacity.Single;
            var lookingForConnection = attribute is IMatchList matchesList ? matchesList.matchingList : fieldInfo.Name;
            Type enumerableType = fieldInfo.FieldType.GetGenericArguments()[0];
            PortFieldData portFieldData = new PortFieldData(viewer, node, fieldInfo, direction, capacity, attribute);

            var amountOfInputs = node.GetCountOfNodesInInput(lookingForConnection);
            for (int i = 0; i < amountOfInputs; i++)
            {
                PortData data = new PortData(node.guid, fieldInfo.Name)
                {
                    listIndex = i,
                    itemIsRearrangable = true
                };
                var port = AutoCreatePort(portFieldData, fieldInfo.Name, enumerableType, data, graph);
                port.portName = string.Format("{0} {1}", fieldInfo.Name, i);
                targetPorts.Add(port);
            }
            if (direction == Direction.Input)
            { //Adding the option to add new node
                PortData data = new PortData(node.guid, fieldInfo.Name)
                {
                    isAddItem = true,
                    itemIsRearrangable = true
                };
                var adder = CreatePort(portFieldData, string.Format("Add {0}", fieldInfo.Name), enumerableType, data, true);
                targetPorts.Add(adder);
            }
        }
        
    }
}