using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands{
    public interface IDisposableJob 
    {
        public void Dispose(JobHandle job);
    }
}