# Cache-Aside Pattern

**Description**: Comprehensive cache-aside (lazy loading) pattern implementations including multi-level caching, cache warming strategies, cache-through operations, distributed cache coordination, cache statistics, and advanced cache-aside variations for scalable and high-performance applications.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.ComponentModel.DataAnnotations;

// Core cache-aside interfaces
public interface ICacheAsideService<TKey, TValue>
{
    Task<TValue> GetAsync(TKey key, Func<TKey, Task<TValue>> valueFactory, 
        TimeSpan? expiration = null, CancellationToken token = default);
    Task<TValue> GetAsync(TKey key, Func<TKey, Task<TValue>> valueFactory, 
        CacheAsideOptions options, CancellationToken token = default);
    Task<IEnumerable<TValue>> GetManyAsync(IEnumerable<TKey> keys, 
        Func<IEnumerable<TKey>, Task<IDictionary<TKey, TValue>>> valueFactory,
        TimeSpan? expiration = null, CancellationToken token = default);
    Task SetAsync(TKey key, TValue value, TimeSpan? expiration = null, 
        CancellationToken token = default);
    Task SetManyAsync(IDictionary<TKey, TValue> keyValuePairs, TimeSpan? expiration = null, 
        CancellationToken token = default);
    Task RemoveAsync(TKey key, CancellationToken token = default);
    Task RemoveManyAsync(IEnumerable<TKey> keys, CancellationToken token = default);
    Task<bool> ExistsAsync(TKey key, CancellationToken token = default);
    Task RefreshAsync(TKey key, Func<TKey, Task<TValue>> valueFactory, 
        CancellationToken token = default);
    Task WarmupAsync(IEnumerable<TKey> keys, Func<IEnumerable<TKey>, Task<IDictionary<TKey, TValue>>> valueFactory,
        CancellationToken token = default);
    Task<ICacheAsideStatistics> GetStatisticsAsync(CancellationToken token = default);
}

public class CacheAsideOptions
{
    public TimeSpan? Expiration { get; set; }
    public bool AllowNullValues { get; set; } = true;
    public bool UseStaleWhileRevalidate { get; set; } = false;
    public TimeSpan StaleThreshold { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxConcurrentFactoryCalls { get; set; } = Environment.ProcessorCount;
    public bool EnableStatistics { get; set; } = true;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}

// Multi-level cache-aside service
public class MultiLevelCacheAsideService<TKey, TValue> : ICacheAsideService<TKey, TValue>, IDisposable
{
    private readonly IMemoryCache memoryCache;
    private readonly IDistributedCache distributedCache;
    private readonly ILogger logger;
    private readonly CacheAsideOptions defaultOptions;
    private readonly SemaphoreSlim factorySemaphore;
    private readonly ConcurrentDictionary<TKey, SemaphoreSlim> keySemaphores;
    private readonly Timer statisticsTimer;
    private readonly CacheAsideStatistics statistics;
    private bool disposed = false;

    public MultiLevelCacheAsideService(
        IMemoryCache memoryCache,
        IDistributedCache distributedCache,
        IOptions<CacheAsideOptions> options = null,
        ILogger<MultiLevelCacheAsideService<TKey, TValue>> logger = null)
    {
        this.memoryCache = memoryCache ?? throw new ArgumentNullException(nameof(memoryCache));
        this.distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        this.defaultOptions = options?.Value ?? new CacheAsideOptions();
        this.logger = logger;
        
        factorySemaphore = new SemaphoreSlim(defaultOptions.MaxConcurrentFactoryCalls, 
            defaultOptions.MaxConcurrentFactoryCalls);
        keySemaphores = new ConcurrentDictionary<TKey, SemaphoreSlim>();
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
            if (memoryCache.TryGetValue(cacheKey, out CacheEntry<TValue> memoryEntry))
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
                    
                    if (IsEntryValid(distributedEntry, options))
                    {
                        // Promote to memory cache
                        var memoryOptions = CreateMemoryEntryOptions(options);
                        memoryCache.Set(cacheKey, distributedEntry, memoryOptions);
                        
                        RecordHit(CacheLevel.Distributed);
                        logger?.LogTrace("Cache hit (Distributed): {Key}", cacheKey);
                        return distributedEntry.Value;
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
            if (memoryCache.TryGetValue(cacheKey, out CacheEntry<TValue> entry) && 
                IsEntryValid(entry, options))
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
        return Task.FromResult<ICacheAsideStatistics>(statistics.Clone());
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
            if (memoryCache.TryGetValue(cacheKey, out CacheEntry<TValue> existingEntry) && 
                IsEntryValid(existingEntry, options))
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
                    if (IsEntryValid(entry, options))
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
            
            logger?.LogTrace("Refreshed cache entry for key {Key}", GenerateCacheKey(key));
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to refresh cache entry for key {Key}", GenerateCacheKey(key));
            throw;
        }
    }

