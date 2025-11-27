using System.Collections.Concurrent;

namespace CSharp.AsyncLazyLoading;

public class AsyncMemoizer<TKey, TValue>(Func<TKey, Task<TValue>> asyncFunc) where TKey : notnull
{
    private readonly Func<TKey, Task<TValue>> asyncFunc = asyncFunc ?? throw new ArgumentNullException(nameof(asyncFunc));
    private readonly ConcurrentDictionary<TKey, AsyncLazy<TValue>> cache = new();

    public Task<TValue> GetAsync(TKey key)
    {
        var lazy = cache.GetOrAdd(key, k => new AsyncLazy<TValue>(() => asyncFunc(k)));
        return lazy.Value;
    }

    public void Invalidate(TKey key)
    {
        cache.TryRemove(key, out _);
    }

    public void Clear()
    {
        cache.Clear();
    }

    public int CacheSize => cache.Count;
}