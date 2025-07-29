using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace sapra.InfiniteLands{
    public class UsedGameObjects{

        public enum State{Disabled, RequestedChunk, RequestedInstances, Completed}
        // Private fields
        private readonly VegetationSettings Settings;
        private readonly VegetationChunkManager ChunksManager;
        private readonly ObjectPool<InstanceDataHolder> InstancePool;

        private bool _singleInstanceMode;
        private Vector2 _singleInstancePosition;
        private Vector2Int ChunkID;

        private InstanceDataHolder _singleInstance;
        private List<InstanceDataHolder> _multipleInstances;
        private VegetationChunk _vegetationChunk;

        // Public property
        public int Uses { get; private set; }
        private State state;

        public UsedGameObjects(VegetationSettings settings, VegetationChunkManager chunksManager, ObjectPool<InstanceDataHolder> instancePool)
        { 
            Settings = settings;
            ChunksManager = chunksManager;
            InstancePool = instancePool;
            ChunksManager.OnCreateChunk += OnChunkGenerated;
            ChunksManager.OnInstancesCreated += OnInstancesGenerated;
            _multipleInstances = new List<InstanceDataHolder>();
            state = State.Disabled;
        }

        public void EnableInstance(Vector2Int chunkID){
            ChunkID = chunkID;
            _singleInstanceMode = false;
            _vegetationChunk = null;
            RetrieveChunk();
        }

        public void EnableInstance(Vector2Int chunkID, Vector2 singleInstancePosition){
            ChunkID = chunkID;
            _singleInstancePosition = singleInstancePosition;
            _singleInstanceMode = true;
            _vegetationChunk = null;
            RetrieveChunk();
        }

        public void DisableInstance(){
            if(_singleInstanceMode)
                ReturnInstance(_singleInstance);
            else
                ReturnInstances(_multipleInstances);

            state = State.Disabled;
            _singleInstance = null;
        }

        void RetrieveChunk(){
            _vegetationChunk = ChunksManager.GetChunk(ChunkID, out _);
            if(_vegetationChunk == null){
                state = State.RequestedChunk;
                ChunksManager.WaitForInstances(ChunkID);
                return;
            }
            RetrieveInstancecs();
        }
        void RetrieveInstancecs(){
            var instances = _vegetationChunk.GetInstances();
            if(instances == null){
                state = State.RequestedInstances;
                return;
            }
            GenerateInstance(instances);
        }
        void GenerateInstance(List<InstanceData> instances){
            if(_singleInstanceMode){
                ReturnInstance(_singleInstance);
                CreateSingleInstance(_vegetationChunk.FlatPosition, instances, ChunkID);
            }
            else{
                ReturnInstances(_multipleInstances);
                CreateMultipleInstances(instances, ChunkID);    
            } 
            state = State.Completed;
        }

        #region MethodInvocations
        private void OnChunkGenerated(Vector2Int chunkID, VegetationChunk chunk)
        {
            if(state != State.RequestedChunk) return;
            if (!ChunkID.Equals(chunkID)) return;   
            _vegetationChunk = chunk;
            RetrieveInstancecs();
        }

        
        public void OnInstancesGenerated(Vector2Int chunkID, List<InstanceData> instances){
            if(state != State.RequestedInstances && state != State.Completed) return;
            if(!ChunkID.Equals(chunkID)) return;
            GenerateInstance(instances);            
        }
        #endregion

        #region Object Creation
        private void CreateMultipleInstances(List<InstanceData> instances, Vector2Int chunkID){
            _multipleInstances.Clear();
            for(int i = 0; i < instances.Count; i++){
                InstanceData data = instances[i];
                var result = CreateInstance(data, i, chunkID);
                if(result == null)
                    continue;
                
                _multipleInstances .Add(result);
            }
        }

        private void CreateSingleInstance(Vector2 flatPosition, List<InstanceData> instances, Vector2Int chunkID){
            if(instances.Count <= 0)
                return;
            Vector2 flattenedIndex = new Vector2(_singleInstancePosition.x, _singleInstancePosition.y)*Settings.DistanceBetweenItems-flatPosition+Settings.GridOffset;

            Vector2 flatten = flattenedIndex+Vector2.one*Settings.ChunkSize/2.0f;

            flatten /= Settings.DistanceBetweenItems;

            int x = Mathf.RoundToInt(flatten.x);
            int y = Mathf.RoundToInt(flatten.y);
            
            if(outOfRange(x, 0, Settings.ChunkInstancesRow-1) || outOfRange(y, 0, Settings.ChunkInstancesRow-1)){
                Debug.LogErrorFormat("Invalid Instance position {0}:{1} from {2}", x, y, Settings.ChunkInstancesRow);
                return;
            }

            int index = Mathf.Clamp(x+y*Settings.ChunkInstancesRow, 0, Settings.ChunkInstances-1);
            _singleInstance = CreateInstance(instances[index], index, chunkID);
        }
        public bool outOfRange(int value, int min, int max){
            return value < min || value > max;
        }
        
        private InstanceDataHolder CreateInstance(InstanceData data, int index, Vector2Int chunkID)
        {
            if (!data.GetValidity()) return null;
        
            InstanceDataHolder AvailableCollider = InstancePool.Get();
            AvailableCollider.gameObject.SetActive(true);
            AvailableCollider.UseData(data, index, chunkID,ChunksManager.Asset);
            return AvailableCollider;
        }

        private void ReturnInstances(List<InstanceDataHolder> instances){
            if(instances.Count <= 0)
                return;
            foreach(InstanceDataHolder instance in instances){
                ReturnInstance(instance);
            }
            instances.Clear();
        }

        private void ReturnInstance(InstanceDataHolder instance)
        {
            if (instance == null) return;
            instance.gameObject.SetActive(false);
            InstancePool.Release(instance);
        }
        #endregion

        public void OriginShift(Vector3 offset)
        {
            if(_singleInstanceMode)
                ShiftPosition(offset, _singleInstance);
            else{
                foreach(var instance in _multipleInstances){
                    ShiftPosition(offset, instance);
                }   
            }
        }
        public void ShiftPosition(Vector3 offset, InstanceDataHolder instance){
            if (instance == null) return;
            instance.OriginShift(offset);
        }

        public void IncreaseUses() => Uses++;

        public bool DecreaseUses(){
            Uses--;
            if (Uses > 0) return false;

            DisableInstance();
            return true;
        }

        public void Dispose(){
            DisableInstance();
            ChunksManager.OnCreateChunk -= OnChunkGenerated;
            ChunksManager.OnInstancesCreated -= OnInstancesGenerated;
        }

    }
}
