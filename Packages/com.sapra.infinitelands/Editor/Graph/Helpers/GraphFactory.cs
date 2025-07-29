using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Search;
using UnityEditor.Searcher;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor
{
    public static class GraphFactory
    {
        private static List<SearcherItem> ConsistentItems = null;
        private static List<Type> ConditionalItems = null;

        static GraphFactory()
        {
            InitializeCacheItems();
        }

        public static void BuildContextualMenu(InfiniteLandsGraphView view, ContextualMenuPopulateEvent evt)
        {
            GroupView actualGroup = view.Query<GroupView>().Where((GroupView a) =>
                a.ContainsPoint(a.WorldToLocal(evt.mousePosition))).First();

            evt.menu.AppendAction("Create Node _N", a => ShowNodeCreatorWindow(view, view.MousePosition));
            evt.menu.AppendAction("Create StickyNote _S", a => CreateStickyNote(view, view.MousePosition));
            if (actualGroup == null)
            {
                evt.menu.AppendAction("Create Group _G", a => CreateGroup(view, view.MousePosition));
            }
            evt.menu.AppendSeparator();
        }

        public static void InitializeCacheItems()
        {
            if (ConditionalItems != null && ConsistentItems != null)
                return;

            var types = TypeCache.GetTypesDerivedFrom<InfiniteLandsNode>();
            ConditionalItems = new();
            ConsistentItems = new();
            foreach (var type in types)
            {
                CustomNodeAttribute attribute = type.GetCustomAttribute<CustomNodeAttribute>();
                if (attribute == null)
                    continue;

                var alwaysTrue = attribute.IsAlwaysValid();
                if (alwaysTrue && !attribute.singleInstance && attribute.canCreate)
                {
                    string nodeType = EditorTools.GetNodeMenuType(attribute, type);
                    SearcherItem nodeItem = new SearcherItem(nodeType + "/" + attribute.name)
                    {
                        UserData = new ItemInfo(type, default, default, default),
                        Synonyms = attribute.synonims
                    };
                    ConsistentItems.Add(nodeItem);
                }
                else
                {
                    ConditionalItems.Add(type);
                }
            }
        }

        public static void CreateStickyNote(InfiniteLandsGraphView view, Vector2 position)
        {
            GroupView actualGroup = view.Query<GroupView>().Where((GroupView a) =>
                a.ContainsPoint(a.WorldToLocal(position))).First();


            StickyNoteBlock note = view.targetGraph.CreateStickyNote(view.viewTransform.matrix.inverse.MultiplyPoint(position));
            StickyNoteView noteView = GraphViewersFactory.CreateStickyNoteView(note);
            view.AddStickyNoteView(noteView);
            if (actualGroup != null)
            {
                actualGroup.AddElement(noteView);
            }
        }

        public static void CreateGroup(InfiniteLandsGraphView view, Vector2 position)
        {
            GroupView actualGroup = view.Query<GroupView>().Where((GroupView a) =>
                a.ContainsPoint(a.WorldToLocal(position))).First();
            if (actualGroup != null)
                return;

            List<GraphElement> elements = view.selection.OfType<GraphElement>().Where(a => a.IsGroupable()).ToList();
            List<string> elementsGuids = elements.Select(a => a.viewDataKey).ToList();

            GroupBlock block = view.targetGraph.CreateGroup("Name", view.viewTransform.matrix.inverse.MultiplyPoint(position), elementsGuids);
            GroupView blockView = GraphViewersFactory.CreateGroupView(block, elements);
            view.AddGroupView(blockView);
        }


        public static void ShowNodeCreatorWindow(InfiniteLandsGraphView view, Vector2 position)
        {
            GroupView groupView = view.Query<GroupView>().Where((GroupView a) =>
                a.ContainsPoint(a.WorldToLocal(position))).First();
            Adapter adapter = new Adapter("Create Node");
            SearcherWindow.Show(EditorWindow.focusedWindow, GetUnfilteredItems(view, groupView, position), adapter, CreateNode, position, default);
        }

        public static void ShowNodeWindowAutoConnect(InfiniteLandsGraphView view, Vector2 position, Edge edge)
        {
            var preSet = edge.input != null ? edge.input : edge.output;
            var direction = preSet.direction;
            var targetFilter = preSet.portType;

            GroupView groupView = view.Query<GroupView>().Where((GroupView a) =>
                a.ContainsPoint(a.WorldToLocal(position))).First();
            Adapter adapter = new Adapter("Create Node");

            List<SearcherItem> items;
            if (direction == Direction.Input)
                items = GetFilteredItems<OutputAttribute>(view, groupView, position, targetFilter);
            else
                items = GetFilteredItems<InputAttribute>(view, groupView, position, targetFilter);

            SearcherWindow.Show(EditorWindow.focusedWindow, items, adapter, (a) => CreateNodeAndAutoConnect(a, (PortData)preSet.userData, direction, targetFilter), position, default);
        }

        private static List<SearcherItem> GetUnfilteredItems(InfiniteLandsGraphView view, GroupView ogGroup, Vector2 position)
        {
            List<SearcherItem> items = GetAllItems(view, ogGroup, position);

            items.Sort((a, b) => a.Name.CompareTo(b.Name));
            items = CustomSearchTreeUtility.CreateFromFlatList(items);
            return items;
        }
        
        private static List<SearcherItem> GetFilteredItems<T>(InfiniteLandsGraphView view, GroupView ogGroup, Vector2 position, Type filter) where T : PropertyAttribute
        {
            List<SearcherItem> items = GetAllItems(view, ogGroup, position);
            items = items.Where((SearcherItem a) => ((ItemInfo)a.UserData).type.GetInputOutputFields<T>().FlattenGenericFields().Any(a => a == filter)).ToList();
            items.Sort((a, b) => a.Name.CompareTo(b.Name));
            items = CustomSearchTreeUtility.CreateFromFlatList(items);
            return items;
        }


        private static List<SearcherItem> GetAllItems(InfiniteLandsGraphView view, GroupView ogGroup, Vector2 position)
        {
            List<SearcherItem> items = new List<SearcherItem>();
            items.AddRange(ConsistentItems);
            var graph = view.targetGraph;
            foreach (var searcher in ConsistentItems)
            {
                searcher.UserData = new ItemInfo(((ItemInfo)searcher.UserData).type, view, ogGroup, position);
            }
            foreach (var type in ConditionalItems)
            {
                CustomNodeAttribute attribute = type.GetCustomAttribute<CustomNodeAttribute>();
                bool validNode = attribute != null && attribute.canCreate && attribute.IsValidInTree(graph.GetType());
                int existingNodes = graph.GetBaseNodes().Count(a => a.GetType().Equals(type));
                bool canCreate = attribute != null && (!attribute.singleInstance || (attribute.singleInstance && existingNodes == 0));
                if (validNode && canCreate)
                {
                    string nodeType = EditorTools.GetNodeMenuType(attribute, type);
                    SearcherItem nodeItem = new SearcherItem(nodeType + "/" + attribute.name)
                    {
                        UserData = new ItemInfo(type, view, ogGroup, position),
                        Synonyms = attribute.synonims
                    };
                    items.Add(nodeItem);
                }
            }
            return items;
        }

        private static bool CreateNode(SearcherItem item)
        {
            if (item == null)
                return false;
            ItemInfo data = (ItemInfo)item.UserData;
            CreateNode(data);
            return true;
        }

        private static bool CreateNodeAndAutoConnect(SearcherItem item, PortData ogPortData, Direction direction, Type lookingForType) {
            if (item == null)
                return false;
            ItemInfo data = (ItemInfo)item.UserData;
            NodeView view = CreateNode(data);

            PortData inputData;
            PortData outputData;
            switch (direction)
            {
                case Direction.Input:
                    {
                        var targetPort = view.ports.FirstOrDefault(a => a.direction == Direction.Output && lookingForType.IsAssignableFrom(a.portType));
                        inputData = ogPortData;
                        outputData = (PortData)targetPort.userData;
                        break;
                    }
                default:
                    {
                        var targetPort = view.ports.FirstOrDefault(a => a.direction == Direction.Input && lookingForType.IsAssignableFrom(a.portType));
                        outputData = ogPortData;
                        inputData = (PortData)targetPort.userData;
                        break;
                    }
            }

            var created = data.view.targetGraph.AddConnection(new EdgeConnection(outputData, inputData));
            if (created)
            {
                var ogView = data.view.GetNodeViewByGuid(ogPortData.nodeGuid);
                data.view.RedrawNodes(new HashSet<NodeView>() { ogView, view });
            }
            return true;
        }

        private static NodeView CreateNode(ItemInfo data) {
            var graph = data.view.targetGraph;
            var view = data.view;
            InfiniteLandsNode nd = graph.CreateNode(data.type,
                view.viewTransform.matrix.inverse.MultiplyPoint(data.mousePosition));

            NodeView nodeView = GraphViewersFactory.CreateNodeView(view, nd);
            view.AddNode(nodeView);
            if (data.groupView != null)
                data.groupView.AddElement(nodeView);
            return nodeView;
        }
    }
}