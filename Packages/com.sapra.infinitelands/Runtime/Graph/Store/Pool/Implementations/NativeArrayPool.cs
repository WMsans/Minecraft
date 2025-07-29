using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands{
    public class NativeArrayPool<T> : IReciveReturnable<NativeArray<T>>, IDisposableJob, IDisposeReturned 
        where T : struct
    {
        protected Dictionary<int, List<NativeArray<T>>> _availableData = new();
        protected HashSet<NativeArray<T>> _usedData = new();

        protected int MaxCreations;
        public void Return(NativeArray<T> data)
        {
            _usedData.Remove(data);
            var length = data.Length;
            if(_availableData.TryGetValue(length, out var values))
                values.Add(data);
        }
        
        public virtual NativeArray<T> GetDataToReturn(ReturnablePack pack, int size)
        {
            NativeArray<T> data;
            if (!_availableData.TryGetValue(size, out var listOfArrays))
            {
                listOfArrays = ListPoolLight<NativeArray<T>>.Get();
                _availableData.Add(size, listOfArrays);
            }

            if (listOfArrays.Count > 0)
            {
                data = listOfArrays[0];
                listOfArrays.RemoveAt(0);
            }
            else
            {
                data = CreateArray(size);
                MaxCreations++;
            }
            _usedData.Add(data);
            var toReturn = GenericPoolLight<ToReturn<NativeArray<T>>>.Get();
            toReturn.Reuse(data, this);
            pack.Add(toReturn);
            return data;
        }
        private static NativeArray<T> CreateArray(int size){
            return new NativeArray<T>(size, Allocator.Persistent,
                    NativeArrayOptions.UninitializedMemory);
        }

        public NativeArray<T> GetDataToReturn(ReturnablePack pack, T[] data)
        {
            var current = GetDataToReturn(pack, data.Length);
            current.CopyFrom(data);
            return current;
        }

        public NativeArray<T> GetDataToReturn(ReturnablePack pack, List<T> data)
        {
            var current = GetDataToReturn(pack, data.Count);
            for(int i = 0; i < data.Count; i++){
                current[i] = data[i];
            }
            return current;
        }
        public void Dispose(JobHandle job)
        {
            foreach (var item in _availableData)
            {
                var values = item.Value;
                foreach (var array in values)
                {
                    try
                    {
                        if (array.IsCreated)
                            array.Dispose(job);
                    }catch{}
                }
            }
            _availableData.Clear();

            if(_usedData.Count > 0){
                Debug.LogWarningFormat("{0} hasn't been returned. Manually disposing {1} elements", typeof(T), _usedData.Count);

                try
                {
                    foreach (var usedItem in _usedData)
                    {
                        if (usedItem.IsCreated)
                            usedItem.Dispose();
                    }
                }catch{}
    
                _usedData.Clear();
            }
        }

        public void DisposeReturned()
        {
            foreach (var item in _availableData)
            {
                var values = item.Value;
                foreach (var array in values)
                {
                    array.Dispose();
                }
                ListPoolLight<NativeArray<T>>.Release(values);
            }
            _availableData.Clear();
        }
    }

}