using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.IO;
using System.Reflection;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
#endif
namespace sapra.InfiniteLands
{
    public abstract class TerrainGenerator : ScriptableObject, IGraph
    {     
        [HideInInspector] public bool AutoUpdate = true;
        public bool _autoUpdate => AutoUpdate;
        public Vector2 position;
        public Vector2 scale = Vector2.one;

        [SerializeReference] public HeightOutputNode output;
        [SerializeReference] public List<InfiniteLandsNode> nodes = new List<InfiniteLandsNode>();
        private Dictionary<string, InfiniteLandsNode> nodesByGuid = new Dictionary<string, InfiniteLandsNode>();

        public List<GroupBlock> groups = new List<GroupBlock>();
        public List<StickyNoteBlock> stickyNotes = new List<StickyNoteBlock>();
        public List<EdgeConnection> edges = new List<EdgeConnection>();

        public Action OnValuesChangedBefore{get; set;} = default;
        public Action OnValuesChangedAfter { get; set; } = default;
        private int current_count = 0;
        public int GetUniqueIndex()
        {
            var tmp = current_count;
            current_count++;
            return current_count;
        }
        #region Initalization        
        private void OnEnable(){
            ValidateThatItHasOutput();
        }
        public void ValidateThatItHasOutput(){
#if UNITY_EDITOR
            if ((output == null && EditorUtility.IsPersistent(this)) || nodes.Count <= 0)
            {
                output = CreateNode<HeightOutputNode>(new Vector2(0, 0), false);
            }
            #endif
        }

        private List<EdgeConnection> allEdgesCache = new();
        private List<InfiniteLandsNode> allNodesCache = new();
        public IEnumerable<EdgeConnection> GetAllEdges() => allEdgesCache;
        public IEnumerable<InfiniteLandsNode> GetAllNodes() => allNodesCache;
        public IEnumerable<InfiniteLandsNode> GetValidNodes() => GetAllNodes().Where(a => a != null && a.isValid);
        
        public IEnumerable<EdgeConnection> GetBaseEdges() => edges;
        public IEnumerable<InfiniteLandsNode> GetBaseNodes() => nodes;
        public HeightOutputNode GetOutputNode() => output;

        public void ValidationCheck()
        {
            ValidateThatItHasOutput();

            current_count = 0;
            allEdgesCache.Clear();
            allNodesCache.Clear();
            nodes.RemoveAll(a => a == null);
            stickyNotes.RemoveAll(a => a == null);
            groups.RemoveAll(a => a == null);
            nodesByGuid.Clear();

            foreach (var node in nodes)
            {
                if (node is IAmplifyGraph amplifyGraph)
                    amplifyGraph.Amplified = false;
            }

            allNodesCache.AddRange(GetBaseNodes());
            allEdgesCache.AddRange(GetBaseEdges());

            GraphTools.AmplifyGraph(this, allNodesCache, allNodesCache, allEdgesCache);

            current_count = 0;
            foreach (var node in allNodesCache)
            {
                node.Restart(this);
                if (!nodesByGuid.TryAdd(node.guid, node))
                    Debug.LogFormat("Duplicated node! {0}", node.guid);
            }

            var nds = GetAllNodes();
            var edgs = GetAllEdges();
            ValdiateListEdges();
            foreach (var node in nds)
            {
                var connections = edgs.Where(a => a.inputPort.nodeGuid == node.guid);
                node.SetUpAndValidateConnections(connections);
            }

#if UNITY_EDITOR
            State = NotificationState.Idle;
#endif
        }

