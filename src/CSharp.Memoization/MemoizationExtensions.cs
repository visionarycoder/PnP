using System.Collections.Concurrent;

namespace CSharp.Memoization;

/// <summary>
/// Extension methods for easy memoization of functions.
/// </summary>
public static class MemoizationExtensions
{
    /// <summary>
    /// Creates a memoized version of a function.
    /// </summary>
    /// <typeparam name="TArg">The type of the function argument.</typeparam>
    /// <typeparam name="TResult">The type of the function result.</typeparam>
    /// <param name="function">The function to memoize.</param>
    /// <param name="options">Optional memoization options.</param>
    /// <returns>A memoized version of the function.</returns>
    public static Func<TArg, TResult> Memoize<TArg, TResult>(
        this Func<TArg, TResult> function, 
        MemoizationOptions? options = null) where TArg : notnull
    {
        var memoizer = new Memoizer<TArg, TResult>(options);
        return arg => memoizer.GetOrCompute(arg, function);
    }

    /// <summary>
    /// Creates a memoized version of an async function.
    /// </summary>
    /// <typeparam name="TArg">The type of the function argument.</typeparam>
    /// <typeparam name="TResult">The type of the function result.</typeparam>
    /// <param name="function">The async function to memoize.</param>
    /// <param name="options">Optional memoization options.</param>
    /// <returns>A memoized version of the async function.</returns>
    public static Func<TArg, Task<TResult>> MemoizeAsync<TArg, TResult>(
        this Func<TArg, Task<TResult>> function, 
        MemoizationOptions? options = null) where TArg : notnull
    {
        var memoizer = new Memoizer<TArg, TResult>(options);
        return arg => memoizer.GetOrComputeAsync(arg, function);
    }
}

/// <summary>
/// Specialized memoizer for expensive mathematical computations.
/// </summary>
public static class FibonacciMemoizer
{
    private static readonly ConcurrentDictionary<long, long> cache = new();

    /// <summary>
    /// Computes Fibonacci numbers with memoization for optimal performance.
    /// </summary>
    /// <param name="n">The Fibonacci sequence position.</param>
    /// <returns>The Fibonacci number at position n.</returns>
    public static long Fibonacci(long n)
    {
        if (n <= 1) return n;

        return cache.GetOrAdd(n, key =>
        {
            return Fibonacci(key - 1) + Fibonacci(key - 2);
        });
    }

    /// <summary>
    /// Clears the Fibonacci cache.
    /// </summary>
    public static void ClearCache() => cache.Clear();

    /// <summary>
    /// Gets the current cache size.
    /// </summary>
    public static int CacheSize => cache.Count;
}

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

/// <summary>
/// Memoization decorator that can be applied to any object to cache method results.
/// </summary>
/// <typeparam name="T">The type of object to decorate.</typeparam>
public class MemoizingDecorator<T> : DisposeProxy where T : class
{
    private readonly T target;
    private readonly ConcurrentDictionary<string, object> methodCache = new();

    public MemoizingDecorator(T target)
    {
        this.target = target ?? throw new ArgumentNullException(nameof(target));
    }

    /// <summary>
    /// Executes a method with memoization based on method name and parameters.
    /// </summary>
    /// <typeparam name="TResult">The return type of the method.</typeparam>
    /// <param name="methodCall">The method to call on the target object.</param>
    /// <param name="methodName">The name of the method (for cache key generation).</param>
    /// <param name="parameters">The parameters passed to the method.</param>
    /// <returns>The cached or computed result.</returns>
    public TResult ExecuteWithMemoization<TResult>(
        Func<T, TResult> methodCall, 
        [System.Runtime.CompilerServices.CallerMemberName] string methodName = "",
        params object[] parameters)
    {
        var cacheKey = GenerateCacheKey(methodName, parameters);
        
        if (methodCache.TryGetValue(cacheKey, out var cachedResult) && cachedResult is TResult typedResult)
        {
            return typedResult;
        }

        var result = methodCall(target);
        methodCache.TryAdd(cacheKey, result!);
        return result;
    }

    /// <summary>
    /// Invalidates cached results for a specific method.
    /// </summary>
    /// <param name="methodName">The method name to invalidate.</param>
    public void InvalidateMethod(string methodName)
    {
        var keysToRemove = methodCache.Keys.Where(key => key.StartsWith(methodName + ":")).ToList();
        foreach (var key in keysToRemove)
        {
            methodCache.TryRemove(key, out _);
        }
    }

    /// <summary>
    /// Clears all cached method results.
    /// </summary>
    public void ClearCache()
    {
        methodCache.Clear();
    }

    private static string GenerateCacheKey(string methodName, object[] parameters)
    {
        if (parameters.Length == 0)
            return methodName;

        var paramString = string.Join(",", parameters.Select(p => p?.ToString() ?? "null"));
        return $"{methodName}:{paramString}";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            methodCache.Clear();
            if (target is IDisposable disposableTarget)
            {
                disposableTarget.Dispose();
            }
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Base class for disposable proxy objects.
/// </summary>
public abstract class DisposeProxy : IDisposable
{
    private bool disposed = false;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        disposed = true;
    }

    ~DisposeProxy()
    {
        Dispose(false);
    }
}