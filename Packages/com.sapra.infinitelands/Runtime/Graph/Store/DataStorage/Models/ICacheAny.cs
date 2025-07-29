namespace sapra.InfiniteLands
{
    public interface ICacheAny<T>
    {
        public bool TryGetDataBoxed(T key, out object data);
    }
}