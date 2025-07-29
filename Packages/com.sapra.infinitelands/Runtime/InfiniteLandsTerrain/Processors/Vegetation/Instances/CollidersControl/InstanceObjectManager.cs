using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

#if UNITY_EDITOR
using UnityEditor;
#endif
namespace sapra.InfiniteLands{
    public class InstanceObjectManager
    {
        private VegetationSettings Settings;
        private VegetationChunkManager ChunksManager;
        private ObjectData objectData;

        ObjectPool<InstanceDataHolder> instancePool;
        ObjectPool<UsedGameObjects> usedGameObjectsPool;

        private Dictionary<Vector2Int, UsedGameObjects> EnabledInstances = new();

        private Transform parent;

        private int VisibleChunks;
        private float CheckUpSize;
        
        private List<TransformInGrid> CreateCollidersAround = new();

        private IControlTerrain infiniteLandsController;

        private float ResizeFactor;

        bool disabled;
        bool FullInstanceMode;
        public InstanceObjectManager(VegetationSettings settings, VegetationChunkManager instancesHolder, IHoldVegetation asset, IControlTerrain infiniteLandsController, 
                IEnumerable<Transform> originalTransforms){            
            
            Settings = settings;
            ChunksManager = instancesHolder;
            objectData = asset.GetObjectData();

            switch (objectData.ColliderMode)
            {
                case IHoldVegetation.ColliderMode.Minimal:
                    CheckUpSize = settings.DistanceBetweenItems;
                    VisibleChunks = 1;
                    FullInstanceMode = false;
                    break;
                case IHoldVegetation.ColliderMode.ByDistance:
                    CheckUpSize = settings.DistanceBetweenItems;
                    VisibleChunks = Mathf.Max(1, Mathf.CeilToInt(objectData.CollisionDistance / CheckUpSize));
                    FullInstanceMode = false;
                    break;
                case IHoldVegetation.ColliderMode.AllObjects:
                    CheckUpSize = settings.ChunkSize;
                    VisibleChunks = Mathf.CeilToInt(settings.ViewDistance / CheckUpSize);
                    FullInstanceMode = true;
                    break;

            }
            this.infiniteLandsController = infiniteLandsController;
            ResizeFactor = CheckUpSize/settings.ChunkSize;

            var renderer = infiniteLandsController.GetInternalComponent<VegetationRenderer>();
            var targetParentObject = renderer.GetVegetationParent();
            parent = RuntimeTools.FindOrCreateObject(asset.name, targetParentObject).transform;
            instancePool = new ObjectPool<InstanceDataHolder>(GetInstance, actionOnDestroy: AdaptiveDestroy);
            usedGameObjectsPool = new ObjectPool<UsedGameObjects>(GetUsableGameObject);
            foreach(var transform in originalTransforms){
                CreateCollidersAround.Add(new TransformInGrid(transform, infiniteLandsController, Settings.GridOffset));
            }
            disabled = objectData.gameObject == null;
        }

        public void ChunkCollision(){
            if(disabled)
                return;
            var ToEnable = ListPoolLight<Vector2Int>.Get();
            var ToDisable = ListPoolLight<Vector2Int>.Get();
            foreach(TransformInGrid transformInGrid in CreateCollidersAround){
                transformInGrid.UpdateChunksAround(ref ToEnable, ref ToDisable, CheckUpSize, VisibleChunks);
            }
            DisableColliders(ToDisable);
            EnableColliders(ToEnable);

            ListPoolLight<Vector2Int>.Release(ToEnable);
            ListPoolLight<Vector2Int>.Release(ToDisable);
        }


        #region Transform Control
        public void AddTransform(Transform transform)
        {
            if(disabled)
                return;
            var exists = CreateCollidersAround.FirstOrDefault(a => a.Transform.Equals(transform));
            if(exists == null){
                CreateCollidersAround.Add(new TransformInGrid(transform, infiniteLandsController, Settings.GridOffset));
            }
        }

        public void RemoveTransform(Transform transform)
        {
            if(disabled)
                return;
            var exists = CreateCollidersAround.FirstOrDefault(a => a.Transform.Equals(transform));
            if(exists != null){
                CreateCollidersAround.Remove(exists);
            }
        }
        #endregion

        public void Dispose(){
            foreach(var pack in EnabledInstances){
                pack.Value.Dispose();
            }
            instancePool.Dispose();
            if(parent != null){
                RuntimeTools.AdaptiveDestroy(parent.gameObject);
            }
        }

        public void OriginShift(Vector3 offset)
        {
            if(disabled)
                return;
            foreach(var pack in EnabledInstances){
                var list = pack.Value;
                list.OriginShift(offset);
            }
        }
       

        private void EnableColliders(List<Vector2Int> enable){
            foreach(Vector2Int toEnable in enable){
                if(EnabledInstances.ContainsKey(toEnable)){
                    EnabledInstances[toEnable].IncreaseUses();
                    continue;
                }

                ResizeFactor = CheckUpSize/Settings.ChunkSize;
                Vector2 chunkPosition = new Vector2(toEnable.x+0.0001f, toEnable.y+0.0001f)*ResizeFactor+Vector2.one*0.5f;
                Vector2Int chunkIndex = Vector2Int.FloorToInt(chunkPosition);

                UsedGameObjects usedGameObject = usedGameObjectsPool.Get();

                if(FullInstanceMode)
                    usedGameObject.EnableInstance(chunkIndex);
                else
                    usedGameObject.EnableInstance(chunkIndex, toEnable);
                usedGameObject.IncreaseUses();

                EnabledInstances.Add(toEnable, usedGameObject);
            }
        }

        private void DisableColliders(List<Vector2Int> disable){
            foreach(Vector2Int toDisable in disable){
                if(!EnabledInstances.ContainsKey(toDisable))
                    continue;
            
                var instance = EnabledInstances[toDisable];
                if(instance.DecreaseUses()){
                    EnabledInstances.Remove(toDisable);
                    usedGameObjectsPool.Release(instance);
                }
                
            }
        }

        private InstanceDataHolder GetInstance(){
            GameObject BJ = Object.Instantiate(objectData.gameObject, parent);
            InstanceDataHolder instanceDataHolder = BJ.AddComponent<InstanceDataHolder>();
            return instanceDataHolder;
        }

        private UsedGameObjects GetUsableGameObject(){
            return new UsedGameObjects(Settings, ChunksManager, instancePool);
        }
        
        private void AdaptiveDestroy(InstanceDataHolder obj){
            if(!obj.Equals(null))
                RuntimeTools.AdaptiveDestroy(obj.gameObject);
        }
    }
}