# Distributed Cache Patterns

**Description**: Comprehensive distributed caching patterns including Redis integration, cache-aside strategy, write-through/write-behind caching, cache warming, distributed cache invalidation, cache partitioning, and high-availability caching patterns for scalable applications.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.StackExchangeRedis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;

// Core distributed cache abstraction with advanced features
public interface IAdvancedDistributedCache : IDistributedCache
{
    Task&lt;T&gt; GetAsync&lt;T&gt;(string key, CancellationToken token = default);
    Task SetAsync&lt;T&gt;(string key, T value, DistributedCacheEntryOptions options = null, 
        CancellationToken token = default);
    Task<(bool found, T value)> TryGetAsync&lt;T&gt;(string key, CancellationToken token = default);
    Task<IDictionary<string, T>> GetManyAsync&lt;T&gt;(IEnumerable<string> keys, 
        CancellationToken token = default);
    Task SetManyAsync&lt;T&gt;(IDictionary<string, T> items, DistributedCacheEntryOptions options = null,
        CancellationToken token = default);
    Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken token = default);
    Task RemoveByPatternAsync(string pattern, CancellationToken token = default);
    Task<bool> ExistsAsync(string key, CancellationToken token = default);
    Task<TimeSpan?> GetTtlAsync(string key, CancellationToken token = default);
    Task<long> IncrementAsync(string key, long value = 1, CancellationToken token = default);
    Task<double> IncrementAsync(string key, double value, CancellationToken token = default);
    Task<ICacheStatistics> GetStatisticsAsync(CancellationToken token = default);
    Task InvalidateTagAsync(string tag, CancellationToken token = default);
}

// Redis-based distributed cache implementation
public class RedisDistributedCache : IAdvancedDistributedCache, IDisposable
{
    private readonly IDatabase database;
    private readonly IConnectionMultiplexer connection;
    private readonly RedisDistributedCacheOptions options;
    private readonly JsonSerializerOptions jsonOptions;
    private readonly ILogger logger;
    private readonly SemaphoreSlim semaphore;
    private bool disposed = false;

    public RedisDistributedCache(IConnectionMultiplexer connection,
        IOptions<RedisDistributedCacheOptions> options = null,
        ILogger<RedisDistributedCache> logger = null)
    {
        this.connection = connection ?? throw new ArgumentNullException(nameof(connection));
        this.options = options?.Value ?? new RedisDistributedCacheOptions();
        this.logger = logger;
        
        database = connection.GetDatabase(this.options.DatabaseId);
        
        jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };
        
