using System.Collections;

namespace CSharp.ConcurrentCollections;

/// <summary>
/// Concurrent hash map implementation using lock striping for high-performance concurrent access.
/// Provides thread-safe dictionary operations with configurable concurrency level.
/// </summary>
/// <typeparam name="TKey">The type of keys in the hash map.</typeparam>
/// <typeparam name="TValue">The type of values in the hash map.</typeparam>
public class ConcurrentHashMap<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>, IDisposable 
    where TKey : notnull
{
    private const int DefaultConcurrencyLevel = 16;
    private const int DefaultCapacity = 31;

    private readonly Bucket[] buckets;
    private readonly ReaderWriterLockSlim[] locks;
    private readonly int lockMask;
    private volatile int count = 0;
    private volatile bool isDisposed = false;

    private class Bucket
    {
        public volatile Node? First;
    }

    private class Node
    {
        public readonly TKey Key;
        public TValue Value;
        public volatile Node? Next;

        public Node(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }

    /// <summary>
    /// Initializes a new ConcurrentHashMap with the specified concurrency level and capacity.
    /// </summary>
    /// <param name="concurrencyLevel">The number of concurrent operations the hash map can support.</param>
    /// <param name="capacity">The initial capacity of the hash map.</param>
    /// <exception cref="ArgumentException">Thrown when concurrencyLevel or capacity is not positive.</exception>
    public ConcurrentHashMap(int concurrencyLevel = DefaultConcurrencyLevel, int capacity = DefaultCapacity)
    {
        if (concurrencyLevel <= 0) throw new ArgumentException("Concurrency level must be positive");
        if (capacity <= 0) throw new ArgumentException("Capacity must be positive");

        // Ensure concurrency level is power of 2 for efficient masking
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

    /// <summary>
    /// Attempts to add a key-value pair to the hash map.
    /// </summary>
    /// <param name="key">The key to add.</param>
    /// <param name="value">The value to add.</param>
    /// <returns>True if the key-value pair was added, false if the key already exists.</returns>
    /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
    public bool TryAdd(TKey key, TValue value)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (isDisposed) return false;

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

    /// <summary>
    /// Attempts to get the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key to search for.</param>
    /// <param name="value">The value associated with the key, if found.</param>
    /// <returns>True if the key was found, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
    public bool TryGetValue(TKey key, out TValue? value)
    {
        ArgumentNullException.ThrowIfNull(key);
        
        if (isDisposed)
        {
            value = default;
            return false;
        }

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

            value = default;
            return false;
        }
        finally
        {
            locks[lockIndex].ExitReadLock();
        }
    }

    /// <summary>
    /// Attempts to update the value for the specified key if it matches the comparison value.
    /// </summary>
    /// <param name="key">The key to update.</param>
    /// <param name="newValue">The new value to set.</param>
    /// <param name="comparisonValue">The expected current value.</param>
    /// <returns>True if the update was successful, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
    public bool TryUpdate(TKey key, TValue newValue, TValue comparisonValue)
    {
        ArgumentNullException.ThrowIfNull(key);
        if (isDisposed) return false;

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

    /// <summary>
    /// Adds a key-value pair or updates the existing value using the specified function.
    /// </summary>
    /// <param name="key">The key to add or update.</param>
    /// <param name="addValue">The value to add if the key doesn't exist.</param>
    /// <param name="updateValueFactory">The function to generate the new value if the key exists.</param>
    /// <returns>The new value for the key.</returns>
    /// <exception cref="ArgumentNullException">Thrown when key or updateValueFactory is null.</exception>
    public TValue AddOrUpdate(TKey key, TValue addValue, Func<TKey, TValue, TValue> updateValueFactory)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(updateValueFactory);
        
        if (isDisposed) return addValue;

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

    /// <summary>
    /// Attempts to remove the specified key from the hash map.
    /// </summary>
    /// <param name="key">The key to remove.</param>
    /// <param name="value">The value that was removed, if successful.</param>
    /// <returns>True if the key was removed, false if it wasn't found.</returns>
    /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
    public bool TryRemove(TKey key, out TValue? value)
    {
        ArgumentNullException.ThrowIfNull(key);
        
        if (isDisposed)
        {
            value = default;
            return false;
        }

        var bucketIndex = GetBucketIndex(key);
        var lockIndex = bucketIndex & lockMask;
        var bucket = buckets[bucketIndex];

        locks[lockIndex].EnterWriteLock();
        try
        {
            var current = bucket.First;
            Node? previous = null;
            
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

            value = default;
            return false;
        }
        finally
        {
            locks[lockIndex].ExitWriteLock();
        }
    }

    /// <summary>
    /// Gets or sets the value associated with the specified key.
    /// </summary>
    /// <param name="key">The key to get or set.</param>
    /// <returns>The value associated with the key.</returns>
    /// <exception cref="KeyNotFoundException">Thrown when getting a key that doesn't exist.</exception>
    /// <exception cref="ArgumentNullException">Thrown when key is null.</exception>
    public TValue this[TKey key]
    {
        get => TryGetValue(key, out var value) ? value! : throw new KeyNotFoundException($"Key '{key}' not found");
        set => AddOrUpdate(key, value, (k, v) => value);
    }

    /// <summary>
    /// Gets the current number of key-value pairs in the hash map.
    /// </summary>
    public int Count => count;

    /// <summary>
    /// Gets a value indicating whether the hash map is empty.
    /// </summary>
    public bool IsEmpty => count == 0;

    /// <summary>
    /// Gets all keys in the hash map.
    /// </summary>
    public ICollection<TKey> Keys
    {
        get
        {
            var keys = new List<TKey>();
            
            foreach (var kvp in this)
            {
                keys.Add(kvp.Key);
            }
            
            return keys;
        }
    }

    /// <summary>
    /// Gets all values in the hash map.
    /// </summary>
    public ICollection<TValue> Values
    {
        get
        {
            var values = new List<TValue>();
            
            foreach (var kvp in this)
            {
                values.Add(kvp.Value);
            }
            
            return values;
        }
    }

    /// <summary>
    /// Removes all key-value pairs from the hash map.
    /// </summary>
    public void Clear()
    {
        for (int i = 0; i < locks.Length; i++)
        {
            locks[i].EnterWriteLock();
        }
        
        try
        {
            for (int i = 0; i < buckets.Length; i++)
            {
                buckets[i].First = null;
            }
            
            count = 0;
        }
        finally
        {
            for (int i = locks.Length - 1; i >= 0; i--)
            {
                locks[i].ExitWriteLock();
            }
        }
    }

    /// <summary>
    /// Returns an enumerator that iterates through the hash map.
    /// Note: The enumerator provides a snapshot and is not thread-safe for modifications.
    /// </summary>
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
    {
        var snapshot = new List<KeyValuePair<TKey, TValue>>();
        
        foreach (var bucket in buckets)
        {
            var current = bucket.First;
            while (current != null)
            {
                snapshot.Add(new KeyValuePair<TKey, TValue>(current.Key, current.Value));
                current = current.Next;
            }
        }
        
        return snapshot.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    /// <summary>
    /// Releases all resources used by the ConcurrentHashMap.
    /// </summary>
    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            
            foreach (var lockSlim in locks)
            {
                lockSlim?.Dispose();
            }
        }
    }

    private int GetBucketIndex(TKey key)
    {
        return Math.Abs(key.GetHashCode()) % buckets.Length;
    }

    private static int GetNextPowerOfTwo(int value)
    {
        if (value <= 1) return 1;
        
        value--;
        value |= value >> 1;
        value |= value >> 2;
        value |= value >> 4;
        value |= value >> 8;
        value |= value >> 16;
        value++;
        
        return value;
    }
}