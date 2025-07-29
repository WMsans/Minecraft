using UnityEngine;

namespace sapra.InfiniteLands{
    [ExecuteAlways]
    public abstract class InfiniteLandsMonoBehaviour : MonoBehaviour, ILandsLifeCycle
    {
        [SerializeField] protected InfiniteLandsTerrain infiniteLandsTerrain;
        public void SetInfiniteLandsTerrain(InfiniteLandsTerrain infiniteLandsTerrain)
        {
            this.infiniteLandsTerrain = infiniteLandsTerrain;
        }
        public virtual void Start()
        {
            if (infiniteLandsTerrain == null)
            {
                infiniteLandsTerrain = transform.GetComponentInParent<InfiniteLandsTerrain>();
            }

            if (infiniteLandsTerrain != null)
            {
                infiniteLandsTerrain.AddMonoForLifetime(this);
            }
            else
            {
                Debug.LogWarning("Infinite Lands Terrain wasn't found on the current hirearchy. Please manually assign it to the field");
            }
        }
        public virtual void OnDestroy()
        {
            if (infiniteLandsTerrain == null)
            {
                infiniteLandsTerrain = transform.GetComponentInParent<InfiniteLandsTerrain>();
            }
            if (infiniteLandsTerrain != null)
            {
                infiniteLandsTerrain?.RemoveMonoForLifetime(this);
            }
            else
            {
                Debug.LogWarning("Infinite Lands Terrain wasn't found on the current hirearchy. Please manually assign it to the field");
            }
            Disable();
        }
        
        /// <summary>
        /// Called when the generator starts
        /// </summary>
        /// <param name="lands"></param>
        public abstract void Disable();
        public virtual void OnGraphUpdated(){}

        public abstract void Initialize(IControlTerrain lands);
    }
}