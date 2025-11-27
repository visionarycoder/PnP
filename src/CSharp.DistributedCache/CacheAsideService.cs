using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Caching.Distributed;

namespace CSharp.DistributedCache;

/// <summary>
/// Cache-aside pattern implementation with advanced features
/// </summary>
public class CacheAsideService<TKey, TValue> : ICacheAsideService<TKey, TValue>
{
    private readonly IAdvancedDistributedCache cache;
    private readonly IKeyGenerator<TKey> keyGenerator;
    private readonly CacheAsideOptions defaultOptions;
    private readonly ILogger<CacheAsideService<TKey, TValue>>? logger;
    private readonly SemaphoreSlim refreshSemaphore;

    public CacheAsideService(IAdvancedDistributedCache cache,
        IKeyGenerator<TKey>? keyGenerator = null,
        IOptions<CacheAsideOptions>? defaultOptions = null,
        ILogger<CacheAsideService<TKey, TValue>>? logger = null)
    {
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.keyGenerator = keyGenerator ?? new DefaultKeyGenerator<TKey>();
        this.defaultOptions = defaultOptions?.Value ?? new CacheAsideOptions();
        this.logger = logger;
        refreshSemaphore = new(this.defaultOptions.MaxConcurrentRefresh, 
            this.defaultOptions.MaxConcurrentRefresh);
    }

    public async Task<TValue> GetAsync(TKey key, Func<TKey, Task<TValue>> dataSource, 
        CacheAsideOptions? options = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        
        var cacheKey = keyGenerator.GenerateKey(key);
        var effectiveOptions = options ?? defaultOptions;

        // Try to get from cache first
        var (found, cachedValue) = await cache.TryGetAsync<CachedItem<TValue>>(cacheKey, token)
            .ConfigureAwait(false);

        if (found && cachedValue != null)
        {
            logger?.LogTrace("Cache hit for key {Key}", cacheKey);
            
            // Check if refresh ahead is needed
            if (effectiveOptions.RefreshAhead && ShouldRefreshAhead(cachedValue, effectiveOptions))
            {
                _ = Task.Run(async () => await RefreshInBackground(key, dataSource, cacheKey, effectiveOptions));
            }
            
            return cachedValue.Value;
        }

        logger?.LogTrace("Cache miss for key {Key}, fetching from data source", cacheKey);

        // Cache miss, get from data source
        var value = await dataSource(key).ConfigureAwait(false);
        
        // Store in cache
        await SetInternalAsync(cacheKey, value, effectiveOptions, token).ConfigureAwait(false);
        
        return value;
    }

    public async Task SetAsync(TKey key, TValue value, CacheAsideOptions? options = null, 
        CancellationToken token = default)
    {
        var cacheKey = keyGenerator.GenerateKey(key);
        var effectiveOptions = options ?? defaultOptions;
        
        await SetInternalAsync(cacheKey, value, effectiveOptions, token).ConfigureAwait(false);
    }

    public async Task RemoveAsync(TKey key, CancellationToken token = default)
    {
        var cacheKey = keyGenerator.GenerateKey(key);
        await cache.RemoveAsync(cacheKey, token).ConfigureAwait(false);
        logger?.LogTrace("Removed cache entry for key {Key}", cacheKey);
    }

    public async Task RefreshAsync(TKey key, Func<TKey, Task<TValue>> dataSource, 
        CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        
        var cacheKey = keyGenerator.GenerateKey(key);
        
        try
        {
            var value = await dataSource(key).ConfigureAwait(false);
            await SetInternalAsync(cacheKey, value, defaultOptions, token).ConfigureAwait(false);
            logger?.LogTrace("Refreshed cache entry for key {Key}", cacheKey);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error refreshing cache entry for key {Key}", cacheKey);
            throw;
        }
    }

    public async Task WarmupAsync(IEnumerable<TKey> keys, Func<TKey, Task<TValue>> dataSource,
        CacheAsideOptions? options = null, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(dataSource);
        
        var keyList = keys.ToList();
        var effectiveOptions = options ?? defaultOptions;
        
        logger?.LogInformation("Starting cache warmup for {Count} keys", keyList.Count);

        var tasks = keyList.Select(async key =>
        {
            try
            {
                var value = await dataSource(key).ConfigureAwait(false);
                var cacheKey = keyGenerator.GenerateKey(key);
                await SetInternalAsync(cacheKey, value, effectiveOptions, token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger?.LogWarning(ex, "Failed to warm up cache for key {Key}", key);
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
        logger?.LogInformation("Cache warmup completed for {Count} keys", keyList.Count);
    }

    private async Task SetInternalAsync(string cacheKey, TValue value, 
        CacheAsideOptions options, CancellationToken token)
    {
        var cachedItem = new CachedItem<TValue>
        {
            Value = value,
            CreatedAt = DateTimeOffset.UtcNow,
            Tags = options.Tags
        };

        var cacheOptions = new DistributedCacheEntryOptions();
        
        if (options.Expiration.HasValue)
        {
            cacheOptions.AbsoluteExpirationRelativeToNow = options.Expiration.Value;
        }

        await cache.SetAsync(cacheKey, cachedItem, cacheOptions, token).ConfigureAwait(false);
    }

    private bool ShouldRefreshAhead(CachedItem<TValue> cachedItem, CacheAsideOptions options)
    {
        if (!options.RefreshAhead) return false;
        
        var age = DateTimeOffset.UtcNow - cachedItem.CreatedAt;
        return age >= options.RefreshWindow;
    }

    private async Task RefreshInBackground(TKey key, Func<TKey, Task<TValue>> dataSource, 
        string cacheKey, CacheAsideOptions options)
    {
        if (!await refreshSemaphore.WaitAsync(0)) return; // Non-blocking, skip if too busy

        try
        {
            var value = await dataSource(key).ConfigureAwait(false);
            await SetInternalAsync(cacheKey, value, options, CancellationToken.None).ConfigureAwait(false);
            logger?.LogTrace("Background refresh completed for key {Key}", cacheKey);
        }
        catch (Exception ex)
        {
            logger?.LogWarning(ex, "Background refresh failed for key {Key}", cacheKey);
        }
        finally
        {
            refreshSemaphore.Release();
        }
    }
}