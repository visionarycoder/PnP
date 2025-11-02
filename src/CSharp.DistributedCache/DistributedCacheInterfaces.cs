using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace CSharp.DistributedCache;

/// <summary>
/// Advanced distributed cache interface with additional functionality
/// </summary>
public interface IAdvancedDistributedCache : IDistributedCache
{
    Task<T> GetAsync<T>(string key, CancellationToken token = default);
    Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions options = null, 
        CancellationToken token = default);
    Task<(bool found, T value)> TryGetAsync<T>(string key, CancellationToken token = default);
    Task<IDictionary<string, T>> GetManyAsync<T>(IEnumerable<string> keys, 
        CancellationToken token = default);
    Task SetManyAsync<T>(IDictionary<string, T> items, DistributedCacheEntryOptions options = null,
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

/// <summary>
/// Cache statistics interface
/// </summary>
public interface ICacheStatistics
{
    long HitCount { get; }
    long MissCount { get; }
    double HitRate { get; }
    long UsedMemory { get; }
    long MaxMemory { get; }
    int KeyCount { get; }
    DateTimeOffset CollectionTime { get; }
}

/// <summary>
/// Redis distributed cache options
/// </summary>
public class RedisDistributedCacheOptions
{
    public string KeyPrefix { get; set; } = "";
    public int DatabaseId { get; set; } = 0;
    public int MaxConcurrentOperations { get; set; } = 100;
    public TimeSpan DefaultExpiration { get; set; } = TimeSpan.FromMinutes(30);
    public bool EnableLogging { get; set; } = true;
    public string[] Tags { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Cache aside pattern options
/// </summary>
public class CacheAsideOptions
{
    public TimeSpan? Expiration { get; set; }
    public bool RefreshAhead { get; set; } = false;
    public TimeSpan RefreshWindow { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxConcurrentRefresh { get; set; } = 3;
    public string[] Tags { get; set; } = Array.Empty<string>();
    public bool UseWriteThrough { get; set; } = false;
    public bool UseWriteBehind { get; set; } = false;
    public TimeSpan WriteBehindDelay { get; set; } = TimeSpan.FromSeconds(5);
}

/// <summary>
/// Key generator interface for cache keys
/// </summary>
public interface IKeyGenerator<TKey>
{
    string GenerateKey(TKey key);
}

/// <summary>
/// Default string-based key generator
/// </summary>
public class DefaultKeyGenerator<TKey> : IKeyGenerator<TKey>
{
    public string GenerateKey(TKey key)
    {
        return key?.ToString() ?? throw new ArgumentNullException(nameof(key));
    }
}

/// <summary>
/// Cache statistics implementation
/// </summary>
public class CacheStatistics : ICacheStatistics
{
    public long HitCount { get; init; }
    public long MissCount { get; init; }
    public double HitRate { get; init; }
    public long UsedMemory { get; init; }
    public long MaxMemory { get; init; }
    public int KeyCount { get; init; }
    public DateTimeOffset CollectionTime { get; init; } = DateTimeOffset.UtcNow;
}