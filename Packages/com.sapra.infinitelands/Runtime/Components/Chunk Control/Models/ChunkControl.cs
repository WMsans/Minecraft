using System.Collections.Generic;
using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace sapra.InfiniteLands{
    [ExecuteAlways]
    public abstract class ChunkControl : InfiniteLandsMonoBehaviour, IRenderChunk
    {
        protected ChunkData chunk;
        protected TerrainConfiguration config;
        protected float MeshScale;
        private bool ChunkGenerated;
        protected IVisualizeTerrain generator;
        protected IControlTerrain infiniteLands;
        public bool DataRequested{get; set;}
        
        private bool childrenReady;
        private bool DataApplied => DataRequested && VisualsDone();
        private bool IsVisible;
        [SerializeField] public bool DrawSpecificChunk;
        [Disabled] public List<ChunkControl> childs = new List<ChunkControl>();

        public sealed override void Disable()
        {
            DisableChunk();
            UnsubscribeEvents();
            if(generator != null)
                generator.onProcessDone -= OnTerrainCreated;
        }
        public sealed override void Initialize(IControlTerrain lands)
        {
            infiniteLands = lands;
            generator = infiniteLands.GetInternalComponent<IVisualizeTerrain>();
            childrenReady = false;
            if(generator != null)
                generator.onProcessDone += OnTerrainCreated;
            InitializeChunk();
        }

        void Update()
        {
            if(ChunkGenerated && !Application.isPlaying){
                var flatPosition = chunk.terrainConfig.Position;
                var worldPosition = infiniteLands.LocalToWorldPoint(flatPosition);
                transform.position = worldPosition;
            }
        }
        public bool VisibilityCheck(List<Vector3> positions,float GenerationDistance, bool parentDisabled)
        {
            if (generator == null)
                return false;

            IsVisible = InView(positions, GenerationDistance);
            float distance = DistanceToBounds(positions);
            if (distance < MeshScale / 2 && config.ID.z > 0) //If inside the chunk but can be divided
            {
                if (childrenReady && DataRequested){ //If the children are ready and loaded
                    UnrequestMesh(); //Delete this data
                    CleanVisuals();
                }

                if (childs.Count <= 0) //If there are no child, generate them
                    GenerateChildChunks();
                else
                    childrenReady = UpdateVisibleChunks(positions, !DataApplied && parentDisabled, GenerationDistance); //Enable the childs or check if they can be activated
            }
            else
            {  
                RequestMesh();
                if(DataApplied){
                    DestroySmallerChunk();
                    childrenReady = false;   
                } 
            }
            UpdateVisuals(IsVisible && parentDisabled && DataApplied);
            return childrenReady || DataApplied;
        }

        protected bool InView(List<Vector3> positions, float generationDistance)
        {
            foreach(var pos in positions){
                float dist = Vector3.Distance(pos, ClosestPoint(pos));
                if(dist < generationDistance)
                    return true;
            }
            return false;
        }
        
        #region Chunk Generation
        private void GenerateChildChunks()
        {
            for (int yOffset = 0; yOffset < 2; yOffset++)
            {
                for (int xOffset = 0; xOffset < 2; xOffset++)
                {
                    Vector3Int newID = new Vector3Int(config.ID.x * 2 + xOffset, config.ID.y * 2 + yOffset,
                        config.ID.z - 1);

                    ChunkControl chunk = generator.GenerateChunk(newID) as ChunkControl;
                    childs.Add(chunk);
                }
            }
        }

        private bool UpdateVisibleChunks(List<Vector3> playerPosition, bool parentDisable, float GenDistance)
        {
            bool allLoaded = true;
            foreach (ChunkControl chunk in childs)
            {
                if (chunk == null) continue;
                allLoaded = chunk.VisibilityCheck(playerPosition, GenDistance, parentDisable) && allLoaded;
            }

            return allLoaded;
        }

        #endregion

        #region Deleting
        private void DestroySmallerChunk()
        {
            if (childs.Count > 0)
            {
                for (int i = 0; i < childs.Count; i++)
                {
                    ChunkControl chunk = childs[i];
                    if(generator != null)
                        generator.DisableChunk(chunk);
                    else
                        chunk.DisableChunk();
                }

                childs.Clear();
            }
        }
        #endregion

        public void DisableChunk(){                
            CleanVisuals();
            DisableIt();
            UnrequestMesh();
            DestroySmallerChunk();
        }

        public void EnableChunk(TerrainConfiguration config, MeshSettings meshSettings){
            DisableChunk();

            this.config = config;
            this.MeshScale = meshSettings.MeshScale;
            this.ChunkGenerated = false;
            this.DataRequested = false;

            CleanVisuals();
            EnableIt(config, meshSettings);
        }
        
        private float SquareDistance(Vector3 diff)
        {
            return Mathf.Max(Mathf.Max(Mathf.Abs(diff.x), Mathf.Abs(diff.y)), Mathf.Abs(diff.z));
        }
        
        private float DistanceToBounds(List<Vector3> positions)
        {
            float min = float.MaxValue;
            foreach(var pos in positions){
                min = MathF.Min(min, SquareDistance(pos - ClosestPoint(pos)));
            }
            return min;
        }

        protected Vector3 ClosestPoint(Vector3 position)
        {
            if(ChunkGenerated)
                return chunk.WorldSpaceBounds.ClosestPoint(position);
            else{
                return new Vector3(
                    Mathf.Clamp(position.x, config.Position.x-MeshScale, config.Position.x+MeshScale),
                    position.y,
                    Mathf.Clamp(position.z, config.Position.z-MeshScale, config.Position.z+MeshScale)
                );
            }
        }

        private void OnTerrainCreated(ChunkData chunk){
            if(!DataRequested)
                return;
            if(!chunk.ID.Equals(config.ID))
                return;

            this.chunk = chunk;
            config = chunk.terrainConfig;
            ChunkGenerated = true;
        }
        

        public void RequestMesh(){
            if(DataRequested)
                return;
            generator?.RequestMesh(config);
            DataRequested = true;
        }

        protected void UnrequestMesh(){
            ChunkGenerated = false;

            if(!DataRequested)
                return;

            DataRequested = false;
            generator?.UnrequestMesh(config); //Unrequest the mesh in case it's on the queue, we will not needed it anymore
            DataRequested = false;
        }
        
        protected T GetOrAddComponent<T>(ref T comp) where T : Component{
            if(this == null)
                return null;

            if(comp != null)
                return comp;
            T found = GetComponent<T>();
            if(found == null)
                found = gameObject.AddComponent<T>();
            return found;
        }

        protected T GetOrAddComponent<T>(ref T comp, GameObject from) where T : Component{
            if(comp != null)
                return comp;
            T found = from.GetComponent<T>();
            if(found == null)
                found = from.AddComponent<T>();
            return found;
        }

        private void OnDrawGizmos()
        {
            if (generator == null || !generator.DrawGizmos && !DrawSpecificChunk)
                return;

            DrawBound(IsVisible);
        }

        protected void DrawBound(bool visible)
        {
            if (!ChunkGenerated)
                return;

            var color = new Color(0, 0, 0, .5f);
            if (visible)
                color = Color.HSVToRGB(config.ID.z / 10.0f, 1f, 1f);

            Gizmos.color = color;
            GUI.color = color;
            var bounds = chunk.WorldSpaceBounds;
            var position = Equals(infiniteLands.GetChunkRenderer()) ? transform.parent.position + Vector3.up * bounds.center.y : bounds.center;
            Gizmos.DrawWireCube(position, bounds.size);
#if UNITY_EDITOR
            Handles.Label(position, config.ID.ToString());
#endif
            
        }

        #region Methods
        /// <summary>
        /// Should delete anything related to visuals
        /// </summary>
        protected abstract void CleanVisuals();
        /// <summary>
        /// Should enable anything visual in the chunk
        /// </summary>
        /// <param name="enabled"></param>
        public abstract void UpdateVisuals(bool enabled);

        /// <summary>
        /// Are the visuals created?
        /// </summary>
        /// <returns></returns>
        public abstract bool VisualsDone();

        /// <summary>
        /// Called Once during the creation of the chunk class
        /// </summary>
        protected abstract void InitializeChunk();

        /// <summary>
        /// Called once before the system closes
        /// </summary>
        public virtual void UnsubscribeEvents(){}

        /// <summary>
        /// Called every time the chunk is reused
        /// </summary>
        /// <param name="config"></param>
        /// <param name="meshSettings"></param>
        protected virtual void EnableIt(TerrainConfiguration config, MeshSettings meshSettings){}

        /// <summary>
        /// Called to disable the chunk
        /// </summary>
        protected virtual void DisableIt(){}
        #endregion
    }
}