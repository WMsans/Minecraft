using System;
using System.Collections.Generic;

public class Pool<T>
{
    private readonly Func<T> createFunc;
    private readonly Action<T> getAction;
    private readonly Action<T> returnAction;
    private readonly Queue<T> pool = new Queue<T>();

    public Pool(Func<T> createFunc, Action<T> getAction, Action<T> returnAction)
    {
        this.createFunc = createFunc;
        this.getAction = getAction;
        this.returnAction = returnAction;
    }

    public T Get()
    {
        T item = pool.Count > 0 ? pool.Dequeue() : createFunc();
        getAction(item);
        return item;
    }

    public void Return(T item)
    {
        returnAction(item);
        pool.Enqueue(item);
    }
}