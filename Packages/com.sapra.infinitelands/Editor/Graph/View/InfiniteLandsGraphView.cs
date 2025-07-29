using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;
using UnityEditor;
using System.Linq;
using System;
using UnityEditor.UIElements;
using System.Reflection;

namespace sapra.InfiniteLands.Editor{
    #if UNITY_6000_0_OR_NEWER
    [UxmlElement]
    #endif

    public partial class InfiniteLandsGraphView : GraphView
    {
        #if !UNITY_6000_0_OR_NEWER
        public new class UxmlFactory : UxmlFactory<InfiniteLandsGraphView, UxmlTraits>{}
#endif

        public InfiniteLandsWindow editorWindow;
        private TerrainGenerator tree;
        public SerializedObject serializedTree;
        public IGraph targetGraph => tree;

        public Action RegenerateTerrain;
        public Vector2 MousePosition;
        private bool RequestedPreviewRedraw = false;
        public Dictionary<string, GraphElement> ElementsByGuid = new();

        public NodeView GetNodeViewByGuid(string nodeGuid){
            if(!ElementsByGuid.TryGetValue(nodeGuid, out var nodeView)){
                nodeView = null;
            }
            return nodeView as NodeView;
        }

        public GraphElement GetElementViewByGuid(string guid){
            if(!ElementsByGuid.TryGetValue(guid, out var nodeView)){
                nodeView = null;
            }
            return nodeView;
        }
        public InfiniteLandsGraphView()
        {
            GraphFactory.InitializeCacheItems();
            
            Insert(0, new GridBackground());
            var styleSheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>(UIStyles.WindowStyles);
            if (styleSheets != null && styleSheet != null)
                styleSheets.Add(styleSheet);
            else
                return;

            UnregisterCallback<KeyDownEvent>(OnKeyDownPressed);
            UnregisterCallback<MouseMoveEvent>(MouseMove);
            UnregisterCallback<GeometryChangedEvent>(FocusAfterCreation);

            canPasteSerializedData -= DataSerializer.CanPaste;
            serializeGraphElements -= SerializeWithGraph;
            unserializeAndPaste -= UnserializeAndPaste;
            deleteSelection -= DeleteSelection;


            SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale * 4f);
            this.AddManipulator(new ContentDragger());
            this.AddManipulator(new SelectionDragger());
            this.AddManipulator(new RectangleSelector());
            this.AddManipulator(new DragAndDropManipulator(this));

            RegisterCallback<MouseMoveEvent>(MouseMove);
            RegisterCallback<KeyDownEvent>(OnKeyDownPressed);
            RegisterCallback<GeometryChangedEvent>(FocusAfterCreation);

            serializeGraphElements += SerializeWithGraph;
            canPasteSerializedData += DataSerializer.CanPaste;
            unserializeAndPaste += UnserializeAndPaste;
            deleteSelection += DeleteSelection;           
        }

        
        public void FocusAfterCreation(GeometryChangedEvent changedEvent)
        {
            if (tree != null && tree.position == Vector2.zero)
                FrameAll();
        }

        public void ReloadView()
        {
            graphViewChanged -= OnGraphViewChanged;
            elementsAddedToGroup -= OnElementsAddedToGroup;
            elementsRemovedFromGroup -= OnElementsRemovedFromGroup;
            viewTransformChanged -= UpdateGraphViewPosition;
            DeleteElements(graphElements);

            graphViewChanged += OnGraphViewChanged;
            elementsAddedToGroup += OnElementsAddedToGroup;
            elementsRemovedFromGroup += OnElementsRemovedFromGroup;
            viewTransformChanged += UpdateGraphViewPosition;

            LoadTreeView();
        }

        public void Initialize(TerrainGenerator terrainGenerator)
        {
            if (tree != null)
                tree.OnValuesChangedAfter -= TrigerPreview;

            this.tree = terrainGenerator;
            tree.OnValuesChangedAfter -= TrigerPreview;
            tree.OnValuesChangedAfter += TrigerPreview;
            this.Unbind();

            serializedTree = new SerializedObject(tree);
            ReloadView();
        }

        private void UpdateGraphViewPosition(GraphView graphView){
            tree.position = graphView.viewTransform.position;
            tree.scale = graphView.viewTransform.scale;
        }

        private string SerializeWithGraph(IEnumerable<GraphElement> elements)
        {
            return DataSerializer.SerializeGraphElements(elements, tree);
        }
        
        private void MouseMove(MouseMoveEvent moveEvent)
        {
            MousePosition = moveEvent.mousePosition;
        }
        