        semaphore = new SemaphoreSlim(this.options.MaxConcurrentOperations, 
            this.options.MaxConcurrentOperations);
    }

    public async Task&lt;T&gt; GetAsync&lt;T&gt;(string key, CancellationToken token = default)
    {
        ValidateKey(key);
        
        await semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            var redisKey = PrepareKey(key);
            var value = await database.HashGetAllAsync(redisKey).ConfigureAwait(false);
            
            if (value.Length == 0)
            {
                logger?.LogTrace("Cache miss for key {Key}", key);
                return default(T);
            }
            
            var dataHash = value.FirstOrDefault(x => x.Name == "data");
            if (!dataHash.HasValue)
            {
                logger?.LogWarning("Invalid cache entry structure for key {Key}", key);
                return default(T);
            }
            
            logger?.LogTrace("Cache hit for key {Key}", key);
            return JsonSerializer.Deserialize&lt;T&gt;(dataHash.Value, jsonOptions);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error getting cache value for key {Key}", key);
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task SetAsync&lt;T&gt;(string key, T value, DistributedCacheEntryOptions options = null,
        CancellationToken token = default)
    {
        ValidateKey(key);
        
        await semaphore.WaitAsync(token).ConfigureAwait(false);
        try
        {
            var redisKey = PrepareKey(key);
            var serializedValue = JsonSerializer.Serialize(value, jsonOptions);
            var createdAt = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            
            var hash = new HashEntry[]
            {
                new("data", serializedValue),
                new("type", typeof(T).AssemblyQualifiedName),
                new("created", createdAt),
                new("version", "1.0")
            };
            
            // Add tags if specified
            if (options?.Tags?.Any() == true)
            {
                var tagsJson = JsonSerializer.Serialize(options.Tags);
                hash = hash.Append(new HashEntry("tags", tagsJson)).ToArray();
            }
            
            await database.HashSetAsync(redisKey, hash).ConfigureAwait(false);
            
            // Set expiration
            if (options?.AbsoluteExpiration.HasValue == true)
            {
                await database.ExpireAtAsync(redisKey, options.AbsoluteExpiration.Value.DateTime)
                    .ConfigureAwait(false);
            }
            else if (options?.AbsoluteExpirationRelativeToNow.HasValue == true)
            {
                await database.ExpireAsync(redisKey, options.AbsoluteExpirationRelativeToNow.Value)
                    .ConfigureAwait(false);
            }
            else if (options?.SlidingExpiration.HasValue == true)
            {
                await database.ExpireAsync(redisKey, options.SlidingExpiration.Value)
                    .ConfigureAwait(false);
            }
            
            logger?.LogTrace("Set cache value for key {Key}", key);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error setting cache value for key {Key}", key);
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task<(bool found, T value)> TryGetAsync&lt;T&gt;(string key, CancellationToken token = default)
    {
        try
        {
            var value = await GetAsync&lt;T&gt;(key, token).ConfigureAwait(false);
            return (!EqualityComparer&lt;T&gt;.Default.Equals(value, default(T)), value);
        }
        catch
        {
            return (false, default(T));
        }
    }

    public async Task<IDictionary<string, T>> GetManyAsync&lt;T&gt;(IEnumerable<string> keys,
        CancellationToken token = default)
    {
        var keyList = keys.ToList();
        var tasks = keyList.Select(async key =>
        {
            var value = await GetAsync&lt;T&gt;(key, token).ConfigureAwait(false);
            return new KeyValuePair<string, T>(key, value);
        });
        
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.Where(kvp => !EqualityComparer&lt;T&gt;.Default.Equals(kvp.Value, default(T)))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public async Task SetManyAsync&lt;T&gt;(IDictionary<string, T> items, 
        DistributedCacheEntryOptions options = null, CancellationToken token = default)
    {
        var tasks = items.Select(kvp => SetAsync(kvp.Key, kvp.Value, options, token));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken token = default)
    {
        var redisKeys = keys.Select(PrepareKey).ToArray();
        await database.KeyDeleteAsync(redisKeys).ConfigureAwait(false);
        
        logger?.LogTrace("Removed {Count} cache entries", redisKeys.Length);
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken token = default)
    {
        var server = connection.GetServer(connection.GetEndPoints().First());
        var keys = server.Keys(database.Database, PrepareKey(pattern));
        
        await database.KeyDeleteAsync(keys.ToArray()).ConfigureAwait(false);
        
        logger?.LogTrace("Removed cache entries matching pattern {Pattern}", pattern);
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken token = default)
    {
        ValidateKey(key);
        var redisKey = PrepareKey(key);
        return await database.KeyExistsAsync(redisKey).ConfigureAwait(false);
    }

    public async Task<TimeSpan?> GetTtlAsync(string key, CancellationToken token = default)
    {
        ValidateKey(key);
        var redisKey = PrepareKey(key);
        return await database.KeyTimeToLiveAsync(redisKey).ConfigureAwait(false);
    }

    public async Task<long> IncrementAsync(string key, long value = 1, CancellationToken token = default)
    {
        ValidateKey(key);
        var redisKey = PrepareKey(key);
        return await database.StringIncrementAsync(redisKey, value).ConfigureAwait(false);
    }

    public async Task<double> IncrementAsync(string key, double value, CancellationToken token = default)
    {
        ValidateKey(key);
        var redisKey = PrepareKey(key);
        return await database.StringIncrementAsync(redisKey, value).ConfigureAwait(false);
    }

    public async Task<ICacheStatistics> GetStatisticsAsync(CancellationToken token = default)
    {
        var server = connection.GetServer(connection.GetEndPoints().First());
        var info = await server.InfoAsync("memory").ConfigureAwait(false);
        
        return new CacheStatistics
        {
            TotalMemoryUsage = ParseMemoryInfo(info, "used_memory"),
            KeyCount = await database.ExecuteAsync("DBSIZE").ConfigureAwait(false),
            HitRate = CalculateHitRate(info),
            LastUpdated = DateTime.UtcNow
        };
    }

    public async Task InvalidateTagAsync(string tag, CancellationToken token = default)
    {
        var server = connection.GetServer(connection.GetEndPoints().First());
        var pattern = PrepareKey("*");
        
        var keysWithTag = new List<RedisKey>();
        
        await foreach (var key in server.KeysAsync(database.Database, pattern))
        {
            var tags = await database.HashGetAsync(key, "tags").ConfigureAwait(false);
            if (tags.HasValue)
            {
                var tagList = JsonSerializer.Deserialize<string[]>(tags);
                if (tagList?.Contains(tag) == true)
                {
                    keysWithTag.Add(key);
                }
            }
        }
        
        if (keysWithTag.Count > 0)
        {
            await database.KeyDeleteAsync(keysWithTag.ToArray()).ConfigureAwait(false);
            logger?.LogInformation("Invalidated {Count} cache entries with tag {Tag}", 
                keysWithTag.Count, tag);
        }
    }

    // IDistributedCache implementation
    public byte[] Get(string key) => GetAsync(key).GetAwaiter().GetResult();
    public Task<byte[]> GetAsync(string key, CancellationToken token = default) =>
        GetAsync<byte[]>(key, token);
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) =>
        SetAsync(key, value, options).GetAwaiter().GetResult();
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, 
        CancellationToken token = default) => SetAsync<byte[]>(key, value, options, token);
    public void Refresh(string key) => RefreshAsync(key).GetAwaiter().GetResult();
    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        if (await ExistsAsync(key, token).ConfigureAwait(false))
        {
            var ttl = await GetTtlAsync(key, token).ConfigureAwait(false);
            if (ttl.HasValue)
            {
                var redisKey = PrepareKey(key);
                await database.ExpireAsync(redisKey, ttl.Value).ConfigureAwait(false);
            }
        }
    }
    public void Remove(string key) => RemoveAsync(key).GetAwaiter().GetResult();
    public async Task RemoveAsync(string key, CancellationToken token = default)
    {
        ValidateKey(key);
        var redisKey = PrepareKey(key);
        await database.KeyDeleteAsync(redisKey).ConfigureAwait(false);
        logger?.LogTrace("Removed cache entry for key {Key}", key);
    }

    private void ValidateKey(string key)
    {
        if (string.IsNullOrEmpty(key))
            throw new ArgumentException("Cache key cannot be null or empty", nameof(key));
    }

    private string PrepareKey(string key)
    {
        if (string.IsNullOrEmpty(options.KeyPrefix))
            return key;
        return $"{options.KeyPrefix}:{key}";
    }

    private long ParseMemoryInfo(IGrouping<string, KeyValuePair<string, string>>[] info, string key)
    {
        var memoryInfo = info.FirstOrDefault(g => g.Key == "Memory");
        var memoryEntry = memoryInfo?.FirstOrDefault(kvp => kvp.Key == key);
        return long.TryParse(memoryEntry?.Value, out var value) ? value : 0;
    }

    private double CalculateHitRate(IGrouping<string, KeyValuePair<string, string>>[] info)
    {
        var stats = info.FirstOrDefault(g => g.Key == "Stats");
        if (stats == null) return 0;
        
        var hits = stats.FirstOrDefault(kvp => kvp.Key == "keyspace_hits").Value;
        var misses = stats.FirstOrDefault(kvp => kvp.Key == "keyspace_misses").Value;
        
        if (long.TryParse(hits, out var hitCount) && long.TryParse(misses, out var missCount))
        {
            var total = hitCount + missCount;
            return total > 0 ? (double)hitCount / total : 0;
        }
        
        return 0;
    }

    public void Dispose()
    {
        if (!disposed)
        {
            semaphore?.Dispose();
            disposed = true;
        }
    }
}

