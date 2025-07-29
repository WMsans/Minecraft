using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor{
    public readonly struct MISSING{};

    [EditorForClass(typeof(MISSING))]
    public class CustomMissingPort : PortView
    {
        public override void GeneratePorts(List<VisualElement> targetPorts, NodeView viewer, InfiniteLandsNode node, FieldInfo fieldInfo, Direction direction, IGraph graph, PropertyAttribute OriginalPortTypeAttribute)
        {
        }

        public Port GenerateMissingPort(NodeView viewer, PortData missingPortData, Direction direction)
        {
            Debug.LogWarningFormat("Missing port {0}", missingPortData.nodeGuid);
            string name = "[missing] " + missingPortData.fieldName;
            if(missingPortData.listIndex >= 0)
                name += " " + missingPortData.listIndex;
            
            PortFieldData portFieldData = new PortFieldData(viewer, viewer.node, null, direction,  Port.Capacity.Single, null);
            return CreatePort(portFieldData, name, typeof(MISSING), missingPortData, false);
        }
    }
}