    private static bool IsEntryValid(CacheEntry<TValue> entry, CacheAsideOptions options)
    {
        if (entry == null) return false;
        
        if (entry.ExpiresAt.HasValue && DateTime.UtcNow > entry.ExpiresAt.Value)
        {
            return false;
        }
        
        return true;
    }

    private MemoryCacheEntryOptions CreateMemoryEntryOptions(CacheAsideOptions options)
    {
        var memoryOptions = new MemoryCacheEntryOptions
        {
            Priority = CacheItemPriority.Normal
        };

        if (options.Expiration.HasValue)
        {
            memoryOptions.SetAbsoluteExpiration(options.Expiration.Value);
        }

        return memoryOptions;
    }

    private string GenerateCacheKey(TKey key)
    {
        return $"{typeof(TValue).Name}:{key}";
    }

    private void RecordHit(CacheLevel level, bool isStale = false)
    {
        if (!defaultOptions.EnableStatistics) return;
        
        Interlocked.Increment(ref statistics.TotalRequests);
        Interlocked.Increment(ref statistics.CacheHits);
        
        switch (level)
        {
            case CacheLevel.Memory:
                Interlocked.Increment(ref statistics.MemoryHits);
                break;
            case CacheLevel.Distributed:
                Interlocked.Increment(ref statistics.DistributedHits);
                break;
        }
        
        if (isStale)
        {
            Interlocked.Increment(ref statistics.StaleHits);
        }
    }

    private void RecordMiss()
    {
        if (!defaultOptions.EnableStatistics) return;
        
        Interlocked.Increment(ref statistics.TotalRequests);
        Interlocked.Increment(ref statistics.CacheMisses);
    }

    private void RecordOperation(TimeSpan duration)
    {
        if (!defaultOptions.EnableStatistics) return;
        
        statistics.AddOperationTime(duration);
    }

    private void RecordFactoryCall(TimeSpan duration)
    {
        if (!defaultOptions.EnableStatistics) return;
        
        Interlocked.Increment(ref statistics.FactoryCalls);
        statistics.AddFactoryTime(duration);
    }

    private void UpdateStatistics(object state)
    {
        statistics.LastUpdated = DateTime.UtcNow;
    }

    public void Dispose()
    {
        if (!disposed)
        {
            statisticsTimer?.Dispose();
            factorySemaphore?.Dispose();
            
            foreach (var semaphore in keySemaphores.Values)
            {
                semaphore?.Dispose();
            }
            keySemaphores.Clear();
            
            disposed = true;
        }
    }
}

// Specialized cache-aside service for specific scenarios
public class ReadThroughCacheService<TKey, TValue> : ICacheAsideService<TKey, TValue>
{
    private readonly ICacheAsideService<TKey, TValue> cacheService;
    private readonly Func<TKey, Task<TValue>> defaultValueFactory;
    private readonly CacheAsideOptions defaultOptions;

    public ReadThroughCacheService(
        ICacheAsideService<TKey, TValue> cacheService,
        Func<TKey, Task<TValue>> defaultValueFactory,
        CacheAsideOptions defaultOptions = null)
    {
        this.cacheService = cacheService ?? throw new ArgumentNullException(nameof(cacheService));
        this.defaultValueFactory = defaultValueFactory ?? throw new ArgumentNullException(nameof(defaultValueFactory));
        this.defaultOptions = defaultOptions ?? new CacheAsideOptions();
    }

