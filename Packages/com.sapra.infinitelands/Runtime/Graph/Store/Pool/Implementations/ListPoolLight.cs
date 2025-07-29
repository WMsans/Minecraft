using System;
using System.Collections.Generic;
using UnityEngine;

namespace sapra.InfiniteLands{
    public static class ListPoolLight<T>{
        internal readonly static List<List<T>> Items = new();
        public static List<T> Get(){
            if(Items.Count > 0){
                int index = Items.Count - 1;
                var item = Items[index];
                Items.RemoveAt(index);
                item.Clear();
                return item;
            }
            else
                return new List<T>();
        }
        public static int GetStoredCount() => Items.Count;
        public static void Release(List<T> data){
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