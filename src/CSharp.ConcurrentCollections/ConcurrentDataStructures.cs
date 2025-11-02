using System.Collections;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace CSharp.ConcurrentCollections;

/// <summary>
/// Lock-free stack implementation using atomic compare-and-swap operations.
/// Provides thread-safe stack operations without blocking.
/// </summary>
/// <typeparam name="T">The type of elements in the stack.</typeparam>
public class LockFreeStack<T> : IEnumerable<T>
{
    private volatile Node? head;

    private class Node
    {
        public T Value { get; }
        public Node? Next { get; set; }

        public Node(T value)
        {
            Value = value;
        }
    }

    /// <summary>
    /// Pushes an item onto the stack in a thread-safe manner.
    /// </summary>
    /// <param name="item">The item to push.</param>
    public void Push(T item)
    {
        ArgumentNullException.ThrowIfNull(item);

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

    /// <summary>
    /// Attempts to pop an item from the stack in a thread-safe manner.
    /// </summary>
    /// <param name="result">The popped item, if any.</param>
    /// <returns>True if an item was popped, false if the stack was empty.</returns>
    public bool TryPop(out T? result)
    {
        while (true)
        {
            var currentHead = head;
            
            if (currentHead == null)
            {
                result = default(T);
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

    /// <summary>
    /// Gets a value indicating whether the stack is empty.
    /// </summary>
    public bool IsEmpty => head == null;

    /// <summary>
    /// Gets the approximate count of items in the stack.
    /// Note: This is a snapshot and may change during enumeration.
    /// </summary>
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

/// <summary>
/// Thread-safe priority queue implementation using concurrent collections.
/// </summary>
/// <typeparam name="T">The type of elements in the priority queue.</typeparam>
public class ConcurrentPriorityQueue<T> where T : IComparable<T>
{
    private readonly object lockObject = new();
    private readonly List<T> heap = new();

    /// <summary>
    /// Adds an item to the priority queue.
    /// </summary>
    /// <param name="item">The item to add.</param>
    public void Enqueue(T item)
    {
        if (item == null) throw new ArgumentNullException(nameof(item));

        lock (lockObject)
        {
            heap.Add(item);
            HeapifyUp(heap.Count - 1);
        }
    }

    /// <summary>
    /// Attempts to remove and return the highest priority item.
    /// </summary>
    /// <param name="result">The dequeued item, if any.</param>
    /// <returns>True if an item was dequeued, false if the queue was empty.</returns>
    public bool TryDequeue(out T? result)
    {
        lock (lockObject)
        {
            if (heap.Count == 0)
            {
                result = default;
                return false;
            }

            result = heap[0];
            
            // Move last element to root and heapify down
            heap[0] = heap[heap.Count - 1];
            heap.RemoveAt(heap.Count - 1);
            
            if (heap.Count > 0)
            {
                HeapifyDown(0);
            }
            
            return true;
        }
    }

    /// <summary>
    /// Attempts to peek at the highest priority item without removing it.
    /// </summary>
    /// <param name="result">The highest priority item, if any.</param>
    /// <returns>True if an item was found, false if the queue was empty.</returns>
    public bool TryPeek(out T? result)
    {
        lock (lockObject)
        {
            if (heap.Count == 0)
            {
                result = default;
                return false;
            }

            result = heap[0];
            return true;
        }
    }

    /// <summary>
    /// Gets the current count of items in the priority queue.
    /// </summary>
    public int Count
    {
        get
        {
            lock (lockObject)
            {
                return heap.Count;
            }
        }
    }

    /// <summary>
    /// Gets a value indicating whether the priority queue is empty.
    /// </summary>
    public bool IsEmpty => Count == 0;

    private void HeapifyUp(int index)
    {
        while (index > 0)
        {
            int parentIndex = (index - 1) / 2;
            
            if (heap[index].CompareTo(heap[parentIndex]) <= 0)
                break;
            
            (heap[index], heap[parentIndex]) = (heap[parentIndex], heap[index]);
            index = parentIndex;
        }
    }

    private void HeapifyDown(int index)
    {
        while (true)
        {
            int largest = index;
            int leftChild = 2 * index + 1;
            int rightChild = 2 * index + 2;

            if (leftChild < heap.Count && heap[leftChild].CompareTo(heap[largest]) > 0)
                largest = leftChild;

            if (rightChild < heap.Count && heap[rightChild].CompareTo(heap[largest]) > 0)
                largest = rightChild;

            if (largest == index)
                break;

            (heap[index], heap[largest]) = (heap[largest], heap[index]);
            index = largest;
        }
    }
}

/// <summary>
/// Thread-safe LRU (Least Recently Used) cache implementation.
/// </summary>
/// <typeparam name="TKey">The type of cache keys.</typeparam>
/// <typeparam name="TValue">The type of cache values.</typeparam>
public class ConcurrentLRUCache<TKey, TValue> where TKey : notnull
{
    private readonly int capacity;
    private readonly ConcurrentDictionary<TKey, LinkedListNode<CacheItem>> cache;
    private readonly LinkedList<CacheItem> accessOrder;
    private readonly ReaderWriterLockSlim rwLock;

    private class CacheItem
    {
        public TKey Key { get; }
        public TValue Value { get; }
        public DateTime LastAccessed { get; set; }

        public CacheItem(TKey key, TValue value)
        {
            Key = key;
            Value = value;
            LastAccessed = DateTime.UtcNow;
        }
    }

    public ConcurrentLRUCache(int capacity)
    {
        if (capacity <= 0) throw new ArgumentOutOfRangeException(nameof(capacity));

        this.capacity = capacity;
        cache = new ConcurrentDictionary<TKey, LinkedListNode<CacheItem>>();
        accessOrder = new LinkedList<CacheItem>();
        rwLock = new ReaderWriterLockSlim();
    }

    /// <summary>
    /// Gets or sets a value in the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <returns>The cached value, or default if not found.</returns>
    public TValue? this[TKey key]
    {
        get => TryGetValue(key, out var value) ? value : default;
        set => AddOrUpdate(key, value!);
    }

    /// <summary>
    /// Attempts to get a value from the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The cached value, if found.</param>
    /// <returns>True if the value was found, false otherwise.</returns>
    public bool TryGetValue(TKey key, out TValue? value)
    {
        if (cache.TryGetValue(key, out var node))
        {
            rwLock.EnterWriteLock();
            try
            {
                // Move to front (most recently used)
                accessOrder.Remove(node);
                accessOrder.AddFirst(node);
                node.Value.LastAccessed = DateTime.UtcNow;
                
                value = node.Value.Value;
                return true;
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        value = default;
        return false;
    }

    /// <summary>
    /// Adds or updates a value in the cache.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    public void AddOrUpdate(TKey key, TValue value)
    {
        rwLock.EnterWriteLock();
        try
        {
            if (cache.TryGetValue(key, out var existingNode))
            {
                // Update existing item
                accessOrder.Remove(existingNode);
                var newItem = new CacheItem(key, value);
                var newNode = accessOrder.AddFirst(newItem);
                cache[key] = newNode;
            }
            else
            {
                // Add new item
                var newItem = new CacheItem(key, value);
                var newNode = accessOrder.AddFirst(newItem);
                cache[key] = newNode;

                // Evict oldest if over capacity
                if (cache.Count > capacity)
                {
                    var lastNode = accessOrder.Last!;
                    accessOrder.RemoveLast();
                    cache.TryRemove(lastNode.Value.Key, out _);
                }
            }
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }

    /// <summary>
    /// Removes a value from the cache.
    /// </summary>
    /// <param name="key">The cache key to remove.</param>
    /// <returns>True if the key was found and removed, false otherwise.</returns>
    public bool TryRemove(TKey key)
    {
        if (cache.TryRemove(key, out var node))
        {
            rwLock.EnterWriteLock();
            try
            {
                accessOrder.Remove(node);
                return true;
            }
            finally
            {
                rwLock.ExitWriteLock();
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the current count of items in the cache.
    /// </summary>
    public int Count => cache.Count;

    /// <summary>
    /// Gets the maximum capacity of the cache.
    /// </summary>
    public int Capacity => capacity;

    /// <summary>
    /// Clears all items from the cache.
    /// </summary>
    public void Clear()
    {
        rwLock.EnterWriteLock();
        try
        {
            cache.Clear();
            accessOrder.Clear();
        }
        finally
        {
            rwLock.ExitWriteLock();
        }
    }

    public void Dispose()
    {
        rwLock?.Dispose();
    }
}