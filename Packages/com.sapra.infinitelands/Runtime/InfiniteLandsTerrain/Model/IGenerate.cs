using System;

namespace sapra.InfiniteLands{
    public interface IGenerate<T>
    {   
        public Action<T> onProcessDone{get;set;}
        public Action<T> onProcessRemoved{get; set;}
    }
}