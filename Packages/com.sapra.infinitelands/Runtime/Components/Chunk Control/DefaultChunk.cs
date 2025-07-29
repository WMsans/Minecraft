using sapra.InfiniteLands.MeshProcess;
using UnityEngine;

namespace sapra.InfiniteLands{  
    public class DefaultChunk : ChunkControl
    {
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        private MeshFilter _meshFilter;

        public bool MeshApplied;
        public bool MaterialApplied;
        private IGenerate<TextureResult> painter;
        private IGenerate<MeshResult> meshMaker;

        protected override void InitializeChunk()
        {
            _meshFilter = GetOrAddComponent(ref _meshFilter);
            _meshRenderer = GetOrAddComponent(ref _meshRenderer);     
            painter = infiniteLands.GetInternalComponent<IGenerate<TextureResult>>();
            meshMaker = infiniteLands.GetInternalComponent<IGenerate<MeshResult>>();

            if(painter != null)
                painter.onProcessDone += OnTextureCreated;
            
            if(meshMaker != null)
                meshMaker.onProcessDone += OnMeshCreated;
        }

        public override void UnsubscribeEvents()
        {
            if(painter != null)
                painter.onProcessDone -= OnTextureCreated;
            
            if(meshMaker != null)
                meshMaker.onProcessDone -= OnMeshCreated;
        }
        protected override void EnableIt(TerrainConfiguration config, MeshSettings meshSettings)
        {
            MeshScale = meshSettings.MeshScale;
        }

        private void OnMeshCreated(MeshResult meshResult){
            if(!DataRequested) return;

            if(!meshResult.ID.Equals(config.ID))
                return;

            MeshApplied = true;
            if(meshResult.PhysicsBaked){
                if(!_meshCollider){
                    _meshCollider = GetOrAddComponent(ref _meshCollider);
                    _meshCollider.cookingOptions = CookPhysicsJob.cookingOptions;
                }

                _meshCollider.enabled = true;
                _meshCollider.sharedMesh = meshResult.mesh;
            }

            _meshFilter.mesh = meshResult.mesh;
        }

        public override bool VisualsDone() => MeshApplied&&(MaterialApplied || painter == null);
        public override void UpdateVisuals(bool enabled)
        {
            _meshRenderer.enabled = enabled;
        }

        private void OnTextureCreated(TextureResult textureResult){
            if(!DataRequested) return;

            if(!textureResult.TerrainConfiguration.ID.Equals(config.ID))
                return;

            MaterialApplied = true;
            _meshRenderer.material = textureResult.groundMaterial;
        }

        protected override void CleanVisuals()
        {
            if(_meshFilter != null)
                _meshFilter.mesh = null;

            if (_meshCollider != null)
            {   
                _meshCollider.enabled = false;
                _meshCollider.sharedMesh = null;
            }

            MeshApplied = false; 
            MaterialApplied = false;
        }
    }
}