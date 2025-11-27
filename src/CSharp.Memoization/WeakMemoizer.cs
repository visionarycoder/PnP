using System.Collections.Concurrent;

namespace CSharp.Memoization;

/// <summary>
/// Weak reference memoizer that allows garbage collection of cached values.
/// </summary>
/// <typeparam name="TKey">The type of cache keys.</typeparam>
/// <typeparam name="TResult">The type of cached results.</typeparam>
public class WeakMemoizer<TKey, TResult> : IDisposable where TKey : notnull where TResult : class
{
    private readonly ConcurrentDictionary<TKey, WeakReference<TResult>> cache = new();
    private readonly Timer cleanupTimer;

    public WeakMemoizer(TimeSpan cleanupInterval = default)
    {
        if (cleanupInterval == default)
            cleanupInterval = TimeSpan.FromMinutes(5);

        cleanupTimer = new Timer(CleanupDeadReferences, null, cleanupInterval, cleanupInterval);
    }

    /// <summary>
    /// Gets or computes a value using weak references for memory efficiency.
    /// </summary>
    /// <param name="key">The cache key.</param>
    /// <param name="factory">The factory function to create the value if not cached.</param>
    /// <returns>The cached or computed value.</returns>
    public TResult GetOrCompute(TKey key, Func<TKey, TResult> factory)
    {
        if (cache.TryGetValue(key, out var weakRef) && weakRef.TryGetTarget(out var cachedValue))
        {
            return cachedValue;
        }

        var newValue = factory(key);
        cache.AddOrUpdate(key, new WeakReference<TResult>(newValue), (k, v) => new WeakReference<TResult>(newValue));
        return newValue;
    }

    /// <summary>
    /// Removes dead weak references from the cache.
    /// </summary>
    private void CleanupDeadReferences(object? state)
    {
        var deadKeys = cache
            .Where(kvp => !kvp.Value.TryGetTarget(out _))
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in deadKeys)
        {
            cache.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Gets the current cache statistics.
    /// </summary>
    /// <returns>Statistics about alive and dead references.</returns>
    public (int AliveReferences, int DeadReferences) GetStatistics()
    {
        int alive = 0, dead = 0;

        foreach (var kvp in cache)
        {
            if (kvp.Value.TryGetTarget(out _))
                alive++;
            else
                dead++;
        }

        return (alive, dead);
    }

    public void Dispose()
    {
        cleanupTimer?.Dispose();
        cache.Clear();
    }
}