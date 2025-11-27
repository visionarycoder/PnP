namespace CSharp.DistributedCache;

/// <summary>
/// Data store interface for write-through operations
/// </summary>
public interface IDataStore<TKey, TValue>
{
    Task<TValue> GetAsync(TKey key, CancellationToken token = default);
    Task SetAsync(TKey key, TValue value, CancellationToken token = default);
    Task RemoveAsync(TKey key, CancellationToken token = default);
}