namespace CSharp.ConcurrentCollections;

/// <summary>
/// Thread-safe bounded buffer with backpressure control and async operations.
/// Provides producer-consumer pattern with automatic blocking when buffer is full.
/// </summary>
/// <typeparam name="T">The type of items stored in the buffer.</typeparam>
public class BoundedBuffer<T> : IDisposable
{
    private readonly T[] buffer;
    private readonly int capacity;
    private volatile int head = 0;
    private volatile int tail = 0;
    private volatile int count = 0;
    private readonly object lockObject = new();
    private readonly SemaphoreSlim semaphore;
    private volatile bool isDisposed = false;

    /// <summary>
    /// Initializes a new instance of the BoundedBuffer with the specified capacity.
    /// </summary>
    /// <param name="capacity">The maximum number of items the buffer can hold.</param>
    /// <exception cref="ArgumentException">Thrown when capacity is not positive.</exception>
    public BoundedBuffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentException("Capacity must be positive", nameof(capacity));
        
        this.capacity = capacity;
        buffer = new T[capacity];
        semaphore = new SemaphoreSlim(capacity, capacity);
    }

    /// <summary>
    /// Asynchronously attempts to add an item to the buffer with timeout.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <param name="timeout">The maximum time to wait for space to become available.</param>
    /// <param name="token">Cancellation token to observe.</param>
    /// <returns>True if the item was added successfully, false if timeout occurred.</returns>
    public async Task<bool> TryAddAsync(T item, TimeSpan timeout, CancellationToken token = default)
    {
        if (isDisposed) return false;

        if (!await semaphore.WaitAsync(timeout, token).ConfigureAwait(false))
        {
            return false; // Timeout occurred
        }

        try
        {
            lock (lockObject)
            {
                if (isDisposed) return false;

                buffer[tail] = item;
                tail = (tail + 1) % capacity;
                Interlocked.Increment(ref count);
                return true;
            }
        }
        catch
        {
            semaphore.Release(); // Release semaphore on error
            throw;
        }
    }

    /// <summary>
    /// Attempts to add an item to the buffer without blocking.
    /// </summary>
    /// <param name="item">The item to add.</param>
    /// <returns>True if the item was added successfully, false if buffer is full.</returns>
    public bool TryAdd(T item)
    {
        if (isDisposed) return false;

        if (!semaphore.Wait(0)) // Non-blocking wait
        {
            return false;
        }

        try
        {
            lock (lockObject)
            {
                if (isDisposed) return false;

                buffer[tail] = item;
                tail = (tail + 1) % capacity;
                Interlocked.Increment(ref count);
                return true;
            }
        }
        catch
        {
            semaphore.Release();
            throw;
        }
    }

    /// <summary>
    /// Attempts to remove and return an item from the buffer.
    /// </summary>
    /// <param name="item">The removed item, if successful.</param>
    /// <returns>True if an item was removed, false if buffer is empty.</returns>
    public bool TryTake(out T? item)
    {
        item = default(T);

        lock (lockObject)
        {
            if (isDisposed || count == 0) return false;

            item = buffer[head];
            buffer[head] = default(T)!; // Clear reference
            head = (head + 1) % capacity;
            Interlocked.Decrement(ref count);
            
            semaphore.Release(); // Signal that space is available
            return true;
        }
    }

    /// <summary>
    /// Removes and returns all items currently in the buffer.
    /// </summary>
    /// <returns>A collection of all items that were in the buffer.</returns>
    public IEnumerable<T> TakeAll()
    {
        var items = new List<T>();

        lock (lockObject)
        {
            while (count > 0)
            {
                items.Add(buffer[head]);
                buffer[head] = default(T)!;
                head = (head + 1) % capacity;
                Interlocked.Decrement(ref count);
                semaphore.Release();
            }
        }

        return items;
    }

    /// <summary>
    /// Gets the current number of items in the buffer.
    /// </summary>
    public int Count => count;

    /// <summary>
    /// Gets the maximum capacity of the buffer.
    /// </summary>
    public int Capacity => capacity;

    /// <summary>
    /// Gets a value indicating whether the buffer is empty.
    /// </summary>
    public bool IsEmpty => count == 0;

    /// <summary>
    /// Gets a value indicating whether the buffer is full.
    /// </summary>
    public bool IsFull => count == capacity;

    /// <summary>
    /// Releases all resources used by the BoundedBuffer.
    /// </summary>
    public void Dispose()
    {
        if (!isDisposed)
        {
            lock (lockObject)
            {
                isDisposed = true;
                Array.Clear(buffer, 0, buffer.Length);
            }
            
            semaphore?.Dispose();
        }
    }
}