using System.Collections.Generic;
using UnityEngine;
using System;

namespace sapra.InfiniteLands{
    public class InfiniteChunkVisualizer : InfiniteLandsComponent, IVisualizeTerrain
    {
        public enum GenerationMode{AsTheyCome, FarFirst, CloseFirst}
        public GenerationMode generationMode = GenerationMode.CloseFirst;

        public bool LoadInViewFirst = true;
        [Layer] public int CreateChunksAtLayer;

        [Header("World generation")] [Tooltip("Distance to stop generating chunks")]
        public bool FastInitialization = true;
        public int MaxStepsInTree = 500;

        [Min(10)] public float GenerationDistance = 10000;
        [Min(1)] public int MaxChunksPerFrame = 1;

        public int DisposeAfterSeconds = 5;
        private float timeSinceLastGeneration;
        private bool disposed;

        private Transform DisabledParent;
        private Transform EnabledParent;

        private InfiniteSettings infiniteSettings;
        private float MaxScale;

        private Dictionary<Vector2Int, IRenderChunk> OverviewChunks = new();
        private List<Vector2Int> VisibleChunks = new();

        private List<IRenderChunk> DisabledChunks = new();
        [Disabled] public List<TerrainConfiguration> ChunkRequests = new();
        [Disabled] public List<WorldGenerationData> GenerationCalls = new List<WorldGenerationData>();
        private Dictionary<Vector3Int, ChunkData> CreatedChunks = new Dictionary<Vector3Int, ChunkData>();

        public int MaxLODGenerated{get; private set;}
        private FloatingOrigin floatingOrigin;
        private ViewSettings viewSettings;
        private WorldGenerator generator;

        public Action<ChunkData> onProcessDone { get; set; }
        public Action<ChunkData> onProcessRemoved { get; set; }
        public Action onReload { get; set; }
        public Action OnInitalizationCompleted { get; set; }
        protected HashSet<Vector3Int> UnnecessaryChunks = new();
        public bool DrawGizmos => DrawChunks;
        private bool StartGenerating = false;
        [Disabled] public bool CompletedInitalization;

        [SerializeField] private bool DrawChunks;

        public Vector2 localGridOffset => new Vector2(infiniteLands.meshSettings.MeshScale / 2, infiniteLands.meshSettings.MeshScale / 2);
        public Matrix4x4 localToWorldMatrix{get; private set;}
        public Matrix4x4 worldToLocalMatrix{get; private set;}

        public bool CanTriggerAutoUpdate => true;
        public bool InstantProcessors => false;

        private ILayoutChunks ChunkLayout;
        private IRenderChunk ChunkRendering;
        private Comparison<TerrainConfiguration> sort;

        public void StartGeneration()
        {
            StartGenerating = true;
        }

        public override void Initialize(IControlTerrain infiniteLands){
            base.Initialize(infiniteLands);
            CompletedInitalization = false;
            StartGenerating = false;
            localToWorldMatrix = transform.localToWorldMatrix;
            worldToLocalMatrix = transform.worldToLocalMatrix;
            floatingOrigin = GetComponent<FloatingOrigin>();
            viewSettings = GetComponent<ViewSettings>();
            ChunkLayout = infiniteLands.GetChunkLayout();
            ChunkRendering = infiniteLands.GetChunkRenderer();
            ChunkRendering.DisableChunk();
            SetTraveller(FastInitialization);

            OverviewChunks.Clear();
            CreatedChunks.Clear();
            VisibleChunks.Clear();
            DisabledChunks.Clear();
            ChunkRequests.Clear();
            GenerationCalls.Clear();
            UnnecessaryChunks.Clear();
            MaxLODGenerated = 0;

            sort = Compare;
            if(floatingOrigin != null)
                floatingOrigin.OnOriginMove += OnOriginShift;

            DisabledParent = RuntimeTools.FindOrCreateObject("Disabled Chunks", transform).transform;
            EnabledParent = RuntimeTools.FindOrCreateObject("Enabled Chunks", transform).transform;
            if(EnabledParent.childCount > 0){
                AdaptiveDestroy(EnabledParent.transform.gameObject);
                EnabledParent = RuntimeTools.FindOrCreateObject("Enabled Chunks", transform).transform;
            }
                

            MeshSettings meshSettings = infiniteLands.meshSettings;
            generator = new WorldGenerator(infiniteLands.graph, infiniteLands.meshSettings.SeparatedBranch);
            infiniteSettings = ChunkLayout.GetInfiniteSettings(meshSettings, GenerationDistance);
            MaxScale = ChunkLayout.GetMeshSettingsFromID(meshSettings, new Vector3Int(0,0, infiniteSettings.LODLevels-1)).MeshScale;
            onReload?.Invoke();
        }

