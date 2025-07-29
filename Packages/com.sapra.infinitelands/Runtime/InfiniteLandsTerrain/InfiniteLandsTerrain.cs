using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using sapra.InfiniteLands.UnityTerrain;
using sapra.InfiniteLands.MeshProcess;
using System.Collections;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

namespace sapra.InfiniteLands{

    [ExecuteAlways]
    [SelectionBase]
    public class InfiniteLandsTerrain : MonoBehaviour, IControlTerrain
    {
        public TerrainGenerator terrainGenerator;
        private IGraph _graph;

        [Tooltip("Layout of chunks used for infinite generation")]
        public LandsLayout SelectedLayout;
        [Tooltip("Setup a Chunk Prefab for infinite generation when using a the Custom template")]
        [EnableIf(nameof(showPrefab))] [SerializeField] private GameObject ChunkPrefab;
        [field: SerializeField] public MeshSettings meshSettings { get; private set; } = MeshSettings.Default;
        [SerializeField] protected ViewSettings viewSettings = new();
        public IGraph graph => _graph;
        
        public Vector2 localGridOffset => visualizer.localGridOffset;
        public Matrix4x4 localToWorldMatrix => visualizer.localToWorldMatrix;
        public Matrix4x4 worldToLocalMatrix => visualizer.worldToLocalMatrix;
        public int maxLodGenerated => visualizer.MaxLODGenerated;

        private IGraph _previousGenerator;
        [SerializeReference] private InfiniteLandsComponent GenerationSettings;
        private bool showPrefab => ProcessorTemplate == LandsTemplate.Custom;
        public bool InstantProcessors => visualizer.InstantProcessors;

        [HideInInspector] public LandsTemplate ProcessorTemplate = LandsTemplate.Default;
        [HideInInspector] public LandsTemplate previousTemplate = LandsTemplate.Custom;
        private Vector3 _previousPosition;
        private Vector3 _previousRotation;

        [SerializeReference] public List<InfiniteLandsComponent> EnabledComponents = new();
        private HashSet<InfiniteLandsMonoBehaviour> EnabledMonobehaviours = new();
        private List<ILandsLifeCycle> InfiniteLandsStuff = new();
        [SerializeReference] private IVisualizeTerrain visualizer;
        private IRenderChunk chunkRenderer;
        private ILayoutChunks chunkLayout;

        [HideInInspector] public bool infiniteMode = false;
        [HideInInspector][SerializeReference] private SingleChunkVisualizer singleVisualizer = new();
        [HideInInspector][SerializeReference] private InfiniteChunkVisualizer infiniteVisualizer = new();
        private bool Initialized;
        void Reset()
        {
            Initialize();
        }

        public void Initialize(bool generate = true){
            _previousPosition = transform.position;
            _previousRotation = transform.eulerAngles;

            Disable();
            ChangeGenerator(terrainGenerator);
            ChangeVisualizer(infiniteMode, false);
            ChangeTemplate(ProcessorTemplate);
            ChangeChunkRenderer(ProcessorTemplate, false);
            ChangeLayout(SelectedLayout);

            CollectLifeCyclers();
            
            if(graph == null || graph.Equals(null))
                return;
            graph.ValidationCheck();
            graph.OnValuesChangedBefore -= OnGraphUpdatedBefore;
            graph.OnValuesChangedBefore += OnGraphUpdatedBefore;

            graph.OnValuesChangedAfter -= OnGraphUpdated;
            graph.OnValuesChangedAfter += OnGraphUpdated;

            foreach(var il in InfiniteLandsStuff){
                il.Initialize(this);
            }

            Initialized = true;
            if(generate)
                visualizer.StartGeneration();
        }
        private void CollectLifeCyclers(){
            EnabledComponents.RemoveAll(a => a == null);
            EnabledMonobehaviours.UnionWith(GetComponentsInChildren<InfiniteLandsMonoBehaviour>());
            EnabledMonobehaviours.RemoveWhere(a => a == null);

            InfiniteLandsStuff.Clear();
            InfiniteLandsStuff.Add(viewSettings);
            InfiniteLandsStuff.Add(GenerationSettings);
            InfiniteLandsStuff.AddRange(EnabledComponents);
            InfiniteLandsStuff.AddRange(EnabledMonobehaviours);
        }
        private void OnGraphUpdated()
        {
            if (this == null) return;
            if (graph == null) return;
            if (!graph._autoUpdate && !infiniteMode) return;
            if (!visualizer.CanTriggerAutoUpdate) return;


            if (infiniteMode)
                Initialize();
            else
            {
                foreach (var il in InfiniteLandsStuff)
                {
                    il.OnGraphUpdated();
                }
            }

            _previousPosition = transform.position;
            _previousRotation = transform.eulerAngles;
        }
        private void OnGraphUpdatedBefore()
        {
            if (infiniteMode)
            {
                Disable();
            }
        }
        public void Disable()
        {
            if (!Initialized) return;

            Initialized = false;
            CollectLifeCyclers();

            for (int i = InfiniteLandsStuff.Count - 1; i >= 0; i--)
            {
                var stuff = InfiniteLandsStuff[i];
                if (stuff != null)
                    stuff.Disable();
            }
        }

