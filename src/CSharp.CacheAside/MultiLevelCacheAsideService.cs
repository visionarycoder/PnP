using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text.Json;

namespace CSharp.CacheAside;

// Multi-level cache-aside service
public class MultiLevelCacheAsideService<TKey, TValue> : ICacheAsideService<TKey, TValue>, IDisposable
    where TKey : notnull
{
    private readonly IMemoryCache memoryCache;
    private readonly IDistributedCache distributedCache;
    private readonly ILogger? logger;
    private readonly CacheAsideOptions defaultOptions;
    private readonly SemaphoreSlim factorySemaphore;
    private readonly ConcurrentDictionary<TKey, SemaphoreSlim> keySemaphores;
    private readonly Timer? statisticsTimer;
    private readonly CacheAsideStatistics statistics;
    private bool disposed = false;

    public MultiLevelCacheAsideService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        IOptions<CacheAsideOptions>? options = null,
        ILogger<MultiLevelCacheAsideService<TKey, TValue>>? logger = null)
    {
        this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        this.distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        this.defaultOptions = options?.Value ?? new CacheAsideOptions();
        this.logger = logger;
        
        factorySemaphore = new(defaultOptions.MaxConcurrentFactoryCalls, 
            defaultOptions.MaxConcurrentFactoryCalls);
        keySemaphores = new();
        statistics = new CacheAsideStatistics();
        
        // Set up statistics timer
        if (defaultOptions.EnableStatistics)
        {
            statisticsTimer = new Timer(UpdateStatistics, null, 
                TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(1));
        }
    }

    public async Task<TValue> GetAsync(TKey key, Func<TKey, Task<TValue>> valueFactory, 
        TimeSpan? expiration = null, CancellationToken token = default)
    {
        var options = new CacheAsideOptions
        {
            Expiration = expiration ?? defaultOptions.Expiration,
            AllowNullValues = defaultOptions.AllowNullValues,
            UseStaleWhileRevalidate = defaultOptions.UseStaleWhileRevalidate,
            StaleThreshold = defaultOptions.StaleThreshold,
            EnableStatistics = defaultOptions.EnableStatistics
        };

        return await GetAsync(key, valueFactory, options, token).ConfigureAwait(false);
    }

    public async Task<TValue> GetAsync(TKey key, Func<TKey, Task<TValue>> valueFactory, 
        CacheAsideOptions options, CancellationToken token = default)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (valueFactory == null) throw new ArgumentNullException(nameof(valueFactory));

        var cacheKey = GenerateCacheKey(key);
        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            // Level 1: Memory cache
            if (memoryCache.TryGetValue(cacheKey, out CacheEntry<TValue>? memoryEntry) && memoryEntry != null)
            {
                if (IsEntryValid(memoryEntry, options))
                {
                    RecordHit(CacheLevel.Memory);
                    logger?.LogTrace("Cache hit (Memory): {Key}", cacheKey);
                    return memoryEntry.Value;
                }
                else if (options.UseStaleWhileRevalidate)
                {
                    // Return stale data while refreshing in background
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await RefreshEntryAsync(key, valueFactory, options, token).ConfigureAwait(false);
                        }
                        catch (Exception ex)
                        {
                            logger?.LogWarning(ex, "Background refresh failed for key {Key}", cacheKey);
                        }
                    }, token);
                    
                    RecordHit(CacheLevel.Memory, isStale: true);
                    logger?.LogTrace("Stale cache hit (Memory): {Key}", cacheKey);
                    return memoryEntry.Value;
                }
            }

            // Level 2: Distributed cache
            var distributedData = await distributedCache.GetAsync(cacheKey, token).ConfigureAwait(false);
            if (distributedData != null)
            {
                try
                {
                    var distributedEntry = JsonSerializer.Deserialize<CacheEntry<TValue>>(distributedData);
                    
                    if (distributedEntry != null && IsEntryValid(distributedEntry, options))
                    {
                        // Promote to memory cache
                        var memoryOptions = CreateMemoryEntryOptions(options);
                        memoryCache.Set(cacheKey, distributedEntry, memoryOptions);
                        
                        RecordHit(CacheLevel.Distributed);
                        logger?.LogTrace("Cache hit (Distributed): {Key}", cacheKey);
                        return distributedEntry.Value;
                    }
                    else if (distributedEntry != null && options.UseStaleWhileRevalidate)
                    {
                        // Return stale data while refreshing in background
                        _ = Task.Run(async () =>
                        {
                            try
                            {
                                await RefreshEntryAsync(key, valueFactory, options, token).ConfigureAwait(false);
                            }
                            catch (Exception ex)
                            {
                                logger?.LogWarning(ex, "Background refresh failed for key {Key}", cacheKey);
                            }
                        }, token);
                        
                        RecordHit(CacheLevel.Distributed, isStale: true);
                        logger?.LogTrace("Stale cache hit (Distributed): {Key}", cacheKey);
                        return distributedEntry.Value;
                    }
                }
                catch (JsonException ex)
                {
                    logger?.LogWarning(ex, "Failed to deserialize cached value for key {Key}", cacheKey);
                }
            }

            // Cache miss - load from source with concurrency control
            RecordMiss();
            return await LoadAndCacheAsync(key, valueFactory, options, token).ConfigureAwait(false);
        }
        finally
        {
            RecordOperation(stopwatch.Elapsed);
        }
    }

    public async Task<IEnumerable<TValue>> GetManyAsync(IEnumerable<TKey> keys, 
        Func<IEnumerable<TKey>, Task<IDictionary<TKey, TValue>>> valueFactory,
        TimeSpan? expiration = null, CancellationToken token = default)
    {
        if (keys == null) throw new ArgumentNullException(nameof(keys));
        if (valueFactory == null) throw new ArgumentNullException(nameof(valueFactory));

        var keyList = keys.ToList();
        if (keyList.Count == 0) return Enumerable.Empty<TValue>();

        var options = new CacheAsideOptions
        {
            Expiration = expiration ?? defaultOptions.Expiration,
            AllowNullValues = defaultOptions.AllowNullValues
        };

        var results = new Dictionary<TKey, TValue>();
        var cacheMisses = new List<TKey>();

        // Check memory cache first
        foreach (var key in keyList)
        {
            var cacheKey = GenerateCacheKey(key);
            if (memoryCache.TryGetValue(cacheKey, out CacheEntry<TValue>? entry) && 
                entry != null && IsEntryValid(entry, options))
            {
                results[key] = entry.Value;
                RecordHit(CacheLevel.Memory);
            }
            else
            {
                cacheMisses.Add(key);
            }
        }

        // Check distributed cache for remaining keys
        if (cacheMisses.Count > 0)
        {
            var distributedResults = await GetManyFromDistributedCacheAsync(cacheMisses, options, token)
                .ConfigureAwait(false);
            
            foreach (var kvp in distributedResults)
            {
                results[kvp.Key] = kvp.Value;
                cacheMisses.Remove(kvp.Key);
                RecordHit(CacheLevel.Distributed);
            }
        }

        // Load remaining keys from source
        if (cacheMisses.Count > 0)
        {
            var sourceResults = await valueFactory(cacheMisses).ConfigureAwait(false);
            
            // Cache the results
            var cacheOperations = sourceResults.Select(async kvp =>
            {
                await SetAsync(kvp.Key, kvp.Value, options.Expiration, token).ConfigureAwait(false);
                return kvp;
            });

            await Task.WhenAll(cacheOperations).ConfigureAwait(false);
            
            foreach (var kvp in sourceResults)
            {
                results[kvp.Key] = kvp.Value;
                RecordMiss();
            }
        }

        return keyList.Where(key => results.ContainsKey(key)).Select(key => results[key]);
    }

    public async Task SetAsync(TKey key, TValue value, TimeSpan? expiration = null, 
        CancellationToken token = default)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        var cacheKey = GenerateCacheKey(key);
        var entry = new CacheEntry<TValue>
        {
            Value = value,
            CreatedAt = DateTime.UtcNow,
            ExpiresAt = expiration.HasValue ? DateTime.UtcNow.Add(expiration.Value) : null,
            Metadata = new Dictionary<string, object>()
        };

        // Set in memory cache
        var memoryOptions = CreateMemoryEntryOptions(new CacheAsideOptions { Expiration = expiration });
        memoryCache.Set(cacheKey, entry, memoryOptions);

        // Set in distributed cache
        var serializedEntry = JsonSerializer.Serialize(entry);
        var distributedOptions = new DistributedCacheEntryOptions();
        
        if (expiration.HasValue)
        {
            distributedOptions.SetAbsoluteExpiration(expiration.Value);
        }

        await distributedCache.SetAsync(cacheKey, System.Text.Encoding.UTF8.GetBytes(serializedEntry), 
            distributedOptions, token).ConfigureAwait(false);

        logger?.LogTrace("Cached value for key {Key}", cacheKey);
    }

    public async Task SetManyAsync(IDictionary<TKey, TValue> keyValuePairs, TimeSpan? expiration = null, 
        CancellationToken token = default)
    {
        if (keyValuePairs == null) throw new ArgumentNullException(nameof(keyValuePairs));

        var tasks = keyValuePairs.Select(kvp => SetAsync(kvp.Key, kvp.Value, expiration, token));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async Task RemoveAsync(TKey key, CancellationToken token = default)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        var cacheKey = GenerateCacheKey(key);
        
        // Remove from memory cache
        memoryCache.Remove(cacheKey);
        
        // Remove from distributed cache
        await distributedCache.RemoveAsync(cacheKey, token).ConfigureAwait(false);

        logger?.LogTrace("Removed cache entry for key {Key}", cacheKey);
    }

    public async Task RemoveManyAsync(IEnumerable<TKey> keys, CancellationToken token = default)
    {
        if (keys == null) throw new ArgumentNullException(nameof(keys));

        var tasks = keys.Select(key => RemoveAsync(key, token));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async Task<bool> ExistsAsync(TKey key, CancellationToken token = default)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        var cacheKey = GenerateCacheKey(key);
        
        // Check memory cache first
        if (memoryCache.TryGetValue(cacheKey, out _))
        {
            return true;
        }

        // Check distributed cache
        var distributedData = await distributedCache.GetAsync(cacheKey, token).ConfigureAwait(false);
        return distributedData != null;
    }

    public async Task RefreshAsync(TKey key, Func<TKey, Task<TValue>> valueFactory, 
        CancellationToken token = default)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (valueFactory == null) throw new ArgumentNullException(nameof(valueFactory));

        await RefreshEntryAsync(key, valueFactory, defaultOptions, token).ConfigureAwait(false);
    }

    public async Task WarmupAsync(IEnumerable<TKey> keys, 
        Func<IEnumerable<TKey>, Task<IDictionary<TKey, TValue>>> valueFactory,
        CancellationToken token = default)
    {
        if (keys == null) throw new ArgumentNullException(nameof(keys));
        if (valueFactory == null) throw new ArgumentNullException(nameof(valueFactory));

        var keyList = keys.ToList();
        if (keyList.Count == 0) return;

        try
        {
            logger?.LogInformation("Starting cache warmup for {Count} keys", keyList.Count);
            
            // Load data from source
            var sourceData = await valueFactory(keyList).ConfigureAwait(false);
            
            // Cache the data
            await SetManyAsync(sourceData, defaultOptions.Expiration, token).ConfigureAwait(false);
            
            logger?.LogInformation("Cache warmup completed for {Count} keys", sourceData.Count);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Cache warmup failed");
            throw;
        }
    }

    public Task<ICacheAsideStatistics> GetStatisticsAsync(CancellationToken token = default)
    {
        return Task.FromResult<ICacheAsideStatistics>(statistics);
    }

    // Private helper methods
    private string GenerateCacheKey(TKey key)
    {
        return $"{typeof(TValue).Name}:{key}";
    }

    private static bool IsEntryValid(CacheEntry<TValue> entry, CacheAsideOptions options)
    {
        if (entry.ExpiresAt.HasValue && DateTime.UtcNow > entry.ExpiresAt.Value)
        {
            return false;
        }

        if (options.UseStaleWhileRevalidate && entry.ExpiresAt.HasValue)
        {
            var staleThreshold = entry.ExpiresAt.Value.Subtract(options.StaleThreshold);
            return DateTime.UtcNow <= entry.ExpiresAt.Value;
        }

        return true;
    }

    private static MemoryCacheEntryOptions CreateMemoryEntryOptions(CacheAsideOptions options)
    {
        var memoryOptions = new MemoryCacheEntryOptions();
        
        if (options.Expiration.HasValue)
        {
            memoryOptions.SetAbsoluteExpiration(options.Expiration.Value);
        }

        return memoryOptions;
    }

    private void RecordHit(CacheLevel level, bool isStale = false)
    {
        if (!defaultOptions.EnableStatistics) return;

        switch (level)
        {
            case CacheLevel.Memory:
                statistics.RecordMemoryHit(isStale);
                break;
            case CacheLevel.Distributed:
                statistics.RecordDistributedHit(isStale);
                break;
        }
    }

    private void RecordMiss()
    {
        if (defaultOptions.EnableStatistics)
        {
            statistics.RecordMiss();
        }
    }

    private void RecordOperation(TimeSpan duration)
    {
        if (defaultOptions.EnableStatistics)
        {
            statistics.RecordOperationTime(duration);
        }
    }

    private void RecordFactoryCall(TimeSpan duration)
    {
        // This could be extended to track factory call statistics
        logger?.LogTrace("Factory call completed in {Duration}", duration);
    }

    private void UpdateStatistics(object? state)
    {
        // This could be used for periodic statistics reporting
        if (defaultOptions.EnableStatistics)
        {
            logger?.LogDebug("Cache Statistics - Hits: {MemoryHits}+{DistributedHits}, Misses: {Misses}, Hit Ratio: {HitRatio:P2}",
                statistics.MemoryHits, statistics.DistributedHits, statistics.Misses, statistics.HitRatio);
        }
    }

    private async Task<TValue> LoadAndCacheAsync(TKey key, Func<TKey, Task<TValue>> valueFactory, 
        CacheAsideOptions options, CancellationToken token)
    {
        // Use per-key semaphore to prevent cache stampede
        var semaphore = keySemaphores.GetOrAdd(key, k => new SemaphoreSlim(1, 1));
        
        await semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            // Double-check cache after acquiring lock
            var cacheKey = GenerateCacheKey(key);
            if (memoryCache.TryGetValue(cacheKey, out CacheEntry<TValue>? existingEntry) && 
                existingEntry != null && IsEntryValid(existingEntry, options))
            {
                RecordHit(CacheLevel.Memory);
                return existingEntry.Value;
            }

            // Load from source
            await factorySemaphore.WaitAsync(token).ConfigureAwait(false);
            try
            {
                var stopwatch = Stopwatch.StartNew();
                var value = await valueFactory(key).ConfigureAwait(false);
                
                RecordFactoryCall(stopwatch.Elapsed);
                
                // Cache the value if it's not null or null values are allowed
                if (value != null || options.AllowNullValues)
                {
                    await SetAsync(key, value, options.Expiration, token).ConfigureAwait(false);
                }
                
                return value;
            }
            finally
            {
                factorySemaphore.Release();
            }
        }
        finally
        {
            semaphore.Release();
            
            // Clean up semaphore if no longer needed
            if (semaphore.CurrentCount == 1)
            {
                keySemaphores.TryRemove(key, out _);
                semaphore.Dispose();
            }
        }
    }

    private async Task<IDictionary<TKey, TValue>> GetManyFromDistributedCacheAsync(
        IEnumerable<TKey> keys, CacheAsideOptions options, CancellationToken token)
    {
        var results = new Dictionary<TKey, TValue>();
        
        foreach (var key in keys)
        {
            var cacheKey = GenerateCacheKey(key);
            var data = await distributedCache.GetAsync(cacheKey, token).ConfigureAwait(false);
            
            if (data != null)
            {
                try
                {
                    var entry = JsonSerializer.Deserialize<CacheEntry<TValue>>(data);
                    if (entry != null && IsEntryValid(entry, options))
                    {
                        results[key] = entry.Value;
                        
                        // Promote to memory cache
                        var memoryOptions = CreateMemoryEntryOptions(options);
                        memoryCache.Set(cacheKey, entry, memoryOptions);
                    }
                }
                catch (JsonException ex)
                {
                    logger?.LogWarning(ex, "Failed to deserialize cached value for key {Key}", cacheKey);
                }
            }
        }
        
        return results;
    }

    private async Task RefreshEntryAsync(TKey key, Func<TKey, Task<TValue>> valueFactory, 
        CacheAsideOptions options, CancellationToken token)
    {
        try
        {
            var newValue = await valueFactory(key).ConfigureAwait(false);
            await SetAsync(key, newValue, options.Expiration, token).ConfigureAwait(false);
            
            logger?.LogTrace("Refreshed cache entry for key {Key}", key);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to refresh cache entry for key {Key}", key);
            throw;
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            disposed = true;
            statisticsTimer?.Dispose();
            factorySemaphore?.Dispose();
            
            // Dispose all key semaphores
            foreach (var semaphore in keySemaphores.Values)
            {
                semaphore.Dispose();
            }
            keySemaphores.Clear();
        }
    }
}