        private void ValdiateListEdges(){
            // Group connections by nodeGuid and fieldName of the inputPort
            var grouped = GetAllEdges()
                .Where(conn => conn.inputPort.listIndex > -1 && conn.inputPort.itemIsRearrangable) // Only consider connections with listIndex >= 0
                .GroupBy(conn => new { conn.inputPort.nodeGuid, conn.inputPort.fieldName });

            foreach (var group in grouped)
            {
                // Extract the connections in the group
                var sortedConnections = group
                    .OrderBy(conn => conn.inputPort.listIndex);
                int index = 0;
                foreach (var conn in sortedConnections)
                {
                    conn.inputPort.listIndex = index++;
                }
            }
        }
        private void InsertEdge(EdgeConnection edgeConnection){
            var conflictingEdge = edges.FirstOrDefault(conn => conn.inputPort.Equals(edgeConnection.inputPort));
            if (conflictingEdge != null)
            {
                // Shift all subsequent listIndices up by 1 to make space
                foreach (var conn in edges
                    .Where(conn =>
                        conn.inputPort.nodeGuid == edgeConnection.inputPort.nodeGuid &&
                        conn.inputPort.fieldName == edgeConnection.inputPort.fieldName &&
                        conn.inputPort.listIndex >= edgeConnection.inputPort.listIndex)
                    .OrderByDescending(conn => conn.inputPort.listIndex)) // Reverse order to avoid overwriting
                {
                    conn.inputPort.listIndex++;
                }
            }

            // Add the new edgeConnection to the list
            edges.Add(edgeConnection);
        }
        
        public InfiniteLandsNode GetNodeFromGUID(string guid)
        {
            if (nodesByGuid.TryGetValue(guid, out var node))
                return node;
            else
            {
                return null;
            }
        }

        public int GetNodeIndex(InfiniteLandsNode node)
        {
            return nodes.IndexOf(node);
        }

        public IEnumerable<InfiniteLandsNode> GetNodesFromGUID(IEnumerable<string> guids){
            return guids.Select(a => GetNodeFromGUID(a)).Where(a => a != null);
        }
        #endregion

        #region EditorMethods
#if UNITY_EDITOR

        public enum NotificationState{Idle, BeforeCalled, AfterScheduled}
        private NotificationState State = NotificationState.Idle;
        public void NotifyValuesChangedBefore()
        {
            if (State == NotificationState.Idle)
            {
                OnValuesChangedBefore?.Invoke();
                State = NotificationState.BeforeCalled;
            }
        }
        public void NotifyValuesChangedAfter()
        {
            if (State == NotificationState.BeforeCalled)
            {
                EditorApplication.delayCall += () =>
                {
                    ValidationCheck();
                    OnValuesChangedAfter?.Invoke();
                    State = NotificationState.Idle;
                };
                State = NotificationState.AfterScheduled;
            }
        }

        #region Nodes Configuration
        public InfiniteLandsNode CreateNodeFromJson(Type type, string JsonData, Vector2 position){
            if(!typeof(InfiniteLandsNode).IsAssignableFrom(type))
                return null;

            NotifyValuesChangedBefore();
            Undo.IncrementCurrentGroup();
            var curGroupID = Undo.GetCurrentGroup();

            InfiniteLandsNode result = Activator.CreateInstance(type) as InfiniteLandsNode;
            RecordAction("Created Node from JSON");
            
            JsonUtility.FromJsonOverwrite(JsonData, result);
            ConfigureNode(result, position);
            
            Undo.SetCurrentGroupName(string.Format("{0}: Creating Node", this.name));
            Undo.CollapseUndoOperations(curGroupID);
            NotifyValuesChangedAfter();
            return result;
        }
        public T CreateNode<T>(Vector2 position, bool record = true) where T : InfiniteLandsNode, new()
        {  
            return CreateNode(typeof(T), position, record) as T;
        }

