using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor{

    public class NodeView : Node, IRenderSerializableGraph
    {
        public InfiniteLandsNode node;
        public InfiniteLandsGraphView treeView;
        public IGraph graph;
        public object GetDataToSerialize()=> node;
        Vector2 IRenderSerializableGraph.GetPosition()=> node.position;
        public string GetGUID()=> node.guid;

        public List<Port> ports = new();
        public SerializedObject obj;

        VisualElement previewContainer;
        VisualElement propertiesContainer;
        VisualElement buttonsContainer;
        VisualElement hiddenProperties;

        CustomNodeAttribute attribute;

        public Action OnPropertyModified;
        public Action OnTogglePreview;

        public List<Toggle> GeneratedToggles = new();
        public OutputPreview ActivePreview;

        public virtual bool TriggerRedrawOnConnection => false;
        public virtual bool OverrideCollapsible => false;
        public virtual bool TriggerRedrawOnFieldChange { get; private set; }

        private const string expandedName = "expanded";
        private const string collapsedName = "collapsed";

        public virtual void AddSelfToElements(InfiniteLandsGraphView view)
        {
            view.AddElementToDictionary(viewDataKey, this);
        }

        
        public NodeView(InfiniteLandsGraphView view, InfiniteLandsNode node)
        {
            this.treeView = view;
            this.obj = view.serializedTree;
            this.graph = view.targetGraph;

            this.node = node;
            this.viewDataKey = node?.guid;
            DebugOptions.ApplyColorMode(this);
            SetTicksColor();
            hiddenProperties = new();
            hiddenProperties.style.display = DebugOptions.DebugMode ? DisplayStyle.Flex : DisplayStyle.None;
            propertiesContainer = new();

            buttonsContainer = new();
            buttonsContainer.AddToClassList("more_buttons_layout");

            previewContainer = new();
            previewContainer.AddToClassList("PreviewContainer");
            previewContainer.style.display = DisplayStyle.None;

            extensionContainer.Clear();
            extensionContainer.Add(propertiesContainer);
            extensionContainer.Add(buttonsContainer);
            extensionContainer.Add(previewContainer);
            extensionContainer.Add(hiddenProperties);

            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UIStyles.NodeViewStyles));
            styleSheets.Add(AssetDatabase.LoadAssetAtPath<StyleSheet>(UIStyles.NodeViewTitleStyles));

            if (node == null)
                return;

            attribute = node.GetType().GetCustomAttribute<CustomNodeAttribute>();

            if (attribute == null || !attribute.canDelete)
            {
                capabilities -= Capabilities.Deletable;
            }

            style.left = node.position.x;
            style.top = node.position.y;

            VisualElement expandable = mainContainer.Q<VisualElement>("collapse-button");
            expandable.RegisterCallback<ClickEvent>(OnCollapseClicked);

            var nodeProperty = GetNodeProperty(node);
            if (nodeProperty != null)
            {
                var validNodeProperty = nodeProperty.FindPropertyRelative(nameof(InfiniteLandsNode.isValid));
                this.TrackPropertyValue(validNodeProperty, (a) => ValidityCheck());
            }
            Redraw();
        }

        public void SetTicksColor()
        {
            var titleElement = this.Q<VisualElement>("title");
            if (DebugOptions.colorMode == DebugOptions.ColorMode.Heat)
            {
                var ticks = node.RetriveTimings();
                var gradientValue = Mathf.Clamp01(ticks / 30000.0f);
                titleElement.style.backgroundColor = Color.HSVToRGB(.6f, 0.7f, gradientValue);
            }
            else
            {
                titleElement.style.backgroundColor = StyleKeyword.Null;

            }
        }

        public void Redraw()
        {
            this.Unbind();

            GenerateTitle();
            SetPosition(new Rect(node.position, Vector3.zero));

            FillPortsContainer();
            FillExtensionContainer();
            ForceCollapsed();
            treeView.DelayBind();
        }

        private void FullRedraw()
        {
            treeView.RedrawNode(this);
        }

        private void Unbinder(VisualElement visualElement)
        {
            foreach (var prop in visualElement.Children())
            {
                prop.Unbind();
            }
            visualElement.Clear();
        }

        #region Drawing of Elements
        #region Ports
        public void FillPortsContainer()
        {
            Unbinder(outputContainer);
            Unbinder(inputContainer);

            ports.Clear();

            var inputs = CreateInputPorts();
            var outputs = CreateOutputPorts();
            foreach (VisualElement prt in inputs)
            {
                var isVariable = prt.GetClasses().Contains(PortView.variableHidden);
                if (isVariable) TriggerRedrawOnFieldChange = true;

                if (prt.style.display != DisplayStyle.None || isVariable)
                    inputContainer.Add(prt);
            }

            foreach (VisualElement prt in outputs)
            {
                var isVariable = prt.GetClasses().Contains(PortView.variableHidden);
                if (isVariable) TriggerRedrawOnFieldChange = true;

                if (prt.style.display != DisplayStyle.None || isVariable)
                    outputContainer.Add(prt);
            }

            OnPropertyModified -= FullRedraw;
            if (TriggerRedrawOnFieldChange)
                OnPropertyModified += FullRedraw;

            var inputPorts = inputs.OfType<Port>();
            var outputPorts = outputs.OfType<Port>();

            ports.AddRange(inputPorts);
            ports.AddRange(outputPorts);

            //WORKAROUND for uncollapsable nodes 
            Port hiddenPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Single, typeof(int));
            hiddenPort.style.display = DisplayStyle.None;
            if (inputContainer.childCount > 0)
                inputContainer.Add(hiddenPort);
            else if (outputContainer.childCount > 0)
                outputContainer.Add(hiddenPort);
            //End of workaround
        }
        
        protected virtual List<VisualElement> CreateInputPorts() => this.CreatePorts<InputAttribute>(node, Direction.Input, graph);
        protected virtual List<VisualElement> CreateOutputPorts() => this.CreatePorts<OutputAttribute>(node, Direction.Output, graph);
        #endregion
            #region Properties
        public SerializedProperty GetNodeProperty(InfiniteLandsNode node){
            int index = graph.GetBaseNodes().ToList().IndexOf(node);
            if(index < 0)
                return null;
            obj.Update();
            var allNodesProperty = obj.FindProperty(nameof(TerrainGenerator.nodes));
            var nodeProperty = allNodesProperty.GetArrayElementAtIndex(index);
            return nodeProperty.Copy();
        }
        private void DrawHiddenProperties(VisualElement hiddenProperties, SerializedProperty nodeProperty)
        {
            for (int i = 0; i < 2; i++)
            {
                var validProperty = new PropertyField(nodeProperty);
                hiddenProperties.Add(validProperty);
                nodeProperty.NextVisible(false); //Skip node guid
            }
            hiddenProperties.Bind(treeView.serializedTree);
        }
        protected virtual void DrawProperties(VisualElement propertiesContainer, SerializedProperty nodeProperty){
            var currentDepth = nodeProperty.depth;
            do
            {
                var name = nodeProperty.propertyPath.Split(".").Last();
                var fieldInfo = node.GetType().GetField(name);
                if (fieldInfo == null)
                    continue;

                var isPort = fieldInfo.GetCustomAttribute<InputAttribute>() != null || fieldInfo.GetCustomAttribute<OutputAttribute>() != null;
                if (isPort)
                    continue;
                var prop = new PropertyField(nodeProperty);
                prop.TrackPropertyValue(nodeProperty, TrackChanges);
                propertiesContainer.Add(prop);
            }            
            while (nodeProperty.NextVisible(false) && nodeProperty.depth >= currentDepth);

            propertiesContainer.Bind(treeView.serializedTree);
        }        
        private void ValidityCheck()
        {
            SetValidity(node.isValid);
        }

        public void SetValidity(bool value)
        {
            if (value)
                RemoveFromClassList("invalid");
            else
                AddToClassList("invalid");
        }

        public void DrawAllProperties()
        {
            var nodeProperty = GetNodeProperty(node);
            if (nodeProperty == null)
                return;

            ValidityCheck();

            if (nodeProperty == null)
                return;

            Unbinder(propertiesContainer);
            Unbinder(hiddenProperties);

            var currentDepth = nodeProperty.depth;
            if (nodeProperty.NextVisible(true))
            { //Enter item
                DrawHiddenProperties(hiddenProperties, nodeProperty);

                if (nodeProperty.depth <= currentDepth)
                    return;

                DrawProperties(propertiesContainer, nodeProperty);
            }
            else
            {
                Debug.Log("wooot");
            }
        }

        #endregion
        #region Extension
        protected void TrackChanges(SerializedProperty property)
        {
            OnPropertyModified?.Invoke();
            GraphTools.MarkNodeAsInvalid(GetGUID());
            //graph?.NotifyValuesChanged();
        }
        protected virtual void FillExtensionContainer(){
            DrawAllProperties();
            DrawButtons();
        }
        public virtual void DrawButtons(){
            Unbinder(buttonsContainer);
            DrawPreview();
            DrawDocs();
        }
        protected void GeneratePreviewButton(OutputPreview preview, PortData portData){
            Toggle GenerateImageFoldout = new Toggle();
            GenerateImageFoldout.AddToClassList("image-foldout");
            GenerateImageFoldout.AddToClassList("more_button");
            GenerateImageFoldout.userData = preview;
            GenerateImageFoldout.RegisterValueChangedCallback(a => ChangePreview(portData, a.newValue));
            if(portData.Equals(node.previewPort)){
                GenerateImageFoldout.SetValueWithoutNotify(true);
                UpdatePreviewButtonState(GenerateImageFoldout, true);
            }
            buttonsContainer.Add(GenerateImageFoldout);
            GeneratedToggles.Add(GenerateImageFoldout);
        }
        public void ChangePreview(PortData portData, bool newValue = true){
            var currentToggle = GeneratedToggles?.Where(a => ((OutputPreview)a.userData) != null && ((OutputPreview)a.userData).PortData.Equals(portData)).FirstOrDefault();
            var preview = currentToggle != null ? (OutputPreview)currentToggle.userData : default;
            var target = currentToggle != null ? ((OutputPreview)currentToggle.userData).PortData : default;
            bool toggleState = currentToggle != null && newValue && preview != null && preview.ValidPreview();
            
            ActivePreview = toggleState ? preview : default;
            previewContainer.style.display = toggleState ? DisplayStyle.Flex : DisplayStyle.None;

            graph.RecordAction(string.Format("Changed active preview of {0}:{1}", node.GetType().Name, node.guid));
            node.previewPort = newValue ? target : default;
            foreach(Toggle toggle in GeneratedToggles){
                var targetValue = toggleState && toggle == currentToggle;
                toggle.SetValueWithoutNotify(targetValue);
                UpdatePreviewButtonState(toggle, targetValue);
            }

            if(toggleState)
                OnTogglePreview?.Invoke();
        }
        private void UpdatePreviewButtonState(Toggle toggle, bool value){
            string add = value.ToString();
            string remove = (!value).ToString();
            toggle.RemoveFromClassList(remove);
            toggle.AddToClassList(add);
        }
        protected virtual void DrawPreview(){
            GeneratedToggles.Clear();
            IEnumerable<PortData> portsData = ports
                .Where(a => a.direction==Direction.Output && a.style.display != DisplayStyle.None)
                .Select(a => (PortData)a.userData);
            foreach(PortData portData in portsData){
                InfiniteLandsNode portNode = node;
                if(node.guid != portData.nodeGuid)
                    portNode = graph.GetNodeFromGUID(portData.nodeGuid);
                OutputPreview preview = GraphViewersFactory.CreateOutputPreview(portData, portNode, this);
                if(preview != null)
                    GeneratePreviewButton(preview, portData);
                    
            }
            
            GenerateExtraPreviewButtons();
            ChangePreview(node.previewPort);
        }
        protected virtual void GenerateExtraPreviewButtons(){}

        protected void DrawDocs()
        {
            if(attribute != null && attribute.docs != ""){
                Button OpenDocs = new Button(OpenDocumentation);
                OpenDocs.AddToClassList("documentation");
                OpenDocs.AddToClassList("more_button");
                buttonsContainer.Add(OpenDocs);
            }
        }

        public void OpenDocumentation(){
            Application.OpenURL(attribute.docs);
        }
        #endregion
        #endregion

        public virtual void ForceCollapsed(){
            TriggerExpandedState(node.expanded);
        }

        protected virtual void GenerateTitle()
        {
            CustomNodeAttribute attribute = node.GetType().GetCustomAttribute<CustomNodeAttribute>();
            if (attribute != null)
                this.title = attribute.name;
            else
                this.title = node.GetType().Name;
        }

        private void OnCollapseClicked(ClickEvent E)
        {
            TriggerExpandedState(!node.expanded);
        }
        
        public void TriggerExpandedState(bool value){
            expanded = true;
            RefreshExpandedState();

            VisualElement collapsible = this.Q<VisualElement>("collapsible-area");
            if(collapsible != null){
                collapsible.style.display = value ? DisplayStyle.Flex : DisplayStyle.None;
            }

            graph.RecordAction(string.Format("Changed expanded state of {0}:{1}", node.GetType().Name, node.guid));
            node.expanded = value;

            RemoveFromClassList(value ? collapsedName : expandedName);
            AddToClassList(value ? expandedName : collapsedName);
            
            if (!value)
                ChangePreview(default);
        }
        public void GeneratePreview(BranchData settings, GraphSettings graphSettings){
            Unbinder(previewContainer);
            var image = ActivePreview.GetPreview(settings, graphSettings);
            if(image != null){
                image.AddToClassList("Preview");
                previewContainer.Add(image);
            }     
            else
                ChangePreview(default);
        }
        
        public override void SetPosition(Rect newPos)
        {
            base.SetPosition(newPos);
            if(node == null)
                return;
            graph.RecordAction(string.Format("Moved position of {0}:{1}", node.GetType().Name, node.guid));
            
            node.position.x = newPos.xMin;
            node.position.y = newPos.yMin;
        }
    }
}