    public Task<TValue> GetAsync(TKey key, Func<TKey, Task<TValue>> valueFactory = null, 
        TimeSpan? expiration = null, CancellationToken token = default)
    {
        return cacheService.GetAsync(key, valueFactory ?? defaultValueFactory, expiration, token);
    }

    public Task<TValue> GetAsync(TKey key, Func<TKey, Task<TValue>> valueFactory, 
        CacheAsideOptions options, CancellationToken token = default)
    {
        return cacheService.GetAsync(key, valueFactory ?? defaultValueFactory, options, token);
    }

    public Task<IEnumerable<TValue>> GetManyAsync(IEnumerable<TKey> keys, 
        Func<IEnumerable<TKey>, Task<IDictionary<TKey, TValue>>> valueFactory = null,
        TimeSpan? expiration = null, CancellationToken token = default)
    {
        if (valueFactory == null)
        {
            valueFactory = async keyList =>
            {
                var results = new Dictionary<TKey, TValue>();
                foreach (var key in keyList)
                {
                    var value = await defaultValueFactory(key).ConfigureAwait(false);
                    results[key] = value;
                }
                return results;
            };
        }
        
        return cacheService.GetManyAsync(keys, valueFactory, expiration, token);
    }

    public Task SetAsync(TKey key, TValue value, TimeSpan? expiration = null, CancellationToken token = default)
        => cacheService.SetAsync(key, value, expiration, token);

    public Task SetManyAsync(IDictionary<TKey, TValue> keyValuePairs, TimeSpan? expiration = null, CancellationToken token = default)
        => cacheService.SetManyAsync(keyValuePairs, expiration, token);

    public Task RemoveAsync(TKey key, CancellationToken token = default)
        => cacheService.RemoveAsync(key, token);

    public Task RemoveManyAsync(IEnumerable<TKey> keys, CancellationToken token = default)
        => cacheService.RemoveManyAsync(keys, token);

    public Task<bool> ExistsAsync(TKey key, CancellationToken token = default)
        => cacheService.ExistsAsync(key, token);

    public Task RefreshAsync(TKey key, Func<TKey, Task<TValue>> valueFactory = null, CancellationToken token = default)
        => cacheService.RefreshAsync(key, valueFactory ?? defaultValueFactory, token);

    public Task WarmupAsync(IEnumerable<TKey> keys, Func<IEnumerable<TKey>, Task<IDictionary<TKey, TValue>>> valueFactory = null,
        CancellationToken token = default)
    {
        if (valueFactory == null)
        {
            valueFactory = async keyList =>
            {
                var results = new Dictionary<TKey, TValue>();
                foreach (var key in keyList)
                {
                    var value = await defaultValueFactory(key).ConfigureAwait(false);
                    results[key] = value;
                }
                return results;
            };
        }
        
        return cacheService.WarmupAsync(keys, valueFactory, token);
    }

    public Task<ICacheAsideStatistics> GetStatisticsAsync(CancellationToken token = default)
        => cacheService.GetStatisticsAsync(token);
}

// Distributed cache-aside coordination
public class DistributedCacheAsideCoordinator : IDisposable
{
    private readonly ICacheAsideService<string, object> localCache;
    private readonly IDistributedCache distributedCache;
    private readonly ICacheInvalidationService invalidationService;
    private readonly ILogger logger;
    private readonly DistributedCacheCoordinatorOptions options;
    private readonly Timer coordinationTimer;
    private bool disposed = false;

    public DistributedCacheAsideCoordinator(
        ICacheAsideService<string, object> localCache,
        IDistributedCache distributedCache,
        ICacheInvalidationService invalidationService = null,
        IOptions<DistributedCacheCoordinatorOptions> options = null,
        ILogger<DistributedCacheAsideCoordinator> logger = null)
    {
        this.localCache = localCache ?? throw new ArgumentNullException(nameof(localCache));
        this.distributedCache = distributedCache ?? throw new ArgumentNullException(nameof(distributedCache));
        this.invalidationService = invalidationService;
        this.logger = logger;
        this.options = options?.Value ?? new DistributedCacheCoordinatorOptions();
        
        // Set up coordination timer
        coordinationTimer = new Timer(PerformCoordination, null, 
            this.options.CoordinationInterval, this.options.CoordinationInterval);
    }