        public InfiniteLandsNode CreateNode(Type type, Vector2 position, bool record = true)
        {  
            if(typeof(InfiniteLandsNode).IsEquivalentTo(type))
                return null;

            var curGroupID = 0;
            if (record)
            {
                Undo.IncrementCurrentGroup();
                curGroupID = Undo.GetCurrentGroup();
            }

            NotifyValuesChangedBefore();
            var attribute = type.GetCustomAttribute<CustomNodeAttribute>();
            InfiniteLandsNode node = Activator.CreateInstance(type) as InfiniteLandsNode;
            node.expanded = !attribute.startCollapsed;
           
            if(record)
                RecordAction("Created Node");
            ConfigureNode(node, position, record);

            if (record)
            {
                Undo.SetCurrentGroupName(string.Format("{0}: Creating {1}", this.name, type.Name));
                Undo.CollapseUndoOperations(curGroupID);
            }

            NotifyValuesChangedAfter();
            return node;
        }

        public void RemoveNode(InfiniteLandsNode node)
        {
            NotifyValuesChangedBefore();
            Undo.IncrementCurrentGroup();
            var curGroupID = Undo.GetCurrentGroup();
            node.OnDeleteNode();
            
            RecordAction("Remove Node");
            edges.RemoveAll(a => a.inputPort.nodeGuid.Contains(node.guid));
            edges.RemoveAll(a => a.outputPort.nodeGuid.Contains(node.guid));
            GraphTools.MarkNodeAsInvalid(node.guid);

            nodes.Remove(node);
            nodesByGuid.Remove(node.guid);
            
            Undo.SetCurrentGroupName(string.Format("{0}: Removing {1}", this.name, node.GetType().Name));
            Undo.CollapseUndoOperations(curGroupID);

            NotifyValuesChangedAfter();
        }

        #endregion

        #region Edges
        public bool AddConnection(EdgeConnection connection, bool deleteOtherConnections = true)
        {
            NotifyValuesChangedBefore();
            bool isValid = ValidateConnection(connection, deleteOtherConnections);
            if (isValid)
            {
                Undo.RecordObject(this, string.Format("Added Connection from {0}:{1} to {2}:{3}", nodesByGuid[connection.outputPort.nodeGuid],
                    connection.outputPort.fieldName, nodesByGuid[connection.inputPort.nodeGuid], connection.inputPort.fieldName));
                if (connection.inputPort.listIndex >= 0)
                    InsertEdge(connection);
                else
                    edges.Add(connection);
            }
            NotifyValuesChangedAfter();
            return isValid;
        }

        private bool ValidateConnection(EdgeConnection connection, bool deleteOtherConnections){
            PortData outputData = connection.outputPort;
            PortData inputData = connection.inputPort;

            nodesByGuid.TryGetValue(inputData.nodeGuid, out InfiniteLandsNode inputNode);
            nodesByGuid.TryGetValue(outputData.nodeGuid, out InfiniteLandsNode outputNode);

            if (inputNode == null || outputNode == null)
                return false;

            return ValidateConnection(inputNode, ref connection.inputPort, this, deleteOtherConnections);
        }

        private bool ValidateConnection(InfiniteLandsNode node, ref PortData data, IGraph graph, bool deleteOtherConnections){
            string fieldName = data.fieldName;
            var inputFields = node.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
                .Where(a => a.GetCustomAttribute<InputAttribute>() != null);
            FieldInfo field = inputFields.FirstOrDefault(a => a.Name.Equals(fieldName));

            if(field == null)
                return false;
            if (typeof(IEnumerable).IsAssignableFrom(field.FieldType))
            {
                //If it's a new one add with index
                if (data.isAddItem)
                {
                    var connections = graph.GetAllEdges();
                    var currentCount = connections.Where(a => a.inputPort.nodeGuid.Equals(node.guid) && a.inputPort.fieldName.Equals(field.Name)).Count();
                    data.isAddItem = false;
                    data.listIndex = currentCount;
                }
            }
            
            if(data.listIndex < 0){ //Make sure that there's no more like that, and delete the others if so
                var similarConnection = graph.GetAllEdges().Where(a => a.inputPort.nodeGuid.Equals(node.guid) && a.inputPort.fieldName.Equals(fieldName)).ToArray();
                if(deleteOtherConnections){
                    foreach(var connection in similarConnection){
                        RemoveConnection(connection);  
                    }
                }
                else if(similarConnection.Any())
                    return false;
            }

            return true;
        }
        public void RemoveConnection(EdgeConnection connection)
        {
            NotifyValuesChangedBefore();
            if (connection == null)
                return;
            var inputNode = nodesByGuid.ContainsKey(connection.inputPort.nodeGuid) ? nodesByGuid[connection.inputPort.nodeGuid].name : "Unkown";
            var outputNode = nodesByGuid.ContainsKey(connection.outputPort.nodeGuid) ? nodesByGuid[connection.outputPort.nodeGuid].name : "Unkown";

            Undo.RecordObject(this, string.Format("Removed Connection from {0}:{1} to {2}:{3}", outputNode, connection.outputPort.fieldName, inputNode, connection.inputPort.fieldName));
            edges.Remove(connection);
            
            NotifyValuesChangedAfter();
        }

