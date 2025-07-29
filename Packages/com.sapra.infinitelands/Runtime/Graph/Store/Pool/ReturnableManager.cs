using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class ReturnableManager: IInitializeBranch{
        private StringObjectStore<object> GlobalStore;
        public ReturnableManager(StringObjectStore<object> store){
            this.GlobalStore = store;
        }
 
        private NativeArrayPool<T> GetPool<T>() where T : struct{
            var key = StringObjectStore<object>.GetNameOfType<T>();
            return GlobalStore.GetOrCreateData<NativeArrayPool<T>>(key);
        }

        public NativeArray<T> GetData<T>(ReturnablePack holder, int size) where T : struct{
            var pool = GetPool<T>();
            return pool.GetDataToReturn(holder, size);
        }

        public NativeArray<T> GetData<T>(ReturnablePack holder, T[] data) where T : struct{
            var pool = GetPool<T>();
            return pool.GetDataToReturn(holder, data);
        }

        public NativeArray<T> GetData<T>(ReturnablePack holder, List<T> data) where T : struct{
            var pool = GetPool<T>();
            return pool.GetDataToReturn(holder, data);
        }
        public void InitializeBranch(BranchData branch, BranchData previousBranch)
        {
            ReturnableBranch returnableData = GenericPoolLight<ReturnableBranch>.Get();
            ReturnablePack pack = GenericPoolLight<ReturnablePack>.Get();
            returnableData.Reuse(this, pack);
            branch.AddData(returnableData);
        }
    }
}