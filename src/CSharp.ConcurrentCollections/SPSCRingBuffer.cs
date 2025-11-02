namespace CSharp.ConcurrentCollections;

/// <summary>
/// Lock-free single-producer/single-consumer ring buffer optimized for high-throughput scenarios.
/// Uses atomic operations and memory barriers for thread safety between exactly one producer and one consumer.
/// </summary>
/// <typeparam name="T">The type of items in the buffer. Must be a value type.</typeparam>
public class SPSCRingBuffer<T> where T : struct
{
    private readonly T[] buffer;
    private readonly int capacity;
    private readonly int mask;
    private long readPosition = 0;
    private long writePosition = 0;

    /// <summary>
    /// Initializes a new SPSCRingBuffer with the specified capacity.
    /// </summary>
    /// <param name="capacity">The capacity of the buffer. Must be a power of 2.</param>
    /// <exception cref="ArgumentException">Thrown when capacity is not a positive power of 2.</exception>
    public SPSCRingBuffer(int capacity)
    {
        if (capacity <= 0 || (capacity & (capacity - 1)) != 0)
        {
            throw new ArgumentException("Capacity must be a positive power of 2", nameof(capacity));
        }

        this.capacity = capacity;
        mask = capacity - 1;
        buffer = new T[capacity];
    }

    /// <summary>
    /// Attempts to write an item to the buffer.
    /// This method should only be called from the producer thread.
    /// </summary>
    /// <param name="item">The item to write.</param>
    /// <returns>True if the item was written successfully, false if the buffer is full.</returns>
    public bool TryWrite(T item)
    {
        var currentWrite = writePosition;
        var nextWrite = currentWrite + 1;

        // Check if buffer is full (write position + 1 == read position in circular buffer)
        if (nextWrite - readPosition > capacity)
        {
            return false; // Buffer is full
        }

        buffer[currentWrite & mask] = item;
        
        // Memory barrier to ensure item is written before position update
        Thread.MemoryBarrier();
        
        writePosition = nextWrite;
        return true;
    }

    /// <summary>
    /// Attempts to read an item from the buffer.
    /// This method should only be called from the consumer thread.
    /// </summary>
    /// <param name="item">The item that was read, if successful.</param>
    /// <returns>True if an item was read successfully, false if the buffer is empty.</returns>
    public bool TryRead(out T item)
    {
        var currentRead = readPosition;

        // Check if buffer is empty
        if (currentRead >= writePosition)
        {
            item = default(T);
            return false;
        }

        item = buffer[currentRead & mask];
        
        // Memory barrier to ensure item is read before position update
        Thread.MemoryBarrier();
        
        readPosition = currentRead + 1;
        return true;
    }

    /// <summary>
    /// Peeks at the next item in the buffer without removing it.
    /// This method should only be called from the consumer thread.
    /// </summary>
    /// <param name="item">The next item in the buffer, if available.</param>
    /// <returns>True if an item is available, false if the buffer is empty.</returns>
    public bool TryPeek(out T item)
    {
        var currentRead = readPosition;

        // Check if buffer is empty
        if (currentRead >= writePosition)
        {
            item = default(T);
            return false;
        }

        item = buffer[currentRead & mask];
        return true;
    }

    /// <summary>
    /// Reads multiple items from the buffer.
    /// This method should only be called from the consumer thread.
    /// </summary>
    /// <param name="items">The array to write items to.</param>
    /// <param name="offset">The offset in the array to start writing.</param>
    /// <param name="count">The maximum number of items to read.</param>
    /// <returns>The number of items actually read.</returns>
    public int TryReadBatch(T[] items, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (offset < 0 || count < 0 || offset + count > items.Length)
            throw new ArgumentException("Invalid offset or count");

        int itemsRead = 0;
        
        while (itemsRead < count && TryRead(out var item))
        {
            items[offset + itemsRead] = item;
            itemsRead++;
        }

        return itemsRead;
    }

    /// <summary>
    /// Writes multiple items to the buffer.
    /// This method should only be called from the producer thread.
    /// </summary>
    /// <param name="items">The array containing items to write.</param>
    /// <param name="offset">The offset in the array to start reading.</param>
    /// <param name="count">The number of items to write.</param>
    /// <returns>The number of items actually written.</returns>
    public int TryWriteBatch(T[] items, int offset, int count)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (offset < 0 || count < 0 || offset + count > items.Length)
            throw new ArgumentException("Invalid offset or count");

        int itemsWritten = 0;
        
        while (itemsWritten < count && TryWrite(items[offset + itemsWritten]))
        {
            itemsWritten++;
        }

        return itemsWritten;
    }

    /// <summary>
    /// Gets the current number of items in the buffer.
    /// Note: This is approximate as it may change during the call.
    /// </summary>
    public int Count => (int)(writePosition - readPosition);

    /// <summary>
    /// Gets the capacity of the buffer.
    /// </summary>
    public int Capacity => capacity;

    /// <summary>
    /// Gets a value indicating whether the buffer is empty.
    /// Note: This is a snapshot and may change immediately after the call.
    /// </summary>
    public bool IsEmpty => readPosition >= writePosition;

    /// <summary>
    /// Gets a value indicating whether the buffer is full.
    /// Note: This is a snapshot and may change immediately after the call.
    /// </summary>
    public bool IsFull => writePosition - readPosition >= capacity;

    /// <summary>
    /// Gets the number of free slots in the buffer.
    /// Note: This is approximate as it may change during the call.
    /// </summary>
    public int AvailableSpace => capacity - Count;

    /// <summary>
    /// Clears all items from the buffer by resetting read and write positions.
    /// This method is not thread-safe and should be used with caution.
    /// </summary>
    public void Clear()
    {
        readPosition = 0;
        writePosition = 0;
        Array.Clear(buffer, 0, buffer.Length);
    }
}