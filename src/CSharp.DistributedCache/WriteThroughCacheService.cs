using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CSharp.DistributedCache;

/// <summary>
/// Write-through cache service
/// </summary>
public class WriteThroughCacheService<TKey, TValue> : ICacheAsideService<TKey, TValue>
{
    private readonly IAdvancedDistributedCache cache;
    private readonly IKeyGenerator<TKey> keyGenerator;
    private readonly IDataStore<TKey, TValue> dataStore;
    private readonly ILogger<WriteThroughCacheService<TKey, TValue>>? logger;

    public WriteThroughCacheService(
        IAdvancedDistributedCache cache,
        IDataStore<TKey, TValue> dataStore,
        IKeyGenerator<TKey>? keyGenerator = null,
        ILogger<WriteThroughCacheService<TKey, TValue>>? logger = null)
    {
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.dataStore = dataStore ?? throw new ArgumentNullException(nameof(dataStore));
        this.keyGenerator = keyGenerator ?? new DefaultKeyGenerator<TKey>();
        this.logger = logger;
    }

    public async Task<TValue> GetAsync(TKey key, Func<TKey, Task<TValue>> dataSource, 
        CacheAsideOptions? options = null, CancellationToken token = default)
    {
        var cacheKey = keyGenerator.GenerateKey(key);
        
        // Try cache first
        var (found, value) = await cache.TryGetAsync<TValue>(cacheKey, token).ConfigureAwait(false);
        
        if (found && value != null)
        {
            logger?.LogTrace("Cache hit for key {Key}", cacheKey);
            return value;
        }

        // Cache miss, get from data store
        logger?.LogTrace("Cache miss for key {Key}, fetching from data store", cacheKey);
        value = await dataStore.GetAsync(key, token).ConfigureAwait(false);
        
        if (value != null)
        {
            // Store in cache
            var cacheOptions = new DistributedCacheEntryOptions();
            if (options?.Expiration.HasValue == true)
            {
                cacheOptions.AbsoluteExpirationRelativeToNow = options.Expiration.Value;
            }
            
            await cache.SetAsync(cacheKey, value, cacheOptions, token).ConfigureAwait(false);
        }

        return value!;
    }

    public async Task SetAsync(TKey key, TValue value, CacheAsideOptions? options = null, 
        CancellationToken token = default)
    {
        // Write to data store first
        await dataStore.SetAsync(key, value, token).ConfigureAwait(false);
        
        // Then update cache
        var cacheKey = keyGenerator.GenerateKey(key);
        var cacheOptions = new DistributedCacheEntryOptions();
        
        if (options?.Expiration.HasValue == true)
        {
            cacheOptions.AbsoluteExpirationRelativeToNow = options.Expiration.Value;
        }
        
        await cache.SetAsync(cacheKey, value, cacheOptions, token).ConfigureAwait(false);
        logger?.LogTrace("Write-through completed for key {Key}", cacheKey);
    }

    public async Task RemoveAsync(TKey key, CancellationToken token = default)
    {
        // Remove from both cache and data store
        var cacheKey = keyGenerator.GenerateKey(key);
        
        await Task.WhenAll(
            cache.RemoveAsync(cacheKey, token),
            dataStore.RemoveAsync(key, token)
        ).ConfigureAwait(false);
        
        logger?.LogTrace("Removed from cache and data store for key {Key}", cacheKey);
    }

    public async Task RefreshAsync(TKey key, Func<TKey, Task<TValue>> dataSource, 
        CancellationToken token = default)
    {
        var value = await dataSource(key).ConfigureAwait(false);
        await SetAsync(key, value, null, token).ConfigureAwait(false);
    }

    public async Task WarmupAsync(IEnumerable<TKey> keys, Func<TKey, Task<TValue>> dataSource,
        CacheAsideOptions? options = null, CancellationToken token = default)
    {
        var keyList = keys.ToList();
        var tasks = keyList.Select(key => GetAsync(key, dataSource, options, token));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }
}