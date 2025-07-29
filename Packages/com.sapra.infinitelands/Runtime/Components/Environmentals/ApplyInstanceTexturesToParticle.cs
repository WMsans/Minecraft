using UnityEngine;

namespace sapra.InfiniteLands{
    public class ApplyInstanceTexturesToParticle : MonoBehaviour
    {
        public TerrainPainter painter;
        public InstanceDataHolder instanceDataHolder;
        public string IndexName = "_texture_index";

        private Material modified;

        // Start is called before the first frame update
        void Start()
        {
            var lands = FindAnyObjectByType<InfiniteLandsTerrain>();
            if(lands != null)
                painter = lands.GetInternalComponent<TerrainPainter>();
            instanceDataHolder = transform.GetComponentInParent<InstanceDataHolder>();

            var particleSystem = GetComponent<ParticleSystemRenderer>();
            if(particleSystem != null){
                modified = new Material(particleSystem.material);
                particleSystem.material = modified;
            }

            if(instanceDataHolder != null && painter != null && modified != null){
                painter.AssignTexturesToMaterials(modified);
                modified.SetInt(IndexName, (int)instanceDataHolder.instanceData.GetTextureIndex());
            }
        }
        void OnEnable()
        {
            if(instanceDataHolder != null && painter != null && modified != null){
                modified.SetInt(IndexName, (int)instanceDataHolder.instanceData.GetTextureIndex());
            }

        }
        // Update is called once per frame
        void Update()
        {
            
        }
    }
}