        public void RemoveConnection(PortData inputPort, PortData outputPort)
        {
            EdgeConnection targetConnection = edges.Find(a =>
                a.outputPort.Equals(outputPort) &&  a.inputPort.Equals(inputPort));
            RemoveConnection(targetConnection);
        }

        #endregion

        #region Groups
        
        public GroupBlock CreateGroupFromJson(string JsonData, Vector2 position){            
            Undo.IncrementCurrentGroup();
            var curGroupID = Undo.GetCurrentGroup();

            GroupBlock result = new GroupBlock();
            JsonUtility.FromJsonOverwrite(JsonData, result);
            ConfigureAsset(result, position);
            return result;
        }
        public GroupBlock CreateGroup(string name, Vector2 position, List<string> elementsGUIDS){
            Undo.IncrementCurrentGroup();
            var curGroupID = Undo.GetCurrentGroup();

            GroupBlock block = new GroupBlock
            {
                Name = name,
                elementGuids = new List<string>(elementsGUIDS),
            };
            ConfigureAsset(block, position);

            Undo.SetCurrentGroupName(string.Format("{0}: Creating group", this.name));
            Undo.CollapseUndoOperations(curGroupID);
            return block;
        }

        public void Remove(GroupBlock group){
            RecordAction("Removed Group");
            groups.Remove(group);
        }

        public void AddElementsToGroup(GroupBlock group, IEnumerable<string> guids)
        {
            RecordAction("Added item to Group");
            group.elementGuids.AddRange(guids);
        }

        public void RemoveElementsFromGroup(GroupBlock group, IEnumerable<string> guids)
        {
            RecordAction("Removed item from group");
            foreach(string guid in guids){
                group.elementGuids.Remove(guid);
            }
        }

        #endregion

        #region StickyNotes
        public StickyNoteBlock CreateStickyNoteFromJson(string JsonData, Vector2 position){
            Undo.IncrementCurrentGroup();
            var curGroupID = Undo.GetCurrentGroup();

            StickyNoteBlock result = new StickyNoteBlock();
            JsonUtility.FromJsonOverwrite(JsonData, result);
            ConfigureAsset(result, position);

            Undo.SetCurrentGroupName(string.Format("{0}: Creating Sticky Node", this.name));
            Undo.CollapseUndoOperations(curGroupID);
            return result;
        }
        public StickyNoteBlock CreateStickyNote(Vector2 position){
            Undo.IncrementCurrentGroup();
            var curGroupID = Undo.GetCurrentGroup();

            StickyNoteBlock sn = new StickyNoteBlock(){
                title = "New note",
                content = "Add content here",
            };      
            ConfigureAsset(sn, position);
                         
            Undo.SetCurrentGroupName(string.Format("{0}: Creating Sticky Node", this.name));
            Undo.CollapseUndoOperations(curGroupID);
            return sn;
        }