        public void DelayedGeneration(bool byEditor){
            if(byEditor && !graph._autoUpdate && !infiniteMode)
                return;

            if(!Application.isPlaying){
                #if UNITY_EDITOR
                EditorApplication.delayCall -= LateInitialize;
                EditorApplication.delayCall += LateInitialize;
                #endif
            }
        }
        private void RegenAfterSceneSaved(UnityEngine.SceneManagement.Scene scene){
            DelayedGeneration(false);
        }
        private void RegenAfterProjectChanged(){
            DelayedGeneration(false);
        }
        private void DisposeAfterSceneSave(UnityEngine.SceneManagement.Scene scene, string path){
            Disable();
        }
        private void LateInitialize(){
            if(this != null){
                Initialize();
            }
        }

        #region Configuration

        public IRenderChunk GetChunkRenderer() => chunkRenderer;
        public ILayoutChunks GetChunkLayout() => chunkLayout;
        public ViewSettings GetViewSettings() => viewSettings;
        public void ChangeVisualizer(bool infiniteMode, bool byEditor){
            bool shouldChange = infiniteMode != this.infiniteMode || visualizer == null || GenerationSettings == null;
            if(infiniteMode != this.infiniteMode){
                #if UNITY_EDITOR
                var txt1 = this.infiniteMode ? "infinite" : "single";
                var txt2 = infiniteMode ? "infinite" : "single";
                Undo.RecordObject(this, "Switch generation mode");
                Undo.SetCurrentGroupName(string.Format("Changed from {0} to {1}", txt1, txt2));
                #endif
            }
            
            this.infiniteMode = infiniteMode;
            if(shouldChange){
                if(byEditor){
                    try{
                        Disable();
                    }catch(Exception e){
                        Debug.LogError("Disabling went wrong!");
                        Debug.LogException(e);
                    }
                }
                GenerationSettings?.Disable();
                if(infiniteMode){
                    if(infiniteVisualizer == null)
                        infiniteVisualizer = new InfiniteChunkVisualizer();
                    GenerationSettings = infiniteVisualizer;
                    visualizer = infiniteVisualizer;
                }
                else{
                    if(singleVisualizer == null)
                        singleVisualizer = new SingleChunkVisualizer();
                    GenerationSettings = singleVisualizer;
                    visualizer = singleVisualizer;
                }
                if(byEditor)
                    Initialize();
            }
        }
        public void ChangeLayout(LandsLayout selectedLayout){
            switch(selectedLayout){
                case LandsLayout.QuadTree:
                    chunkLayout = new QuadLayout();
                    break;
                default: 
                    chunkLayout = new SingleLayout();
                    break;
            }
            SelectedLayout = selectedLayout;
        }

