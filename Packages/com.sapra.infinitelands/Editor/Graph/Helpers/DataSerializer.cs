using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;

using System.Linq;
using System;

using UnityEditor;
using UnityEngine.UIElements;
using System.Reflection;
namespace sapra.InfiniteLands.Editor{
    public static class DataSerializer 
    {
        public class NewlyPastedData
        {
            public List<InfiniteLandsNode> newNodes;
            public List<StickyNoteBlock> newNotes;
            public List<GroupBlock> newGroups;
            public List<EdgeConnection> newConnections;
            public List<string> reloadable;
        }
        #region Legacy Contextual Menu
        public static NewlyPastedData UnserializeAndPaste(Vector2 position, string data, IGraph tree)
        {
            SerializedGuids serializedG = JsonUtility.FromJson<SerializedGuids>(data);

            Dictionary<string, InfiniteLandsNode> NewNodesGenerated = new Dictionary<string, InfiniteLandsNode>();
            Dictionary<string, StickyNoteBlock> NewNotes = new Dictionary<string, StickyNoteBlock>();
            List<GroupBlock> NewGroups = new List<GroupBlock>();
            List<EdgeConnection> NewConnections = new List<EdgeConnection>();
            List<EdgeConnection> ValidConnections = new List<EdgeConnection>();
            List<string> RequireReload = new List<string>();

            //Add the items
            Undo.IncrementCurrentGroup();
            var curGroupID = Undo.GetCurrentGroup();

            foreach(SerializedItem serializedItem in serializedG.guids){
                Type targetType = serializedItem.TheType();
                Vector2 desiredPosition = serializedItem.position-serializedG.center+position;
                string OriginalGuid = serializedItem.guid;
                if(typeof(InfiniteLandsNode).IsAssignableFrom(targetType)){
                    CustomNodeAttribute attribute = targetType.GetCustomAttribute<CustomNodeAttribute>();
                    bool validNode = attribute != null && attribute.canCreate && attribute.IsValidInTree(tree.GetType());
                    if(validNode){
                        InfiniteLandsNode node = tree.CreateNodeFromJson(targetType, serializedItem.item, desiredPosition);
                        NewNodesGenerated.Add(OriginalGuid, node);
                    }
                    continue;
                }
                
                if(typeof(GroupBlock).IsAssignableFrom(targetType)){
                    GroupBlock block = tree.CreateGroupFromJson(serializedItem.item, desiredPosition);
                    NewGroups.Add(block);
                    continue;
                }

                if(typeof(StickyNoteBlock).IsAssignableFrom(targetType)){
                    StickyNoteBlock noteView = tree.CreateStickyNoteFromJson(serializedItem.item, desiredPosition);
                    NewNotes.Add(OriginalGuid, noteView);
                    continue;
                }

                
                if(typeof(EdgeConnection).IsAssignableFrom(targetType)){
                    EdgeConnection newConnection = JsonUtility.FromJson<EdgeConnection>(serializedItem.item);
                    NewConnections.Add(newConnection);
                    if (newConnection.inputPort.listIndex >= 0)
                        newConnection.inputPort.isAddItem = true;
                    if (newConnection.outputPort.listIndex >= 0)
                        newConnection.outputPort.isAddItem = true;
                    continue;
                }
            }

            foreach (EdgeConnection connection in NewConnections)
            {
                if (NewNodesGenerated.TryGetValue(connection.outputPort.nodeGuid, out InfiniteLandsNode possibleOutput))
                {
                    connection.outputPort.nodeGuid = possibleOutput.guid;
                }
                else
                {
                    RequireReload.Add(connection.outputPort.nodeGuid);
                }

                if (NewNodesGenerated.TryGetValue(connection.inputPort.nodeGuid, out InfiniteLandsNode possibleInput))
                {
                    connection.inputPort.nodeGuid = possibleInput.guid;
                }
                else
                {
                    RequireReload.Add(connection.inputPort.nodeGuid);
                }

                bool validated = tree.AddConnection(connection, false);
                if (validated)
                    ValidConnections.Add(connection);
            }

            //Create Groups
            foreach(GroupBlock group in NewGroups){
                List<string> newGuids = new List<string>();
                foreach(string guid in group.elementGuids){
                    if(NewNodesGenerated.TryGetValue(guid, out InfiniteLandsNode newNode)){
                        newGuids.Add(newNode.guid);
                    }else if(NewNotes.TryGetValue(guid, out StickyNoteBlock newNote)){
                        newGuids.Add(newNote.guid);
                    }
                }
                group.elementGuids.Clear();
                tree.AddElementsToGroup(group, newGuids);
            }
            tree.ValidationCheck();

            Undo.SetCurrentGroupName(string.Format("{0}: Pasting data", tree.GetType().Name));
            Undo.CollapseUndoOperations(curGroupID);

            return new NewlyPastedData()
            {
                newNodes = NewNodesGenerated.Values.ToList(),
                newNotes = NewNotes.Values.ToList(),
                newGroups = NewGroups,
                newConnections = ValidConnections,
                reloadable = RequireReload
            };
        }

