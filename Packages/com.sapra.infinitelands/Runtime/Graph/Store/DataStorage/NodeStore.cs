using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace sapra.InfiniteLands{
    
    public class NodeStore{
        private class SelfReleasable<K, T> : DataStore<K, T>, ICacheAny<K>
        {
            public override void Release()
            {
                GenericPoolLight.Release(this);
            }
            
            public bool TryGetDataBoxed(K key, out object data)
            {   
                bool result = TryGetValue(key, out var dataStored);
                data = dataStored;
                return result;
            }
        }
        private DataStore<Type, IRelease> StorageOfCachedData = new();

        private Type LastChecked;
        private IRelease LastStoreChecked;

        public DataStore<string, T> GetStoreOfType<T>(bool create){
            var targetType = typeof(T);
            if (targetType == LastChecked)
                return (DataStore<string, T>)LastStoreChecked;
                
            if (!StorageOfCachedData.TryGetValue(targetType, out IRelease store))
            {
                if (!create)
                    Debug.LogErrorFormat("Something went wrong, no store of type {0} when retrieving data", typeof(T));
                var extStore = GenericPoolLight<SelfReleasable<string, T>>.Get();
                extStore.Reuse();
                store = extStore;
                StorageOfCachedData.AddData(targetType, store);
            }
            LastChecked = targetType;
            LastStoreChecked = store;
            return (DataStore<string, T>)store;
        }
    
        public void Release()
        {
            var originalStores = StorageOfCachedData.GetManyDataRaw();
            foreach (var store in originalStores)
            {
                store.Release();
            }
        }
        
        public bool TryGetData<T>(string key, out T result)
        {
            if (typeof(T) == typeof(object))
            {
                var existingData = StorageOfCachedData.GetManyDataRaw();
                for (int i = 0; i < existingData.Count; i++)
                {
                    var actualStore = (ICacheAny<string>)existingData[i];
                    if (actualStore.TryGetDataBoxed(key, out var actuallyDataStored))
                    {
                        result = (T)actuallyDataStored;
                        return true;
                    }
                }

                result = default;
                return false;
            }
            else
            {
                var cachedDataStore = GetStoreOfType<T>(false);
                return cachedDataStore.TryGetValue(key, out result);
            }
        }

        public bool TryGetData<T>(string key, int itemIndex, out T result){
            if (itemIndex < 0)
                return TryGetData(key, out result);

            if (!TryGetData(key, out IEnumerable<T> data))
                {
                    result = default;
                    return false;
                }
                else
                {
                    result = data.ElementAtOrDefault(itemIndex);
                    return true;
                }
        }

        public void AddData<T>(string id, T data){
            var store = GetStoreOfType<T>(true);
            store.AddData(id, data);
        }

        public void AddData<T>(string id, IEnumerable<T> data){
            var store = GetStoreOfType<IEnumerable<T>>(true);
            store.AddData(id, data);
        }

        public void Reuse()
        {
            StorageOfCachedData.Reuse();

            LastChecked = null;
            LastStoreChecked = null;
        }
    }
}