        private void OnKeyDownPressed(KeyDownEvent evt)
        {
            ShortcutHandler.OnShortcutPressed(evt, this, selection);
        }
        public NodeView FindNodeView(string guid)
        {   
            Node nd = GetNodeViewByGuid(guid);
            if (nd is NodeView view)
            {
                return view;
            }
            else{
                if(nd == null){
                    var created = tree.CreateNode<MissingNode>(Vector2.zero);
                    created.guid = guid;
                    var missingView = GraphViewersFactory.CreateNodeView(this, created);
                    AddNode(missingView);
                    Debug.LogWarningFormat("Missing {0}", guid);
                    return missingView;
                }
            }

            return null;
        }

        private void LoadTreeView()
        {
            ElementsByGuid.Clear();
            if (tree.scale == Vector2.zero)
                tree.scale = Vector2.one;

            tree.ValidationCheck();

            var nodes = DebugOptions.ShowFullComplexGraph ? tree.GetAllNodes() : tree.GetBaseNodes();
            var edges = DebugOptions.ShowFullComplexGraph ? tree.GetAllEdges() : tree.GetBaseEdges();

            DrawData(nodes, edges, tree.stickyNotes, tree.groups);
            UpdateViewTransform(tree.position, tree.scale);
        }

        public void DrawData(IEnumerable<InfiniteLandsNode> nodes, IEnumerable<EdgeConnection> edges, IEnumerable<StickyNoteBlock> stickyNotes, IEnumerable<GroupBlock> groups, HashSet<NodeView> toRedraw = null)
        {
            foreach (InfiniteLandsNode node in nodes.Reverse())
            {
                NodeView nodeView = GraphViewersFactory.CreateNodeView(this, node);
                AddNode(nodeView);
            }

            if (toRedraw != null)
            {
                RedrawNodes(toRedraw);
            }

            foreach (EdgeConnection edge in edges)
            {
                ConnectEdge(edge);
            }

            foreach (StickyNoteBlock note in stickyNotes)
            {
                AddStickyNoteView(GraphViewersFactory.CreateStickyNoteView(note));
            }

            foreach (GroupBlock group in groups)
            {
                List<GraphElement> elements = group.elementGuids.Select(a => GetElementViewByGuid(a)).ToList();
                GroupView groupView = GraphViewersFactory.CreateGroupView(group, elements);
                AddGroupView(groupView);
            }
        }

        private void ConnectEdge(EdgeConnection connection)
        {
            NodeView inputNodeView = FindNodeView(connection.inputPort.nodeGuid);
            Port inputPort = null;
            if (inputNodeView != null)
            {
                inputPort = inputNodeView.ports.Find(a => ((PortData)a.userData).Equals(connection.inputPort));
            }


            NodeView outputNodeView = FindNodeView(connection.outputPort.nodeGuid);
            Port outputPort = null;
            if (outputNodeView != null)
            {
                outputPort = outputNodeView.ports.Find(a => ((PortData)a.userData).Equals(connection.outputPort));
            }

            bool inputHidden = inputPort != null && inputPort.style.display == DisplayStyle.None;
            bool outputHidden = outputPort != null && outputPort.style.display == DisplayStyle.None;

            bool onlyInputHidden = inputHidden && !outputHidden;
            bool onlyOutputHidden = !inputHidden && outputHidden;

            if (inputPort == null || onlyInputHidden)
            {
                inputPort = inputNodeView.ports.Find(a => ((PortData)a.userData).fieldName.Equals(connection.inputPort.fieldName) && a.portType.Equals(typeof(MISSING)));
                if (inputPort == null)
                    inputPort = EditorTools.AddMissingPort(inputNodeView, connection.inputPort, Direction.Input);
            }

            if (outputPort == null ||
                (!inputPort.portType.IsAssignableFrom(outputPort.portType) && outputPort.portType != typeof(MISSING) && inputPort.portType != typeof(MISSING))
                || onlyOutputHidden)
            {
                outputPort = outputNodeView.ports.Find(a => ((PortData)a.userData).fieldName.Equals(connection.outputPort.fieldName) && a.portType.Equals(typeof(MISSING)));
                if (outputPort == null)
                    outputPort = EditorTools.AddMissingPort(outputNodeView, connection.outputPort, Direction.Output);
            }

            if (inputPort != null && outputPort != null)
            {
                AddEdge(inputPort, outputPort);
            }
        }

