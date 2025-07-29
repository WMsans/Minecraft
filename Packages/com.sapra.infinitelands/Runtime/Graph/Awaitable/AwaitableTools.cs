using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace sapra.InfiniteLands{
    public static class AwaitableTools
    {
        public static Dictionary<string, string> PreGeneratedKeyNames = new();
        public static string GetPregeneratedName(string key)
        {
            if (!PreGeneratedKeyNames.TryGetValue(key, out var waiterKey))
            {
                waiterKey = key + "-waiter";
                PreGeneratedKeyNames.Add(key, waiterKey);
            }
            return waiterKey;
        }
        public static bool Wait<TAwaitable, TResult, TReuser>(DataStore<string, TAwaitable> awaitableStore, DataStore<string, TResult> dataStore, ref TReuser Reuser, out TResult ResultingData, string key)
            where TAwaitable : class, AwaitableData<TResult>, new()
            where TReuser : struct, IReuseObject<TAwaitable>
        {
            var isCompleted = dataStore.TryGetValue(key, out var resultingObject);
            if (!isCompleted)
            {
                string waiterKey = GetPregeneratedName(key);
                if (UnsafeWait(awaitableStore, ref Reuser, out ResultingData, waiterKey))
                {
                    dataStore.AddData(key, ResultingData);
                    isCompleted = true;
                }
                else
                {
                    ResultingData = default;
                }
            }
            else
            {
                ResultingData = resultingObject;
            }

            return isCompleted;
        }

        public static bool Wait<TAwaitable, TResult, TReuser>(DataStore<string, object> store, ref TReuser Reuser, out TResult ResultingData, string key)
            where TAwaitable : class, AwaitableData<TResult>, new()
            where TReuser : struct, IReuseObject<TAwaitable>
        {
            var isCompleted = store.TryGetValue(key, out var resultingObject);
            if (!isCompleted)
            {
                string waiterKey = GetPregeneratedName(key);
                if (UnsafeWaitGeneric<TAwaitable, TResult, TReuser>(store, ref Reuser, out ResultingData, waiterKey))
                {
                    store.AddData(key, ResultingData);
                    isCompleted = true;
                }
                else
                {
                    ResultingData = default;
                }
            }
            else
            {
                ResultingData = (TResult)resultingObject;
            }

            return isCompleted;
        }

        public static bool UnsafeWait<TAwaitable, TResult, TReuser>(DataStore<string, TAwaitable> store, ref TReuser Reuser, out TResult ResultingData, string key)
            where TAwaitable : class, AwaitableData<TResult>, new()
            where TReuser : struct, IReuseObject<TAwaitable>
        {
            var factory = new ReusableFactory<TAwaitable, TReuser>(Reuser);
            var currentData = store.GetOrCreateData<TAwaitable, ReusableFactory<TAwaitable, TReuser>>(key, ref factory);
            var isCompleted = currentData.ProcessData();
            if (isCompleted)
            {
                ResultingData = currentData.Result;
                GenericPoolLight.Release(currentData);
            }
            else
                ResultingData = default;

            return isCompleted;
        }

        public static bool UnsafeWaitGeneric<TAwaitable, TResult, TReuser>(DataStore<string, object> store, ref TReuser Reuser, out TResult ResultingData, string key)
            where TAwaitable : class, AwaitableData<TResult>, new()
            where TReuser : struct, IReuseObject<TAwaitable>
        {
            var factory = new ReusableFactory<TAwaitable, TReuser>(Reuser);
            var currentData = store.GetOrCreateData<TAwaitable, ReusableFactory<TAwaitable, TReuser>>(key, ref factory);
            var isCompleted = currentData.ProcessData();
            if (isCompleted)
            {
                ResultingData = currentData.Result;
                GenericPoolLight.Release(currentData);
            }
            else
                ResultingData = default;

            return isCompleted;
        }

        public static bool IterateOverItems<TItems, TCallback>(List<TItems> items, ref TCallback method)
            where TCallback : struct, ICallMethod<TItems>
        {
            for (int i = items.Count - 1; i >= 0; i--)
            {
                bool result = method.Callback(items[i]);
                if (result)
                {
                    items.RemoveAt(i);
                }
            }

            return items.Count <= 0;
        }

        public static bool CompactData<T>(List<AwaitableData<T>> WaitingFor, List<T> Targets)
        {
            var compactor = new ICompactData<T>(Targets);
            return IterateOverItems(WaitingFor, ref compactor);
        }

        private struct ICompactData<T> : ICallMethod<AwaitableData<T>>
        {
            List<T> Results;
            public ICompactData(List<T> Results)
            {
                this.Results = Results;
            }
            public bool Callback(AwaitableData<T> value)
            {
                if (!value.ProcessData()) return false;

                Results.Add(value.Result);
                return true;
            }
        }
        
        
        private struct ReusableFactory<A, R> : IFactory<A>
            where A : class, new()
            where R : struct, IReuseObject<A>
        {
            private R reuser;
            public ReusableFactory(R reuser)
            {
                this.reuser = reuser;
            }
            public A Create()
            {
                var value = GenericPoolLight<A>.Get();
                reuser.Reuse(value);
                return value;
            }
        }        
    }
}