using System.Collections.Concurrent;

namespace CSharp.Memoization;

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