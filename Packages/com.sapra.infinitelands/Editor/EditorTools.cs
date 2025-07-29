using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
namespace sapra.InfiniteLands.Editor
{
    public static class EditorTools
    {
        public static string GetNodeMenuType(CustomNodeAttribute attribute, Type type)
        {
            if (attribute == null)
                return "INTERNAL";
            if (!string.IsNullOrEmpty(attribute.customType))
                    return attribute.customType;

            string[] guids = AssetDatabase.FindAssets($"{type.Name} t:script");
            if (guids.Length > 0)
            {
                string targetGuid = guids.FirstOrDefault(a => Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(a)) == type.Name);
                string assetPath = AssetDatabase.GUIDToAssetPath(targetGuid);
                string folderPath = Path.GetDirectoryName(assetPath);
                string nodesMarker = "Nodes" + Path.DirectorySeparatorChar;
                int nodesIndex = folderPath.IndexOf(nodesMarker, StringComparison.OrdinalIgnoreCase);

                if (nodesIndex >= 0)
                {
                    return folderPath.Substring(nodesIndex + nodesMarker.Length)
                        .Replace(Path.DirectorySeparatorChar, '/');
                }
                return folderPath.Replace(Path.DirectorySeparatorChar, '/');
            }

            return "";
        }

        public static List<VisualElement> CreatePorts<T>(this NodeView viewer, InfiniteLandsNode node, Direction direction, IGraph graph) where T : PropertyAttribute
        {
            var NodeType = node.GetType();
            List<VisualElement> ports = new List<VisualElement>();
            FieldInfo[] fields = NodeType.GetFields(BindingFlags.Public | BindingFlags.Instance);
            IEnumerable<FieldInfo> inputFields = fields.Where(a => a.GetCustomAttribute<T>() != null);
            foreach (FieldInfo field in inputFields)
            {
                T attribute = field.GetCustomAttribute<T>();
                PortView prt = GraphViewersFactory.CreatePortView(field.FieldType);
                prt.GeneratePorts(ports, viewer, node, field, direction, graph, attribute);
            }
            return ports;
        }

        public static Port AddMissingPort(NodeView view, PortData missingData, Direction direction)
        {
            var container = direction == Direction.Input ? view.inputContainer : view.outputContainer;
            CustomMissingPort prt = new CustomMissingPort();
            Port generated = prt.GenerateMissingPort(view, missingData, direction);
            view.ports.Add(generated);
            container.Add(generated);
            view.RefreshExpandedState();
            return generated;
        }

        public static void RemoveMissingPorts(NodeView view)
        {
            var foundPorts = view.ports.Where(a => a.portType == typeof(MISSING)).ToArray();
            foreach (var port in foundPorts)
            {
                view.ports.Remove(port);
                var direction = port.direction;
                var container = direction == Direction.Input ? view.inputContainer : view.outputContainer;
                container.Remove(port);
            }

            if (foundPorts.Length > 0)
            {
                view.RefreshExpandedState();
            }
        }

        public static string GetMissingName(string name)
        {
            return string.Format("[M] {0}", name);
        }

        public static string ClearMissingName(string name)
        {
            if (name.Contains("[M]"))
                return name.Replace("[M] ", "");
            return name;
        }

