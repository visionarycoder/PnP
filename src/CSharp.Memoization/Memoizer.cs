using System.Collections.Concurrent;
using System.Diagnostics;

namespace CSharp.Memoization;

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