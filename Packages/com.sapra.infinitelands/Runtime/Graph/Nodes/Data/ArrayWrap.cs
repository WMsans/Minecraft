using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class ArrawyWrap<T> : IDisposableJob where T : struct
    {
        public NativeArray<T> values;
        public ArrawyWrap(NativeArray<T> values){
            this.values = values;
        }
        public void Dispose(JobHandle job)
        {
            if(values.IsCreated){
                values.Dispose(job);
            }
        }
    }
}