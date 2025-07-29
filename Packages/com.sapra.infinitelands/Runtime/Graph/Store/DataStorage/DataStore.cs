using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class DataStore<K, T> : IDisposableJob, IDisposeReturned, IRelease{
        protected Dictionary<K, T> DataStored = new();
        protected List<T> AllItems = new();

        public T GetData(K key, bool required = true){
            if(TryGetValue(key, out var data))
                return data;
            
            if(required)
                Debug.LogWarningFormat("Nothing was generated with key {0}, wrong config", key);
            return default;
        }
        

        public bool TryGetValue(K key, out T data)
        {
            return DataStored.TryGetValue(key, out data);
        }

        public virtual bool AddData<Z>(K key, Z data) where Z : T{
            if(!DataStored.TryAdd(key, data)){
                Debug.LogErrorFormat("Cannot add data with key {0}", key, data);
                return false;
            }
            else{
                AllItems.Add(data);
                return true;
            }
        }

        public object RemoveData(K key){
            if(!DataStored.TryGetValue(key, out var result)){
                result = default;
            }
            else{
                AllItems.Remove(result);
                DataStored.Remove(key);
            }

            return result;
        }   

        public Z GetOrCreateData<Z, R>(K key, ref R FactoryMaker) 
                where Z : T 
                where R : struct, IFactory<Z>{
            if(!DataStored.TryGetValue(key, out var result)){
                result = FactoryMaker.Create();
                AddData(key, result);
            }
            return (Z)result;
        }

        public Z GetOrCreateData<Z>(K key)
                where Z : T, new()
        {
            if(!DataStored.TryGetValue(key, out var result)){
                result = new Z();
                AddData(key, result);
            }
            return (Z)result;
        }

        public List<T> GetManyDataRaw() => AllItems;
        
        public virtual void Dispose(JobHandle job)
        {
            var storedData = GetManyDataRaw();
            foreach(var data in storedData){
                if(data is IDisposableJob disposable){
                    disposable.Dispose(job);
                }
            }
        }

        public virtual void DisposeReturned()
        {
            var storedData = GetManyDataRaw();
            foreach(var data in storedData){
                if(data is IDisposeReturned disposable){
                    disposable.DisposeReturned();
                }
            }
        }

        public virtual void Release()
        {
            var storedData = GetManyDataRaw();
            foreach(var data in storedData){
                if(data is IRelease releasable){
                    releasable.Release();
                }
            }
        }

        public void Reuse(){
            DataStored.Clear();
            AllItems.Clear();
        }
    }
}