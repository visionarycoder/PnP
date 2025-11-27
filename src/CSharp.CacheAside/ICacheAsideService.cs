namespace CSharp.CacheAside;

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