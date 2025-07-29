using System.Collections.Generic;
using Unity.Collections;

namespace sapra.InfiniteLands{
    public class ReturnableBranch : IRelease {
        private ReturnableManager manager;
        private ReturnablePack returnablePack;
        public void Reuse(ReturnableManager manager, ReturnablePack returnablePack)
        {
            this.manager = manager;
            this.returnablePack = returnablePack;
        }
        public NativeArray<T> GetData<T>(int size) where T : struct{
            return manager.GetData<T>(returnablePack, size);
        }

        public NativeArray<T> GetData<T>(T[] data) where T : struct{
            return manager.GetData(returnablePack, data);
        }

        public NativeArray<T> GetData<T>(List<T> data) where T : struct{
            return manager.GetData(returnablePack, data);
        }

        public void Release()
        {
            returnablePack.Release();
            GenericPoolLight.Release(this);
        }
    }
}