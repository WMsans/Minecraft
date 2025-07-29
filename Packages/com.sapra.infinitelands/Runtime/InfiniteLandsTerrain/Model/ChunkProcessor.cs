using UnityEngine;

namespace sapra.InfiniteLands{
    [System.Serializable]
    public abstract class ChunkProcessor : InfiniteLandsComponent{}
    public abstract class ChunkProcessor<T> : ChunkProcessor
    {   
        protected IGenerate<T> provider;
        public override sealed void Initialize(IControlTerrain infiniteLands)
        {
            base.Initialize(infiniteLands);
            provider = GetComponent<IGenerate<T>>();
            if (provider == null)
            {
                Debug.LogWarning(string.Format("Missing a processor of type {0}", typeof(IGenerate<T>).ToString()));
            }
            else
            {
                provider.onProcessDone -= OnProcessAdded;
                provider.onProcessDone += OnProcessAdded;

                provider.onProcessRemoved -= OnProcessRemoved;
                provider.onProcessRemoved += OnProcessRemoved;
            }
            InitializeProcessor();
        }
        public override sealed void Disable()
        {
            if(provider != null){
                provider.onProcessDone -= OnProcessAdded;
                provider.onProcessRemoved -= OnProcessRemoved;
            }
            DisableProcessor();
        }
        /// <summary>
        /// Similar to Start in a MonoBehaviour, called before any generation is done after the main initalizer
        /// </summary>
        protected abstract void InitializeProcessor();

        /// <summary>
        /// Similar to OnDestroy and OnDisable in a MonoBehaviour, called once everything is done and the system is closing after the main disabler
        /// </summary>
        protected abstract void DisableProcessor();

        protected abstract void OnProcessRemoved(T chunk);
        protected abstract void OnProcessAdded(T chunk);
    }
}