using UnityEngine;

namespace sapra.InfiniteLands{
    [System.Serializable]
    public abstract class InfiniteLandsComponent : ILandsLifeCycle
    {
        [HideInInspector] public Transform transform;
        [HideInInspector] public GameObject gameObject;
        [HideInInspector] protected IControlTerrain infiniteLands;
        [HideInInspector] public bool expanded = true;
        protected IGraph graph => infiniteLands.graph;
        
        /// <summary>
        /// Called when the generator starts
        /// </summary>
        /// <param name="lands"></param>
        public virtual void Initialize(IControlTerrain lands)
        {
            infiniteLands = lands;
            if(lands != null){
                transform = lands.transform;
                gameObject = lands.gameObject;
            }
        }

        /// <summary>
        /// Called when the generator finishes
        /// </summary>
        /// <param name="lands"></param>
        public abstract void Disable();
        
        /// <summary>
        /// Called when the graph changes
        /// </summary>
        /// <param name="lands"></param>
        public virtual void OnGraphUpdated(){}

        #region From Monobehvaiours
        public virtual void Update(){}
        public virtual void LateUpdate(){}
        public virtual void OnValidate(){}
        public virtual void OnDrawGizmos(){}

        protected T GetComponentInChildren<T>() => gameObject.GetComponentInChildren<T>();
        protected T GetComponent<T>(){
            if(infiniteLands == null)
                return default;

            var internalComponent = infiniteLands.GetInternalComponent<T>();
            if(internalComponent == null && typeof(MonoBehaviour).IsAssignableFrom(typeof(T)))
                return gameObject.GetComponent<T>();
            else
                return internalComponent;
        }
        public static void AdaptiveDestroy(UnityEngine.Object obj) => RuntimeTools.AdaptiveDestroy(obj);

        public static T Instantiate<T>(T original, Transform parent) where T : Object => GameObject.Instantiate(original, parent);
        public static T[] FindObjectsByType<T>(FindObjectsSortMode sortMode) where T : Object => GameObject.FindObjectsByType<T>(sortMode);
        #endregion
    }
}