        public static object GetValueDynamic(object field_holder, string field_name)
        {
            var type = field_holder.GetType();
            FieldInfo field = type.GetField(field_name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (field != null)
                return field.GetValue(field_holder);

            PropertyInfo property = type.GetProperty(field_name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (property != null)
                return property.GetValue(field_holder);
            return default;
        }

        public static VisualElement CreateSeparator()
        {
            VisualElement separator = new VisualElement();
            separator.name = "divider";
            separator.AddToClassList("horizontal");
            return separator;
        }

        public static VisualElement SideBySide(VisualElement left, VisualElement right)
        {
            VisualElement container = new VisualElement();
            container.Add(left);
            container.Add(right);
            container.style.flexDirection = FlexDirection.Row;
            return container;
        }

        public static SerializedProperty GetConditionSerializedProperty(SerializedProperty property, string conditionName)
        {
            var path = property.propertyPath.Split('.');
            var prop = property.serializedObject.FindProperty(path[0]);

            for (int i = 1; i < path.Length - 1; i++)
            {
                if (path[i] == "Array")
                {
                    i++;
                    int index = int.Parse(path[i].Substring(path[i].IndexOf("[") + 1).TrimEnd(']'));
                    prop = prop.GetArrayElementAtIndex(index);
                }
                else
                {
                    prop = prop.FindPropertyRelative(path[i]);
                }
                if (prop == null) return null;
            }
            return prop?.FindPropertyRelative(conditionName);
        }

        public static object GetFieldContainer(SerializedProperty property, object target)
        {
            try
            {
                var path = property.propertyPath.Split('.');
                object obj = target;
                for (int i = 0; i < path.Length - 1; i++)
                {
                    if (path[i] == "Array")
                    {
                        i++;
                        int index = int.Parse(path[i].Substring(path[i].IndexOf("[") + 1).TrimEnd(']'));
                        obj = (obj as System.Collections.IList)?[index];
                    }
                    else
                    {
                        var f = obj.GetType().GetField(path[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                        obj = f != null ? f.GetValue(obj) :
                              obj.GetType().GetProperty(path[i], BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(obj);
                    }
                    if (obj == null) return null;
                }
                return obj;
            }
            catch { return null; }
        }

        public static void RegenerateGraph(IGraph treeInstance)
        {
            bool generated = false;
            var visualizers = EditorWindow.FindObjectsByType<InfiniteLandsTerrain>(FindObjectsSortMode.None);
            foreach (InfiniteLandsTerrain visualizer in visualizers)
            {
                if (IsTheSameTree(treeInstance, visualizer.graph))
                {
                    visualizer.Initialize();
                    generated = true;
                }
                else if (visualizer.graph != null)
                {
                    bool containsGraph = visualizer.graph.GetBaseNodes().OfType<BiomeNode>().Any(a => a.biomeTree.Equals(treeInstance));
                    if (containsGraph)
                    {
                        visualizer.Initialize();
                        generated = true;
                    }
                }
            }
            if (!generated)
            {
                Debug.LogWarningFormat("There are no generators with {0}. Nothing has been generated", treeInstance.name);
            }
        }

        public static bool IsTheSameTree(IGraph treeInstance, IGraph target)
        {
            return treeInstance != null && target != null && treeInstance.Equals(target);
        }


        public static Toggle Generate_WorldPreview(NodeView nodeView, PortData portData)
        {
            var modeToggle = new Toggle();
            modeToggle.AddToClassList("world-preview-toggle");
            modeToggle.AddToClassList("preview-toggle");
            modeToggle.style.width = Length.Percent(100);
            modeToggle.RegisterValueChangedCallback(evt =>
            {
                if (evt.newValue)
                    GraphSettingsController.SetPortData(portData);
                else
                    GraphSettingsController.Clear();

                SingleToggleUpdate(modeToggle, nodeView, portData);
                RegenerateGraph(nodeView.graph);
            });
            SingleToggleUpdate(modeToggle, nodeView, portData);
            return modeToggle;
        }

        private static void SingleToggleUpdate(Toggle ogToggle, NodeView view, PortData portData)
        {
            var rootElement = view.treeView.contentContainer.Query<Toggle>(className: "world-preview-toggle").ToList();
            var isOgActive = GraphSettingsController.IsPortThePreview(portData);
            foreach (var element in rootElement)
            {
                UpdateToggleState(element, false);
            }
            UpdateToggleState(ogToggle, isOgActive);
        }

        public static void UpdateToggleState(Toggle toggle, bool value)
        {
            toggle.SetValueWithoutNotify(value);
            string add = value.ToString();
            string remove = (!value).ToString();
            toggle.RemoveFromClassList(remove);
            toggle.AddToClassList(add);
        }
    }
}