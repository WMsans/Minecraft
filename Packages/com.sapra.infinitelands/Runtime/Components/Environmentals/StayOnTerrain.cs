using UnityEngine;

namespace sapra.InfiniteLands{
    public class StayOnTerrain : MonoBehaviour
    {
        public InfiniteLandsTerrain infiniteLands;
        private PointStore store;
        public float verticalOffset;
        public float treshold = 0;
        
        private float physicalVerticalLimit;
        private bool enableStopping;
        private Rigidbody rb;
        private bool stoped;
        // Start is called before the first frame update
        void Start()
        {
            rb = GetComponent<Rigidbody>();
            enableStopping = false;

            infiniteLands = FindFirstObjectByType<InfiniteLandsTerrain>();
            store = infiniteLands?.GetInternalComponent<PointStore>();

            if(store != null){
                store.onProcessDone += OnChunkChanges;
                if(store.GetHolderAt(transform.position, out var result))
                    OnChunkChanges(result);
            }
        }

        void OnChunkChanges(CoordinateResult data){
            if(!infiniteLands.IsPointInChunk(transform.position, data.terrainConfiguration))
                return;
            physicalVerticalLimit = data.MinMaxHeight.x;
            Vector3 flattened = infiniteLands.WorldToLocalPoint(transform.position);
            Vector3 ground = data.GetCoordinateDataAtGrid(new Vector2(flattened.x, flattened.z), true).position;
            PlaceOnGround(ground);
        }

        void PlaceOnGround(Vector3 groundPosition){
            Vector3 current = infiniteLands.WorldToLocalPoint(transform.position);
            var dif = current-groundPosition;
            if(dif.y < treshold){
                transform.position = infiniteLands.LocalToWorldPoint(groundPosition+Vector3.up*verticalOffset); 
                if(stoped){
                    rb.isKinematic = false;
                    stoped = false;
                }
            }
            enableStopping = true;
        }

        void CheckPosition(){
            if(!enableStopping) return;
            if(!rb) return;
            if(stoped) return;
            
            Vector3 current = infiniteLands.WorldToLocalPoint(transform.position);
            if(current.y < physicalVerticalLimit){
                stoped = true;
                rb.isKinematic = true;
            }
        }
        void Update()
        {
            CheckPosition();
        }

        void OnDisable()
        {
            store.onProcessDone -= OnChunkChanges;
        }
    }
}