// Cache-aside pattern implementation
public interface ICacheAsideService<TKey, TValue>
{
    Task<TValue> GetAsync(TKey key, Func<TKey, Task<TValue>> dataSource, 
        CacheAsideOptions options = null, CancellationToken token = default);
    Task SetAsync(TKey key, TValue value, CacheAsideOptions options = null, 
        CancellationToken token = default);
    Task RemoveAsync(TKey key, CancellationToken token = default);
    Task RefreshAsync(TKey key, Func<TKey, Task<TValue>> dataSource, 
        CancellationToken token = default);
    Task WarmupAsync(IEnumerable<TKey> keys, Func<TKey, Task<TValue>> dataSource,
        CacheAsideOptions options = null, CancellationToken token = default);
}

public class CacheAsideService<TKey, TValue> : ICacheAsideService<TKey, TValue>
{
    private readonly IAdvancedDistributedCache cache;
    private readonly IKeyGenerator<TKey> keyGenerator;
    private readonly CacheAsideOptions defaultOptions;
    private readonly ILogger logger;
    private readonly SemaphoreSlim semaphore;

    public CacheAsideService(IAdvancedDistributedCache cache,
        IKeyGenerator<TKey> keyGenerator = null,
        IOptions<CacheAsideOptions> defaultOptions = null,
        ILogger<CacheAsideService<TKey, TValue>> logger = null)
    {
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.keyGenerator = keyGenerator ?? new DefaultKeyGenerator<TKey>();
        this.defaultOptions = defaultOptions?.Value ?? new CacheAsideOptions();
        this.logger = logger;
        this.semaphore = new SemaphoreSlim(this.defaultOptions.MaxConcurrentOperations,
            this.defaultOptions.MaxConcurrentOperations);
    }

    public async Task<TValue> GetAsync(TKey key, Func<TKey, Task<TValue>> dataSource,
        CacheAsideOptions options = null, CancellationToken token = default)
    {
        var effectiveOptions = options ?? defaultOptions;
        var cacheKey = keyGenerator.GenerateKey(key);
        
        // Try to get from cache first
        var (found, cachedValue) = await cache.TryGetAsync<TValue>(cacheKey, token).ConfigureAwait(false);
        if (found)
        {
            logger?.LogTrace("Cache hit for key {Key}", cacheKey);
            return cachedValue;
        }
        }
        }

        // Cache miss - get from data source with locking
        await semaphore.WaitAsync(token).ConfigureAwait(false);
        try
            var (foundAfterLock, cachedValue) = await cache.TryGetAsync<TValue>(cacheKey, token).ConfigureAwait(false);
            if (foundAfterLock)
            {
                logger?.LogTrace("Cache hit after lock for key {Key}", cacheKey);
                return cachedValue;
            }
                return cachedValue;
            }
                return cachedValue;
            }

            logger?.LogTrace("Cache miss for key {Key}, loading from data source", cacheKey);
            
            // Load from data source
            var value = await dataSource(key).ConfigureAwait(false);
            
            if (value != null || effectiveOptions.CacheNullValues)
            {
                var cacheOptions = CreateCacheOptions(effectiveOptions);
                await cache.SetAsync(cacheKey, value, cacheOptions, token).ConfigureAwait(false);
                logger?.LogTrace("Cached value for key {Key}", cacheKey);
            }
            
            return value;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error loading data for key {Key}", cacheKey);
                var (foundStale, staleValue) = await cache.TryGetAsync<TValue>(cacheKey, token).ConfigureAwait(false);
                if (foundStale)
                {
                    logger?.LogWarning("Returning stale cache value for key {Key} due to error", cacheKey);
                    return staleValue;
                }
                    logger?.LogWarning("Returning stale cache value for key {Key} due to error", cacheKey);
                    return staleValue;
                }
                    logger?.LogWarning("Returning stale cache value for key {Key} due to error", cacheKey);
                    return staleValue;
                }
            }
            
            throw;
        }
        finally
        {
            semaphore.Release();
        }
    }

    public async Task SetAsync(TKey key, TValue value, CacheAsideOptions options = null,
        CancellationToken token = default)
    {
        var effectiveOptions = options ?? defaultOptions;
        var cacheKey = keyGenerator.GenerateKey(key);
        var cacheOptions = CreateCacheOptions(effectiveOptions);
        
        await cache.SetAsync(cacheKey, value, cacheOptions, token).ConfigureAwait(false);
        logger?.LogTrace("Set cache value for key {Key}", cacheKey);
    }

    public async Task RemoveAsync(TKey key, CancellationToken token = default)
    {
        var cacheKey = keyGenerator.GenerateKey(key);
        await cache.RemoveAsync(cacheKey, token).ConfigureAwait(false);
        logger?.LogTrace("Removed cache value for key {Key}", cacheKey);
    }

    public async Task RefreshAsync(TKey key, Func<TKey, Task<TValue>> dataSource,
        CancellationToken token = default)
    {
        var cacheKey = keyGenerator.GenerateKey(key);
        
        try
        {
            var value = await dataSource(key).ConfigureAwait(false);
            var cacheOptions = CreateCacheOptions(defaultOptions);
            
            await cache.SetAsync(cacheKey, value, cacheOptions, token).ConfigureAwait(false);
            logger?.LogTrace("Refreshed cache value for key {Key}", cacheKey);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error refreshing cache for key {Key}", cacheKey);
            throw;
        }
    }

    public async Task WarmupAsync(IEnumerable<TKey> keys, Func<TKey, Task<TValue>> dataSource,
        CacheAsideOptions options = null, CancellationToken token = default)
    {
        var effectiveOptions = options ?? defaultOptions;
        var keyList = keys.ToList();
        
        logger?.LogInformation("Starting cache warmup for {Count} keys", keyList.Count);
        
        var semaphoreSlim = new SemaphoreSlim(effectiveOptions.MaxConcurrentOperations,
            effectiveOptions.MaxConcurrentOperations);
        
        var tasks = keyList.Select(async key =>
        {
            await semaphoreSlim.WaitAsync(token).ConfigureAwait(false);
            try
            {
                var cacheKey = keyGenerator.GenerateKey(key);
                
                // Skip if already cached
                if (await cache.ExistsAsync(cacheKey, token).ConfigureAwait(false))
                {
                    return;
                }
                
                var value = await dataSource(key).ConfigureAwait(false);
                var cacheOptions = CreateCacheOptions(effectiveOptions);
                
                await cache.SetAsync(cacheKey, value, cacheOptions, token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to warm up cache for key {Key}", key);
            }
            finally
            {
                semaphoreSlim.Release();
            }
        });
        
        await Task.WhenAll(tasks).ConfigureAwait(false);
        semaphoreSlim.Dispose();
        
        logger?.LogInformation("Completed cache warmup");
    }

    private DistributedCacheEntryOptions CreateCacheOptions(CacheAsideOptions options)
    {
        var cacheOptions = new DistributedCacheEntryOptions();
        
        if (options.AbsoluteExpiration.HasValue)
            cacheOptions.AbsoluteExpiration = options.AbsoluteExpiration;
        if (options.SlidingExpiration.HasValue)
            cacheOptions.SlidingExpiration = options.SlidingExpiration;
        if (options.Tags?.Any() == true)
            cacheOptions.Tags = options.Tags;
        
        return cacheOptions;
    }
}