        private void OnOriginShift(Vector3Double newOrigin, Vector3Double previousOrigin){
            Matrix4x4 copy = worldToLocalMatrix;
            Vector3 worldOrigin = transform.worldToLocalMatrix.MultiplyPoint(newOrigin);
            copy.SetColumn(3, new Vector4(worldOrigin.x,worldOrigin.y,worldOrigin.z,1));

            worldToLocalMatrix = copy;
            localToWorldMatrix = worldToLocalMatrix.inverse;
        }  
        
        public override void OnValidate()
        {        
            if(ChunkLayout != null)
                infiniteSettings = ChunkLayout.GetInfiniteSettings(infiniteLands.meshSettings, GenerationDistance);
        }

        public override void Disable()
        {        
            StartGenerating = false;
            if(EnabledParent != null)
                AdaptiveDestroy(EnabledParent.transform.gameObject);
            if(DisabledParent != null)
                AdaptiveDestroy(DisabledParent.transform.gameObject);

            foreach (var call in GenerationCalls)
            {
                if (call.ForceComplete())
                    call.Result.CompletedInvocations();
                else
                    Debug.LogError("Force complete went wrong");
            }
            GenerationCalls.Clear();

            if(generator != null){
                generator.Dispose(default);
            }

            if(floatingOrigin != null)
                floatingOrigin.OnOriginMove -= OnOriginShift; 

            foreach(var chunk in CreatedChunks.Values){
                onProcessRemoved?.Invoke(chunk);
            }
            CreatedChunks.Clear();
        }

        // Update is called once per frame
        public override void Update()
        {
            if (!StartGenerating || !generator.ValidGenerator)
                return;

            UpdateVisibleChunks();
            UpdateScheduledJobs();
            CheckFinishedJobs();
        }
        public override void OnGraphUpdated()
        {
            Disable();
            Initialize(infiniteLands);
            StartGeneration();
        }
        #region Chunk Generation

        private void UpdateVisibleChunks()
        {
            var toDisable = ListPoolLight<Vector2Int>.Get();
            var cameraPositions = ListPoolLight<Vector3>.Get();
            var preVerified = HashSetPoolLight<Vector2Int>.Get();

            foreach(Vector2Int id in VisibleChunks){
                toDisable.Add(id);
            }

            foreach(var camera in viewSettings.GetCurrentCameras()){
                cameraPositions.Add(worldToLocalMatrix.MultiplyPoint(camera.transform.position));
            }
            foreach(var position in cameraPositions){
                int currentChunkCoordX = Mathf.FloorToInt(position.x / MaxScale);
                int currentChunkCoordY = Mathf.FloorToInt(position.z / MaxScale);
                for (int yOffset = -infiniteSettings.VisibleChunks; yOffset <= infiniteSettings.VisibleChunks; yOffset++)
                {
                    for (int xOffset = -infiniteSettings.VisibleChunks; xOffset <= infiniteSettings.VisibleChunks; xOffset++)
                    {
                        Vector2Int flatID = new Vector2Int(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);
                        if(preVerified.Contains(flatID))
                            continue;

                        Vector3Int ID = new Vector3Int(flatID.x,flatID.y, infiniteSettings.LODLevels-1);
                        IRenderChunk generator;
                        if (OverviewChunks.TryGetValue(flatID, out generator))                    
                            generator.VisibilityCheck(cameraPositions, GenerationDistance, true);
                        else{
                            generator = GenerateChunk(ID);
                            OverviewChunks.Add(flatID, generator);
                        }

                        if (!VisibleChunks.Contains(flatID))
                            VisibleChunks.Add(flatID);
                        else
                            toDisable.Remove(flatID);
                        preVerified.Add(flatID);
                    }
                }
            }


            foreach (Vector2Int coord in toDisable)
            {
                if (OverviewChunks.TryGetValue(coord, out IRenderChunk generator))
                {   
                    VisibleChunks.Remove(coord);
                    OverviewChunks.Remove(coord);
                    DisableChunk(generator);
                }
            }

            ListPoolLight<Vector3>.Release(cameraPositions);
            ListPoolLight<Vector2Int>.Release(toDisable);
            HashSetPoolLight<Vector2Int>.Release(preVerified);
            
        }

