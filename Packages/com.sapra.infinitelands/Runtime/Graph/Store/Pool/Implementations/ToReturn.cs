namespace sapra.InfiniteLands{
    public class ToReturn<T> : ToReturn{
        private IReciveReturnable<T> reciever;
        private T data;
        public T GetData() => data;

        public override void Release()
        {
            reciever.Return(data);
            GenericPoolLight.Release(this);
        }

        public void Reuse(T data, IReciveReturnable<T> reciever){
            this.data = data;
            this.reciever = reciever;
        }
    }

    public abstract class ToReturn : IRelease{
        public abstract void Release();
    }
}