// Write-through cache pattern
public interface IWriteThroughCache<TKey, TValue>
{
    Task<TValue> GetAsync(TKey key, CancellationToken token = default);
    Task SetAsync(TKey key, TValue value, CancellationToken token = default);
    Task RemoveAsync(TKey key, CancellationToken token = default);
    Task<bool> ExistsAsync(TKey key, CancellationToken token = default);
}

public class WriteThroughCache<TKey, TValue> : IWriteThroughCache<TKey, TValue>
{
    private readonly IAdvancedDistributedCache cache;
    private readonly IDataStore<TKey, TValue> dataStore;
    private readonly IKeyGenerator<TKey> keyGenerator;
    private readonly WriteThroughOptions options;
    private readonly ILogger logger;

    public WriteThroughCache(IAdvancedDistributedCache cache,
        IDataStore<TKey, TValue> dataStore,
        IKeyGenerator<TKey> keyGenerator = null,
        IOptions<WriteThroughOptions> options = null,
        ILogger<WriteThroughCache<TKey, TValue>> logger = null)
    {
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
        this.keyGenerator = keyGenerator ?? new DefaultKeyGenerator<TKey>();
        this.options = options?.Value ?? new WriteThroughOptions();
        this.logger = logger;
    }

    public async Task<TValue> GetAsync(TKey key, CancellationToken token = default)
    {
        var cacheKey = keyGenerator.GenerateKey(key);
        
        // Try cache first
        if (await cache.TryGetAsync<TValue>(cacheKey, out var cachedValue, token).ConfigureAwait(false))
        {
            logger?.LogTrace("Cache hit for key {Key}", cacheKey);
            return cachedValue;
        }
        
        // Load from data store
        logger?.LogTrace("Cache miss for key {Key}, loading from data store", cacheKey);
        var value = await dataStore.GetAsync(key, token).ConfigureAwait(false);
        
        // Cache the value if not null or if caching null values
        if (value != null || options.CacheNullValues)
        {
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = options.DefaultExpiration
            };
            
            await cache.SetAsync(cacheKey, value, cacheOptions, token).ConfigureAwait(false);
            logger?.LogTrace("Cached value for key {Key}", cacheKey);
        }
        
        return value;
    }

    public async Task SetAsync(TKey key, TValue value, CancellationToken token = default)
    {
        var cacheKey = keyGenerator.GenerateKey(key);
        
        // Write to data store first
        await dataStore.SetAsync(key, value, token).ConfigureAwait(false);
        logger?.LogTrace("Wrote value to data store for key {Key}", key);
        
        // Then update cache
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = options.DefaultExpiration
        };
        
        await cache.SetAsync(cacheKey, value, cacheOptions, token).ConfigureAwait(false);
        logger?.LogTrace("Updated cache for key {Key}", cacheKey);
    }

    public async Task RemoveAsync(TKey key, CancellationToken token = default)
    {
        var cacheKey = keyGenerator.GenerateKey(key);
        
        // Remove from data store first
        await dataStore.RemoveAsync(key, token).ConfigureAwait(false);
        logger?.LogTrace("Removed value from data store for key {Key}", key);
        
        // Then remove from cache
        await cache.RemoveAsync(cacheKey, token).ConfigureAwait(false);
        logger?.LogTrace("Removed value from cache for key {Key}", cacheKey);
    }

    public async Task<bool> ExistsAsync(TKey key, CancellationToken token = default)
    {
        var cacheKey = keyGenerator.GenerateKey(key);
        
        // Check cache first
        if (await cache.ExistsAsync(cacheKey, token).ConfigureAwait(false))
        {
            return true;
        }
        
        // Check data store
        return await dataStore.ExistsAsync(key, token).ConfigureAwait(false);
    }
}