        public IRenderChunk GenerateChunk(Vector3Int ID)
        {
            IRenderChunk chunk;
            GameObject terrain;
            if (DisabledChunks.Count > 0)
            {
                chunk = DisabledChunks[0];
                terrain = chunk.gameObject;
                DisabledChunks.RemoveAt(0);
                terrain.SetActive(true);
            }
            else
            {
                terrain = Instantiate(ChunkRendering.gameObject, EnabledParent);
                terrain.layer = CreateChunksAtLayer;   
                chunk = terrain.GetComponent<IRenderChunk>();       
            }

            MeshSettings settings = ChunkLayout.GetMeshSettingsFromID(infiniteLands.meshSettings, ID);
            TerrainConfiguration config = new TerrainConfiguration(ID, transform.up, settings.MeshScale);
            
            #if UNITY_EDITOR
            terrain.name = ID.ToString();
            #endif

            terrain.transform.SetParent(EnabledParent);
            terrain.transform.rotation = Quaternion.LookRotation(localToWorldMatrix.GetColumn(2), localToWorldMatrix.GetColumn(1));
            terrain.transform.position = localToWorldMatrix.MultiplyPoint(config.Position);
            terrain.transform.localScale = Vector3.one;
            chunk.EnableChunk(config, settings);
            return chunk;
        }

        public void DisableChunk(IRenderChunk chunk)
        {
            if(chunk != null && DisabledParent != null && DisabledParent.gameObject.activeInHierarchy)
                chunk.gameObject.transform.SetParent(DisabledParent);
            chunk.DisableChunk();
            DisabledChunks.Add(chunk);
        }
        #endregion

        #region Mesh Generation

        public void RequestMesh(TerrainConfiguration config)
        {
            if(CreatedChunks.ContainsKey(config.ID))
                Debug.LogErrorFormat("Chunk already generated: {0}",config.ID);

            if(UnnecessaryChunks.Contains(config.ID))
                UnnecessaryChunks.Remove(config.ID);
            ChunkRequests.Add(config);
            MaxLODGenerated = Mathf.Max(MaxLODGenerated, config.ID.z);
        }

        public void UnrequestMesh(TerrainConfiguration config)
        {
            for(int i = 0; i < ChunkRequests.Count; i++){
                var chunkRequest = ChunkRequests[i];
                if(chunkRequest.ID.Equals(config.ID)){
                    ChunkRequests.RemoveAt(i);
                    return;
                }
            }

            foreach(var genCall in GenerationCalls){
                if(genCall.terrain.ID.Equals(config.ID)){
                    UnnecessaryChunks.Add(config.ID);
                    break;
                }
            }
                        
            if(CreatedChunks.TryGetValue(config.ID, out ChunkData chunk)){
                onProcessRemoved?.Invoke(chunk);
                GenericPoolLight.Release(chunk);
                CreatedChunks.Remove(config.ID);
            }
        }

        #region Sorting of data
        void SettingsFromRequests(List<TerrainConfiguration> requests, ref List<MeshSettings> settings)
        {
            for (int i = 0; i < requests.Count; i++)
            {
                Vector3Int ID = requests[i].ID;
                MeshSettings selected = ChunkLayout.GetMeshSettingsFromID(infiniteLands.meshSettings, ID);
                settings.Add(selected);
            }
        }

        private List<TerrainConfiguration> GetCurrentRequests()
        {
            if(ChunkRequests.Count <= 1 || generationMode == GenerationMode.AsTheyCome)
                return ChunkRequests;

            ChunkRequests.Sort(sort);
            return ChunkRequests;    
        }
        public int Compare(TerrainConfiguration a, TerrainConfiguration b)
        {
            var currentCameras = viewSettings.GetCurrentCameras();
            Vector3 posA = infiniteLands.LocalToWorldPoint(a.Position);
            Vector3 posB = infiniteLands.LocalToWorldPoint(b.Position);
            float minDistaA = float.MaxValue;
            float minDistaB = float.MaxValue;
            
            for (int i = 0; i < currentCameras.Count; i++)
            {
                var cam = currentCameras[i];
                if (cam == null) continue;

                minDistaA = MathF.Min(minDistaA, Vector3.Distance(cam.transform.position, posA));
                minDistaB = MathF.Min(minDistaB, Vector3.Distance(cam.transform.position, posB));
            }

            if (LoadInViewFirst)
                return CompareWithVisibility(minDistaA, minDistaB, posA, posB, currentCameras);
                            
            // Default sorting by distance without visibility consideration
            return generationMode == GenerationMode.FarFirst ? 
                minDistaB.CompareTo(minDistaA) : 
                minDistaA.CompareTo(minDistaB);
        }

