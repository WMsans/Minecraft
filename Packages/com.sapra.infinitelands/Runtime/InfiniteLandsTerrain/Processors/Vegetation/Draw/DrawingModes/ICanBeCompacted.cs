using UnityEngine;

namespace sapra.InfiniteLands{
    public interface ICanBeCompacted 
    {
        public void ApplyCorners(Matrix4x4 localToWorld, Vector3 minBounds, Vector3 maxBounds);
        public void SetInstanceCount(int totalInstances, float threadGroupSize);
        public int Threads{get;}
        public int TotalInstancesAdded{get;}
        public ComputeBuffer TargetLODs{get;}
    }
}