// Write-behind (write-back) cache pattern
public interface IWriteBehindCache<TKey, TValue>
{
    Task<TValue> GetAsync(TKey key, CancellationToken token = default);
    Task SetAsync(TKey key, TValue value, CancellationToken token = default);
    Task RemoveAsync(TKey key, CancellationToken token = default);
    Task FlushAsync(CancellationToken token = default);
    Task<IWriteBehindStatistics> GetStatisticsAsync(CancellationToken token = default);
}

public class WriteBehindCache<TKey, TValue> : IWriteBehindCache<TKey, TValue>, IDisposable
{
    private readonly IAdvancedDistributedCache cache;
    private readonly IDataStore<TKey, TValue> dataStore;
    private readonly IKeyGenerator<TKey> keyGenerator;
    private readonly WriteBehindOptions options;
    private readonly ILogger logger;
    private readonly ConcurrentQueue<WriteOperation<TKey, TValue>> writeQueue;
    private readonly Timer flushTimer;
    private readonly SemaphoreSlim flushSemaphore;
    private long pendingWrites = 0;
    private long totalWrites = 0;
    private long failedWrites = 0;
    private bool disposed = false;

    public WriteBehindCache(IAdvancedDistributedCache cache,
        IDataStore<TKey, TValue> dataStore,
        IKeyGenerator<TKey> keyGenerator = null,
        IOptions<WriteBehindOptions> options = null,
        ILogger<WriteBehindCache<TKey, TValue>> logger = null)
    {
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
        this.keyGenerator = keyGenerator ?? new DefaultKeyGenerator<TKey>();
        this.options = options?.Value ?? new WriteBehindOptions();
        this.logger = logger;
        
        writeQueue = new ConcurrentQueue<WriteOperation<TKey, TValue>>();
        flushSemaphore = new SemaphoreSlim(1, 1);
        
        // Start periodic flush timer
        flushTimer = new Timer(async _ => await FlushAsync().ConfigureAwait(false),
            null, this.options.FlushInterval, this.options.FlushInterval);
    }

    public async Task<TValue> GetAsync(TKey key, CancellationToken token = default)
    {
        var cacheKey = keyGenerator.GenerateKey(key);
        
        // Try cache first
        if (await cache.TryGetAsync<TValue>(cacheKey, out var cachedValue, token).ConfigureAwait(false))
        {
            logger?.LogTrace("Cache hit for key {Key}", cacheKey);
            return cachedValue;
        }
        
        // Load from data store
        logger?.LogTrace("Cache miss for key {Key}, loading from data store", cacheKey);
        var value = await dataStore.GetAsync(key, token).ConfigureAwait(false);
        
        // Cache the value
        if (value != null || options.CacheNullValues)
        {
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = options.DefaultExpiration
            };
            
            await cache.SetAsync(cacheKey, value, cacheOptions, token).ConfigureAwait(false);
            logger?.LogTrace("Cached value for key {Key}", cacheKey);
        }
        
        return value;
    }

    public async Task SetAsync(TKey key, TValue value, CancellationToken token = default)
    {
        var cacheKey = keyGenerator.GenerateKey(key);
        
        // Update cache immediately
        var cacheOptions = new DistributedCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = options.DefaultExpiration
        };
        
        await cache.SetAsync(cacheKey, value, cacheOptions, token).ConfigureAwait(false);
        logger?.LogTrace("Updated cache for key {Key}", cacheKey);
        
        // Queue write operation for later
        var writeOperation = new WriteOperation<TKey, TValue>
        {
            Key = key,
            Value = value,
            Operation = WriteOperationType.Set,
            QueuedAt = DateTime.UtcNow
        };
        
        writeQueue.Enqueue(writeOperation);
        Interlocked.Increment(ref pendingWrites);
        
        // Flush immediately if queue is full
        if (writeQueue.Count >= options.MaxQueueSize)
        {
            _ = Task.Run(async () => await FlushAsync().ConfigureAwait(false), token);
        }
    }

    public async Task RemoveAsync(TKey key, CancellationToken token = default)
    {
        var cacheKey = keyGenerator.GenerateKey(key);
        
        // Remove from cache immediately
        await cache.RemoveAsync(cacheKey, token).ConfigureAwait(false);
        logger?.LogTrace("Removed value from cache for key {Key}", cacheKey);
        
        // Queue remove operation for later
        var writeOperation = new WriteOperation<TKey, TValue>
        {
            Key = key,
            Operation = WriteOperationType.Remove,
            QueuedAt = DateTime.UtcNow
        };
        
        writeQueue.Enqueue(writeOperation);
        Interlocked.Increment(ref pendingWrites);
    }

    public async Task FlushAsync(CancellationToken token = default)
    {
        if (!await flushSemaphore.WaitAsync(100, token).ConfigureAwait(false))
        {
            return; // Another flush is already in progress
        }
        
        try
        {
            var operations = new List<WriteOperation<TKey, TValue>>();
            
            // Dequeue operations
            while (writeQueue.TryDequeue(out var operation) && operations.Count < options.BatchSize)
            {
                operations.Add(operation);
            }
            
            if (operations.Count == 0)
            {
                return;
            }
            
            logger?.LogTrace("Flushing {Count} write operations", operations.Count);
            
            // Group operations by type
            var setOperations = operations.Where(op => op.Operation == WriteOperationType.Set).ToList();
            var removeOperations = operations.Where(op => op.Operation == WriteOperationType.Remove).ToList();
            
            // Execute batch operations
            var tasks = new List<Task>();
            
            if (setOperations.Count > 0)
            {
                tasks.Add(ExecuteBatchSet(setOperations, token));
            }
            
            if (removeOperations.Count > 0)
            {
                tasks.Add(ExecuteBatchRemove(removeOperations, token));
            }
            
            await Task.WhenAll(tasks).ConfigureAwait(false);
            
            Interlocked.Add(ref pendingWrites, -operations.Count);
            Interlocked.Add(ref totalWrites, operations.Count);
            
            logger?.LogTrace("Successfully flushed {Count} write operations", operations.Count);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref failedWrites);
            logger?.LogError(ex, "Error flushing write operations");
        }
        finally
        {
            flushSemaphore.Release();
        }
    }

    public Task<IWriteBehindStatistics> GetStatisticsAsync(CancellationToken token = default)
    {
        var statistics = new WriteBehindStatistics
        {
            PendingWrites = pendingWrites,
            TotalWrites = totalWrites,
            FailedWrites = failedWrites,
            QueueSize = writeQueue.Count
        };
        
        return Task.FromResult<IWriteBehindStatistics>(statistics);
    }

    private async Task ExecuteBatchSet(List<WriteOperation<TKey, TValue>> operations, 
        CancellationToken token)
    {
        try
        {
            var setTasks = operations.Select(async op =>
            {
                await dataStore.SetAsync(op.Key, op.Value, token).ConfigureAwait(false);
            });
            
            await Task.WhenAll(setTasks).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error executing batch set operations");
            throw;
        }
    }

    private async Task ExecuteBatchRemove(List<WriteOperation<TKey, TValue>> operations,
        CancellationToken token)
    {
        try
        {
            var removeTasks = operations.Select(async op =>
            {
                await dataStore.RemoveAsync(op.Key, token).ConfigureAwait(false);
            });
            
            await Task.WhenAll(removeTasks).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error executing batch remove operations");
            throw;
        }
    }

    public void Dispose()
    {
        if (!disposed)
        {
            flushTimer?.Dispose();
            
            // Final flush before disposing
            try
            {
                FlushAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error during final flush on dispose");
            }
            
            flushSemaphore?.Dispose();
            disposed = true;
        }
    }
}