        void CreateRerouteOnDoubleClick(Edge edge, MouseDownEvent vnt){
            if(vnt.clickCount >= 2){
                Vector3 size = new Vector3(70/2.0f, 30,0);
                var created = tree.CreateNode<RerouteNode>(viewTransform.matrix.inverse.MultiplyPoint(vnt.mousePosition)-size);
                NodeView nodeView = GraphViewersFactory.CreateNodeView(this, created);
                AddNode(nodeView);

                var targetInputPort = nodeView.ports.Where(a => a.direction==Direction.Input).FirstOrDefault();
                var targetOutputPort = nodeView.ports.Where(a => a.direction==Direction.Output).FirstOrDefault();

                var targetInput = (PortData)targetInputPort.userData;
                var targetOutput = (PortData)targetOutputPort.userData;
                var oldInput = (PortData)edge.input.userData;
                var oldOutput = (PortData)edge.output.userData;

                tree.RemoveConnection(oldInput, oldOutput);
                bool validGo = tree.AddConnection(new EdgeConnection(oldOutput, targetInput));
                bool validBack = tree.AddConnection(new EdgeConnection(targetOutput, oldInput));
                if (!validGo && !validBack)
                    Debug.LogError("Something went wrong creating the reroute node");
                
                RedrawNodes(new HashSet<NodeView>(){
                    nodeView,
                    edge.input.node as NodeView,
                    edge.output.node as NodeView
                });
            }
        }

        
        private void OnElementsAddedToGroup(Group group, IEnumerable<GraphElement> elements)
        {
            GroupView groupView = group as GroupView;
            List<string> guids = elements.Select(a => a.viewDataKey).ToList();
            tree.AddElementsToGroup(groupView.group, guids);
        }

        private void OnElementsRemovedFromGroup(Group group, IEnumerable<GraphElement> elements)
        {
            GroupView groupView = group as GroupView;
            List<string> guids = elements.Select(a => a.viewDataKey).ToList();
            tree.RemoveElementsFromGroup(groupView.group, guids);
        }

        public void OnAssetDropped(InfiniteLandsAsset asset, Vector2 position){           
            var nodeType = asset.GetType().GetCustomAttribute<AssetNodeAttribute>();
            if(nodeType != null){
                InfiniteLandsNode nd = tree.CreateNode(nodeType.DefaultNode, viewTransform.matrix.inverse.MultiplyPoint(position));
                ISetAsset loader = nd as ISetAsset;
                if(loader != null){
                    loader.SetAsset(asset);
                }
                NodeView nodeView = GraphViewersFactory.CreateNodeView(this, nd);
                AddNode(nodeView);
            }
        }

        private GraphViewChange OnGraphViewChanged(GraphViewChange graphViewChange)
        {
            HashSet<NodeView> NeedUpdate = new();
            if(graphViewChange.elementsToRemove != null){
                graphViewChange.elementsToRemove.ForEach(elem =>
                {
                    Edge edge = elem as Edge;
                    if (edge != null)
                    {
                        var inputUserData = (PortData)edge.input.userData;
                        var outputUserData = (PortData)edge.output.userData;

                        NodeView outputNode = GetNodeViewByGuid(outputUserData.nodeGuid);
                        NodeView inputNode = GetNodeViewByGuid(inputUserData.nodeGuid);

                        tree.RemoveConnection(inputUserData, outputUserData);
                        
                        NeedUpdate.Add(outputNode);
                        NeedUpdate.Add(inputNode);
                    }
                });
                graphViewChange.elementsToRemove.Clear();
            }

            if(graphViewChange.edgesToCreate != null){
                for(int i = graphViewChange.edgesToCreate.Count-1; i >= 0; i--){
                    Edge edge = graphViewChange.edgesToCreate[i];
                    var inputUserData = (PortData)edge.input.userData;
                    var outputUserData = (PortData)edge.output.userData;
                    
                    NodeView outputNode = GetNodeViewByGuid(outputUserData.nodeGuid);
                    NodeView inputNode = GetNodeViewByGuid(inputUserData.nodeGuid);

                    EdgeConnection connection = new EdgeConnection(outputUserData, inputUserData);
                    tree.AddConnection(connection);
                    
                    NeedUpdate.Add(outputNode);
                    NeedUpdate.Add(inputNode);

                    foreach(NodeView nd in nodes){
                        if(nd.TriggerRedrawOnConnection)
                            NeedUpdate.Add(nd);
                    }
                }
                graphViewChange.edgesToCreate.Clear();
            }
            RedrawNodes(NeedUpdate);
            return graphViewChange;
        }

