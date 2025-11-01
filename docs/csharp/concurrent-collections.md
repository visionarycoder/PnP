# Concurrent Collections

**Description**: Comprehensive thread-safe collections and concurrent data structures including lock-free algorithms, atomic operations, high-performance concurrent access patterns, custom concurrent collections, and synchronization-free data structures for scalable multi-threaded applications.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Collections;
using System.Runtime.InteropServices;

// Lock-free stack implementation
public class LockFreeStack<T> : IEnumerable<T> where T : class
{
    private volatile Node head;

    private class Node
    {
        public T Value { get; }
        public Node Next { get; set; }

        public Node(T value)
        {
            Value = value;
        }
    }

    public void Push(T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        var newNode = new Node(item);
        
        while (true)
        {
            var currentHead = head;
            newNode.Next = currentHead;
            
            // Atomic compare-and-swap
            if (Interlocked.CompareExchange(ref head, newNode, currentHead) == currentHead)
            {
                break;
            }
            
            // If CAS failed, retry with new head value
        }
    }

    public bool TryPop(out T result)
    {
        while (true)
        {
            var currentHead = head;
            
            if (currentHead == null)
            {
                result = null;
                return false;
            }

            // Atomic compare-and-swap to remove head
            if (Interlocked.CompareExchange(ref head, currentHead.Next, currentHead) == currentHead)
            {
                result = currentHead.Value;
                return true;
            }
            
            // If CAS failed, retry with new head value
        }
    }

    public bool IsEmpty => head == null;