    public async Task SynchronizeCacheAsync(string key, CancellationToken token = default)
    {
        try
        {
            var localExists = await localCache.ExistsAsync(key, token).ConfigureAwait(false);
            var distributedData = await distributedCache.GetAsync(key, token).ConfigureAwait(false);
            var distributedExists = distributedData != null;

            if (localExists && !distributedExists)
            {
                // Promote local cache to distributed
                logger?.LogTrace("Promoting local cache entry to distributed: {Key}", key);
                // Implementation would depend on how you store the local value
            }
            else if (!localExists && distributedExists)
            {
                // Demote distributed cache to local (if within size limits)
                logger?.LogTrace("Demoting distributed cache entry to local: {Key}", key);
                // Implementation would depend on deserialization and size limits
            }
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Failed to synchronize cache for key {Key}", key);
        }
    }

    public async Task InvalidateEverywhereAsync(string key, CancellationToken token = default)
    {
        try
        {
            // Remove from local cache
            await localCache.RemoveAsync(key, token).ConfigureAwait(false);
            
            // Use invalidation service if available, otherwise remove directly
            if (invalidationService != null)
            {
                await invalidationService.InvalidateAsync(key, token).ConfigureAwait(false);
            }
            else
            {
                await distributedCache.RemoveAsync(key, token).ConfigureAwait(false);
            }
            
            logger?.LogTrace("Invalidated key everywhere: {Key}", key);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to invalidate key everywhere: {Key}", key);
            throw;
        }
    }

    private void PerformCoordination(object state)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                // Perform background coordination tasks
                await PerformBackgroundCoordinationAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error during cache coordination");
            }
        });
    }

    private async Task PerformBackgroundCoordinationAsync()
    {
        // Implementation would include:
        // 1. Checking for cache inconsistencies
        // 2. Promoting frequently accessed local cache entries
        // 3. Cleaning up expired coordination metadata
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public void Dispose()
    {
        if (!disposed)
        {
            coordinationTimer?.Dispose();
            disposed = true;
        }
    }
}

// Repository pattern with cache-aside
public class CachedRepository<TEntity, TKey> : ICachedRepository<TEntity, TKey>
    where TEntity : class, IEntity<TKey>
{
    private readonly IRepository<TEntity, TKey> repository;
    private readonly ICacheAsideService<TKey, TEntity> cache;
    private readonly CacheAsideOptions cacheOptions;
    private readonly ILogger logger;

    public CachedRepository(
        IRepository<TEntity, TKey> repository,
        ICacheAsideService<TKey, TEntity> cache,
        IOptions<CacheAsideOptions> cacheOptions = null,
        ILogger<CachedRepository<TEntity, TKey>> logger = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.cacheOptions = cacheOptions?.Value ?? new CacheAsideOptions();
        this.logger = logger;
    }

    public async Task<TEntity> GetByIdAsync(TKey id, CancellationToken token = default)
    {
        return await cache.GetAsync(id, async key =>
        {
            logger?.LogTrace("Loading entity from repository: {Key}", key);
            return await repository.GetByIdAsync(key, token).ConfigureAwait(false);
        }, cacheOptions.Expiration, token).ConfigureAwait(false);
    }

    public async Task<IEnumerable<TEntity>> GetManyAsync(IEnumerable<TKey> ids, CancellationToken token = default)
    {
        return await cache.GetManyAsync(ids, async keys =>
        {
            logger?.LogTrace("Loading {Count} entities from repository", keys.Count());
            var entities = await repository.GetManyAsync(keys, token).ConfigureAwait(false);
            return entities.ToDictionary(e => e.Id);
        }, cacheOptions.Expiration, token).ConfigureAwait(false);
    }

    public async Task<TEntity> CreateAsync(TEntity entity, CancellationToken token = default)
    {
        var created = await repository.CreateAsync(entity, token).ConfigureAwait(false);
        
        // Cache the new entity
        await cache.SetAsync(created.Id, created, cacheOptions.Expiration, token).ConfigureAwait(false);
        
        logger?.LogTrace("Created and cached entity: {Id}", created.Id);
        return created;
    }

    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken token = default)
    {
        var updated = await repository.UpdateAsync(entity, token).ConfigureAwait(false);
        
        // Update cache
        await cache.SetAsync(updated.Id, updated, cacheOptions.Expiration, token).ConfigureAwait(false);
        
        logger?.LogTrace("Updated and cached entity: {Id}", updated.Id);
        return updated;
    }

    public async Task DeleteAsync(TKey id, CancellationToken token = default)
    {
        await repository.DeleteAsync(id, token).ConfigureAwait(false);
        
        // Remove from cache
        await cache.RemoveAsync(id, token).ConfigureAwait(false);
        
        logger?.LogTrace("Deleted entity and removed from cache: {Id}", id);
    }

    public async Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, 
        CancellationToken token = default)
    {
        // For queries, we typically don't cache (or use query result caching)
        return await repository.FindAsync(predicate, token).ConfigureAwait(false);
    }
}