        public void SetChunkRenderer<T>(bool forced) where T : InfiniteLandsMonoBehaviour, IRenderChunk
        {
            if(forced){ //If we force it we want to delete all of the existing ones since we will generate one
                ChunkPrefab = null;
                var chunksSpawned = GetComponentsInChildren<IRenderChunk>();
                foreach(var chunk in chunksSpawned){
                    RuntimeTools.AdaptiveDestroy(chunk.gameObject);
                }
            }

            //If it's not forced let's verify that the prefab is good first
            var renderer = ChunkPrefab != null ? ChunkPrefab.GetComponent<IRenderChunk>() : null;
            var spawnedRenderer = transform.GetComponentInChildren<IRenderChunk>();
            if(renderer != null){ //if prefab is good, let's check that it has been spawned
                var targetObject = transform.Find(ChunkPrefab.name);
                if(targetObject == null){ //If it's not there, spawn it
                    targetObject = Instantiate(ChunkPrefab, transform).transform;
                } 
                chunkRenderer = targetObject.GetComponent<IRenderChunk>();
            }else if(spawnedRenderer != null){ //The prefab field is null, or it's not valid, let's check if there's anything as a child
                chunkRenderer = spawnedRenderer;
            }else{ //No spawned, no prefab, we gotta make one
                if(!forced){
                    Debug.LogWarningFormat("There's no gameObject assigned to {0} or no {1} inside the prefab. Using the default {2}", 
                        nameof(ChunkPrefab), typeof(IRenderChunk).ToString(), typeof(T).ToString());
                }
                var targetObject = RuntimeTools.FindOrCreateObject(typeof(T).Name, transform);
                chunkRenderer = targetObject.AddComponent<T>();
                if(GetComponent<FloatingOrigin>() != null)
                    targetObject.gameObject.AddComponent<FloatingPoint>();
            }

            chunkRenderer.DisableChunk();
            T created = (T)chunkRenderer;
            created.SetInfiniteLandsTerrain(this);
            
            var TargetGameObject = chunkRenderer.gameObject;
            var floatingPoint = TargetGameObject.GetComponent<FloatingPoint>();
            if(floatingPoint == null && GetComponent<FloatingOrigin>() != null){
                Debug.LogWarningFormat("{0} doesn't have a Floating Point component, but {1} has {2}. This might bring unexpected results. Manually adding it", 
                    TargetGameObject.name, gameObject.name, typeof(FloatingOrigin).Name);
                TargetGameObject.AddComponent<FloatingPoint>();
            }
        }
        public void ChangeChunkRenderer(LandsTemplate enumValue, bool forced){
            switch (enumValue)
            {
                case LandsTemplate.UnityTerrain:
                    SetChunkRenderer<UnityTerrainChunk>(forced);
                    break;
                case LandsTemplate.Default:
                    SetChunkRenderer<DefaultChunk>(forced);
                    break;
                default:
                    SetChunkRenderer<DefaultChunk>(false);
                    break;
            }
        }
        public void ChangeTemplate(LandsTemplate enumValue){           
            LandsTemplate target = enumValue;
            if(target != previousTemplate){
                ChangeChunkRenderer(enumValue, true);
                ProcessorTemplate = target;
                previousTemplate = target;
                switch(target){
                    case LandsTemplate.Default:
                        ClearComponents();
                        AddComponent<PointStore>(false);
                        AddComponent<MeshMaker>(false);
                        AddComponent<TerrainPainter>(false);
                        AddComponent<VegetationRenderer>(false);
                        AddComponent<LandmarkPlacer>(true);
                        break;
                    case LandsTemplate.UnityTerrain:
                        if(infiniteVisualizer.GenerationDistance > meshSettings.MeshScale*3){
                            Debug.LogWarning("Switched to Unity Terrain with really high rendering distance. This is not recommended since it might bring high performance issues. Reducing rendering distance");
                            infiniteVisualizer.GenerationDistance = meshSettings.MeshScale*3;
                        }
                        ClearComponents();
                        AddComponent<PointStore>(false);
                        AddComponent<UnityTerrainMaker>(false);
                        AddComponent<UnityTerrainPainter>(false);
                        AddComponent<UnityTerrainVegetation>(false);
                        AddComponent<LandmarkPlacer>(true);
                        break;
                }
            }
        }
        #endregion

        #region Unity Life Time
        private void OnValidate()
        {
            ChangeGenerator(terrainGenerator);
            DelayedGeneration(false);
        }

        public void AddComponent(Type type, bool initialize = true){
            var newItem = (InfiniteLandsComponent)System.Activator.CreateInstance(type);
            EnabledComponents.Add(newItem);
            if(initialize)
                Initialize();
        }
        void IControlTerrain.StartCoroutine(IEnumerator coroutine) => StartCoroutine(coroutine);
        public void AddComponent<T>(bool initialize = true){
            AddComponent(typeof(T), initialize);
        }

