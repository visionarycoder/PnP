using Microsoft.Extensions.Caching.Distributed;

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