        public void RedrawNodes(HashSet<NodeView> nodes){
            if(nodes.Count > 0){
                tree.ValidationCheck();
                foreach(var nodeView in nodes){
                    if(nodeView.node != null){
                        nodeView.Redraw();
                        nodeView.AddSelfToElements(this);
                    }
                }

                foreach(var nodeView in nodes){
                    if(nodeView.node != null)
                        EdgeRedraw(nodeView);
                }
            }
        }

        public void RedrawNode(NodeView node){
            RedrawNodes(new HashSet<NodeView>(){node});
        }

        public void EdgeRedraw(NodeView reciver){
            var targetNode = reciver.node.guid;
            RemoveEdgesToNode(reciver);
            EditorTools.RemoveMissingPorts(reciver);

            var edgesRelated = tree.GetBaseEdges().Where(a => a.inputPort.nodeGuid.Contains(targetNode) || a.outputPort.nodeGuid.Contains(targetNode));
            foreach(var edg in edgesRelated){
                ConnectEdge(edg);
            }
        }        
        
        void RemoveEdgesToNode(NodeView reciver){
            var connectedOutput = edges.Where(a => a.output != null && NodeEqualFromGuid(reciver, ((PortData)a.output.userData).nodeGuid));
            var connectedInputs = edges.Where(a => a.input != null && NodeEqualFromGuid(reciver, ((PortData)a.input.userData).nodeGuid));

            foreach (var outp in connectedOutput)
            {
                RemoveEdge(outp);
            }
            foreach(var outp in connectedInputs){
                RemoveEdge(outp);
            }
        }
        
        private bool NodeEqualFromGuid(NodeView node, string lookFor)
        {
            if (!ElementsByGuid.TryGetValue(lookFor, out var result))
                return false;
            return node.Equals(result);
        }

        public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
        {
            return ports.Where(endport =>
                endport != startPort &&
                endport.direction != startPort.direction &&
                endport.node != startPort.node &&
                endport.portType.IsAssignableFrom(startPort.portType)).ToList();
        }

        #region Elements
        public void AddStickyNoteView(StickyNoteView noteView){
            ElementsByGuid.Add(noteView.viewDataKey, noteView);
            AddElement(noteView);
        }

        public void RemoveStickyNoteView(StickyNoteView noteView){
            ElementsByGuid.Remove(noteView.viewDataKey);
            RemoveElement(noteView);
        }

        public void AddGroupView(GroupView groupView){
            groupView.AddManipulator(new ContextualMenuManipulator(BuildContextualMenu));
            ElementsByGuid.Add(groupView.viewDataKey, groupView);
            AddElement(groupView);
        }
        
        public void RemoveGroupView(GroupView groupView){
            ElementsByGuid.Remove(groupView.viewDataKey);
            RemoveElement(groupView);
        }

        public void AddEdge(Port inputPort, Port outputPort){
            if (inputPort.direction == outputPort.direction)
            {
                Debug.LogError("Same direction! Error");
                return;
            }
            Edge edge = outputPort.ConnectTo(inputPort);
            edge.RegisterCallback<MouseDownEvent>(e => CreateRerouteOnDoubleClick(edge, e));
            
            ElementsByGuid.Add(edge.viewDataKey, edge);
            AddElement(edge);
        }
        public void RemoveEdge(Edge edge){
            ElementsByGuid.Remove(edge.viewDataKey);
            RemoveElement(edge);
        }

        public void AddNode(NodeView nodeView)
        {
            nodeView.OnPropertyModified += OnPropertiesModified;
            nodeView.OnTogglePreview += TrigerPreview;

            nodeView.AddSelfToElements(this);
            AddElement(nodeView);
        }

        private bool activeDelay;
        public void DelayBind()
        {
            if (!activeDelay)
            {
                EditorApplication.delayCall += InternalBind;
                activeDelay = true;
            }
        }
        private void InternalBind()
        {
            this.Bind(serializedTree);
            activeDelay = false;
        }
        public void AddElementToDictionary(string guid, GraphElement element, bool removeAndAdd = false)
        {
            if (!ElementsByGuid.TryAdd(guid, element))
            {
                if (removeAndAdd)
                {
                    var currentElement = ElementsByGuid[guid];
                    if (currentElement != element)
                    {
                        RemoveElement(currentElement);
                        ElementsByGuid.Remove(guid);
                        ElementsByGuid.Add(guid, element);
                    }
                }
            }
        }
        public void RemoveNode(NodeView nodeView, bool delete){
            nodeView.OnTogglePreview -= TrigerPreview;
            nodeView.OnPropertyModified -= OnPropertiesModified;

            ElementsByGuid.Remove(nodeView.viewDataKey);
            RemoveElement(nodeView);

            if (nodeView.node != null)
            {
                if (delete)
                    tree.RemoveNode(nodeView.node);
            }

        }
        #endregion

