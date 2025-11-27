namespace CSharp.Memoization;

/// <summary>
/// Core memoization interface for caching function results.
/// </summary>
/// <typeparam name="TKey">The type of cache keys.</typeparam>
/// <typeparam name="TResult">The type of cached results.</typeparam>
public interface IMemoizer<TKey, TResult>
{
    TResult GetOrCompute(TKey key, Func<TKey, TResult> factory);
    Task<TResult> GetOrComputeAsync(TKey key, Func<TKey, Task<TResult>> factory);
    bool TryGetValue(TKey key, out TResult result);
    void Invalidate(TKey key);
    void InvalidateAll();
    void InvalidateWhere(Func<TKey, bool> predicate);
    MemoizationStatistics GetStatistics();
}