        private int CompareWithVisibility(float distA, float distB, Vector3 posA, Vector3 posB, List<Camera> cameras)
        {
            bool visibleA = IsVisible(posA, cameras);
            bool visibleB = IsVisible(posB, cameras);
            
            // If both have same visibility status, sort by distance
            if (visibleA == visibleB)
            {
                return generationMode == GenerationMode.FarFirst ? 
                    distB.CompareTo(distA) : 
                    distA.CompareTo(distB);
            }
            
            // If visibility differs, visible ones come first
            return visibleA ? -1 : 1;
        }

        // Check if an object is visible to the camera
        private bool IsVisible(Vector3 position, List<Camera> cameras)
        {
            // Check if object is in camera's frustum
            foreach(var cam in cameras){
                if (cam == null) continue;
                Vector3 viewportPoint = cam.WorldToViewportPoint(position);
                bool isInView = viewportPoint.z > 0 && // In front of camera
                            viewportPoint.x >= 0 && viewportPoint.x <= 1 && // Within horizontal bounds
                            viewportPoint.y >= 0 && viewportPoint.y <= 1;   // Within vertical bounds
                if(isInView)
                    return true;
            }
            return false;
        }
        #endregion

        private void UpdateScheduledJobs()
        {
            if (GenerationCalls.Count >= MaxChunksPerFrame)
                return;

            List<TerrainConfiguration> TargetRequests = GetCurrentRequests();
            if(TargetRequests == null)
                return;
            int dif = MaxChunksPerFrame-GenerationCalls.Count;
            int SimulatinousManaging = Mathf.Min(dif, Mathf.Min(TargetRequests.Count, MaxChunksPerFrame));
            if(SimulatinousManaging <= 0)
                return;

            List<TerrainConfiguration> subset = ListPoolLight<TerrainConfiguration>.Get();
            List<MeshSettings> meshSettings = ListPoolLight<MeshSettings>.Get();

            for(int i = 0; i < SimulatinousManaging; i++){
                subset.Add(TargetRequests[i]);
            }

            SettingsFromRequests(subset, ref meshSettings);
            int index = 0;
            foreach(var configuration in subset){
                var worldGenerationData = GenericPoolLight<WorldGenerationData>.Get();
                worldGenerationData.Reuse(generator,configuration, meshSettings[index]);
                GenerationCalls.Add(worldGenerationData);
                index++;
            }
            TargetRequests.RemoveRange(0, SimulatinousManaging);

            ListPoolLight<TerrainConfiguration>.Release(subset);
            ListPoolLight<MeshSettings>.Release(meshSettings);
        }

        private void CompleteChunk(WorldGenerationData generatedChunk){
            var chunk = generatedChunk.Result;
            GenericPoolLight.Release(generatedChunk);

            if (chunk == null)
                return;

            if(UnnecessaryChunks.Contains(chunk.ID))
                UnnecessaryChunks.Remove(chunk.ID);
            else{
                if(CreatedChunks.TryAdd(chunk.ID, chunk)){
                    onProcessDone?.Invoke(chunk);
                }
                else
                    Debug.LogWarningFormat("Already there {0}", chunk.ID);
            }
            chunk.CompletedInvocations();
        }

        private void CheckFinishedJobs()
        {
            if (!Traveller.ProcessCheckpoints()) return;

            if (GenerationCalls.Count > 0)
            {
                timeSinceLastGeneration = 0;
                disposed = false;
                for (int i = GenerationCalls.Count - 1; i >= 0; i--)
                {
                    WorldGenerationData call = GenerationCalls[i];
                    if (call.ProcessData())
                    {
                        CompleteChunk(call);
                        GenerationCalls.RemoveAt(i);
                    }
                }

                if (GenerationCalls.Count <= 0 && ChunkRequests.Count <= 0)
                    CompleteInitialization();
            }
            else
            {
                if (!disposed)
                {
                    timeSinceLastGeneration += Time.deltaTime;
                    if (timeSinceLastGeneration > DisposeAfterSeconds && DisposeAfterSeconds > 0)
                    {
                        generator.DisposeReturned();
                        disposed = true;
                    }
                }
            }
        }

        private void CompleteInitialization()
        {
            if (CompletedInitalization) return;

            CompletedInitalization = true;
            SetTraveller(false);
            OnInitalizationCompleted?.Invoke();
        }

        private void SetTraveller(bool fastInit)
        {
            Traveller.Limit = (fastInit && Application.isPlaying) ? -1 : MaxStepsInTree;
            Traveller.DisableTraveller(false);
        }
        #endregion
    }
}