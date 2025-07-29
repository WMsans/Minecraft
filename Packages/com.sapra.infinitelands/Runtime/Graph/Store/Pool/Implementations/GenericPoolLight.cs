using System;
using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands{
    public static class GenericPoolLight<T> where T : class, new(){
        internal readonly static List<T> Items = new();
        public static T Get(){
            if(Items.Count > 0){
                int index = Items.Count - 1;
                var item = Items[index];
                Items.RemoveAt(index);
                return item;
            }
            else
                return new T();
        }
        public static int GetStoredCount() => Items.Count;
        public static void Release(T data){
/*             #if UNITY_EDITOR
            if(!Items.Contains(data))
            #endif */
                Items.Add(data);
/*             #if UNITY_EDITOR
            else
                Debug.LogWarningFormat("Returning a duplicate!! {0}", data);
            #endif */
        }
    }

    public static class GenericPoolLight{
        public static T Get<T>() where T : class, new(){
            return GenericPoolLight<T>.Get();
        }
        public static void Release<T>(T item) where T : class, new()
        {
            if (item != null)
                GenericPoolLight<T>.Release(item);
            else
                Debug.LogError("Someone returned a null object");
        }
    }

}