        private void OnPropertiesModified(){
            TrigerPreview();
            editorWindow.TriggerTreeUpdate();
        }
        public void TrigerPreview(){
            if(!RequestedPreviewRedraw){
                RequestedPreviewRedraw = true;
                EditorApplication.delayCall += () => {
                    RegenerateTerrain?.Invoke();
                    RequestedPreviewRedraw = false;
                };
            }
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            GraphFactory.BuildContextualMenu(this, evt);
            DataSerializer.BuildContextualMenu(evt, tree);
            var targetNode = evt.target as NodeView;
            evt.target = this;
            base.BuildContextualMenu(evt);
            DebugOptions.BuildContextualMenu(this, evt);
            ShortcutHandler.BuildContextualMenu(targetNode, this, evt);
            evt.StopImmediatePropagation();
        }

        public void asd()
        {
            base.BuildContextualMenu(default);
        }

        #region Legacy Contextual Menu
        private void DeleteSelection(string operationName, AskUser askUser)
        {
            List<GraphElement> elements = selection.OfType<GraphElement>().ToList();
            Undo.IncrementCurrentGroup();
            var curGroupID = Undo.GetCurrentGroup();
            HashSet<NodeView> NeedUpdate = new HashSet<NodeView>();
            bool deletedNodes = false;
            foreach (GraphElement element in elements)
            {
                if (element is NodeView nodeView)
                {
                    if (nodeView.node != null)
                    {
                        var nodeType = nodeView.node.GetType();
                        CustomNodeAttribute attribute = nodeType.GetCustomAttribute<CustomNodeAttribute>();
                        if (attribute != null && !attribute.canDelete)
                            continue;
                    }

                    //Getting side to side from node to update     
                    var connectedOutput = edges
                        .Where(a => a.output != null && ((PortData)a.output.userData).nodeGuid.Contains(nodeView.viewDataKey))
                        .Select(a => GetElementViewByGuid(((PortData)a.input.userData).nodeGuid))
                        .Cast<NodeView>().Where(a => a != null);
                    var connectedInputs = edges
                        .Where(a => a.input != null && ((PortData)a.input.userData).nodeGuid.Contains(nodeView.viewDataKey))
                        .Select(a => GetElementViewByGuid(((PortData)a.output.userData).nodeGuid))
                        .Cast<NodeView>().Where(a => a != null);
                    NeedUpdate.UnionWith(connectedOutput);
                    NeedUpdate.UnionWith(connectedInputs);

                    RemoveNode(nodeView, true);
                    deletedNodes = true;
                }
                if (element is GroupView groupView)
                {
                    tree.Remove(groupView.group);
                    RemoveGroupView(groupView);
                }
                if (element is StickyNoteView stickyNote)
                {
                    tree.Remove(stickyNote.note);
                    RemoveStickyNoteView(stickyNote);
                }
                if (element is Edge edge)
                {
                    if (edge.input.style.display != DisplayStyle.None && edge.output.style.display != DisplayStyle.None)
                    {
                        var inputUserData = (PortData)edge.input.userData;
                        var outputUserData = (PortData)edge.output.userData;

                        var inputNode = edge.input.node as NodeView;
                        var outputNode = edge.output.node as NodeView;

                        NeedUpdate.Add(inputNode);
                        NeedUpdate.Add(outputNode);

                        tree.RemoveConnection(inputUserData, outputUserData);
                        RemoveEdge(edge);
                    }
                }
            }

            if (deletedNodes)
            {
                foreach (NodeView views in ElementsByGuid.Values.OfType<NodeView>())
                {
                    views.DrawAllProperties();
                }
            }
            Undo.SetCurrentGroupName(string.Format("{0}: Deleting data", tree.GetType().Name));
            Undo.CollapseUndoOperations(curGroupID);
            RedrawNodes(NeedUpdate);
        }

        private void UnserializeAndPaste(string operationName, string data)
        {
            Vector2 position = viewTransform.matrix.inverse.MultiplyPoint(MousePosition);
            var generatedData = DataSerializer.UnserializeAndPaste(position, data, tree);
            HashSet<NodeView> views = new();
            foreach (var node in generatedData.reloadable)
            {
                views.Add(GetNodeViewByGuid(node));
            }
            DrawData(generatedData.newNodes, generatedData.newConnections, generatedData.newNotes, generatedData.newGroups, views);
        }
        #endregion

    }
}