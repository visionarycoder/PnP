namespace CSharp.DistributedCache;

/// <summary>
/// Cache-aside pattern service interface
/// </summary>
public interface ICacheAsideService<TKey, TValue>
{
    Task<TValue> GetAsync(TKey key, Func<TKey, Task<TValue>> dataSource, 
        CacheAsideOptions? options = null, CancellationToken token = default);
    Task SetAsync(TKey key, TValue value, CacheAsideOptions? options = null, 
        CancellationToken token = default);
    Task RemoveAsync(TKey key, CancellationToken token = default);
    Task RefreshAsync(TKey key, Func<TKey, Task<TValue>> dataSource, 
        CancellationToken token = default);
    Task WarmupAsync(IEnumerable<TKey> keys, Func<TKey, Task<TValue>> dataSource,
        CacheAsideOptions? options = null, CancellationToken token = default);
}