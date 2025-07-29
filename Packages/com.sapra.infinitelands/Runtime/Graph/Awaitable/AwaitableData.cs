namespace sapra.InfiniteLands
{
    public interface AwaitableData<T>{
        public T Result{get;}
        public bool ProcessData();    
    }

}