// Supporting classes and interfaces
public class CacheEntry<T>
{
    public T Value { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public IDictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}

public enum CacheLevel
{
    Memory,
    Distributed
}

public interface ICacheAsideStatistics
{
    long TotalRequests { get; }
    long CacheHits { get; }
    long CacheMisses { get; }
    long MemoryHits { get; }
    long DistributedHits { get; }
    long StaleHits { get; }
    long FactoryCalls { get; }
    double HitRatio { get; }
    double MemoryHitRatio { get; }
    TimeSpan AverageOperationTime { get; }
    TimeSpan AverageFactoryTime { get; }
    DateTime LastUpdated { get; }
}

public class CacheAsideStatistics : ICacheAsideStatistics
{
    internal long TotalRequests;
    internal long CacheHits;
    internal long CacheMisses;
    internal long MemoryHits;
    internal long DistributedHits;
    internal long StaleHits;
    internal long FactoryCalls;
    internal long TotalOperationTime;
    internal long TotalFactoryTime;

    long ICacheAsideStatistics.TotalRequests => TotalRequests;
    long ICacheAsideStatistics.CacheHits => CacheHits;
    long ICacheAsideStatistics.CacheMisses => CacheMisses;
    long ICacheAsideStatistics.MemoryHits => MemoryHits;
    long ICacheAsideStatistics.DistributedHits => DistributedHits;
    long ICacheAsideStatistics.StaleHits => StaleHits;
    long ICacheAsideStatistics.FactoryCalls => FactoryCalls;
    
    public double HitRatio => TotalRequests > 0 ? (double)CacheHits / TotalRequests : 0;
    public double MemoryHitRatio => CacheHits > 0 ? (double)MemoryHits / CacheHits : 0;
    
    public TimeSpan AverageOperationTime => TotalRequests > 0 
        ? TimeSpan.FromTicks(TotalOperationTime / TotalRequests) 
        : TimeSpan.Zero;
        
    public TimeSpan AverageFactoryTime => FactoryCalls > 0 
        ? TimeSpan.FromTicks(TotalFactoryTime / FactoryCalls) 
        : TimeSpan.Zero;
        
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

    internal void AddOperationTime(TimeSpan duration)
    {
        Interlocked.Add(ref TotalOperationTime, duration.Ticks);
    }

    internal void AddFactoryTime(TimeSpan duration)
    {
        Interlocked.Add(ref TotalFactoryTime, duration.Ticks);
    }

    internal CacheAsideStatistics Clone()
    {
        return new CacheAsideStatistics
        {
            TotalRequests = this.TotalRequests,
            CacheHits = this.CacheHits,
            CacheMisses = this.CacheMisses,
            MemoryHits = this.MemoryHits,
            DistributedHits = this.DistributedHits,
            StaleHits = this.StaleHits,
            FactoryCalls = this.FactoryCalls,
            TotalOperationTime = this.TotalOperationTime,
            TotalFactoryTime = this.TotalFactoryTime,
            LastUpdated = this.LastUpdated
        };
    }
}

public class DistributedCacheCoordinatorOptions
{
    public TimeSpan CoordinationInterval { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxLocalCacheSize { get; set; } = 1000;
    public bool AutoPromoteFrequentlyAccessed { get; set; } = true;
}

// Repository interfaces
public interface IEntity<TKey>
{
    TKey Id { get; }
}

public interface IRepository<TEntity, TKey> where TEntity : class, IEntity<TKey>
{
    Task<TEntity> GetByIdAsync(TKey id, CancellationToken token = default);
    Task<IEnumerable<TEntity>> GetManyAsync(IEnumerable<TKey> ids, CancellationToken token = default);
    Task<TEntity> CreateAsync(TEntity entity, CancellationToken token = default);
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken token = default);
    Task DeleteAsync(TKey id, CancellationToken token = default);
    Task<IEnumerable<TEntity>> FindAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken token = default);
}

