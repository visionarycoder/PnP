namespace CSharp.DistributedCache;

public class SimulatedDataStore<TKey, TValue> : IDataStore<TKey, TValue>
{
    private readonly Dictionary<TKey, TValue> store = new();

    public Task<TValue> GetAsync(TKey key, CancellationToken token = default)
    {
        store.TryGetValue(key, out var value);
        return Task.FromResult(value!);
    }

    public Task SetAsync(TKey key, TValue value, CancellationToken token = default)
    {
        store[key] = value;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(TKey key, CancellationToken token = default)
    {
        store.Remove(key);
        return Task.CompletedTask;
    }
}