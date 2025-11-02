using System.Collections.Concurrent;
using System.Diagnostics;

namespace CSharp.Memoization;

/// <summary>
/// Options for configuring memoization behavior.
/// </summary>
public class MemoizationOptions
{
    public int InitialCapacity { get; set; } = 16;
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;
    public int MaxCacheSize { get; set; } = 1000;
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(60);
    public bool EnableAutoCleanup { get; set; } = true;
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);
}

/// <summary>
/// Statistics about memoization cache performance.
/// </summary>
public class MemoizationStatistics
{
    public long HitCount { get; set; }
    public long MissCount { get; set; }
    public int CacheSize { get; set; }
    public double HitRatio => (HitCount + MissCount) > 0 ? (double)HitCount / (HitCount + MissCount) : 0.0;
    public DateTime LastCleanup { get; set; }
}

/// <summary>
/// Core memoization interface for caching function results.
/// </summary>
/// <typeparam name="TKey">The type of cache keys.</typeparam>
/// <typeparam name="TResult">The type of cached results.</typeparam>
public interface IMemoizer<TKey, TResult>
{
    TResult GetOrCompute(TKey key, Func<TKey, TResult> factory);
    Task<TResult> GetOrComputeAsync(TKey key, Func<TKey, Task<TResult>> factory);
    bool TryGetValue(TKey key, out TResult result);
    void Invalidate(TKey key);
    void InvalidateAll();
    void InvalidateWhere(Func<TKey, bool> predicate);
    MemoizationStatistics GetStatistics();
}

/// <summary>
/// Cache entry with expiration and access tracking.
/// </summary>
/// <typeparam name="TResult">The type of the cached result.</typeparam>
public class CacheEntry<TResult>
{
    public TResult Value { get; }
    public DateTime CreatedAt { get; }
    public DateTime LastAccessed { get; private set; }
    public TimeSpan? Expiration { get; }

    public CacheEntry(TResult value, DateTime createdAt, TimeSpan? expiration = null)
    {
        Value = value;
        CreatedAt = createdAt;
        LastAccessed = createdAt;
        Expiration = expiration;
    }

    public bool IsExpired(DateTime now)
    {
        return Expiration.HasValue && now - CreatedAt > Expiration.Value;
    }

    public void UpdateLastAccessed(DateTime accessTime)
    {
        LastAccessed = accessTime;
    }
}

/// <summary>
/// Thread-safe memoization implementation with expiration and cleanup.
/// </summary>
/// <typeparam name="TKey">The type of cache keys.</typeparam>
/// <typeparam name="TResult">The type of cached results.</typeparam>
public class Memoizer<TKey, TResult> : IMemoizer<TKey, TResult>, IDisposable where TKey : notnull
{
    private readonly ConcurrentDictionary<TKey, CacheEntry<TResult>> cache;
    private readonly MemoizationOptions options;
    private readonly Timer? cleanupTimer;
    private long hitCount = 0;
    private long missCount = 0;
    private DateTime lastCleanup = DateTime.UtcNow;
    private bool disposed = false;

    public Memoizer(MemoizationOptions? options = null)
    {
        this.options = options ?? new MemoizationOptions();
        
        cache = new ConcurrentDictionary<TKey, CacheEntry<TResult>>();
        
        if (this.options.EnableAutoCleanup && this.options.CleanupInterval > TimeSpan.Zero)
        {
            cleanupTimer = new Timer(PerformCleanup, null, 
                this.options.CleanupInterval, this.options.CleanupInterval);
        }
    }

    public TResult GetOrCompute(TKey key, Func<TKey, TResult> factory)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        var now = DateTime.UtcNow;
        
        if (cache.TryGetValue(key, out var cachedEntry))
        {
            if (!cachedEntry.IsExpired(now))
            {
                cachedEntry.UpdateLastAccessed(now);
                Interlocked.Increment(ref hitCount);
                return cachedEntry.Value;
            }
            
            // Entry is expired, remove it
            cache.TryRemove(key, out _);
        }

        Interlocked.Increment(ref missCount);

        // Check cache size limit before adding new entry
        if (options.MaxCacheSize > 0 && cache.Count >= options.MaxCacheSize)
        {
            EvictOldestEntries();
        }

        var newEntry = new CacheEntry<TResult>(
            factory(key), 
            now, 
            options.DefaultExpiration);
        
        cache.TryAdd(key, newEntry);
        return newEntry.Value;
    }

    public async Task<TResult> GetOrComputeAsync(TKey key, Func<TKey, Task<TResult>> factory)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (factory == null) throw new ArgumentNullException(nameof(factory));

        var now = DateTime.UtcNow;
        
        if (cache.TryGetValue(key, out var cachedEntry))
        {
            if (!cachedEntry.IsExpired(now))
            {
                cachedEntry.UpdateLastAccessed(now);
                Interlocked.Increment(ref hitCount);
                return cachedEntry.Value;
            }
            
            cache.TryRemove(key, out _);
        }

        Interlocked.Increment(ref missCount);

        if (options.MaxCacheSize > 0 && cache.Count >= options.MaxCacheSize)
        {
            EvictOldestEntries();
        }

        var result = await factory(key);
        var newEntry = new CacheEntry<TResult>(
            result, 
            now, 
            options.DefaultExpiration);
        
        cache.TryAdd(key, newEntry);
        return result;
    }

    public bool TryGetValue(TKey key, out TResult result)
    {
        result = default(TResult)!;
        
        if (!cache.TryGetValue(key, out var cachedEntry))
            return false;

        if (cachedEntry.IsExpired(DateTime.UtcNow))
        {
            cache.TryRemove(key, out _);
            return false;
        }

        result = cachedEntry.Value;
        return true;
    }

    public void Invalidate(TKey key)
    {
        cache.TryRemove(key, out _);
    }

    public void InvalidateAll()
    {
        cache.Clear();
    }

    public void InvalidateWhere(Func<TKey, bool> predicate)
    {
        var keysToRemove = cache.Keys.Where(predicate).ToList();
        foreach (var key in keysToRemove)
        {
            cache.TryRemove(key, out _);
        }
    }

    public MemoizationStatistics GetStatistics()
    {
        return new MemoizationStatistics
        {
            HitCount = hitCount,
            MissCount = missCount,
            CacheSize = cache.Count,
            LastCleanup = lastCleanup
        };
    }

    private void EvictOldestEntries()
    {
        var entriesToEvict = cache
            .OrderBy(kvp => kvp.Value.LastAccessed)
            .Take(cache.Count / 4) // Remove 25% of entries
            .ToList();

        foreach (var entry in entriesToEvict)
        {
            cache.TryRemove(entry.Key, out _);
        }
    }

    private void PerformCleanup(object? state)
    {
        var now = DateTime.UtcNow;
        var expiredKeys = cache
            .Where(kvp => kvp.Value.IsExpired(now))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            cache.TryRemove(key, out _);
        }

        lastCleanup = now;
    }

    public void Dispose()
    {
        if (!disposed)
        {
            cleanupTimer?.Dispose();
            cache.Clear();
            disposed = true;
        }
    }
}