        public void AddMonoForLifetime(InfiniteLandsMonoBehaviour monoBehaviour){
            EnabledMonobehaviours.Add(monoBehaviour);
            if(Initialized)
                monoBehaviour.Initialize(this);
        }
        public void RemoveMonoForLifetime(InfiniteLandsMonoBehaviour monoBehaviour){
            EnabledMonobehaviours.Remove(monoBehaviour);
        }

        public void RemoveComponent(Type type){
            EnabledComponents.RemoveAll(a => {
                bool found = a.GetType().Equals(type);
                if(found){
                    a.Disable();
                }
                return found;
            });
            Initialize();
        }
        public void ClearComponents(){
            foreach(var component in EnabledComponents){
                component.Disable();
            }
            EnabledComponents.Clear();
        }
        void Start(){
            Initialize();
        }
        void OnEnable()
        {
            #if UNITY_EDITOR
            EditorApplication.update -= UpdateComponents;
            if(!Application.isPlaying){
                EditorApplication.update += UpdateComponents;
            }

            EditorSceneManager.sceneSaved -= RegenAfterSceneSaved;
            EditorSceneManager.sceneSaved += RegenAfterSceneSaved;

            EditorApplication.projectChanged -= RegenAfterProjectChanged;
            EditorApplication.projectChanged += RegenAfterProjectChanged;
           
            EditorSceneManager.sceneSaving -= DisposeAfterSceneSave;
            EditorSceneManager.sceneSaving += DisposeAfterSceneSave;
            AssemblyReloadEvents.beforeAssemblyReload += Disable;

            Undo.undoRedoPerformed -= RegenAfterProjectChanged;
            Undo.undoRedoPerformed += RegenAfterProjectChanged;
            #endif
        }

        private void OnDisable()
        {        
            #if UNITY_EDITOR
            EditorApplication.update -= UpdateComponents;
            EditorApplication.delayCall -= LateInitialize;
            EditorSceneManager.sceneSaved -= RegenAfterSceneSaved;
            EditorSceneManager.sceneSaving -= DisposeAfterSceneSave;
            AssemblyReloadEvents.beforeAssemblyReload -= Disable;
            Undo.undoRedoPerformed -= RegenAfterProjectChanged;
#endif

            Disable();
        }
        private void OnDestroy()
        {
            Disable();
        }
        #endregion

        public T GetInternalComponent<T>()
        {
            if (typeof(T).IsAssignableFrom(GenerationSettings.GetType()))
                return (T)(object)GenerationSettings;

            if (typeof(T).IsAssignableFrom(viewSettings.GetType()))
                return (T)(object)viewSettings;

            return (T)(object)EnabledComponents.FirstOrDefault(c => typeof(T).IsAssignableFrom(c.GetType()));
        }


        public void ChangeGenerator(IGraph generator)
        {
            var newTerrain = generator as TerrainGenerator;
            terrainGenerator = newTerrain;
            _graph = generator;

            #if UNITY_EDITOR
            if(_previousGenerator != graph){
                if (_previousGenerator != null)
                {
                    _previousGenerator.OnValuesChangedAfter-= OnGraphUpdated;
                    _previousGenerator.OnValuesChangedBefore -= OnGraphUpdatedBefore;
                }
                DelayedGeneration(false);
            }
            _previousGenerator = graph;
            #endif
        }
        void Update()
        {
            if(!Application.isPlaying)
                return;
            UpdateComponents();
        }
        void OnDrawGizmos()
        {
            GenerationSettings?.OnDrawGizmos();
            foreach(var component in EnabledComponents){
                component.OnDrawGizmos();
            }
        }
        // Update is called once per frame
        public void UpdateComponents()
        {
            if(!Initialized || chunkRenderer.Equals(null)) return;
                
            if ((transform.position != _previousPosition 
                || transform.eulerAngles != _previousRotation)
                && graph != null && (graph._autoUpdate || infiniteMode)){
                
                OnGraphUpdated();
            }

            GenerationSettings.Update();
            foreach(var component in EnabledComponents){
                component.Update();
            }
        }

        
        public void LateUpdate(){
            if(!Initialized || chunkRenderer == null) return;

            GenerationSettings.LateUpdate();
            foreach(var component in EnabledComponents){
                component.LateUpdate();
            }
        }
    }
}