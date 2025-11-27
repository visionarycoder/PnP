using Microsoft.Extensions.Caching.Distributed;

namespace CSharp.DistributedCache;

public class SimulatedDistributedCache : IAdvancedDistributedCache
{
    private readonly Dictionary<string, object> cache = new();
    private readonly Dictionary<string, DateTime> expiration = new();
    private long hitCount = 0;
    private long missCount = 0;

    public Task<T> GetAsync<T>(string key, CancellationToken token = default)
    {
        if (IsExpired(key))
        {
            cache.Remove(key);
            expiration.Remove(key);
        }

        if (cache.TryGetValue(key, out var value))
        {
            Interlocked.Increment(ref hitCount);
            if (value is CachedItem<T> cachedItem)
            {
                return Task.FromResult(cachedItem.Value);
            }
            if (value is T directValue)
            {
                return Task.FromResult(directValue);
            }
        }

        Interlocked.Increment(ref missCount);
        return Task.FromResult(default(T)!);
    }

    public Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions? options = null,
        CancellationToken token = default)
    {
        cache[key] = value!;
        
        if (options?.AbsoluteExpirationRelativeToNow.HasValue == true)
        {
            expiration[key] = DateTime.UtcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);
        }
        
        return Task.CompletedTask;
    }

    public async Task<(bool found, T value)> TryGetAsync<T>(string key, CancellationToken token = default)
    {
        var value = await GetAsync<T>(key, token);
        return (!EqualityComparer<T>.Default.Equals(value, default(T)), value);
    }

    public async Task<IDictionary<string, T>> GetManyAsync<T>(IEnumerable<string> keys,
        CancellationToken token = default)
    {
        var result = new Dictionary<string, T>();
        
        foreach (var key in keys)
        {
            var value = await GetAsync<T>(key, token);
            if (!EqualityComparer<T>.Default.Equals(value, default(T)))
            {
                result[key] = value;
            }
        }
        
        return result;
    }

    public async Task SetManyAsync<T>(IDictionary<string, T> items,
        DistributedCacheEntryOptions? options = null, CancellationToken token = default)
    {
        foreach (var item in items)
        {
            await SetAsync(item.Key, item.Value, options, token);
        }
    }

    public Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken token = default)
    {
        foreach (var key in keys)
        {
            cache.Remove(key);
            expiration.Remove(key);
        }
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken token = default)
    {
        var keysToRemove = cache.Keys.Where(k => k.Contains(pattern.Replace("*", ""))).ToList();
        foreach (var key in keysToRemove)
        {
            cache.Remove(key);
            expiration.Remove(key);
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken token = default)
    {
        return Task.FromResult(cache.ContainsKey(key) && !IsExpired(key));
    }

    public Task<TimeSpan?> GetTtlAsync(string key, CancellationToken token = default)
    {
        if (expiration.TryGetValue(key, out var exp))
        {
            var ttl = exp - DateTime.UtcNow;
            return Task.FromResult<TimeSpan?>(ttl.TotalSeconds > 0 ? ttl : null);
        }
        return Task.FromResult<TimeSpan?>(null);
    }

    public Task<long> IncrementAsync(string key, long value = 1, CancellationToken token = default)
    {
        if (cache.TryGetValue(key, out var existing) && existing is long currentValue)
        {
            var newValue = currentValue + value;
            cache[key] = newValue;
            return Task.FromResult(newValue);
        }
        
        cache[key] = value;
        return Task.FromResult(value);
    }

    public Task<double> IncrementAsync(string key, double value, CancellationToken token = default)
    {
        if (cache.TryGetValue(key, out var existing) && existing is double currentValue)
        {
            var newValue = currentValue + value;
            cache[key] = newValue;
            return Task.FromResult(newValue);
        }
        
        cache[key] = value;
        return Task.FromResult(value);
    }

    public Task<ICacheStatistics> GetStatisticsAsync(CancellationToken token = default)
    {
        var total = hitCount + missCount;
        var hitRate = total > 0 ? (double)hitCount / total : 0;
        
        return Task.FromResult<ICacheStatistics>(new CacheStatistics
        {
            HitCount = hitCount,
            MissCount = missCount,
            HitRate = hitRate,
            KeyCount = cache.Count,
            UsedMemory = cache.Count * 100, // Simulated
            MaxMemory = 1000000 // Simulated
        });
    }

    public Task InvalidateTagAsync(string tag, CancellationToken token = default)
    {
        // Simulated tag-based invalidation
        return Task.CompletedTask;
    }

    // IDistributedCache implementation
    public byte[]? Get(string key) => GetAsync(key).GetAwaiter().GetResult();
    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => GetAsync<byte[]?>(key, token)!;
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => SetAsync(key, value, options).GetAwaiter().GetResult();
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => SetAsync<byte[]>(key, value, options, token);
    public void Refresh(string key) { }
    public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
    public void Remove(string key) => RemoveAsync(key).GetAwaiter().GetResult();
    public Task RemoveAsync(string key, CancellationToken token = default) { cache.Remove(key); expiration.Remove(key); return Task.CompletedTask; }

    private bool IsExpired(string key)
    {
        return expiration.TryGetValue(key, out var exp) && DateTime.UtcNow > exp;
    }
}