// Supporting classes and interfaces
public interface IKeyGenerator<in T>
{
    string GenerateKey(T input);
}

public class DefaultKeyGenerator&lt;T&gt; : IKeyGenerator&lt;T&gt;
{
    public string GenerateKey(T input)
    {
        return input?.ToString() ?? "null";
    }
}

public interface IDataStore<TKey, TValue>
{
    Task<TValue> GetAsync(TKey key, CancellationToken token = default);
    Task SetAsync(TKey key, TValue value, CancellationToken token = default);
    Task RemoveAsync(TKey key, CancellationToken token = default);
    Task<bool> ExistsAsync(TKey key, CancellationToken token = default);
}

public class WriteOperation<TKey, TValue>
{
    public TKey Key { get; set; }
    public TValue Value { get; set; }
    public WriteOperationType Operation { get; set; }
    public DateTime QueuedAt { get; set; }
}

public enum WriteOperationType
{
    Set,
    Remove
}

// Options and configuration
public class RedisDistributedCacheOptions
{
    public int DatabaseId { get; set; } = 0;
    public string KeyPrefix { get; set; } = string.Empty;
    public int MaxConcurrentOperations { get; set; } = 100;
}

public class CacheAsideOptions
{
    public TimeSpan? AbsoluteExpiration { get; set; }
    public TimeSpan? SlidingExpiration { get; set; }
    public bool CacheNullValues { get; set; } = false;
    public bool ReturnStaleOnError { get; set; } = true;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public int MaxConcurrentOperations { get; set; } = 10;
}

public class WriteThroughOptions
{
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);
    public bool CacheNullValues { get; set; } = false;
}

public class WriteBehindOptions
{
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);
    public bool CacheNullValues { get; set; } = false;
    public TimeSpan FlushInterval { get; set; } = TimeSpan.FromSeconds(30);
    public int MaxQueueSize { get; set; } = 1000;
    public int BatchSize { get; set; } = 100;
}

// Statistics interfaces
public interface ICacheStatistics
{
    long TotalMemoryUsage { get; }
    long KeyCount { get; }
    double HitRate { get; }
    DateTime LastUpdated { get; }
}

public class CacheStatistics : ICacheStatistics
{
    public long TotalMemoryUsage { get; set; }
    public long KeyCount { get; set; }
    public double HitRate { get; set; }
    public DateTime LastUpdated { get; set; }
}

public interface IWriteBehindStatistics
{
    long PendingWrites { get; }
    long TotalWrites { get; }
    long FailedWrites { get; }
    int QueueSize { get; }
}

public class WriteBehindStatistics : IWriteBehindStatistics
{
    public long PendingWrites { get; set; }
    public long TotalWrites { get; set; }
    public long FailedWrites { get; set; }
    public int QueueSize { get; set; }
}

// Extension methods for IDistributedCache
public static class DistributedCacheExtensions
{
    // Extension methods for IDistributedCache can be added here if needed.
}
```

**Usage**:

```csharp
// Example 1: Redis Distributed Cache Setup
Console.WriteLine("Redis Distributed Cache Examples:");

// Configure Redis connection
var connectionString = "localhost:6379";
var connection = ConnectionMultiplexer.Connect(connectionString);

// Create Redis distributed cache
var redisOptions = Options.Create(new RedisDistributedCacheOptions
{
    DatabaseId = 0,
    KeyPrefix = "myapp",
    MaxConcurrentOperations = 50
});

using var redisCache = new RedisDistributedCache(connection, redisOptions);

// Basic operations
await redisCache.SetAsync("user:123", new User 
{ 
    Id = 123, 
    Name = "John Doe", 
    Email = "john@example.com" 
});

var user = await redisCache.GetAsync<User>("user:123");
Console.WriteLine($"Retrieved user: {user.Name} ({user.Email})");

// Bulk operations
var users = new Dictionary<string, User>
{
    ["user:124"] = new User { Id = 124, Name = "Jane Smith", Email = "jane@example.com" },
    ["user:125"] = new User { Id = 125, Name = "Bob Johnson", Email = "bob@example.com" }
};