        public static bool CanPaste(string data)
        {
            try{
                SerializedGuids serializedG = JsonUtility.FromJson<SerializedGuids>(data);
                if(serializedG.guids.Count <= 0)
                    return false;
                foreach(SerializedItem guid in serializedG.guids){
                    if(guid.ValidSerialization())
                        return true;
                }
                return false;
            }
            catch(Exception){
                return false;
            }
        }

        public static bool CanPasteInto(string data, Type type)
        {
            try{
                SerializedGuids serializedG = JsonUtility.FromJson<SerializedGuids>(data);
                var equivalent = serializedG.guids.FirstOrDefault(a => a.TheType()==type);
                if(equivalent == null)
                    return false;
                foreach(SerializedItem guid in serializedG.guids){
                    if(guid.ValidSerialization())
                        return true;
                }
                return false;
            }
            catch(Exception){
                return false;
            }
        }

        private static string ClearData(string data){
            if (data.StartsWith("application/vnd.unity.graphview.elements"))
                return data.Substring("application/vnd.unity.graphview.elements".Length + 1);
            else
                return data;
        }
        private static string GetClipboard(){
            return ClearData(EditorGUIUtility.systemCopyBuffer);
        }

        public static void BuildContextualMenu(ContextualMenuPopulateEvent evt, IGraph graph){
            if(evt.target is NodeView targetNode){
                string clipboard = GetClipboard();
                bool isEnabled = CanPasteInto(clipboard, targetNode.node.GetType());
                evt.menu.AppendAction("Paste Values", a => PasteComponentValues(targetNode.node, clipboard, graph), 
                    isEnabled ? DropdownMenuAction.Status.Normal : DropdownMenuAction.Status.Disabled);
                evt.menu.AppendSeparator();
            }
        }
        private static void PasteComponentValues(InfiniteLandsNode target, string data, IGraph graph){
            SerializedGuids serializedG = JsonUtility.FromJson<SerializedGuids>(data);
            SerializedItem serializedItem = serializedG.guids[0];

            Type originalType = serializedItem.TheType();
            InfiniteLandsNode result = JsonUtility.FromJson(serializedItem.item, originalType)  as InfiniteLandsNode;
            graph.CopyDataFromTo(result, target);
        }

        public static string SerializeGraphElements(IEnumerable<GraphElement> elements, IGraph graph)
        {
            IEnumerable<IRenderSerializableGraph> ValidData = elements.OfType<IRenderSerializableGraph>();
            SerializedGuids guids = new SerializedGuids();
            HashSet<string> copiedGuids = new HashSet<string>();
            Vector2 center = Vector2.zero;
            int validCount = 0;
            foreach(IRenderSerializableGraph element in ValidData){
                if (copiedGuids.Contains(element.GetGUID())) continue;

                object serializedData = element.GetDataToSerialize();
                CustomNodeAttribute attribute = serializedData.GetType().GetCustomAttribute<CustomNodeAttribute>();
                bool validNode = attribute == null || (attribute != null && attribute.canCreate);
                if (validNode)
                {
                    validCount++;
                    SerializedItem sn = new SerializedItem(element);
                    guids.guids.Add(sn);
                    center += element.GetPosition();
                    copiedGuids.Add(element.GetGUID());
                }
            }

            IEnumerable<Edge> ValidEdges = elements.OfType<Edge>().Distinct();
            foreach (Edge element in ValidEdges)
            {
                var inputData = (PortData)element.input.userData;
                var outputData = (PortData)element.output.userData;
                var connection = graph.GetAllEdges().Where(a => a.inputPort.Equals(inputData) && a.outputPort.Equals(outputData)).FirstOrDefault();
                if (connection == null) continue;
                SerializedEdge storeEdge = new SerializedEdge(connection);

                if (copiedGuids.Contains(storeEdge.GetGUID())) continue;

                SerializedItem sn = new SerializedItem(storeEdge);
                guids.guids.Add(sn);
                copiedGuids.Add(storeEdge.GetGUID());

            }
            
            if(validCount > 0)
                guids.center = center/validCount;

            string data = JsonUtility.ToJson(guids);
            return data;
        }

        [Serializable]
        public class SerializedGuids{
            public Vector2 center = Vector2.zero;
            public List<SerializedItem> guids = new List<SerializedItem>();
        }

        [Serializable]
        public class SerializedItem{
            public string type;
            public string guid;
            public Vector2 position;
            public string item;
            public SerializedItem(IRenderSerializableGraph serializable){
                object data = serializable.GetDataToSerialize();
                this.type = data.GetType().AssemblyQualifiedName;
                this.guid = serializable.GetGUID();
                this.position = serializable.GetPosition();
                this.item = JsonUtility.ToJson(data);
            }
            public Type TheType() => Type.GetType(type);

            public bool ValidSerialization(){
                return item != "" && TheType() != null;
            }
        }
        #endregion
    }
}