    public int Count
    {
        get
        {
            int count = 0;
            var current = head;
            
            while (current != null)
            {
                count++;
                current = current.Next;
            }
            
            return count;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        var current = head;
        
        while (current != null)
        {
            yield return current.Value;
            current = current.Next;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

// Lock-free queue implementation using Michael & Scott algorithm
public class LockFreeQueue<T> : IEnumerable<T> where T : class
{
    private volatile Node head;
    private volatile Node tail;

    private class Node
    {
        public volatile T Value;
        public volatile Node Next;

        public Node(T value = null)
        {
            Value = value;
        }
    }

    public LockFreeQueue()
    {
        var sentinel = new Node();
        head = tail = sentinel;
    }

    public void Enqueue(T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        var newNode = new Node(item);

        while (true)
        {
            var currentTail = tail;
            var tailNext = currentTail.Next;

            // Check if tail still points to the last node
            if (currentTail == tail)
            {
                if (tailNext == null)
                {
                    // Attempt to link new node to the end of the list
                    if (Interlocked.CompareExchange(ref currentTail.Next, newNode, null) == null)
                    {
                        // Successfully added new node, now update tail
                        Interlocked.CompareExchange(ref tail, newNode, currentTail);
                        break;
                    }
                }
                else
                {
                    // Tail was lagging, try to advance it
                    Interlocked.CompareExchange(ref tail, tailNext, currentTail);
                }
            }
        }
    }

    public bool TryDequeue(out T result)
    {
        while (true)
        {
            var currentHead = head;
            var currentTail = tail;
            var headNext = currentHead.Next;

            // Check if head still points to the first node
            if (currentHead == head)
            {
                if (currentHead == currentTail)
                {
                    if (headNext == null)
                    {
                        // Queue is empty
                        result = null;
                        return false;
                    }

                    // Tail is lagging, try to advance it
                    Interlocked.CompareExchange(ref tail, headNext, currentTail);
                }
                else
                {
                    if (headNext == null)
                    {
                        // Inconsistent state, retry
                        continue;
                    }

                    // Read value before attempting CAS
                    result = headNext.Value;

                    // Attempt to move head to the next node
                    if (Interlocked.CompareExchange(ref head, headNext, currentHead) == currentHead)
                    {
                        return true;
                    }
                }
            }
        }
    }

    public bool IsEmpty => head.Next == null;

    public int Count
    {
        get
        {
            int count = 0;
            var current = head.Next; // Skip sentinel

            while (current != null)
            {
                count++;
                current = current.Next;
            }

            return count;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        var current = head.Next; // Skip sentinel

        while (current != null)
        {
            yield return current.Value;
            current = current.Next;
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

// Thread-safe bounded buffer with backpressure
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

    public BoundedBuffer(int capacity)
    {
        if (capacity <= 0) throw new ArgumentException("Capacity must be positive", nameof(capacity));
        
        this.capacity = capacity;
        buffer = new T[capacity];
        semaphore = new(capacity, capacity);
    }

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

    public bool TryTake(out T item)
    {
        item = default(T);

        lock (lockObject)
        {
            if (isDisposed || count == 0) return false;

            item = buffer[head];
            buffer[head] = default(T); // Clear reference
            head = (head + 1) % capacity;
            Interlocked.Decrement(ref count);
            
            semaphore.Release(); // Signal that space is available
            return true;
        }
    }

    public IEnumerable<T> TakeAll()
    {
        var items = new();

        lock (lockObject)
        {
            while (count > 0)
            {
                items.Add(buffer[head]);
                buffer[head] = default(T);
                head = (head + 1) % capacity;
                Interlocked.Decrement(ref count);
                semaphore.Release();
            }
        }

        return items;
    }

    public int Count => count;
    public int Capacity => capacity;
    public bool IsEmpty => count == 0;
    public bool IsFull => count == capacity;

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

// Atomic counter with various operations
public class AtomicCounter
{
    private long value = 0;

    public long Value => Interlocked.Read(ref value);

    public long Increment() => Interlocked.Increment(ref value);

    public long Decrement() => Interlocked.Decrement(ref value);

    public long Add(long delta) => Interlocked.Add(ref value, delta);

    public long Exchange(long newValue) => Interlocked.Exchange(ref value, newValue);

    public bool CompareAndSwap(long expected, long newValue)
    {
        return Interlocked.CompareExchange(ref value, newValue, expected) == expected;
    }

    public long GetAndIncrement()
    {
        while (true)
        {
            var current = Interlocked.Read(ref value);
            if (Interlocked.CompareExchange(ref value, current + 1, current) == current)
            {
                return current;
            }
        }
    }

    public long GetAndAdd(long delta)
    {
        while (true)
        {
            var current = Interlocked.Read(ref value);
            if (Interlocked.CompareExchange(ref value, current + delta, current) == current)
            {
                return current;
            }
        }
    }

    public void Reset() => Interlocked.Exchange(ref value, 0);

    public override string ToString() => Value.ToString();

    public static implicit operator long(AtomicCounter counter) => counter.Value;
}

// Thread-safe object pool
public class ConcurrentObjectPool<T> : IDisposable where T : class
{
    private readonly ConcurrentQueue<T> objects = new();
    private readonly Func<T> objectFactory;
    private readonly Action<T> resetAction;
    private readonly int maxSize;
    private volatile int currentSize = 0;
    private volatile bool isDisposed = false;

    public ConcurrentObjectPool(Func<T> factory, Action<T> reset = null, int maxSize = 100)
    {
        objectFactory = factory ?? throw new ArgumentNullException(nameof(factory));
        resetAction = reset;
        this.maxSize = maxSize;
    }

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

    public void Return(T obj)
    {
        if (isDisposed || obj == null) return;

        if (currentSize < maxSize)
        {
            // Reset object state if reset action is provided
            resetAction?.Invoke(obj);
            
            objects.Enqueue(obj);
            Interlocked.Increment(ref currentSize);
        }
        // If pool is full, let the object be garbage collected
    }

    public int Count => currentSize;
    public int MaxSize => maxSize;

    public void Clear()
    {
        while (objects.TryDequeue(out _))
        {
            Interlocked.Decrement(ref currentSize);
        }
    }

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

// Lock-free single-producer/single-consumer ring buffer
public class SPSCRingBuffer<T> where T : struct
{
    private readonly T[] buffer;
    private readonly int capacity;
    private volatile long readPosition = 0;
    private volatile long writePosition = 0;

    public SPSCRingBuffer(int capacity)
    {
        if (capacity <= 0 || (capacity & (capacity - 1)) != 0)
        {
            throw new ArgumentException("Capacity must be a positive power of 2", nameof(capacity));
        }

        this.capacity = capacity;
        buffer = new T[capacity];
    }

    public bool TryWrite(T item)
    {
        var currentWrite = writePosition;
        var nextWrite = currentWrite + 1;

        // Check if buffer is full (write position + 1 == read position in circular buffer)
        if (nextWrite - readPosition > capacity)
        {
            return false; // Buffer is full
        }

        buffer[currentWrite & (capacity - 1)] = item;
        
        // Memory barrier to ensure item is written before position update
        Thread.MemoryBarrier();
        
        writePosition = nextWrite;
        return true;
    }

    public bool TryRead(out T item)
    {
        var currentRead = readPosition;

        // Check if buffer is empty
        if (currentRead >= writePosition)
        {
            item = default(T);
            return false;
        }

        item = buffer[currentRead & (capacity - 1)];
        
        // Memory barrier to ensure item is read before position update
        Thread.MemoryBarrier();
        
        readPosition = currentRead + 1;
        return true;
    }

    public int Count => (int)(writePosition - readPosition);
    public int Capacity => capacity;
    public bool IsEmpty => readPosition >= writePosition;
    public bool IsFull => writePosition - readPosition >= capacity;
}

// Concurrent hash map with lock striping
public class ConcurrentHashMap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IDisposable
{
    private const int DefaultConcurrencyLevel = 16;
    private const int DefaultCapacity = 31;

    private readonly Bucket[] buckets;
    private readonly ReaderWriterLockSlim[] locks;
    private readonly int lockMask;
    private volatile int count = 0;

    private class Bucket
    {
        public volatile Node First;
    }

    private class Node
    {
        public readonly TKey Key;
        public volatile TValue Value;
        public volatile Node Next;

        public Node(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }

    public ConcurrentHashMap(int concurrencyLevel = DefaultConcurrencyLevel, int capacity = DefaultCapacity)
    {
        if (concurrencyLevel <= 0) throw new ArgumentException("Concurrency level must be positive");
        if (capacity <= 0) throw new ArgumentException("Capacity must be positive");

        // Ensure concurrency level is power of 2
        var lockCount = GetNextPowerOfTwo(concurrencyLevel);
        lockMask = lockCount - 1;

        locks = new ReaderWriterLockSlim[lockCount];
        for (int i = 0; i < lockCount; i++)
        {
            locks[i] = new ReaderWriterLockSlim();
        }

        buckets = new Bucket[capacity];
        for (int i = 0; i < capacity; i++)
        {
            buckets[i] = new Bucket();
        }
    }

    public bool TryAdd(TKey key, TValue value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        var bucketIndex = GetBucketIndex(key);
        var lockIndex = bucketIndex & lockMask;
        var bucket = buckets[bucketIndex];

        locks[lockIndex].EnterWriteLock();
        try
        {
            var current = bucket.First;
            
            // Check if key already exists
            while (current != null)
            {
                if (EqualityComparer<TKey>.Default.Equals(current.Key, key))
                {
                    return false; // Key already exists
                }
                current = current.Next;
            }

            // Add new node at the beginning
            var newNode = new Node(key, value) { Next = bucket.First };
            bucket.First = newNode;
            Interlocked.Increment(ref count);
            return true;
        }
        finally
        {
            locks[lockIndex].ExitWriteLock();
        }
    }

    public bool TryGetValue(TKey key, out TValue value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        var bucketIndex = GetBucketIndex(key);
        var lockIndex = bucketIndex & lockMask;
        var bucket = buckets[bucketIndex];

        locks[lockIndex].EnterReadLock();
        try
        {
            var current = bucket.First;
            
            while (current != null)
            {
                if (EqualityComparer<TKey>.Default.Equals(current.Key, key))
                {
                    value = current.Value;
                    return true;
                }
                current = current.Next;
            }

            value = default(TValue);
            return false;
        }
        finally
        {
            locks[lockIndex].ExitReadLock();
        }
    }

    public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        var bucketIndex = GetBucketIndex(key);
        var lockIndex = bucketIndex & lockMask;
        var bucket = buckets[bucketIndex];

        locks[lockIndex].EnterWriteLock();
        try
        {
            var current = bucket.First;
            
            while (current != null)
            {
                if (EqualityComparer<TKey>.Default.Equals(current.Key, key))
                {
                    if (EqualityComparer<TValue>.Default.Equals(current.Value, comparisonValue))
                    {
                        current.Value = newValue;
                        return true;
                    }
                    return false;
                }
                current = current.Next;
            }

            return false; // Key not found
        }
        finally
        {
            locks[lockIndex].ExitWriteLock();
        }
    }

    public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (updateValueFactory == null) throw new ArgumentNullException(nameof(updateValueFactory));

        var bucketIndex = GetBucketIndex(key);
        var lockIndex = bucketIndex & lockMask;
        var bucket = buckets[bucketIndex];

        locks[lockIndex].EnterWriteLock();
        try
        {
            var current = bucket.First;
            
            while (current != null)
            {
                if (EqualityComparer<TKey>.Default.Equals(current.Key, key))
                {
                    current.Value = updateValueFactory(key, current.Value);
                    return current.Value;
                }
                current = current.Next;
            }

            // Key not found, add new
            var newNode = new Node(key, addValue) { Next = bucket.First };
            bucket.First = newNode;
            Interlocked.Increment(ref count);
            return addValue;
        }
        finally
        {
            locks[lockIndex].ExitWriteLock();
        }
    }

    public bool TryRemove(TKey key, out TValue value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        var bucketIndex = GetBucketIndex(key);
        var lockIndex = bucketIndex & lockMask;
        var bucket = buckets[bucketIndex];

        locks[lockIndex].EnterWriteLock();
        try
        {
            var current = bucket.First;
            Node previous = null;
            
            while (current != null)
            {
                if (EqualityComparer<TKey>.Default.Equals(current.Key, key))
                {
                    value = current.Value;
                    
                    if (previous == null)
                    {
                        bucket.First = current.Next;
                    }
                    else
                    {
                        previous.Next = current.Next;
                    }
                    
                    Interlocked.Decrement(ref count);
                    return true;
                }
                
                previous = current;
                current = current.Next;
            }

            value = default(TValue);
            return false;
        }
        finally
        {
            locks[lockIndex].ExitWriteLock();
        }
    }

    public int Count => count;

    public bool IsEmpty => count == 0;

    public IEnumerable<TKey> Keys
    {
        get
        {
            foreach (var kvp in this)
            {
                yield return kvp.Key;
            }
        }
    }

    public IEnumerable<TValue> Values
    {
        get
        {
            foreach (var kvp in this)
            {
                yield return kvp.Value;
            }
        }
    }

    private int GetBucketIndex(TKey key)
    {
        return Math.Abs(key.GetHashCode()) % buckets.Length;
    }

    private static int GetNextPowerOfTwo(int value)
    {
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        return value + 1;
    }

    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        // Acquire all locks for consistent enumeration
        for (int i = 0; i < locks.Length; i++)
        {
            locks[i].EnterReadLock();
        }

        try
        {
            foreach (var bucket in buckets)
            {
                var current = bucket.First;
                
                while (current != null)
                {
                    yield return new KeyValuePair<TKey, TValue>(current.Key, current.Value);
                    current = current.Next;
                }
            }
        }
        finally
        {
            for (int i = locks.Length - 1; i >= 0; i--)
            {
                locks[i].ExitReadLock();
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Dispose()
    {
        if (locks != null)
        {
            foreach (var lockObj in locks)
            {
                lockObj?.Dispose();
            }
        }
    }
}

// Performance monitoring for concurrent collections
public class ConcurrentCollectionMetrics
{
    private readonly AtomicCounter totalOperations = new AtomicCounter();
    private readonly AtomicCounter successfulOperations = new AtomicCounter();
    private readonly AtomicCounter contentions = new AtomicCounter();
    private readonly object lockObject = new();
    private readonly List<TimeSpan> operationTimes = new();

    public void RecordOperation(bool successful, TimeSpan duration, bool contended = false)
    {
        totalOperations.Increment();
        
        if (successful)
        {
            successfulOperations.Increment();
        }
        
        if (contended)
        {
            contentions.Increment();
        }

        lock (lockObject)
        {
            operationTimes.Add(duration);
            
            // Keep only recent measurements
            if (operationTimes.Count > 10000)
            {
                operationTimes.RemoveRange(0, 5000);
            }
        }
    }

    public ConcurrentCollectionStats GetStatistics()
    {
        lock (lockObject)
        {
            return new ConcurrentCollectionStats
            {
                TotalOperations = totalOperations.Value,
                SuccessfulOperations = successfulOperations.Value,
                Contentions = contentions.Value,
                SuccessRate = totalOperations.Value > 0 ? 
                    (double)successfulOperations.Value / totalOperations.Value : 0.0,
                ContentionRate = totalOperations.Value > 0 ?
                    (double)contentions.Value / totalOperations.Value : 0.0,
                AverageOperationTime = operationTimes.Count > 0 ?
                    TimeSpan.FromTicks((long)operationTimes.Average(t => t.Ticks)) : TimeSpan.Zero,
                MinOperationTime = operationTimes.Count > 0 ? 
                    operationTimes.Min() : TimeSpan.Zero,
                MaxOperationTime = operationTimes.Count > 0 ?
                    operationTimes.Max() : TimeSpan.Zero
            };
        }
    }

    public void Reset()
    {
        totalOperations.Reset();
        successfulOperations.Reset();
        contentions.Reset();
        
        lock (lockObject)
        {
            operationTimes.Clear();
        }
    }
}

public class ConcurrentCollectionStats
{
    public long TotalOperations { get; set; }
    public long SuccessfulOperations { get; set; }
    public long Contentions { get; set; }
    public double SuccessRate { get; set; }
    public double ContentionRate { get; set; }
    public TimeSpan AverageOperationTime { get; set; }
    public TimeSpan MinOperationTime { get; set; }
    public TimeSpan MaxOperationTime { get; set; }
}

// Thread-safe priority queue
public class ConcurrentPriorityQueue<T> : IDisposable
{
    private readonly SortedDictionary<int, ConcurrentQueue<T>> queues;
    private readonly ReaderWriterLockSlim lockSlim;
    private volatile int totalCount = 0;
    private volatile bool isDisposed = false;

    public ConcurrentPriorityQueue()
    {
        queues = new SortedDictionary<int, ConcurrentQueue<T>>(Comparer<int>.Create((x, y) => y.CompareTo(x))); // Higher priority first
        lockSlim = new ReaderWriterLockSlim();
    }

    public void Enqueue(T item, int priority)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(ConcurrentPriorityQueue<T>));

        lockSlim.EnterReadLock();
        try
        {
            if (!queues.TryGetValue(priority, out var queue))
            {
                lockSlim.ExitReadLock();
                lockSlim.EnterWriteLock();
                try
                {
                    if (!queues.TryGetValue(priority, out queue))
                    {
                        queue = new();
                        queues[priority] = queue;
                    }
                }
                finally
                {
                    lockSlim.ExitWriteLock();
                    lockSlim.EnterReadLock();
                }
            }

            queue.Enqueue(item);
            Interlocked.Increment(ref totalCount);
        }
        finally
        {
            lockSlim.ExitReadLock();
        }
    }

    public bool TryDequeue(out T item)
    {
        item = default(T);

        if (isDisposed || totalCount == 0) return false;

        lockSlim.EnterReadLock();
        try
        {
            foreach (var kvp in queues)
            {
                if (kvp.Value.TryDequeue(out item))
                {
                    Interlocked.Decrement(ref totalCount);
                    return true;
                }
            }

            return false;
        }
        finally
        {
            lockSlim.ExitReadLock();
        }
    }

    public bool TryPeek(out T item)
    {
        item = default(T);

        if (isDisposed || totalCount == 0) return false;

        lockSlim.EnterReadLock();
        try
        {
            foreach (var kvp in queues)
            {
                if (kvp.Value.TryPeek(out item))
                {
                    return true;
                }
            }

            return false;
        }
        finally
        {
            lockSlim.ExitReadLock();
        }
    }

    public int Count => totalCount;
    public bool IsEmpty => totalCount == 0;

    public void Clear()
    {
        lockSlim.EnterWriteLock();
        try
        {
            foreach (var queue in queues.Values)
            {
                while (queue.TryDequeue(out _))
                {
                    Interlocked.Decrement(ref totalCount);
                }
            }
            queues.Clear();
        }
        finally
        {
            lockSlim.ExitWriteLock();
        }
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            Clear();
            lockSlim?.Dispose();
        }
    }
}
```

**Usage**:

```csharp
// Example 1: Lock-Free Stack Operations
Console.WriteLine("Lock-Free Stack Examples:");

var lockFreeStack = new LockFreeStack<string>();

// Multi-threaded push operations
var pushTasks = Enumerable.Range(1, 100).Select(i =>
    Task.Run(() => lockFreeStack.Push($"Item-{i}"))
).ToArray();

await Task.WhenAll(pushTasks);

Console.WriteLine($"Stack count after concurrent pushes: {lockFreeStack.Count}");

// Multi-threaded pop operations
var popResults = new();
var popTasks = Enumerable.Range(1, 50).Select(_ =>
    Task.Run(() =>
    {
        if (lockFreeStack.TryPop(out string item))
        {
            popResults.Add(item);
        }
    })
).ToArray();

await Task.WhenAll(popTasks);

Console.WriteLine($"Popped {popResults.Count} items");
Console.WriteLine($"Remaining in stack: {lockFreeStack.Count}");

// Example 2: Lock-Free Queue Operations
Console.WriteLine("\nLock-Free Queue Examples:");

var lockFreeQueue = new LockFreeQueue<int>();

// Producer tasks
var producerTasks = Enumerable.Range(1, 5).Select(producerId =>
    Task.Run(async () =>
    {
        for (int i = 0; i < 20; i++)
        {
            var value = producerId * 1000 + i;
            lockFreeQueue.Enqueue(value);
            await Task.Delay(1); // Small delay to simulate work
        }
    })
).ToArray();

// Consumer tasks
var consumedItems = new();
var consumerTasks = Enumerable.Range(1, 3).Select(_ =>
    Task.Run(async () =>
    {
        while (producerTasks.Any(t => !t.IsCompleted) || !lockFreeQueue.IsEmpty)
        {
            if (lockFreeQueue.TryDequeue(out int item))
            {
                consumedItems.Add(item);
            }
            else
            {
                await Task.Delay(1); // Brief wait if queue is empty
            }
        }
    })
).ToArray();

await Task.WhenAll(producerTasks);
await Task.WhenAll(consumerTasks);

Console.WriteLine($"Produced 100 items, consumed {consumedItems.Count} items");
Console.WriteLine($"Queue final count: {lockFreeQueue.Count}");

// Example 3: Bounded Buffer with Backpressure
Console.WriteLine("\nBounded Buffer Examples:");

var boundedBuffer = new BoundedBuffer<string>(10); // Capacity of 10

// Producer that may experience backpressure
var bufferProducer = Task.Run(async () =>
{
    for (int i = 1; i <= 50; i++)
    {
        var item = $"BufferItem-{i}";
        
        // Try to add with timeout
        var added = await boundedBuffer.TryAddAsync(item, TimeSpan.FromMilliseconds(100));
        
        if (added)
        {
            Console.WriteLine($"Added: {item} (Buffer count: {boundedBuffer.Count})");
        }
        else
        {
            Console.WriteLine($"Failed to add {item} - buffer full or timeout");
        }
        
        await Task.Delay(10); // Simulate production work
    }
});

// Consumer that processes items
var bufferConsumer = Task.Run(async () =>
{
    await Task.Delay(100); // Let buffer fill up first
    
    while (!bufferProducer.IsCompleted || !boundedBuffer.IsEmpty)
    {
        if (boundedBuffer.TryTake(out string item))
        {
            Console.WriteLine($"Consumed: {item} (Buffer count: {boundedBuffer.Count})");
            await Task.Delay(20); // Simulate slower consumption
        }
        else
        {
            await Task.Delay(5);
        }
    }
});

await Task.WhenAll(bufferProducer, bufferConsumer);

Console.WriteLine($"Buffer final state - Count: {boundedBuffer.Count}, Is Empty: {boundedBuffer.IsEmpty}");

// Example 4: Atomic Counter Operations
Console.WriteLine("\nAtomic Counter Examples:");

var atomicCounter = new AtomicCounter();
var counterTasks = new();

// Concurrent increment operations
for (int i = 0; i < 10; i++)
{
    counterTasks.Add(Task.Run(() =>
    {
        for (int j = 0; j < 100; j++)
        {
            atomicCounter.Increment();
        }
    }));
}

// Concurrent decrement operations
for (int i = 0; i < 5; i++)
{
    counterTasks.Add(Task.Run(() =>
    {
        for (int j = 0; j < 50; j++)
        {
            atomicCounter.Decrement();
        }
    }));
}

// Concurrent add operations
for (int i = 0; i < 3; i++)
{
    counterTasks.Add(Task.Run(() =>
    {
        for (int j = 0; j < 20; j++)
        {
            atomicCounter.Add(5);
        }
    }));
}

await Task.WhenAll(counterTasks);

Console.WriteLine($"Final counter value: {atomicCounter.Value}");
Console.WriteLine($"Expected value: {(10 * 100) - (5 * 50) + (3 * 20 * 5)}");

// Compare-and-swap operations
var initialValue = atomicCounter.Value;
var swapSuccessful = atomicCounter.CompareAndSwap(initialValue, 1000);
Console.WriteLine($"Compare-and-swap successful: {swapSuccessful}, New value: {atomicCounter.Value}");

// Example 5: Concurrent Object Pool
Console.WriteLine("\nConcurrent Object Pool Examples:");

// Object pool for expensive-to-create objects
var objectPool = new ConcurrentObjectPool<StringBuilder>(
    factory: () => new StringBuilder(1000),
    reset: sb => sb.Clear(),
    maxSize: 20
);

var poolTasks = Enumerable.Range(1, 100).Select(i =>
    Task.Run(() =>
    {
        // Rent an object from the pool
        var sb = objectPool.Rent();
        
        try
        {
            // Use the object
            sb.Append($"Task-{i} processed data: ");
            for (int j = 0; j < 10; j++)
            {
                sb.Append($"{j} ");
            }
            
            // Simulate some work
            Thread.Sleep(1);
            
            return sb.Length;
        }
        finally
        {
            // Always return the object to the pool
            objectPool.Return(sb);
        }
    })
).ToArray();

var poolResults = await Task.WhenAll(poolTasks);

Console.WriteLine($"Pool processed {poolResults.Length} tasks");
Console.WriteLine($"Pool final count: {objectPool.Count}");
Console.WriteLine($"Average string length: {poolResults.Average():F2}");

// Example 6: Single-Producer/Single-Consumer Ring Buffer
Console.WriteLine("\nSPSC Ring Buffer Examples:");

var ringBuffer = new SPSCRingBuffer<int>(1024); // Power of 2 capacity
var ringBufferMetrics = new ConcurrentCollectionMetrics();

// Single producer
var producer = Task.Run(async () =>
{
    for (int i = 1; i <= 10000; i++)
    {
        var stopwatch = Stopwatch.StartNew();
        
        while (!ringBuffer.TryWrite(i))
        {
            await Task.Yield(); // Yield if buffer is full
        }
        
        stopwatch.Stop();
        ringBufferMetrics.RecordOperation(true, stopwatch.Elapsed);
        
        if (i % 1000 == 0)
        {
            Console.WriteLine($"Produced {i} items, buffer count: {ringBuffer.Count}");
        }
    }
});

// Single consumer
var consumedCount = 0;
var consumer = Task.Run(async () =>
{
    while (consumedCount < 10000)
    {
        var stopwatch = Stopwatch.StartNew();
        
        if (ringBuffer.TryRead(out int item))
        {
            consumedCount++;
            stopwatch.Stop();
            ringBufferMetrics.RecordOperation(true, stopwatch.Elapsed);
            
            // Simulate processing time
            if (consumedCount % 100 == 0)
            {
                await Task.Delay(1);
            }
        }
        else
        {
            stopwatch.Stop();
            ringBufferMetrics.RecordOperation(false, stopwatch.Elapsed);
            await Task.Yield();
        }
    }
});

await Task.WhenAll(producer, consumer);

var ringStats = ringBufferMetrics.GetStatistics();
Console.WriteLine($"Ring buffer stats - Success rate: {ringStats.SuccessRate:P2}");
Console.WriteLine($"Average operation time: {ringStats.AverageOperationTime.TotalMicroseconds:F2} μs");

// Example 7: Concurrent Hash Map Operations
Console.WriteLine("\nConcurrent Hash Map Examples:");

var concurrentMap = new ConcurrentHashMap<string, int>();

// Concurrent additions
var mapAddTasks = Enumerable.Range(1, 1000).Select(i =>
    Task.Run(() =>
    {
        var key = $"Key-{i % 100}"; // Some key collisions
        var value = i;
        
        if (concurrentMap.TryAdd(key, value))
        {
            return 1; // Successfully added
        }
        else
        {
            // Key already exists, try update
            concurrentMap.AddOrUpdate(key, value, (k, v) => v + value);
            return 0; // Updated existing
        }
    })
).ToArray();

var addResults = await Task.WhenAll(mapAddTasks);
var newAdditions = addResults.Sum();

Console.WriteLine($"Map operations completed - New additions: {newAdditions}");
Console.WriteLine($"Map final count: {concurrentMap.Count}");

// Concurrent reads and updates
var mapReadTasks = Enumerable.Range(1, 500).Select(i =>
    Task.Run(() =>
    {
        var key = $"Key-{i % 100}";
        
        if (concurrentMap.TryGetValue(key, out int value))
        {
            // Try to double the value
            concurrentMap.TryUpdate(key, value * 2, value);
            return value;
        }
        
        return 0;
    })
).ToArray();

var readResults = await Task.WhenAll(mapReadTasks);
Console.WriteLine($"Read operations: {readResults.Count(r => r > 0)} successful");

// Display some values
Console.WriteLine("Sample values:");
foreach (var kvp in concurrentMap.Take(10))
{
    Console.WriteLine($"  {kvp.Key}: {kvp.Value}");
}

// Example 8: Concurrent Priority Queue
Console.WriteLine("\nConcurrent Priority Queue Examples:");

var priorityQueue = new ConcurrentPriorityQueue<string>();

// Add items with different priorities
var priorityTasks = Enumerable.Range(1, 100).Select(i =>
    Task.Run(() =>
    {
        var priority = i % 5; // Priorities 0-4
        var item = $"Priority-{priority}-Item-{i}";
        priorityQueue.Enqueue(item, priority);
    })
).ToArray();

await Task.WhenAll(priorityTasks);

Console.WriteLine($"Priority queue count: {priorityQueue.Count}");

// Dequeue items (should come out in priority order)
Console.WriteLine("Dequeuing items by priority:");
var dequeuedCount = 0;
while (priorityQueue.TryDequeue(out string item) && dequeuedCount < 20)
{
    Console.WriteLine($"  Dequeued: {item}");
    dequeuedCount++;
}

Console.WriteLine($"Remaining in priority queue: {priorityQueue.Count}");

// Example 9: Performance Comparison
Console.WriteLine("\nPerformance Comparison Examples:");

const int iterations = 100000;

// Test ConcurrentQueue vs LockFreeQueue
var concurrentQueue = new();
var lockFreeQueueTest = new LockFreeQueue<int>();

// ConcurrentQueue performance
var sw1 = Stopwatch.StartNew();
Parallel.For(0, iterations, i => concurrentQueue.Enqueue(i));
sw1.Stop();

var sw2 = Stopwatch.StartNew();
Parallel.For(0, iterations, i => concurrentQueue.TryDequeue(out _));
sw2.Stop();

Console.WriteLine($"ConcurrentQueue - Enqueue: {sw1.ElapsedMilliseconds}ms, Dequeue: {sw2.ElapsedMilliseconds}ms");

// LockFreeQueue performance
var sw3 = Stopwatch.StartNew();
Parallel.For(0, iterations, i => lockFreeQueueTest.Enqueue(i));
sw3.Stop();

var sw4 = Stopwatch.StartNew();
Parallel.For(0, iterations, i => lockFreeQueueTest.TryDequeue(out _));
sw4.Stop();

Console.WriteLine($"LockFreeQueue - Enqueue: {sw3.ElapsedMilliseconds}ms, Dequeue: {sw4.ElapsedMilliseconds}ms");

// Example 10: Memory and Cleanup
Console.WriteLine("\nMemory and Cleanup Examples:");

// Demonstrate proper disposal
using (var disposableBuffer = new BoundedBuffer<int>(100))
{
    for (int i = 0; i < 50; i++)
    {
        disposableBuffer.TryAdd(i);
    }
    
    Console.WriteLine($"Buffer before disposal: Count = {disposableBuffer.Count}");
} // Automatically disposed here

Console.WriteLine("Buffer disposed successfully");

// Clean up other resources
boundedBuffer.Dispose();
objectPool.Dispose();
concurrentMap.Dispose();
priorityQueue.Dispose();

Console.WriteLine("\nConcurrent collections examples completed!");
```

**Notes**:

- Implement comprehensive lock-free data structures using compare-and-swap (CAS) operations for maximum performance
- Support various concurrent collection types: stacks, queues, hash maps, priority queues, and bounded buffers  
- Use atomic operations and memory barriers to ensure thread safety without traditional locking
- Implement backpressure handling in bounded collections to prevent memory exhaustion
- Provide performance monitoring and metrics collection for operational visibility
- Support single-producer/single-consumer optimizations for high-throughput scenarios
- Use lock striping in concurrent hash maps to minimize contention while maintaining consistency
- Implement proper disposal patterns and resource cleanup for long-running applications
- Support both blocking and non-blocking operations depending on use case requirements
- Use memory-efficient ring buffers for high-frequency producer-consumer scenarios
- Implement object pooling to reduce garbage collection pressure in high-allocation scenarios
- Provide comprehensive performance comparisons between different concurrent collection implementations
- **After performance tests, ensure to dispose or clear any wrapper objects (such as custom queues or buffers) to avoid memory leaks in real-world scenarios.**

**Prerequisites**:

- Understanding of concurrent programming concepts and thread safety requirements
- Knowledge of atomic operations, memory models, and compare-and-swap algorithms
- Familiarity with lock-free programming techniques and ABA problem mitigation
- Experience with performance optimization and memory management in multi-threaded environments
- Understanding of producer-consumer patterns and backpressure handling strategies

**Related Snippets**:

- [Actor Model](actor-model.md) - Actor-based concurrency patterns with message-passing isolation
- [Producer-Consumer](producer-consumer.md) - Producer-consumer patterns with channels and bounded queues  
- [Reader-Writer Locks](reader-writer-locks.md) - Advanced locking patterns and synchronization primitives
