using System;
using System.Collections.Generic;

namespace sapra.InfiniteLands{
    public class TypeStore<T> : DataStore<Type, T>{
        public Z GetData<Z>(bool required = true) where Z : T{
            return (Z)GetData(typeof(Z), required);
        }

        public void AddData<Z>(Z data) where Z : T{
            AddData(typeof(Z), data);
        }
        public Z RemoveData<Z>(){
            return (Z)RemoveData(typeof(Z));
        }   
    }
}