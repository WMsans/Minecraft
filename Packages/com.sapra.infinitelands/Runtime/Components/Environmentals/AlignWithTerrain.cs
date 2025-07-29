using UnityEngine;

namespace sapra.InfiniteLands{
    [ExecuteAlways]
    public class AlignWithTerrain : InfiniteLandsMonoBehaviour
    {
        public enum GroundAlignement{None, Ground, Terrain}
        public GroundAlignement AlignementMode = GroundAlignement.None;
        public IControlTerrain infiniteLands;
        private PointStore store;

        public float SmoothTransitionToPosition = 0;

        private Vector3 targetPosition;
        private Vector3 targetNormal;

        private Vector3 refVelocity;

        private FloatingPoint pnt;
        private bool Move;
        
        public override void Initialize(IControlTerrain lands)
        {
            pnt = GetComponent<FloatingPoint>();
            infiniteLands = lands;
            store = lands.GetInternalComponent<PointStore>();

            targetPosition = transform.position;
            if (pnt != null)
                pnt.OnOffsetAdded += ApplyOffset;
            if (store != null)
            {
                store.onProcessDone += OnChunkChanges;
                if(store.GetHolderAt(transform.position, out var result))
                    OnChunkChanges(result);
            }
        }

        public override void Disable()
        {
            if(store != null)
                store.onProcessDone -= OnChunkChanges;
            if(pnt != null)
                pnt.OnOffsetAdded -= ApplyOffset;
        }

        void OnChunkChanges(CoordinateResult data){
            if(!infiniteLands.IsPointInChunk(transform.position, data.terrainConfiguration))
                return;
            
            Vector3 flattened = infiniteLands.WorldToLocalPoint(transform.position);
            CoordinateData current = data.GetCoordinateDataAtGrid(new Vector2(flattened.x, flattened.z), true);
            current = current.ApplyMatrix(infiniteLands.localToWorldMatrix);

            Vector3 position = current.position;
            Vector3 normal = current.normal;
            if(position != targetPosition){
                targetPosition = position;
                targetNormal = normal;
                Move = true;
            }
            if(current.ID.z <= 0)
                store.onProcessDone -= OnChunkChanges;
        }
        void ApplyOffset(Vector3 offset){
            targetPosition += offset;
        }

        void Update()
        {
            if(transform.position != targetPosition && Move){
                transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref refVelocity, SmoothTransitionToPosition);
                if(Vector3.SqrMagnitude(targetPosition-transform.position) < 0.1f){
                    transform.position = targetPosition;
                    Move = false;
                    UpdateRotation();
                }
            }
        }

        private void UpdateRotation(){
            switch(AlignementMode){
                case GroundAlignement.Ground:
                    transform.rotation = Quaternion.FromToRotation(transform.up, targetNormal)*transform.rotation;
                    break;
                case GroundAlignement.Terrain:
                    transform.rotation = Quaternion.FromToRotation(transform.up, infiniteLands.LocalToWorldVector(Vector3.up))*transform.rotation;
                    break;

            }
        }
    }
}