public interface ICachedRepository<TEntity, TKey> : IRepository<TEntity, TKey> where TEntity : class, IEntity<TKey>
{
    // Additional cache-specific methods could be added here
}

// Cache invalidation service interface (referenced from cache-invalidation.md)
public interface ICacheInvalidationService
{
    Task InvalidateAsync(string key, CancellationToken token = default);
    Task InvalidateAsync(IEnumerable<string> keys, CancellationToken token = default);
}

using System.Linq.Expressions;
```

**Usage**:

```csharp
// Example 1: Basic Cache-Aside Setup
Console.WriteLine("Basic Cache-Aside Examples:");

var services = new ServiceCollection()
    .AddMemoryCache()
    .AddDistributedMemoryCache()
    .AddLogging(builder => builder.AddConsole())
    .Configure<CacheAsideOptions>(opts =>
    {
        opts.Expiration = TimeSpan.FromMinutes(15);
        opts.AllowNullValues = true;
        opts.UseStaleWhileRevalidate = true;
        opts.StaleThreshold = TimeSpan.FromMinutes(5);
        opts.MaxConcurrentFactoryCalls = 4;
        opts.EnableStatistics = true;
    })
    .AddSingleton<ICacheAsideService<int, User>, MultiLevelCacheAsideService<int, User>>()
    .BuildServiceProvider();

var cacheService = services.GetRequiredService<ICacheAsideService<int, User>>();

// Simulate a data loading function
Func<int, Task<User>> userLoader = async userId =>
{
    Console.WriteLine($"Loading user {userId} from database...");
    await Task.Delay(100); // Simulate database delay
    return new User { Id = userId, Name = $"User {userId}", Email = $"user{userId}@example.com" };
};

// Get single user (cache miss on first call)
var user1 = await cacheService.GetAsync(1, userLoader);
Console.WriteLine($"Loaded: {user1.Name}");

// Get same user again (cache hit)
var user1Cached = await cacheService.GetAsync(1, userLoader);
Console.WriteLine($"Cached: {user1Cached.Name}");

// Get multiple users
var userIds = new[] { 2, 3, 4, 5 };
var users = await cacheService.GetManyAsync(userIds, async ids =>
{
    var result = new Dictionary<int, User>();
    foreach (var id in ids)
    {
        result[id] = new User { Id = id, Name = $"User {id}", Email = $"user{id}@example.com" };
    }
    return result;
});

Console.WriteLine($"Loaded {users.Count()} users");

// Example 2: Read-Through Cache
Console.WriteLine("\nRead-Through Cache Examples:");

var readThroughCache = new ReadThroughCacheService<int, User>(
    cacheService,
    userLoader,
    new CacheAsideOptions 
    { 
        Expiration = TimeSpan.FromMinutes(30),
        UseStaleWhileRevalidate = true
    });

// No need to provide value factory each time
var user6 = await readThroughCache.GetAsync(6);
var user7 = await readThroughCache.GetAsync(7);
Console.WriteLine($"Read-through loaded: {user6.Name}, {user7.Name}");

// Bulk operations
var moreUsers = await readThroughCache.GetManyAsync(new[] { 8, 9, 10 });
Console.WriteLine($"Read-through bulk loaded {moreUsers.Count()} users");

// Example 3: Cache Warming
Console.WriteLine("\nCache Warming Examples:");

var popularUserIds = Enumerable.Range(100, 50).ToArray(); // Users 100-149

await cacheService.WarmupAsync(popularUserIds, async ids =>
{
    Console.WriteLine($"Warming up cache with {ids.Count()} popular users...");
    var result = new Dictionary<int, User>();
    
    foreach (var id in ids)
    {
        result[id] = new User 
        { 
            Id = id, 
            Name = $"Popular User {id}", 
            Email = $"popular{id}@example.com",
            IsVip = true
        };
    }
    
    return result;
});

