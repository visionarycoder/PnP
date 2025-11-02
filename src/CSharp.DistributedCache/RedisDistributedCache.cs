using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;

namespace CSharp.DistributedCache;

/// <summary>
/// Redis-based distributed cache implementation with advanced features
/// </summary>
public class RedisDistributedCache : IAdvancedDistributedCache, IDisposable
{
    private readonly IDatabase database;
    private readonly IConnectionMultiplexer connection;
    private readonly RedisDistributedCacheOptions options;
    private readonly JsonSerializerOptions jsonOptions;
    private readonly ILogger<RedisDistributedCache>? logger;
    private readonly SemaphoreSlim semaphore;
    private bool disposed = false;

    public RedisDistributedCache(IConnectionMultiplexer connection,
        IOptions<RedisDistributedCacheOptions>? options = null,
        ILogger<RedisDistributedCache>? logger = null)
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
        
        semaphore = new(this.options.MaxConcurrentOperations, 
            this.options.MaxConcurrentOperations);
    }

    public async Task<T> GetAsync<T>(string key, CancellationToken token = default)
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
            if (dataHash == default(HashEntry) || !dataHash.Value.HasValue)
            {
                logger?.LogWarning("Invalid cache entry structure for key {Key}", key);
                return default(T)!;
            }
            
            logger?.LogTrace("Cache hit for key {Key}", key);
            return JsonSerializer.Deserialize<T>(dataHash.Value!, jsonOptions)!;
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

    public async Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions? options = null,
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
            if (this.options.Tags?.Any() == true)
            {
                var tagsJson = JsonSerializer.Serialize(this.options.Tags);
                hash = hash.Append(new HashEntry("tags", tagsJson)).ToArray();
            }
            
            await database.HashSetAsync(redisKey, hash).ConfigureAwait(false);
            
            // Set expiration
            if (options?.AbsoluteExpiration.HasValue == true)
            {
                await database.KeyExpireAsync(redisKey, options.AbsoluteExpiration.Value.DateTime)
                    .ConfigureAwait(false);
            }
            else if (options?.AbsoluteExpirationRelativeToNow.HasValue == true)
            {
                await database.KeyExpireAsync(redisKey, options.AbsoluteExpirationRelativeToNow.Value)
                    .ConfigureAwait(false);
            }
            else if (options?.SlidingExpiration.HasValue == true)
            {
                await database.KeyExpireAsync(redisKey, options.SlidingExpiration.Value)
                    .ConfigureAwait(false);
            }
            else
            {
                await database.KeyExpireAsync(redisKey, this.options.DefaultExpiration)
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

    public async Task<(bool found, T value)> TryGetAsync<T>(string key, CancellationToken token = default)
    {
        try
        {
            var value = await GetAsync<T>(key, token).ConfigureAwait(false);
            return (!EqualityComparer<T>.Default.Equals(value, default(T)), value);
        }
        catch
        {
            return (false, default(T)!);
        }
    }

    public async Task<IDictionary<string, T>> GetManyAsync<T>(IEnumerable<string> keys,
        CancellationToken token = default)
    {
        var keyList = keys.ToList();
        var tasks = keyList.Select(async key =>
        {
            var value = await GetAsync<T>(key, token).ConfigureAwait(false);
            return new KeyValuePair<string, T>(key, value);
        });
        
        var results = await Task.WhenAll(tasks).ConfigureAwait(false);
        return results.Where(kvp => !EqualityComparer<T>.Default.Equals(kvp.Value, default(T)))
            .ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public async Task SetManyAsync<T>(IDictionary<string, T> items, 
        DistributedCacheEntryOptions? options = null, CancellationToken token = default)
    {
        var tasks = items.Select(kvp => SetAsync(kvp.Key, kvp.Value, options, token));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken token = default)
    {
        var keyList = keys.Select(PrepareKey).Select(k => (RedisKey)k).ToArray();
        await database.KeyDeleteAsync(keyList).ConfigureAwait(false);
    }

    public async Task RemoveByPatternAsync(string pattern, CancellationToken token = default)
    {
        var server = connection.GetServer(connection.GetEndPoints().First());
        var keys = server.Keys(database.Database, PrepareKey(pattern));
        await database.KeyDeleteAsync(keys.Select(k => (RedisKey)k).ToArray()).ConfigureAwait(false);
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
        var info = await server.InfoAsync("stats").ConfigureAwait(false);
        
        var stats = info.FirstOrDefault(g => g.Key == "Stats");
        if (stats == null) 
        {
            return new CacheStatistics();
        }
        
        var hits = ParseLong(stats.FirstOrDefault(kvp => kvp.Key == "keyspace_hits").Value);
        var misses = ParseLong(stats.FirstOrDefault(kvp => kvp.Key == "keyspace_misses").Value);
        var usedMemory = ParseLong(stats.FirstOrDefault(kvp => kvp.Key == "used_memory").Value);
        var maxMemory = ParseLong(stats.FirstOrDefault(kvp => kvp.Key == "maxmemory").Value);
        
        var total = hits + misses;
        var hitRate = total > 0 ? (double)hits / total : 0;

        return new CacheStatistics
        {
            HitCount = hits,
            MissCount = misses,
            HitRate = hitRate,
            UsedMemory = usedMemory,
            MaxMemory = maxMemory,
            KeyCount = (int)await server.DatabaseSizeAsync(database.Database).ConfigureAwait(false)
        };
    }

    public async Task InvalidateTagAsync(string tag, CancellationToken token = default)
    {
        var server = connection.GetServer(connection.GetEndPoints().First());
        var keys = server.Keys(database.Database, PrepareKey("*"));
        
        foreach (var key in keys)
        {
            var hash = await database.HashGetAllAsync(key).ConfigureAwait(false);
            var tagsHash = hash.FirstOrDefault(x => x.Name == "tags");
            
            if (tagsHash.Value.HasValue)
            {
                try
                {
                    var tags = JsonSerializer.Deserialize<string[]>(tagsHash.Value!, jsonOptions);
                    if (tags?.Contains(tag) == true)
                    {
                        await database.KeyDeleteAsync(key).ConfigureAwait(false);
                    }
                }
                catch (JsonException)
                {
                    // Invalid JSON, skip
                }
            }
        }
    }

    // IDistributedCache implementation
    public byte[]? Get(string key) => GetAsync(key).GetAwaiter().GetResult();

    public async Task<byte[]?> GetAsync(string key, CancellationToken token = default)
    {
        var result = await GetAsync<byte[]>(key, token).ConfigureAwait(false);
        return result;
    }

    public void Set(string key, byte[] value, DistributedCacheEntryOptions options)
    {
        SetAsync(key, value, options).GetAwaiter().GetResult();
    }

    public async Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, 
        CancellationToken token = default)
    {
        await SetAsync<byte[]>(key, value, options, token).ConfigureAwait(false);
    }

    public void Refresh(string key) => RefreshAsync(key).GetAwaiter().GetResult();

    public async Task RefreshAsync(string key, CancellationToken token = default)
    {
        if (await ExistsAsync(key, token).ConfigureAwait(false))
        {
            var ttl = await GetTtlAsync(key, token).ConfigureAwait(false);
            if (ttl.HasValue)
            {
                var redisKey = PrepareKey(key);
                await database.KeyExpireAsync(redisKey, ttl.Value).ConfigureAwait(false);
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

    // Helper methods
    private void ValidateKey(string key)
    {
        ArgumentException.ThrowIfNullOrEmpty(key, nameof(key));
    }

    private string PrepareKey(string key)
    {
        if (string.IsNullOrEmpty(options.KeyPrefix))
            return key;
        return $"{options.KeyPrefix}:{key}";
    }

    private static long ParseLong(string? value)
    {
        return long.TryParse(value, out var result) ? result : 0;
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