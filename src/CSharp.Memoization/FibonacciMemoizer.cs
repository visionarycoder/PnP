using System.Collections.Concurrent;

namespace CSharp.Memoization;

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