Console.WriteLine("Cache warming completed");

// Verify warmed cache
var warmUser = await cacheService.GetAsync(125, userLoader);
Console.WriteLine($"Warmed user retrieved: {warmUser.Name} (VIP: {warmUser.IsVip})");

// Example 4: Repository Pattern with Caching
Console.WriteLine("\nCached Repository Examples:");

// Mock repository
var mockRepository = new MockUserRepository();
var cachedRepository = new CachedRepository<User, int>(
    mockRepository,
    cacheService,
    Options.Create(new CacheAsideOptions { Expiration = TimeSpan.FromMinutes(10) }));

// Create user (automatically cached)
var newUser = await cachedRepository.CreateAsync(new User 
{ 
    Id = 200, 
    Name = "Repository User", 
    Email = "repo@example.com" 
});

// Get user (from cache)
var cachedRepoUser = await cachedRepository.GetByIdAsync(200);
Console.WriteLine($"Repository cached user: {cachedRepoUser.Name}");

// Update user (cache updated automatically)
newUser.Name = "Updated Repository User";
await cachedRepository.UpdateAsync(newUser);

// Get updated user (from cache)
var updatedUser = await cachedRepository.GetByIdAsync(200);
Console.WriteLine($"Updated cached user: {updatedUser.Name}");

// Example 5: Advanced Cache Options
Console.WriteLine("\nAdvanced Cache Options Examples:");

var advancedOptions = new CacheAsideOptions
{
    Expiration = TimeSpan.FromMinutes(30),
    AllowNullValues = false,
    UseStaleWhileRevalidate = true,
    StaleThreshold = TimeSpan.FromMinutes(10),
    EnableStatistics = true,
    Tags = new[] { "users", "profiles" },
    Metadata = new Dictionary<string, object>
    {
        ["department"] = "engineering",
        ["priority"] = "high"
    }
};

// Function that might return null
Func<int, Task<User>> nullableUserLoader = async userId =>
{
    if (userId > 1000) return null; // Simulate user not found
    
    return new User 
    { 
        Id = userId, 
        Name = $"Advanced User {userId}", 
        Email = $"advanced{userId}@example.com" 
    };
};

var advancedUser = await cacheService.GetAsync(999, nullableUserLoader, advancedOptions);
Console.WriteLine($"Advanced user: {advancedUser?.Name ?? "Not found"}");

// Try to get non-existent user (won't be cached due to AllowNullValues = false)
var nullUser = await cacheService.GetAsync(1001, nullableUserLoader, advancedOptions);
Console.WriteLine($"Null user: {nullUser?.Name ?? "Not found"}");

// Example 6: Cache Refresh
Console.WriteLine("\nCache Refresh Examples:");

// Manually refresh a cache entry
await cacheService.RefreshAsync(1, async userId =>
{
    Console.WriteLine($"Refreshing user {userId}...");
    return new User 
    { 
        Id = userId, 
        Name = $"Refreshed User {userId}", 
        Email = $"refreshed{userId}@example.com",
        LastUpdated = DateTime.UtcNow
    };
});

var refreshedUser = await cacheService.GetAsync(1, userLoader);
Console.WriteLine($"Refreshed user: {refreshedUser.Name} (Updated: {refreshedUser.LastUpdated})");

// Example 7: Cache Statistics
Console.WriteLine("\nCache Statistics Examples:");

var statistics = await cacheService.GetStatisticsAsync();
Console.WriteLine($"Cache Statistics:");
Console.WriteLine($"  Total Requests: {statistics.TotalRequests}");
Console.WriteLine($"  Cache Hits: {statistics.CacheHits}");
Console.WriteLine($"  Cache Misses: {statistics.CacheMisses}");
Console.WriteLine($"  Hit Ratio: {statistics.HitRatio:P2}");
Console.WriteLine($"  Memory Hits: {statistics.MemoryHits}");
Console.WriteLine($"  Distributed Hits: {statistics.DistributedHits}");
Console.WriteLine($"  Memory Hit Ratio: {statistics.MemoryHitRatio:P2}");
Console.WriteLine($"  Stale Hits: {statistics.StaleHits}");
Console.WriteLine($"  Factory Calls: {statistics.FactoryCalls}");
Console.WriteLine($"  Average Operation Time: {statistics.AverageOperationTime.TotalMilliseconds:F2}ms");
Console.WriteLine($"  Average Factory Time: {statistics.AverageFactoryTime.TotalMilliseconds:F2}ms");

