namespace Algorithm.DynamicProgramming;

/// <summary>
/// Dynamic programming implementation of the Fibonacci sequence using multiple approaches.
/// </summary>
public static class FibonacciCalculator
{
    private static readonly Dictionary<int, long> FibMemo = new();
    
    /// <summary>
    /// Calculates Fibonacci number using memoization (top-down approach).
    /// </summary>
    public static long FibonacciMemo(int n)
    {
        if (n <= 1) return n;
        
        if (FibMemo.TryGetValue(n, out var cached))
            return cached;
        
        var result = FibonacciMemo(n - 1) + FibonacciMemo(n - 2);
        FibMemo[n] = result;
        return result;
    }
    
    /// <summary>
    /// Calculates Fibonacci number using tabulation (bottom-up approach).
    /// </summary>
    public static long FibonacciTabulated(int n)
    {
        if (n <= 1) return n;
        
        var dp = new long[n + 1];
        dp[0] = 0;
        dp[1] = 1;
        
        for (var i = 2; i <= n; i++)
        {
            dp[i] = dp[i - 1] + dp[i - 2];
        }
        
        return dp[n];
    }
    
    /// <summary>
    /// Space-optimized Fibonacci calculation using only three variables.
    /// </summary>
    public static long FibonacciOptimized(int n)
    {
        if (n <= 1) return n;
        
        long prev2 = 0, prev1 = 1, current = 0;
        
        for (var i = 2; i <= n; i++)
        {
            current = prev1 + prev2;
            prev2 = prev1;
            prev1 = current;
        }
        
        return current;
    }
    
    /// <summary>
    /// Clears the memoization cache.
    /// </summary>
    public static void ClearCache() => FibMemo.Clear();
}