await redisCache.SetManyAsync(users);
var retrievedUsers = await redisCache.GetManyAsync<User>(users.Keys);
Console.WriteLine($"Bulk retrieved {retrievedUsers.Count} users");

// Example 2: Cache-Aside Pattern
Console.WriteLine("\nCache-Aside Pattern Examples:");

var logger = LoggerFactory.Create(builder => builder.AddConsole())
    .CreateLogger<CacheAsideService<int, User>>();

var userRepository = new UserRepository(); // Implement IDataStore<int, User>
var keyGenerator = new DefaultKeyGenerator<int>();

var cacheAsideService = new CacheAsideService<int, User>(
    redisCache, 
    keyGenerator,
    Options.Create(new CacheAsideOptions
    {
        SlidingExpiration = TimeSpan.FromMinutes(10),
        CacheNullValues = false,
        ReturnStaleOnError = true,
        MaxConcurrentOperations = 20
    }),
    logger);

// Load with cache-aside pattern
Func<int, Task<User>> loadUser = async userId =>
{
    // Simulate database query
    await Task.Delay(100);
    return new User 
    { 
        Id = userId, 
        Name = $"User {userId}",
        Email = $"user{userId}@example.com"
    };
};

var sw = Stopwatch.StartNew();
var user1 = await cacheAsideService.GetAsync(123, loadUser);
sw.Stop();
Console.WriteLine($"First load: {user1.Name} in {sw.ElapsedMilliseconds}ms");

sw.Restart();
var user2 = await cacheAsideService.GetAsync(123, loadUser); // Should be cached
sw.Stop();
Console.WriteLine($"Second load: {user2.Name} in {sw.ElapsedMilliseconds}ms");

// Cache warming
var userIds = Enumerable.Range(200, 50).ToArray();
await cacheAsideService.WarmupAsync(userIds, loadUser);
Console.WriteLine($"Warmed up cache for {userIds.Length} users");

// Example 3: Write-Through Cache Pattern
Console.WriteLine("\nWrite-Through Cache Pattern Examples:");

var writeThroughCache = new WriteThroughCache<int, User>(
    redisCache,
    userRepository,
    keyGenerator,
    Options.Create(new WriteThroughOptions
    {
        DefaultExpiration = TimeSpan.FromMinutes(15),
        CacheNullValues = false
    }));

// Set operation (writes to both cache and data store)
var newUser = new User { Id = 301, Name = "Alice Brown", Email = "alice@example.com" };
await writeThroughCache.SetAsync(301, newUser);
Console.WriteLine($"Saved user {newUser.Name} via write-through");

// Get operation (cache-aside behavior)
var retrievedUser = await writeThroughCache.GetAsync(301);
Console.WriteLine($"Retrieved user: {retrievedUser.Name}");

// Example 4: Write-Behind Cache Pattern
Console.WriteLine("\nWrite-Behind Cache Pattern Examples:");

var writeBehindLogger = LoggerFactory.Create(builder => builder.AddConsole())
    .CreateLogger<WriteBehindCache<int, User>>();

using var writeBehindCache = new WriteBehindCache<int, User>(
    redisCache,
    userRepository,
    keyGenerator,
    Options.Create(new WriteBehindOptions
    {
        DefaultExpiration = TimeSpan.FromMinutes(20),
        FlushInterval = TimeSpan.FromSeconds(10),
        MaxQueueSize = 100,
        BatchSize = 50
    }),
    writeBehindLogger);

// Set operations (immediate cache update, queued data store writes)
for (int i = 400; i < 410; i++)
{
    var user = new User 
    { 
        Id = i, 
        Name = $"User {i}", 
        Email = $"user{i}@example.com" 
    };
    
    await writeBehindCache.SetAsync(i, user);
    Console.WriteLine($"Queued write for user {i}");
}

// Check statistics
var stats = await writeBehindCache.GetStatisticsAsync();
Console.WriteLine($"Write-behind stats: {stats.PendingWrites} pending, {stats.TotalWrites} total");

// Manual flush
await writeBehindCache.FlushAsync();
var statsAfterFlush = await writeBehindCache.GetStatisticsAsync();
Console.WriteLine($"After flush: {statsAfterFlush.PendingWrites} pending");

// Example 5: Advanced Cache Operations
Console.WriteLine("\nAdvanced Cache Operations:");

// Increment operations
await redisCache.IncrementAsync("counter:page-views", 1);
await redisCache.IncrementAsync("counter:page-views", 5);
var pageViews = await redisCache.IncrementAsync("counter:page-views", 0);
Console.WriteLine($"Total page views: {pageViews}");

// TTL operations
var cacheOptions = new DistributedCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5)
};

await redisCache.SetAsync("temp:session", "session-data", cacheOptions);
var ttl = await redisCache.GetTtlAsync("temp:session");
Console.WriteLine($"Session TTL: {ttl?.TotalSeconds:F0} seconds");

// Pattern-based removal
await redisCache.SetAsync("temp:user:123", "data1");
await redisCache.SetAsync("temp:user:124", "data2");
await redisCache.SetAsync("temp:product:456", "data3");

await redisCache.RemoveByPatternAsync("temp:user:*");
Console.WriteLine("Removed all temp user data");

// Example 6: Cache Statistics and Monitoring
Console.WriteLine("\nCache Statistics and Monitoring:");

var cacheStats = await redisCache.GetStatisticsAsync();
Console.WriteLine($"Cache Statistics:");
Console.WriteLine($"  Memory Usage: {cacheStats.TotalMemoryUsage:N0} bytes");
Console.WriteLine($"  Key Count: {cacheStats.KeyCount}");
Console.WriteLine($"  Hit Rate: {cacheStats.HitRate:P2}");
Console.WriteLine($"  Last Updated: {cacheStats.LastUpdated:yyyy-MM-dd HH:mm:ss}");