// Example 8: Cache Management
Console.WriteLine("\nCache Management Examples:");

// Check if key exists
bool userExists = await cacheService.ExistsAsync(1);
Console.WriteLine($"User 1 exists in cache: {userExists}");

// Remove specific keys
await cacheService.RemoveAsync(1);
Console.WriteLine("Removed user 1 from cache");

// Remove multiple keys
await cacheService.RemoveManyAsync(new[] { 2, 3, 4 });
Console.WriteLine("Removed users 2-4 from cache");

// Verify removal
bool user1ExistsAfterRemoval = await cacheService.ExistsAsync(1);
Console.WriteLine($"User 1 exists after removal: {user1ExistsAfterRemoval}");

Console.WriteLine("\nCache-aside pattern examples completed!");
```

**Helper classes for examples:**

```csharp
public class User : IEntity<int>
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
    public bool IsVip { get; set; }
    public DateTime LastUpdated { get; set; } = DateTime.UtcNow;
}

public class MockUserRepository : IRepository<User, int>
{
    private readonly ConcurrentDictionary<int, User> users = new();

    public Task<User> GetByIdAsync(int id, CancellationToken token = default)
    {
        users.TryGetValue(id, out var user);
        return Task.FromResult(user);
    }

    public Task<IEnumerable<User>> GetManyAsync(IEnumerable<int> ids, CancellationToken token = default)
    {
        var result = ids.Select(id => users.TryGetValue(id, out var user) ? user : null)
                        .Where(u => u != null);
        return Task.FromResult(result);
    }

    public Task<User> CreateAsync(User entity, CancellationToken token = default)
    {
        users[entity.Id] = entity;
        return Task.FromResult(entity);
    }

    public Task<User> UpdateAsync(User entity, CancellationToken token = default)
    {
        users[entity.Id] = entity;
        return Task.FromResult(entity);
    }

    public Task DeleteAsync(int id, CancellationToken token = default)
    {
        users.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<User>> FindAsync(Expression<Func<User, bool>> predicate, 
        CancellationToken token = default)
    {
        var compiled = predicate.Compile();
        var result = users.Values.Where(compiled);
        return Task.FromResult(result);
    }
}
```

**Notes**:

- Implement multi-level cache-aside pattern with memory and distributed cache layers for optimal performance
- Use proper concurrency control with semaphores to prevent cache stampede scenarios
- Implement stale-while-revalidate pattern for better user experience during cache updates
- Use bulk operations for efficient batch cache operations and reduced network overhead
- Implement comprehensive statistics tracking to monitor cache performance and hit ratios
- Use read-through cache pattern for simplified cache usage with default value factories
- Implement cache warming strategies for proactive loading of frequently accessed data
- Use proper error handling and fallback mechanisms for cache failures
- Implement cache coordination for distributed scenarios with multiple cache instances
- Use repository pattern integration for seamless cache management with data access layers
- Configure appropriate cache expiration policies based on data volatility and business requirements
- Implement proper key generation strategies to avoid cache key collisions
- Use JSON serialization for distributed cache storage with proper error handling for deserialization failures

**Prerequisites**:

- Understanding of cache-aside pattern and lazy loading concepts
- Knowledge of distributed caching and multi-level cache architectures  
- Familiarity with concurrent programming and synchronization primitives
- Experience with dependency injection and service registration patterns
- Understanding of repository pattern and data access layer abstractions
- Knowledge of JSON serialization and distributed system coordination

**Related Snippets**:

- [Distributed Cache](distributed-cache.md) - Advanced distributed caching implementations
- [Cache Invalidation](cache-invalidation.md) - Comprehensive cache invalidation strategies
- [Memoization](memoization.md) - In-memory function result caching patterns
