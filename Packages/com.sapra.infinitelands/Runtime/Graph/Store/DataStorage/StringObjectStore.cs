using System;
using System.Collections.Generic;

namespace sapra.InfiniteLands{
    public class StringObjectStore<T> : DataStore<string, T>{
        private static readonly Dictionary<Type, string> TypeNameCache = new Dictionary<Type, string>();
        public static string GetNameOfType<Z>(){
            var type = typeof(Z);
            if (!TypeNameCache.TryGetValue(type, out string key))
            {
                key = type.Name;
                TypeNameCache[type] = key;
            }
            return key;
        }
        public Z GetData<Z>(bool required = true) where Z : T{
            string key = GetNameOfType<Z>();
            return (Z)GetData(key, required);
        }

        public void AddData<Z>(Z data) where Z : T{
            string key = GetNameOfType<Z>();
            AddData(key, data);
        }
        public Z RemoveData<Z>(){
            string key = GetNameOfType<Z>();
            return (Z)RemoveData(key);
        }   
    }
}