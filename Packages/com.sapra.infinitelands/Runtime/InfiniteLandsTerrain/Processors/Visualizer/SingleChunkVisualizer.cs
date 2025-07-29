using UnityEngine;
using System;

namespace sapra.InfiniteLands{  

    [ExecuteInEditMode]
    public class SingleChunkVisualizer : InfiniteLandsComponent, IVisualizeTerrain
    {
        private System.Diagnostics.Stopwatch _watch = new System.Diagnostics.Stopwatch();
        private WorldGenerator generator;
        private WorldGenerationData _worldData = null;

        [Header("Flags")]
        private bool _generating = false;

        public Action<ChunkData> onProcessDone { get; set; }
        public Action<ChunkData> onProcessRemoved { get; set; }

        public Vector2 localGridOffset=> new Vector2(transform.position.x, transform.position.z);
        public Matrix4x4 localToWorldMatrix => Matrix4x4.TRS(
            transform.localToWorldMatrix.MultiplyPoint(-new Vector3(transform.position.x,0, transform.position.z)), 
            transform.rotation, 
            transform.localScale);

        public Matrix4x4 worldToLocalMatrix => Matrix4x4.Inverse(localToWorldMatrix);
        public int MaxLODGenerated => 0;
        public bool DrawGizmos => true;
        public bool CanTriggerAutoUpdate => !_generating;
        public bool InstantProcessors => true;

        [SerializeField] private bool LogTimings;
        public override void Initialize(IControlTerrain infiniteLands)
        {
            base.Initialize(infiniteLands);
            Traveller.DisableTraveller(true);
            transform.localScale = Vector3.one;
        }

        public void StartGeneration()
        {
            CancelGeneration();
            ForceGeneration(true);
        }

        public override void OnGraphUpdated()
        {
            if (infiniteLands.graph != null && infiniteLands.graph._autoUpdate)
                ForceGeneration(false);
            
        }

        public override void Disable()
        {
            CancelGeneration();
            if (generator != null)
            {
                generator.Dispose(default);
                generator = null;
            }
        }

        private void CancelGeneration(){
            if(_worldData != null){
                if(_worldData.ForceComplete()){
                    _worldData.Result?.CompletedInvocations();
                }
                _worldData = null;
            }
            _generating = false;
        }

        public override void Update()
        {
            if (!_generating) return;
            if (_worldData == null) return;
            if(!_worldData.ProcessData()) return;

            if (this != null)
            {
                ApplyResults();
            }
        }

        public void ForceGeneration(bool instantGen)
        {
            if (_generating && instantGen)
            {
                CancelGeneration();
                Debug.Log("this was called!");
            }

            if (LogTimings)
            {
                _watch = new System.Diagnostics.Stopwatch();
                _watch.Start();
            }

            GenerateMesh();
            if (instantGen)
            {
                ApplyResults();
            }
        }

        private void GenerateMesh()
        {
            MeshSettings meshSettings = infiniteLands.meshSettings;
            Vector3 position = worldToLocalMatrix.MultiplyPoint(transform.position);
            Vector2 simplePos = new Vector2(position.x, position.z);
            Vector2Int coord = Vector2Int.FloorToInt(simplePos / meshSettings.MeshScale);
            Vector3Int id = new Vector3Int(coord.x, coord.y, 0);

            TerrainConfiguration configuration = new TerrainConfiguration(id, transform.up, new Vector3(transform.position.x, 0, transform.position.z));
            RequestMesh(configuration);

            GraphSettingsController.ChangeValueSettings(meshSettings.MeshScale, new Vector2(configuration.Position.x, configuration.Position.z), meshSettings.Seed);
        }


        private void ApplyResults()
        {
            _generating = false;
            if (_worldData == null) return;
            if (!_worldData.ForceComplete()) return;

            var chunk = _worldData.Result;
            var chunkLoader = infiniteLands.GetChunkRenderer();
            if (chunkLoader != null)
            {
                chunkLoader.DataRequested = false;
                chunkLoader.EnableChunk(chunk.terrainConfig, chunk.meshSettings);
                chunkLoader.DataRequested = true;
            }

            if (LogTimings)
            {
                _watch.Stop();
                RuntimeTools.LogTimings(_watch);
            }

            onProcessDone?.Invoke(chunk);
            chunk.CompletedInvocations();
        }

        public void RequestMesh(TerrainConfiguration config)
        {
            UnrequestMesh(config);
            if (generator != null)
                generator.Dispose(default);
            
            generator = new WorldGenerator(infiniteLands.graph, infiniteLands.meshSettings.SeparatedBranch);
            if (generator.ValidGenerator)
            {
                _worldData = GenericPoolLight<WorldGenerationData>.Get();
                _worldData.Reuse(generator, config, infiniteLands.meshSettings);
                _generating = true;
            }
            else
                _worldData = null;
        }

        public void UnrequestMesh(TerrainConfiguration config)
        {
            if (_worldData != null)
            {
                GenericPoolLight.Release(_worldData);
                var chunkData = _worldData.Result;
                if (chunkData != null)
                {
                    onProcessRemoved?.Invoke(chunkData);
                    //chunkData.CompletedInvocations();
                }
                _worldData = null;
            }            
        }

        public void DisableChunk(IRenderChunk chunk)
        {
            chunk.gameObject.SetActive(false);
        }

        public IRenderChunk GenerateChunk(Vector3Int ID)
        {
            Debug.LogWarning("Single Chunk visualizer doesn't generate more chunks");
            return null;
        }
    }
}
