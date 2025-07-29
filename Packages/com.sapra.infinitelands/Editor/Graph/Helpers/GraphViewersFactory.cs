using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

namespace sapra.InfiniteLands.Editor{
    public static class GraphViewersFactory
    {
        #region Nodes
        public static NodeView CreateNodeView(InfiniteLandsGraphView view, InfiniteLandsNode node)
        {
            Type nodeViewType = GetTypeFromAtribute<NodeView>(node.GetType());

            if (nodeViewType == null)
                nodeViewType = typeof(NodeView);
            NodeView nodeView = Activator.CreateInstance(nodeViewType, new object[] { view, node }) as NodeView;
            return nodeView;
        }
        #endregion

        #region StickyNotes
        public static StickyNoteView CreateStickyNoteView(StickyNoteBlock sn){
            StickyNoteView note = new StickyNoteView(sn);
            return note;
        }
        #endregion

        #region Groups
        public static GroupView CreateGroupView(GroupBlock g, List<GraphElement> selection)
        {
            GroupView group = new GroupView(g)
            {
                title = g.Name,
                viewDataKey = g.guid,
            };

            selection.ForEach(n =>
            {
                if(n!= null)
                    group.AddElement(n);
            });

            return group;
        }
        #endregion

        #region Ports
        public static PortView CreatePortView(Type forType)
        {
            Type nodeViewType = GetTypeFromAtribute<PortView>(forType);
            if (nodeViewType == null)
                nodeViewType = typeof(PortView);
            PortView nodeView = Activator.CreateInstance(nodeViewType) as PortView;
            return nodeView;
        }
        #endregion

        #region Previews
        public static OutputPreview CreateOutputPreview(PortData portData, InfiniteLandsNode node, NodeView view)
        {
            if (node == null) return null;
            var field = node.GetType().GetField(portData.fieldName);
            if(field == null) return null;

            var type = field.GetSimpleField();
            
            if(type==typeof(object)){
                type = RuntimeTools.GetTypeFromOutputField(portData.fieldName, node, view.graph);
            }
            
            return CreateOutputPreview(portData, type, node, view);
        }

        public static OutputPreview CreateOutputPreview(PortData portData, Type previewType, InfiniteLandsNode node, NodeView view)
        {
            Type nodeViewType = GetTypeFromAtribute<OutputPreview>(previewType);
            if (nodeViewType == null){
                return null;
            }
            return Activator.CreateInstance(nodeViewType, new object[] { portData, node, view }) as OutputPreview;
        }
        #endregion

        private static Type GetTypeFromAtribute<T>(Type type)
        {
            GetNodeViewAndAttributeType<T>(type, out Type nodeType);
            return nodeType;
        }

        private static void GetNodeViewAndAttributeType<T>(Type type, out Type nodeType)
        {
            var types = TypeCache.GetTypesDerivedFrom(typeof(T)).ToList();
            foreach (Type foundTypes in types)
            {
                Attribute[] attrs = Attribute.GetCustomAttributes(foundTypes);
                foreach (Attribute attribute in attrs)
                {
                    if (attribute is EditorForClass customNode)
                    {                        
                        if (customNode.target.IsAssignableFrom(type))
                        {
                            nodeType = foundTypes;
                            return;
                        }
                    }
                }
            }

            nodeType = null;
            return;
        }
    }
}