        public void Remove(StickyNoteBlock stickyNote){
            RecordAction("Remove Sticky Note");
            stickyNotes.Remove(stickyNote);
        }
        #endregion

        #region Configure
        private void ConfigureNode(InfiniteLandsNode node, Vector2 position, bool record = true){
            if(record)
                RecordAction("Configuring it");
            node.SetupNode(GUID.Generate().ToString(), position);

            if (record)
                RecordAction("Added Node " + node.GetType().Name);
            nodes.Add(node);
            nodesByGuid.Add(node.guid, node);
        }
        
        private void ConfigureAsset(StickyNoteBlock note, Vector2 position){
            note.guid = GUID.Generate().ToString();
            note.position = position;
            note.size = new Vector2(300,200);

            RecordAction("Added Sticky Note");
            stickyNotes.Add(note);
        }

        private void ConfigureAsset(GroupBlock block, Vector2 position){
            block.guid = GUID.Generate().ToString();
            block.position = position;

            RecordAction("Added Group");
            groups.Add(block);
        }
        #endregion
        
        public void RecordAction(string action){
            Undo.RecordObject(this, string.Format("{0}: {1}", this.name, action));
            EditorUtility.SetDirty(this);
        }

		[MenuItem("Assets/Create/Infinite Lands/Simple Biome", priority = 102)]
        public static void CreateDefaultBiome(){
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(0, 
                CreateInstance<OnceCreated>(), 
                "Infinite Biome.asset", EditorGUIUtility.IconContent("ScriptableObject Icon").image as Texture2D, null);
        }

        public void CopyDataFromTo(InfiniteLandsNode from, InfiniteLandsNode to)
        {
            IEnumerable<FieldInfo> FromFields = from.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).Where(a => 
                    a.GetCustomAttribute<HideInInspector>() == null &&
                    a.GetCustomAttribute<InputAttribute>() == null &&
                    !a.FieldType.IsClass);
            IEnumerable<FieldInfo> TargetFields = to.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance).Where(a => 
                    a.GetCustomAttribute<HideInInspector>() == null &&
                    a.GetCustomAttribute<InputAttribute>() == null &&
                    !a.FieldType.IsClass);
            RecordAction("Copying values from node into another node");
            foreach(FieldInfo field in FromFields){
                FieldInfo similarField = TargetFields.FirstOrDefault(a => EqualFieldInfo(field, a));
                if(similarField != null){
                    similarField.SetValue(to, field.GetValue(from));
                }
            }
        }
        private static bool EqualFieldInfo(FieldInfo A, FieldInfo B){
            return A.FieldType.Equals(B.FieldType) &&
                    A.Name.Equals(B.Name);
        }

        public static void PreconfigureDefaultGraph(TerrainGenerator world){
            if(world.output == null)
             world.output = world.CreateNode<HeightOutputNode>(Vector2.zero);
            SimplexNoiseNode simplex = world.CreateNode<SimplexNoiseNode>(new Vector2(-235,0));
            simplex.TileSize = 200;
            simplex.MinMaxHeight = new Vector2(0,250);
            simplex.Octaves = 5;
            EdgeConnection connection = new EdgeConnection(new PortData(simplex.guid, nameof(simplex.Output)), new PortData(world.output.guid, nameof(world.output.HeightMap))); 
            world.AddConnection(connection, true);
        }


        class OnceCreated : UnityEditor.ProjectWindowCallback.EndNameEditAction
        {
            public override void Action(int instanceId, string pathName, string resourceFile)
            {
                BiomeTree biome = CreateInstance<BiomeTree>();
                biome.name = Path.GetFileName(pathName);
                AssetDatabase.CreateAsset(biome, pathName);
                PreconfigureDefaultGraph(biome);
                ProjectWindowUtil.ShowCreatedAsset(biome);
            } 
        }
        #endif
        #endregion
    }
}