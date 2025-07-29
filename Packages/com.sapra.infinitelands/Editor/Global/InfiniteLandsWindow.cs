using System.Linq;
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace sapra.InfiniteLands.Editor{
    public class InfiniteLandsWindow : EditorWindow, IHasCustomMenu
    {
        [SerializeField] TerrainGenerator treeInstance;
        InfiniteLandsGraphView treeView;
        IBurstTexturePool texturePool;

        [SerializeField] int EditorResolution = 100;
        [SerializeField] GraphSettings activeSettings;

        GraphSettings settings{
            get{
                if(internalSettings == null)
                    internalSettings = CreateInstance<GraphSettings>();
                return internalSettings;
            }
        }
        [SerializeField] GraphSettings internalSettings;
        [SerializeField] bool SyncSettings = false;
        [SerializeField] private VisualTreeAsset m_VisualTreeAsset = default;

        ToolbarButton RefreshButton;
        ToolbarButton SaveButton;
        ToolbarButton ExportButton;

        Toggle AutoUpdateToggle;

        Vector2Field WorldOffsetField;
        IntegerField SeedField;
        IntegerField ResolutionField;
        FloatField ScaleField;

        Toggle SyncSettingsToggle;
        
        #region Window Options
        private void OnEnable() {
            Undo.undoRedoPerformed -= OnUndoRedo;
            AssemblyReloadEvents.beforeAssemblyReload -= SaveTree;
            
            GrabUIElements();
        
            AssemblyReloadEvents.beforeAssemblyReload += SaveTree;
            Undo.undoRedoPerformed += OnUndoRedo;
        }
        private void OnDisable(){
            Undo.undoRedoPerformed -= OnUndoRedo;
            AssemblyReloadEvents.beforeAssemblyReload -= SaveTree;
        }
        private static InfiniteLandsWindow GenerateWindow(TerrainGenerator asset){
            bool ThereAreInstances = HasOpenInstances<InfiniteLandsWindow>();

            if (ThereAreInstances){
                InfiniteLandsWindow opened = FocusWindowIfOpened(asset);
                if(opened != null)
                    return opened;
            }

            var requiredAttribute = typeof(EditorWindow).Assembly.GetType("UnityEditor.InspectorWindow");
            InfiniteLandsWindow wnd = CreateWindow<InfiniteLandsWindow>(requiredAttribute);
            return wnd;
        }

        private static InfiniteLandsWindow FocusWindowIfOpened(TerrainGenerator asset){
            InfiniteLandsWindow window = FindWindow(asset);
            if(window != null)
                window.Focus();
            return window;
        }

        public static InfiniteLandsWindow FindWindow(TerrainGenerator asset){
            InfiniteLandsWindow[] allWindows = Resources.FindObjectsOfTypeAll<InfiniteLandsWindow>();
            foreach(InfiniteLandsWindow window in allWindows){
                if(!window)
                    continue;
                if(EditorTools.IsTheSameTree(window.treeInstance, asset))
                    return window;
            }
            return null;
        }

        public static void ReloadWindow(TerrainGenerator asset){
            InfiniteLandsWindow window = FindWindow(asset);
            if(window == null)
                return;
            window.LoadNewAsset(asset);
        }

        [OnOpenAsset]
        public static bool OpenGraphAsset(int instanceID)
        {
            Object asset = EditorUtility.InstanceIDToObject(instanceID);
            if (!(asset is TerrainGenerator generator)) return false;
            else{
                return OpenGraphAsset(generator);
            }
        }

        public static bool OpenGraphAsset(TerrainGenerator asset)
        {
            var wnd = GenerateWindow(asset);
            wnd.LoadNewAsset(asset);
            return true;
        }
        
        private static void CloseAllWindows(){
            InfiniteLandsWindow[] allWindows = Resources.FindObjectsOfTypeAll<InfiniteLandsWindow>();
            foreach(InfiniteLandsWindow window in allWindows){
                CloseIt(window);
            }
        }
        private void FocusAsset(){
            if(treeInstance != null){
                EditorGUIUtility.PingObject(treeInstance);
            }
        }

        private void OnDestroy()
        {
            if (GraphSettingsController.ValidateNode(treeInstance, out _))
            {
                GraphSettingsController.Clear();
            }
            SaveTree();
        }
        public static void CloseWindow(TerrainGenerator asset)
        {
            CloseIt(FindWindow(asset));
        }

        private static void CloseIt(InfiniteLandsWindow window)
        {
            window?.Close();
        }
        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Close all Tabs"), false, CloseAllWindows);
            menu.AddItem(new GUIContent("Find reference in Project"), false, FocusAsset);
        }
        #endregion

        #region UI
        public void CreateGUI()
        {
            if(treeInstance != null)
                ReloadGraph();
        }


        private void GrabUIElements(){
            VisualElement root = rootVisualElement;
            m_VisualTreeAsset.CloneTree(rootVisualElement);

            var styleSheet =
                AssetDatabase.LoadAssetAtPath<StyleSheet>(UIStyles.WindowStyles);
            root.styleSheets.Add(styleSheet);

            GetTreeView();
            
            RefreshButton = root.Q<ToolbarButton>("regenerate");
            SaveButton = root.Q<ToolbarButton>("save");
            ExportButton = root.Q<ToolbarButton>("export");


            ScaleField = root.Q<FloatField>("editor-scale");
            WorldOffsetField = root.Q<Vector2Field>("editor-offset");
            SeedField = root.Q<IntegerField>("editor-seed");
            SeedField = root.Q<IntegerField>("editor-seed");
            SyncSettingsToggle = root.Q<Toggle>("editor-sync");
            AutoUpdateToggle = root.Q<Toggle>("auto-regenerate");
            ResolutionField = root.Q<IntegerField>("editor-resolution");
        }
        private void GetTreeView(){
            VisualElement root = rootVisualElement;
            treeView = root.Q<InfiniteLandsGraphView>();
            treeView.editorWindow = this;
            treeView.focusable = true;
            treeView.RegenerateTerrain -= RegeneratePreviews;
            treeView.RegenerateTerrain += RegeneratePreviews;
        }
        private void UpdateAllUI(){
            SettingsWindow();
            ToolbarButtons();
            treeView.Focus();
        }

        void SettingsWindow(){
            ScaleField.bindingPath = nameof(settings.MeshScale);
            ScaleField.isDelayed = true;
            ScaleField.RegisterValueChangedCallback(a =>
            {
                float value = Mathf.Max(100, a.newValue);
                ScaleField.SetValueWithoutNotify(value);
                UpdateTextures();
            });

            ResolutionField.value = EditorResolution;
            ResolutionField.isDelayed = true;
            ResolutionField.RegisterValueChangedCallback(a =>
            {
                int resolution = Mathf.Clamp(a.newValue, 10, 200);
                EditorResolution = resolution;
                ResolutionField.SetValueWithoutNotify(resolution);
                UpdateTextures();
            });
            
            WorldOffsetField.bindingPath = nameof(settings.WorldOffset);
            WorldOffsetField.RegisterValueChangedCallback(a => UpdateTextures());

            SeedField.bindingPath = nameof(settings.Seed);
            SeedField.RegisterValueChangedCallback(a => UpdateTextures());

            SyncSettingsToggle.value = SyncSettings;
            SyncSettingsToggle.RegisterValueChangedCallback(a =>
            {
                SyncSettings = a.newValue;
                UpdateUI(SyncSettings);
            });

            AutoUpdateToggle.SetEnabled(treeInstance != null);         
            if(treeInstance != null){
                var serObject = new SerializedObject(treeInstance);
                var property = serObject.FindProperty(nameof(treeInstance.AutoUpdate));
                AutoUpdateToggle.TrackPropertyValue(property, a => TriggerTreeUpdate());
                AutoUpdateToggle.bindingPath = property.propertyPath;
                AutoUpdateToggle.Bind(serObject);
            }
            
            UpdateUI(SyncSettings);
        }
        public void TriggerTreeUpdate()
        {
            treeInstance.NotifyValuesChangedBefore();
            treeInstance.NotifyValuesChangedAfter();
        }
        void ToolbarButtons(){
            RefreshButton.SetEnabled(treeInstance != null);
            if (RefreshButton != null)
            {
                RefreshButton.clicked -= RegenerateThis;
                RefreshButton.clicked += RegenerateThis;
            }

            SaveButton.SetEnabled(treeInstance != null);
            if (SaveButton != null)
            {
                SaveButton.clicked -= SaveTree;
                SaveButton.clicked += SaveTree;
            }

            ExportButton.SetEnabled(treeInstance != null);
            if (ExportButton != null)
            {
                ExportButton.clicked -= ExportTree;
                ExportButton.clicked += ExportTree;
            }
        }
        #endregion

        void SaveTree()
        {   
            if(treeInstance == null)
                return;

            EditorUtility.SetDirty(treeInstance);
            AssetDatabase.SaveAssetIfDirty(treeInstance);
        }
        

        void ExportTree()
        {
            ExportPopup.OpenPopup(treeInstance, activeSettings);
        }

        public void RegeneratePreviews()
        {
            if (treeInstance != null){
                UpdateTextures();
            }
        }

        public void UpdateTextures(){
            if(treeInstance != null){
                EditorGenerator generator = new EditorGenerator(treeInstance);
                if(texturePool == null || texturePool.GetTextureResolution() != EditorResolution){
                    texturePool?.DestroyBurstTextures(DestroyImmediate);
                    texturePool = new BurstTexturePool(EditorResolution);
                }

                activeSettings.Resolution = EditorResolution;
                var targetPreviews = treeView.ElementsByGuid.Values.OfType<NodeView>().Distinct().Select(a => a.ActivePreview).Where(a => a != null);
                generator.GenerateEditorVisuals(activeSettings, texturePool, targetPreviews);
            }
        }

        private void OnUndoRedo()
        {
            if(!AnythingDirty())
                return;
                
            if(treeInstance != null)
                treeView.Initialize(treeInstance);
            SaveTree();
        }

        private bool AnythingDirty(){
            return EditorUtility.IsDirty(treeInstance);
        }
        private void RegenerateThis()
        {
            EditorTools.RegenerateGraph(treeInstance);
        }
        
        private void UpdateUI(bool synchornize)
        {
            SeedField.SetEnabled(!synchornize);
            SetEnabledVector2(WorldOffsetField, !synchornize);
            ScaleField.SetEnabled(!synchornize);

            SerializedObject serializedSettings;
            if (synchornize)            
                activeSettings = GraphSettingsController.GetSettings();
            else
                activeSettings = settings;

            serializedSettings = new SerializedObject(activeSettings);
        
            WorldOffsetField.Bind(serializedSettings);
            SeedField.Bind(serializedSettings);
            ScaleField.Bind(serializedSettings);
        }

        private void SetEnabledVector2(Vector2Field field, bool value)
        {
            Label lbl = field.Q<Label>();
            FloatField vlx = field.Q<FloatField>("unity-x-input");
            FloatField vly = field.Q<FloatField>("unity-y-input");

            lbl.SetEnabled(value);
            vlx.SetEnabled(value);
            vly.SetEnabled(value);
        }
        private void ChangeAssetState(bool value){
            Label assets = this.rootVisualElement.Q<Label>("no-asset");
            assets.visible = value;
        }

        private void LoadNewAsset(TerrainGenerator tree){
            if(EditorTools.IsTheSameTree(treeInstance, tree))
                return;
            if(tree != null && !AssetDatabase.CanOpenAssetInEditor(tree.GetInstanceID()))
                return;

            treeInstance = tree;
            ReloadGraph();
        }

        public void ReloadGraph(){
            if(treeView == null)
                GetTreeView();
            treeView.Initialize(treeInstance);
            UpdateAllUI();
            ReloadTitle(treeInstance);
            ChangeAssetState(treeInstance == null);
            AutoUpdateToggle.SetEnabled(treeInstance != null);
        }
        private void ReloadTitle(TerrainGenerator tree){
            if(tree == null)
                titleContent = new GUIContent("None");
            else
                titleContent = new GUIContent(tree.name, GetIcon(tree));
        }
        private Texture GetIcon(TerrainGenerator tree){
            if(tree.GetType() == typeof(BiomeTree))
                return EditorGUIUtility.IconContent("d_AnimatorOverrideController Icon").image;
            else
                return EditorGUIUtility.IconContent("d_AnimatorController Icon").image;
        }
    }
}