using UnityEngine;

namespace sapra.InfiniteLands{
    public interface ILandsLifeCycle
    {
        /// <summary>
        /// Called when the generator starts
        /// </summary>
        /// <param name="lands"></param>
        public void Initialize(IControlTerrain lands);
        public void Disable();
        public void OnGraphUpdated();
    }
}