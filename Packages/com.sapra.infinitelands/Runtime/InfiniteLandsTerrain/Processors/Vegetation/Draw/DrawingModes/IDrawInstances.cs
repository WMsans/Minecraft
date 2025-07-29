using UnityEngine;
using UnityEngine.Rendering;

namespace sapra.InfiniteLands{
    public interface IDrawInstances
    {
        public void DrawItems(MaterialPropertyBlock propertyBlock, InstancesBuffer buffer, int targetDrawIndex, Camera camera);
        public void PrepareDrawData(CommandBuffer bf, IndexCompactor compactor,  int targetDrawIndex);
        public ICanBeCompacted GetAvailableCompactable(int askingFor, Camera camera);
        public void Dispose();
        public void OnDrawGizmos();
        public void AutoRelease();
    }
}