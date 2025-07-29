using UnityEngine;

namespace sapra.InfiniteLands{
    [ExecuteAlways]
    public class ShowInLOD : InfiniteLandsMonoBehaviour
    {
        public GameObject Target;
        public int LODValue = -1;

        public IControlTerrain infiniteLands;
        private PointStore store;
        public void Awake()
        {
            if(Target != null)
                Target.SetActive(false);
        }

        void OnChunkChanges(CoordinateResult data){
            if(!infiniteLands.IsPointInChunk(transform.position, data.terrainConfiguration))
                return;
            if(LODValue < 0 || data.terrainConfiguration.ID.z <= LODValue)
                Target.SetActive(true);
            else
                Target.SetActive(false);
        }
        
        public override void Disable()
        {
            if(store != null)
                store.onProcessDone -= OnChunkChanges;
        }

        public override void Initialize(IControlTerrain lands)
        {
            infiniteLands = lands;
            store = lands.GetInternalComponent<PointStore>();
            
            if(store != null && Target != null){
                store.onProcessDone += OnChunkChanges;
                if(store.GetHolderAt(transform.position, out var result))
                    OnChunkChanges(result);
            }
        }
    }
}