// Example 7: Tag-based Cache Invalidation
Console.WriteLine("\nTag-based Cache Invalidation:");

var taggedOptions = new DistributedCacheEntryOptions
{
    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
    Tags = new[] { "user-data", "profile" }
};

await redisCache.SetAsync("profile:123", new UserProfile 
{ 
    UserId = 123, 
    DisplayName = "John D.",
    Avatar = "avatar123.jpg"
}, taggedOptions);

await redisCache.SetAsync("profile:124", new UserProfile 
{ 
    UserId = 124, 
    DisplayName = "Jane S.",
    Avatar = "avatar124.jpg"
}, taggedOptions);

// Invalidate all entries with "user-data" tag
await redisCache.InvalidateTagAsync("user-data");
Console.WriteLine("Invalidated all user-data tagged entries");

// Example 8: Multi-Level Caching
Console.WriteLine("\nMulti-Level Caching Examples:");

var memoryCache = new MemoryCache(new MemoryCacheOptions
{
    SizeLimit = 1000
});

var multiLevelCache = new MultiLevelCache(memoryCache, redisCache);

// Data flows: Memory -> Redis -> Data Source
var productService = new ProductService(multiLevelCache);

var product = await productService.GetProductAsync(12345);
Console.WriteLine($"Product: {product.Name} - ${product.Price}");

// Second call should hit memory cache
var product2 = await productService.GetProductAsync(12345);
Console.WriteLine($"Cached product: {product2.Name}");

Console.WriteLine("\nDistributed cache patterns examples completed!");
```

**Helper classes for examples:**

```csharp
public class User
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Email { get; set; }
}

public class UserProfile
{
    public int UserId { get; set; }
    public string DisplayName { get; set; }
    public string Avatar { get; set; }
}

public class Product
{
    public int Id { get; set; }
    public string Name { get; set; }
    public decimal Price { get; set; }
    public string Category { get; set; }
}

public class UserRepository : IDataStore<int, User>
{
    private readonly Dictionary<int, User> users = new();

    public Task<User> GetAsync(int key, CancellationToken token = default)
    {
        users.TryGetValue(key, out var user);
        return Task.FromResult(user);
    }

    public Task SetAsync(int key, User value, CancellationToken token = default)
    {
        users[key] = value;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(int key, CancellationToken token = default)
    {
        users.Remove(key);
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(int key, CancellationToken token = default)
    {
        return Task.FromResult(users.ContainsKey(key));
    }
}

public class MultiLevelCache
{
    private readonly IMemoryCache memoryCache;
    private readonly IAdvancedDistributedCache distributedCache;

    public MultiLevelCache(IMemoryCache memoryCache, IAdvancedDistributedCache distributedCache)
    {
        this.memoryCache = memoryCache;
        this.distributedCache = distributedCache;
    }

    public async Task&lt;T&gt; GetAsync&lt;T&gt;(string key, Func<Task&lt;T&gt;> factory)
    {
        // Try memory cache first
        var (found, value) = await distributedCache.TryGetAsync&lt;T&gt;(key);
        if (found)
        {
            // Cache in memory for faster access
            memoryCache.Set(key, value, TimeSpan.FromMinutes(5));
            return value;
        }
            // Cache in memory for faster access
            memoryCache.Set(key, value, TimeSpan.FromMinutes(5));
            return value;
        }
            // Cache in memory for faster access
            memoryCache.Set(key, value, TimeSpan.FromMinutes(5));
            return value;
        }

        // Load from source
        value = await factory();
        
        // Cache at both levels
        memoryCache.Set(key, value, TimeSpan.FromMinutes(5));
        await distributedCache.SetAsync(key, value);
        
        return value;
    }
}

public class ProductService
{
    private readonly MultiLevelCache cache;

    public ProductService(MultiLevelCache cache)
    {
        this.cache = cache;
    }

    public async Task<Product> GetProductAsync(int productId)
    {
        return await cache.GetAsync($"product:{productId}", async () =>
        {
            // Simulate database call
            await Task.Delay(100);
            return new Product 
            { 
                Id = productId, 
                Name = $"Product {productId}",
                Price = Random.Shared.Next(10, 1000),
                Category = "Electronics"
            };
        });
    }
}
```

**Notes**:

- Use Redis for distributed caching in multi-server environments to share cache across application instances
- Implement cache-aside pattern for read-heavy workloads where you control when data is cached
- Use write-through caching when data consistency between cache and data store is critical
- Implement write-behind caching for write-heavy workloads to improve write performance
- Configure appropriate expiration times to balance data freshness with cache effectiveness
- Use bulk operations (GetMany/SetMany) to reduce network round trips for multiple keys
- Implement tag-based invalidation to efficiently invalidate related cache entries
- Monitor cache statistics (hit rate, memory usage) to optimize cache configuration and identify issues
- Use TTL operations to understand and control cache entry lifetimes
- Implement pattern-based operations for efficient bulk cache management
- Consider multi-level caching (memory + distributed) for optimal performance
- Use semaphores to control concurrency and prevent cache stampede scenarios
- Implement proper error handling and fallback strategies for cache failures
- Use JSON serialization for complex objects with proper configuration for performance

**Prerequisites**:

- Redis server installation and configuration knowledge
- Understanding of distributed systems and caching strategies  
- Familiarity with async/await patterns and concurrent programming
- Experience with dependency injection and service configuration
- Knowledge of serialization/deserialization techniques and performance implications
- Understanding of cache invalidation strategies and consistency models

**Related Snippets**:

- [Memoization](memoization.md) - In-memory function result caching patterns
- [Cache Invalidation](cache-invalidation.md) - Cache invalidation strategies and patterns
- [Cache-Aside Pattern](cache-aside.md) - Cache-aside and write-through cache implementations
