using System;
using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands{
    public static class HashSetPoolLight<T>{
        internal readonly static List<HashSet<T>> Items = new();
        public static HashSet<T> Get(){
            if(Items.Count > 0){
                int index = Items.Count - 1;
                var item = Items[index];
                Items.RemoveAt(index);
                item.Clear();
                return item;
            }
            else
                return new HashSet<T>();
        }
        public static int GetStoredCount() => Items.Count;
        public static void Release(HashSet<T> data){
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

}