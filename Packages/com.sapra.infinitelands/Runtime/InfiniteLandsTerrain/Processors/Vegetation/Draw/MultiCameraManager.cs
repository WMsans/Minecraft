using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace sapra.InfiniteLands{
    public class MultiCameraManager 
    {
        public IHoldVegetation VegetationAsset{get; private set;}
        private FillingData colorData;
        private ObjectData objectData;

        private List<ChunkVisibilityRenderer> cameraAndFrustrumPlanes;
        private VegetationSettings settings; //NEEDS CHANGE

        private VegetationChunkManager ChunksManager;
        private IDrawInstances drawer;
        private IndexCompactor compactor;
        private InstanceObjectManager ObjectManager;

        private ArgumentsData ArgumentsData;
        private IControlTerrain infiniteLandsController;

        public MultiCameraManager(IHoldVegetation vegetationAsset,VegetationSettings settings, IEnumerable<Camera> cameras, IEnumerable<Transform> transforms, IControlTerrain infiniteLandsController)
        {
            VegetationAsset = vegetationAsset;
            colorData = vegetationAsset.GetColorData();
            objectData = vegetationAsset.GetObjectData();

            this.settings = settings;
            this.infiniteLandsController = infiniteLandsController;
            ArgumentsData = vegetationAsset.InitializeMeshes();
            ChunksManager = new VegetationChunkManager(vegetationAsset, settings, infiniteLandsController);
            
            if(ArgumentsData.ValidArguments){
                if((!colorData.lodCrossFade || ArgumentsData.LODLength <= 1) && ArgumentsData.LODLength <= 3)
                    drawer = new AppendDraw(settings, ArgumentsData, colorData.lodCrossFade);
                else
                    drawer = new CountedDraw(settings, ArgumentsData);
                compactor = new IndexCompactor(vegetationAsset, settings);
            }

            cameraAndFrustrumPlanes = new List<ChunkVisibilityRenderer>();
            foreach(Camera cam in cameras){
                CreateCameraFrustrum(cam);
            }

            if(!objectData.SpawnsObject)
                return;

            ObjectManager = new InstanceObjectManager(settings, ChunksManager, vegetationAsset, infiniteLandsController, transforms);
        }

        #region Dynamic Changes
        public void AddCamera(Camera cam){
            var exists = cameraAndFrustrumPlanes.FirstOrDefault(a => a.camera.Equals(cam));
            if(exists == null){
                CreateCameraFrustrum(cam);
            }
        }
        private void CreateCameraFrustrum(Camera cam){
            var newCam = new ChunkVisibilityRenderer(cam, ChunksManager, settings, infiniteLandsController, VegetationAsset.name);
            cameraAndFrustrumPlanes.Add(newCam);
        }

        public void RemoveCamera(Camera cam){
            var exists = cameraAndFrustrumPlanes.FirstOrDefault(a => a.camera.Equals(cam));
            if(exists != null){
                exists.Dispose();
                cameraAndFrustrumPlanes.Remove(exists);
            }
        }
        public void AddTransform(Transform transform){if(objectData.SpawnsObject) ObjectManager.AddTransform(transform);}
        public void RemoveTransform(Transform transform){if(objectData.SpawnsObject) ObjectManager.RemoveTransform(transform);}

        public void OnOriginShift(CommandBuffer bf, ComputeShader compute, int kernel, Vector3 offset){
            ChunksManager.OnOriginShift(bf, compute, kernel, offset);
            ObjectManager?.OriginShift(offset);
        }
        #endregion

        #region Updating
        public bool Update(MaterialPropertyBlock propertyBlock, bool skipGenerationCheck){
            AutoRelease();

            if(!settings.Render)
                return false;
            if(VegetationAsset.SkipRendering())
                return false;

            bool CreatedData = false;
            if(!skipGenerationCheck)
                CreatedData = ChunkVisiblity();
                
            if(ArgumentsData.ValidArguments)
                RenderIntoCameras(propertyBlock);

            if(ObjectManager != null)
                ObjectManager.ChunkCollision();     

            return CreatedData;   
        }    

        private void RenderIntoCameras(MaterialPropertyBlock propertyBlock){
            foreach(ChunkVisibilityRenderer cameraAndFrustrumPlane in cameraAndFrustrumPlanes){
                cameraAndFrustrumPlane.Render(propertyBlock, drawer, compactor);
            }
        }

        private void AutoRelease(){
            drawer?.AutoRelease();
            ChunksManager.UpdateReleaseOfCompactors();
        }

        private bool ChunkVisiblity(){
            var ToEnable = ListPoolLight<Vector2Int>.Get();
            var ToDisable = ListPoolLight<Vector2Int>.Get();
            foreach(ChunkVisibilityRenderer cameraAndFrustrumPlanes in cameraAndFrustrumPlanes){
                cameraAndFrustrumPlanes.UpdateChunksAround(ref ToEnable, ref ToDisable);
            }

            DisableChunks(ToDisable);
            EnableChunks(ToEnable);
            var isVisible = ToEnable.Count > 0 || ToDisable.Count > 0;

            ListPoolLight<Vector2Int>.Release(ToEnable);
            ListPoolLight<Vector2Int>.Release(ToDisable);
            return isVisible;
        }

        private void DisableChunks(List<Vector2Int> toDisable){
            foreach(Vector2Int disable in toDisable){
                ChunksManager.DisableChunk(disable, false);
            }
        }
        private void EnableChunks(List<Vector2Int> toEnable){
            foreach(Vector2Int id in toEnable){
                if(ChunksManager.IsChunkPrepared(id))
                    continue;

                var exists = ChunksManager.GetChunk(id, out _);
                if(exists == null)
                   exists = ChunksManager.EnableChunk(id);

                exists.IncreaseUses();
            }
        }
        #endregion

        public void Dispose(){
            ChunksManager.Dispose();
            compactor?.Dispose();
            foreach(ChunkVisibilityRenderer cameraAndFrustrumPlane in cameraAndFrustrumPlanes){
                cameraAndFrustrumPlane.Dispose();
            }
            ObjectManager?.Dispose();
            if(drawer != null)
                drawer.Dispose();
        }

        public void OnDrawGizmos(){
            if(VegetationAsset.DrawBoundingBox())
                ChunksManager.OnDrawGizmos();
            if(drawer != null && VegetationAsset.DrawBoundingBox())
                drawer.OnDrawGizmos();
            foreach(ChunkVisibilityRenderer cameraAndFrustrumPlane in cameraAndFrustrumPlanes){
                if(cameraAndFrustrumPlane.camera == null)
                    return;
                if(VegetationAsset.DrawBoundingBox())
                    cameraAndFrustrumPlane.OnDrawGizmos();
                VegetationAsset.GizmosDrawDistances(cameraAndFrustrumPlane.camera.transform.position);
            }
        }
    }
}