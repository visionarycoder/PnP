using System.Collections.Concurrent;

namespace CSharp.ConcurrentCollections;

/// <summary>
/// Thread-safe object pool that reuses objects to reduce garbage collection pressure.
/// Provides configurable factory function, reset action, and maximum pool size.
/// </summary>
/// <typeparam name="T">The type of objects to pool. Must be a reference type.</typeparam>
public class ConcurrentObjectPool<T> : IDisposable where T : class
{
    private readonly ConcurrentQueue<T> objects = new();
    private readonly Func<T> objectFactory;
    private readonly Action<T>? resetAction;
    private readonly int maxSize;
    private volatile int currentSize = 0;
    private volatile bool isDisposed = false;

    /// <summary>
    /// Initializes a new ConcurrentObjectPool with the specified parameters.
    /// </summary>
    /// <param name="factory">Function to create new instances when the pool is empty.</param>
    /// <param name="reset">Optional action to reset object state before returning to pool.</param>
    /// <param name="maxSize">Maximum number of objects to keep in the pool.</param>
    /// <exception cref="ArgumentNullException">Thrown when factory is null.</exception>
    public ConcurrentObjectPool(Func<T> factory, Action<T>? reset = null, int maxSize = 100)
    {
        objectFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        resetAction = reset;
        this.maxSize = maxSize;
    }

    /// <summary>
    /// Gets an object from the pool or creates a new one if the pool is empty.
    /// </summary>
    /// <returns>An object of type T.</returns>
    /// <exception cref="ObjectDisposedException">Thrown when the pool has been disposed.</exception>
    public T Rent()
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(ConcurrentObjectPool<T>));

        if (objects.TryDequeue(out var obj))
        {
            Interlocked.Decrement(ref currentSize);
            return obj;
        }

        // No available object, create new one
        return objectFactory();
    }

    /// <summary>
    /// Returns an object to the pool for reuse.
    /// </summary>
    /// <param name="obj">The object to return to the pool.</param>
    public void Return(T? obj)
    {
        if (isDisposed || obj == null) return;

        if (currentSize < maxSize)
        {
            try
            {
                // Reset object state if reset action is provided
                resetAction?.Invoke(obj);
                
                objects.Enqueue(obj);
                Interlocked.Increment(ref currentSize);
            }
            catch
            {
                // If reset action throws, don't add object back to pool
                // This prevents corrupted objects from being reused
            }
        }
        // If pool is full, let the object be garbage collected
    }

    /// <summary>
    /// Gets a temporary object from the pool and automatically returns it when disposed.
    /// </summary>
    /// <returns>A disposable wrapper around the pooled object.</returns>
    public PooledObject<T> RentDisposable()
    {
        return new PooledObject<T>(this, Rent());
    }

    /// <summary>
    /// Gets the current number of objects in the pool.
    /// </summary>
    public int Count => currentSize;

    /// <summary>
    /// Gets the maximum size of the pool.
    /// </summary>
    public int MaxSize => maxSize;

    /// <summary>
    /// Removes all objects from the pool.
    /// </summary>
    public void Clear()
    {
        while (objects.TryDequeue(out _))
        {
            Interlocked.Decrement(ref currentSize);
        }
    }

    /// <summary>
    /// Releases all resources used by the object pool.
    /// </summary>
    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            Clear();

            // If objects implement IDisposable, dispose them
            while (objects.TryDequeue(out var obj))
            {
                if (obj is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }
        }
    }
}