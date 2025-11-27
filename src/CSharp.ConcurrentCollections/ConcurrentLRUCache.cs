using System.Collections.Concurrent;

namespace CSharp.ConcurrentCollections;

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