using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace sapra.InfiniteLands{
    public class ChunkVisibilityRenderer 
    {
        private static readonly int
            cameraPositionID = Shader.PropertyToID("_CameraPosition"),
            frustrumTrianglesID = Shader.PropertyToID("_FrustrumTriangles"),
            matrixID = Shader.PropertyToID("_MATRIX_VP");
        public Camera camera{get; private set;}

        #region Parameters

        private Plane[] frustrumPlanes;
        
        private Dictionary<Vector2Int, VegetationChunk> CachedVisibleChunks = new();
        private HashSet<Vector2Int> TrackingIDs = new();
        private TransformInGrid TransformInGrid;

        private List<Vector3> FrustrumCorners = new();
        private Triangle[] FrustrumTriangles = new Triangle[12];
        private Vector3[] frustumCornersNear = new Vector3[4];
        private Vector3[] frustumCornersFar = new Vector3[4];

        private VegetationSettings settings;
        private VegetationChunkManager ChunksManager;
        private IControlTerrain infiniteLandsController;

        private ProfilingSampler VisibilityCheckSampler;
        private ComputeBuffer TriangleCorners;

        private Dictionary<int, List<int>> GroupedIndices;
        private string AssetName;

        private Texture DepthTexture;
        #endregion

        #region Constructors
        public ChunkVisibilityRenderer(Camera cam, VegetationChunkManager holder, VegetationSettings settings, 
            IControlTerrain infiniteLandsController, string assetName)
        {    
            this.camera = cam;
            this.settings = settings;
            ChunksManager = holder;
            this.AssetName = assetName;
            this.infiniteLandsController = infiniteLandsController;
            GroupedIndices = new();
            TriangleCorners = new ComputeBuffer(12, sizeof(float)*9);
            TransformInGrid = new TransformInGrid(cam.transform, infiniteLandsController, settings.GridOffset);
            VisibilityCheckSampler = new ProfilingSampler(string.Format("{0}/Visibility Check", camera.name));
            frustrumPlanes = new Plane[6];
            holder.OnCreateChunk += OnChunkCreated;
            holder.OnDisableChunk += OnChunkRemoved;
        }

        public void OnChunkCreated(Vector2Int ID, VegetationChunk chunk){
            if(TrackingIDs.Contains(ID)){
                CachedVisibleChunks.Add(ID, chunk);
                TrackingIDs.Remove(ID);
            }
        }
        public void OnChunkRemoved(Vector2Int ID, VegetationChunk chunk){
            if(TrackingIDs.Contains(ID)){
                TrackingIDs.Remove(ID);
            }

            if(CachedVisibleChunks.ContainsKey(ID))
                CachedVisibleChunks.Remove(ID);
        }
        #endregion

        #region Rendering
        public void UpdateChunksAround(ref List<Vector2Int> toEnable, ref List<Vector2Int> toDisable)
        {
            var ToEnable = ListPoolLight<Vector2Int>.Get();
            var ToDisable = ListPoolLight<Vector2Int>.Get();

            TransformInGrid.UpdateChunksAround(ref ToEnable, ref ToDisable, settings.ChunkSize, settings.ChunksVisible);
            foreach (var idToDisable in ToDisable)
            {
                if (TrackingIDs.Contains(idToDisable))
                    TrackingIDs.Remove(idToDisable);
                if (CachedVisibleChunks.ContainsKey(idToDisable))
                    CachedVisibleChunks.Remove(idToDisable);
            }

            foreach (var idToEnable in ToEnable)
            {
                var current = ChunksManager.GetChunk(idToEnable, out _);
                if (current != null)
                    CachedVisibleChunks.Add(idToEnable, current);
                else
                    TrackingIDs.Add(idToEnable);
            }

            toEnable.AddRange(ToEnable);
            toDisable.AddRange(ToDisable);

            ListPoolLight<Vector2Int>.Release(ToEnable);
            ListPoolLight<Vector2Int>.Release(ToDisable);
        }
        public void Render(MaterialPropertyBlock propertyBlock, IDrawInstances drawer, IndexCompactor compactor){
            FindUniqueChunks();
            RenderVisibleChunks(propertyBlock, drawer, compactor);
        }
        private Plane TransformPlaneFast(Plane plane, Matrix4x4 inverse){
            float x = plane.normal.x;
            float y = plane.normal.y;
            float z = plane.normal.z;
            float distance = plane.distance;
            float x2 = inverse.m00 * x + inverse.m10 * y + inverse.m20 * z + inverse.m30 * distance;
            float y2 = inverse.m01 * x + inverse.m11 * y + inverse.m21 * z + inverse.m31 * distance;
            float z2 = inverse.m02 * x + inverse.m12 * y + inverse.m22 * z + inverse.m32 * distance;
            float d = inverse.m03 * x + inverse.m13 * y + inverse.m23 * z + inverse.m33 * distance;
            plane.distance = d;
            plane.normal = new Vector3(x2, y2, z2);
            return plane;
        }
        private void FindUniqueChunks(){
            if(camera == null)
                return;
            
            RuntimeTools.GetFrustrumPlanes(camera, settings.ViewDistance, ref frustrumPlanes);
            for(int p = 0; p < frustrumPlanes.Length; p++){
                frustrumPlanes[p] = TransformPlaneFast(frustrumPlanes[p], infiniteLandsController.localToWorldMatrix); //we need to use the inverse, but is actually doing world to local
            }

            foreach(var group in GroupedIndices){
                group.Value.Clear();
            }

            Vector3 cameraPosition = CamPosition();
            foreach(var chunk in CachedVisibleChunks){
                var vegetationChunk = chunk.Value;
                var bufferIndex = vegetationChunk.bufferData;
                if(vegetationChunk == null || !vegetationChunk.IsVisible(cameraPosition, frustrumPlanes))
                    continue;

                if (!GroupedIndices.TryGetValue(bufferIndex.instanceBufferIndex, out var list))
                {
                    list = new List<int>();
                    GroupedIndices[bufferIndex.instanceBufferIndex] = list;
                }
                list.Add(bufferIndex.chunkIndex);
            }
        }
        private Vector3 CamPosition() => infiniteLandsController.worldToLocalMatrix.MultiplyPoint(camera.transform.position);
        static Comparison<int> compare_int = (a, b) =>
        {
            if (a < b)
            {
                return -1;
            }
            if (a > b)
            {
                return 1;
            }
            return 0;
        };
        private void RenderVisibleChunks(MaterialPropertyBlock propertyBlock, IDrawInstances drawer, IndexCompactor compactor){
            if(camera == null)
                return;

            CommandBuffer bf = CommandBufferPool.Get(AssetName);
            using(new ProfilingScope(bf, VisibilityCheckSampler))
            {
                int drawCallCount = 0;
                SetVisibilityBufferData(bf);
                foreach(var group in GroupedIndices){
                    var instanceBufferIndex = group.Key;
                    if(group.Value.Count <= 0)
                        continue;
                    var instanceBuffer = ChunksManager.GetInstanceBuffer(instanceBufferIndex);
                    group.Value.Sort(compare_int);
                    var drawCall = drawer.GetAvailableCompactable(drawCallCount, camera);
                    compactor.InitialCompact(bf, group.Value, drawCall, instanceBuffer, infiniteLandsController.localToWorldMatrix);
                    compactor.VisibilityCheck(bf, drawCall, instanceBuffer, compactor);
                    drawer.PrepareDrawData(bf, compactor, drawCallCount);
                    drawer.DrawItems(propertyBlock, instanceBuffer, drawCallCount, camera);

                    drawCallCount++;
                }
            }
            Graphics.ExecuteCommandBuffer(bf);
            CommandBufferPool.Release(bf);
        }

        private void SetVisibilityBufferData(CommandBuffer bf){
            ComputeShader VisibilityCheck = VegetationRenderer.VisibilityCheck;
            int kernel = VegetationRenderer.VisibilityCheckKernel;

            Matrix4x4 VP = GL.GetGPUProjectionMatrix(camera.projectionMatrix, false) * camera.worldToCameraMatrix;
            RuntimeTools.GetTriangles(camera, ref FrustrumCorners, ref FrustrumTriangles, ref frustumCornersNear, ref frustumCornersFar);
            TriangleCorners.SetData(FrustrumTriangles);

            bf.SetComputeBufferParam(VisibilityCheck, kernel, frustrumTrianglesID, TriangleCorners);
            bf.SetComputeVectorParam(VisibilityCheck, cameraPositionID, camera.transform.position);
            bf.SetComputeMatrixParam(VisibilityCheck, matrixID, VP);

/*             
            if(DepthTexture == null){
                DepthTexture = Shader.GetGlobalTexture("_TerrainDepthTexture");
                bf.SetComputeTextureParam(VisibilityCheck, VegetationRenderer.VisibilityCheckKernel, "_DepthTexture", DepthTexture);
            } */
        }
        #endregion

        #region Public            
        public void Dispose(){
            if(TriangleCorners != null){
                TriangleCorners.Release();
                TriangleCorners = null;
            }
        }

        public void OnDrawGizmos()
        {
            Gizmos.color = Color.red;
            if (camera == null)
                return;

            List<Bounds> visible = ListPoolLight<Bounds>.Get();
            List<Bounds> notVisible = ListPoolLight<Bounds>.Get();
            Vector3 cameraPosition = CamPosition();

            foreach (Vector2Int previousChunkID in TransformInGrid.VisibleChunks)
            {
                var chunk = ChunksManager.GetChunk(previousChunkID, out BufferIndex index);
                if (chunk == null)
                    continue;

                bool isVisible = chunk.IsVisible(cameraPosition, frustrumPlanes);
                if (isVisible)
                    visible.Add(chunk.Bounds);
                else
                    notVisible.Add(chunk.Bounds);
            }

            Gizmos.color = Color.red;
            foreach (Bounds bnd in notVisible)
            {
                Gizmos.DrawWireCube(bnd.center, bnd.size);
            }

            Gizmos.color = Color.blue;
            foreach (Bounds bnd in visible)
            {
                Gizmos.DrawWireCube(bnd.center, bnd.size);
            }

            ListPoolLight<Bounds>.Release(visible);
            ListPoolLight<Bounds>.Release(notVisible);
        }
        #endregion
    }
}