using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor{
    public class PortView 
    {
        public const string hiddenName = "hide";
        public const string variableHidden = "variableHide";

        public struct PortFieldData
        {
            public NodeView nodeView;
            public InfiniteLandsNode node;
            public FieldInfo fieldInfo;
            public Direction direction;
            public Port.Capacity capacity;

            public PropertyAttribute OriginalPortTypeAttribute;
            public PortFieldData(NodeView nodeView, InfiniteLandsNode node, FieldInfo fieldInfo, Direction direction, Port.Capacity capacity, PropertyAttribute OriginalPortTypeAttribute)
            {
                this.nodeView = nodeView;
                this.node = node;
                this.fieldInfo = fieldInfo;
                this.direction = direction;
                this.capacity = capacity;
                this.OriginalPortTypeAttribute = OriginalPortTypeAttribute;
            }
        }
        public virtual void GeneratePorts(List<VisualElement> targetPorts, NodeView viewer, InfiniteLandsNode node, FieldInfo fieldInfo, Direction direction, IGraph graph, PropertyAttribute OriginalPortTypeAttribute)
        {
            var capacity = direction == Direction.Output ? Port.Capacity.Multi : Port.Capacity.Single;
            PortData data = new PortData(node.guid, fieldInfo.Name);

            PortFieldData fieldData = new PortFieldData(viewer, node, fieldInfo, direction, capacity, OriginalPortTypeAttribute);
            targetPorts.Add(AutoCreatePort(fieldData, fieldInfo.Name, fieldInfo.GetSimpleField(), data, graph));
        }   

        public static Port AutoCreatePort(PortFieldData basicFieldData, string visualName, Type portType, PortData data, IGraph graph){
            var fullHideAttribute = basicFieldData.fieldInfo.GetCustomAttribute<HideAttribute>();
            bool hidden = fullHideAttribute != null;
            var hideIfAttribute = basicFieldData.fieldInfo.GetCustomAttribute<HideIfAttribute>();
            var showIfAttribute = basicFieldData.fieldInfo.GetCustomAttribute<ShowIfAttribute>();
            var multipleConditionalFields = hideIfAttribute != null && showIfAttribute != null;
            if (multipleConditionalFields)
                Debug.LogWarningFormat("{0} in {1} has too many conditional fields, this is not supported", basicFieldData.node, basicFieldData.fieldInfo.Name);

            ConditionalAttribute conditionalVisibleAttribute = hideIfAttribute != null ? hideIfAttribute : showIfAttribute;
            bool reverseMode = conditionalVisibleAttribute == showIfAttribute;
            if (conditionalVisibleAttribute != null) {
                hidden = ExtractIsHidden(conditionalVisibleAttribute, basicFieldData.node, reverseMode);
            }

            var targetField = basicFieldData.OriginalPortTypeAttribute is ICanBeRenamed renamedAttribute ? renamedAttribute.name_field : ""; 
            string fieldName = "";
            if(basicFieldData.OriginalPortTypeAttribute is IMatchInputType matchingAttribute && matchingAttribute.matchingType != ""){
                fieldName = matchingAttribute.matchingType;
            }
            else if(portType.Equals(typeof(object)) && basicFieldData.direction == Direction.Input){
                fieldName = data.fieldName;
            }

            bool optional = ExtractIsOptional(basicFieldData);
            var port = CreatePort(basicFieldData, visualName, portType, data, optional, targetField);
            if(fieldName != ""){
                PortRefresh(port, fieldName, basicFieldData.node, graph, basicFieldData.direction);
            }

            if (conditionalVisibleAttribute != null && conditionalVisibleAttribute.conditionName != "")
            {
                port.AddToClassList(variableHidden);
            }

            if (hidden)
            {
                port.style.display = DisplayStyle.None;
                port.AddToClassList(hiddenName);
            }
            return port;
        }
        private static bool ExtractIsOptional(PortFieldData basicFieldData)
        {
            bool optional = basicFieldData.fieldInfo.GetCustomAttribute<DisabledAttribute>() != null;
            if (optional)
                return true;

            var enableIf = basicFieldData.fieldInfo.GetCustomAttribute<EnableIfAttribute>();
            if (enableIf != null)
            {
                var currentValue = EditorTools.GetValueDynamic(basicFieldData.node, enableIf.conditionName);
                if (currentValue != null)
                    return !(bool)currentValue;
            }

            return false;
        }
        private static bool ExtractIsHidden(ConditionalAttribute attribute, InfiniteLandsNode node, bool reverse)
        {
            bool result = false;
            if (attribute != null)
            {
                if (attribute.conditionName != "")
                {
                    var currentValue = EditorTools.GetValueDynamic(node, attribute.conditionName);
                    if (currentValue != null)
                        result = (bool)currentValue;
                }
            }

            if (reverse) result = !result;
            return result;
        }

        public static void PortRefresh(Port port, string fieldName, InfiniteLandsNode node, IGraph graph, Direction direction){
            var newType = RuntimeTools.GetTypeFromInputField(fieldName, node, graph);  
            if(direction == Direction.Input){   
                var classes = port.GetClasses();
                var toRemove = classes.Where(a => a.Contains("type")).ToArray();
                foreach(var classToRemove in toRemove){
                    port.RemoveFromClassList(classToRemove);
                }
                port.AddToClassList(string.Format("type{0}", newType.Name));
            } 
            else
                port.portType = newType;
        }

        public static Port CreatePort(PortFieldData basicFieldData, string visualName, Type portType, PortData data, bool optional = false, string name_field = ""){
            var viewer = basicFieldData.nodeView;
            var direction = basicFieldData.direction;
            var capacity = basicFieldData.capacity;
            var node = basicFieldData.node;

            Port port = viewer.InstantiatePort(Orientation.Horizontal, direction, capacity, portType);
            port.userData = data;
            port.styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UIStyles.PortStyles));
            port.AddManipulator(new EdgeConnector<Edge>(new CustomEdgeConnectorListener(basicFieldData.nodeView.treeView)));

            if (optional)
                port.AddToClassList("optional");

            if(name_field != ""){
                FieldInfo targetField = node.GetType().GetField(name_field);
                var currentValue = (string)targetField.GetValue(node);                

                currentValue = ValidateOutputName(currentValue, viewer, node, targetField);
                TextField field = new TextField
                {
                    name = name_field,
                    value = currentValue,
                    bindingPath = name_field
                };
                field.RegisterCallback<BlurEvent>(a => {    
                    field.SetValueWithoutNotify(ValidateOutputName(field.value, viewer, node, targetField));
                });

                field.style.minWidth = 80;

                var label = port.contentContainer.Q<Label>("type");
                label.style.color = new StyleColor(new Color(0, 0, 0, 0));
                label.style.marginLeft = 0;
                label.style.marginRight = 0;
                label.Add(field);
            }

            port.portName = ObjectNames.NicifyVariableName(visualName);        
            return port;
        }

        public static string ValidateOutputName(string newValue, NodeView view, InfiniteLandsNode node, FieldInfo field){
            var graph = view.graph;
            var guid = node.guid;
            var InputName = newValue != "" ? newValue:"Value";

            if(graph == null)
                return InputName;

            var graphView = view.GetFirstAncestorOfType<GraphView>();
            
            var currentNames = graph.GetAllNodes()
                .Where(a => !a.guid.Equals(guid) && a.GetType().Equals(node.GetType()))
                .Select(a => field.GetValue(a));
            if(currentNames.Contains(InputName)){
                string baseName = InputName;
                int counter = 1;
                while (currentNames.Contains($"{baseName} {counter}")){
                    counter++;
                }

                InputName = $"{baseName} {counter}";
            }
            field.SetValue(node, InputName);
            return InputName;
        }
    }
}