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
    public static class DebugOptions
    {
        [SerializeField] public static bool DebugMode;
        public static bool ForcedValid;
        [SerializeField] public static bool ShowFullComplexGraph;
        public enum ColorMode{Categories, Output, Heat}

        [SerializeField] public static ColorMode colorMode = ColorMode.Categories;
        private static Dictionary<Type, Dictionary<string, List<string>>> NodeClasses = new();

        [MenuItem("Window/Infinite Lands/Debug Mode", false, 10000)]
        public static void ToggleFeature()
        {
            DebugMode = !DebugMode;
        }


        [MenuItem("Window/Infinite Lands/Debug Mode", true)]
        public static bool ToggleFeature_Validate()
        {
            Menu.SetChecked("Window/Infinite Lands/Debug Mode", DebugMode);
            return true;
        }

        [MenuItem("Window/Infinite Lands/Show Full Complex Graph", false, 10000)]
        public static void ToggleComplexGraph()
        {
            ShowFullComplexGraph = !ShowFullComplexGraph;
            InfiniteLandsWindow[] allWindows = Resources.FindObjectsOfTypeAll<InfiniteLandsWindow>();
            foreach (var window in allWindows)
            {
                window.ReloadGraph();
            }        
    }

        [MenuItem("Window/Infinite Lands/Show Full Complex Graph", true, 10000)]
        public static bool ToggleComplexGraph_Validate()
        {
            Menu.SetChecked("Window/Infinite Lands/Show Full Complex Graph", ShowFullComplexGraph);
            return true;
        }

        public static void BuildContextualMenu(InfiniteLandsGraphView view, ContextualMenuPopulateEvent evt)
        {
            evt.menu.AppendAction("Color Mode/Node Type", a => SetAllToOutputMode(view, ColorMode.Categories), GetDebugStatusOption(ColorMode.Categories));
            evt.menu.AppendAction("Color Mode/Main Output", a => SetAllToOutputMode(view, ColorMode.Output), GetDebugStatusOption(ColorMode.Output));
            //evt.menu.AppendAction("Color Mode/Heat Map", a => SetAllToOutputMode(view, ColorMode.Heat), GetDebugStatusOption(ColorMode.Heat));
            if (DebugMode)
            {
                evt.menu.AppendAction("Create All Nodes", a => CreateAllNodes(view));
                evt.menu.AppendAction("Force all Valid", a => ForceAllValid(view));
            } 
        }

        public static void ForceAllValid(InfiniteLandsGraphView view)
        {
            var graph = view.targetGraph;
            var AllNodes = graph.GetBaseNodes();
            foreach (var node in AllNodes)
            {
                var nodeView = view.GetNodeViewByGuid(node.guid);
                nodeView.SetValidity(true);
            }
        }


        public static void CreateAllNodes(InfiniteLandsGraphView view)
        {
            var graph = view.targetGraph;
            var types = TypeCache.GetTypesDerivedFrom<InfiniteLandsNode>();
            Dictionary<string, List<Type>> Groups = new();
            foreach (var nodeType in types)
            {
                CustomNodeAttribute attribute = nodeType.GetCustomAttribute<CustomNodeAttribute>();
                bool validNode = attribute != null && attribute.canCreate;
                if (validNode)
                {
                    string nodeGroup = EditorTools.GetNodeMenuType(attribute, nodeType);
                    if (!Groups.TryGetValue(nodeGroup, out var list))
                    {
                        list = new List<Type>();
                        Groups.Add(nodeGroup, list);
                    }
                    list.Add(nodeType);
                }
            }

            List<string> guids = new();
            List<GraphElement> views = new();

            int length = 5;
            foreach (var groupandType in Groups)
            {
                guids.Clear();
                views.Clear();
                int total = 0;
                foreach (var nodeType in groupandType.Value)
                {
                    float x = total % length;
                    float y = total / length;
                    total++;
                    InfiniteLandsNode nd = graph.CreateNode(nodeType, new Vector2(x, y) * 220.0f);
                    NodeView nodeView = GraphViewersFactory.CreateNodeView(view, nd);
                    guids.Add(nd.guid);
                    views.Add(nodeView);
                    view.AddNode(nodeView);
                }

                var group = graph.CreateGroup(groupandType.Key, Vector2.zero, guids);
                var groupView = GraphViewersFactory.CreateGroupView(group, views);
                view.AddGroupView(groupView);
            }
        }

        private static Dictionary<string, List<string>> GetNodeClasses(Type nodeType)
        {
            if (!NodeClasses.TryGetValue(nodeType, out var currentDictionary))
            {
                currentDictionary = new();
                CustomNodeAttribute attribute = nodeType.GetCustomAttribute<CustomNodeAttribute>();
                var classToAdd = EditorTools.GetNodeMenuType(attribute, nodeType);
                var elements = classToAdd.Split("/");

                List<string> Categories = new List<string>(elements)
                {
                    nodeType.Name
                };

                List<string> Outputs = new List<string>()
                {
                    elements[0]
                };

                currentDictionary.Add("CAT", Categories);
                currentDictionary.Add("OUT", Outputs);
                NodeClasses.Add(nodeType, currentDictionary);
            }

            return currentDictionary;
        }

        private static void RemoveAllCategories(NodeView element)
        {
            internal_ApplyOutputClassToNodeView(element, false);
            internal_ApplyCategoryToNodeView(element, false);
            element.SetTicksColor();
        }
        private static DropdownMenuAction.Status GetDebugStatusOption(DebugOptions.ColorMode colorMode)
        {
            return DebugOptions.colorMode == colorMode ? DropdownMenuAction.Status.Checked : DropdownMenuAction.Status.Normal;
        }
        public static void SetAllToOutputMode(InfiniteLandsGraphView view, ColorMode colorMode)
        {
            var nodeViews = view.nodes.OfType<NodeView>();
            DebugOptions.colorMode = colorMode;

            foreach (var node in nodeViews)
            {
                ApplyColorMode(node);
            }
        }

        public static void ApplyColorMode(NodeView element)
        {
            if (element.node == null)
                return;

            RemoveAllCategories(element);
            switch (colorMode)
            {
                case ColorMode.Categories:
                    internal_ApplyCategoryToNodeView(element, true);
                    break;
                case ColorMode.Output:
                    internal_ApplyOutputClassToNodeView(element, true);
                    break;
                case ColorMode.Heat:
                    element.SetTicksColor();
                    break;
            }
        }

        private static void internal_ApplyOutputClassToNodeView(NodeView element, bool add = true)
        {
            var nodeType = element.node.GetType();
            genericApplyClass(element, "OUT", GetNodeClasses(nodeType), add);
        }

        private static void internal_ApplyCategoryToNodeView(NodeView element, bool add = true)
        {
            var nodeType = element.node.GetType();
            genericApplyClass(element, "CAT", GetNodeClasses(nodeType), add);
        }

        private static void genericApplyClass(NodeView element, string type, Dictionary<string, List<string>> classes, bool add)
        {
            var classToAdd = classes[type];
            foreach (var cls in classToAdd)
            {
                AddRemoveClass(element, cls, add);
            }
        }

        private static void AddRemoveClass(NodeView element, string name, bool add)
        {
            if (add)
                element.AddToClassList(name);
            else
                element.RemoveFromClassList(name);
        }
    }
}