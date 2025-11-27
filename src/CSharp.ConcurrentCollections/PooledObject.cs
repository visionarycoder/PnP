namespace CSharp.ConcurrentCollections;

/// <summary>
/// A wrapper that automatically returns an object to the pool when disposed.
/// </summary>
/// <typeparam name="T">The type of the pooled object.</typeparam>
public class PooledObject<T> : IDisposable where T : class
{
    private readonly ConcurrentObjectPool<T> pool;
    private T? obj;

    internal PooledObject(ConcurrentObjectPool<T> pool, T obj)
    {
        this.pool = pool;
        this.obj = obj;
    }

    /// <summary>
    /// Gets the wrapped object.
    /// </summary>
    /// <exception cref="ObjectDisposedException">Thrown when accessed after disposal.</exception>
    public T Value => obj ?? throw new ObjectDisposedException(nameof(PooledObject<T>));

    /// <summary>
    /// Returns the object to the pool.
    /// </summary>
    public void Dispose()
    {
        if (obj != null)
        {
            pool.Return(obj);
            obj = null;
        }
    }
}