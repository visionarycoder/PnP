namespace Algorithm.DynamicProgramming;

/// <summary>
/// 0/1 Knapsack problem implementations.
/// </summary>
public static class KnapsackSolver
{
    /// <summary>
    /// Solves the 0/1 knapsack problem using dynamic programming.
    /// </summary>
    public static int Solve(KnapsackItem[] items, int capacity)
    {
        var n = items.Length;
        var dp = new int[n + 1, capacity + 1];
        
        for (var i = 1; i <= n; i++)
        {
            for (var w = 1; w <= capacity; w++)
            {
                var currentWeight = items[i - 1].Weight;
                var currentValue = items[i - 1].Value;
                
                if (currentWeight <= w)
                {
                    // Take the maximum of including or excluding current item
                    dp[i, w] = Math.Max(
                        dp[i - 1, w], // Exclude current item
                        dp[i - 1, w - currentWeight] + currentValue // Include current item
                    );
                }
                else
                {
                    // Cannot include current item
                    dp[i, w] = dp[i - 1, w];
                }
            }
        }
        
        return dp[n, capacity];
    }
    
    /// <summary>
    /// Space-optimized version of the 0/1 knapsack problem.
    /// </summary>
    public static int SolveOptimized(KnapsackItem[] items, int capacity)
    {
        var dp = new int[capacity + 1];
        
        foreach (var item in items)
        {
            // Traverse backwards to avoid using updated values
            for (var w = capacity; w >= item.Weight; w--)
            {
                dp[w] = Math.Max(dp[w], dp[w - item.Weight] + item.Value);
            }
        }
        
        return dp[capacity];
    }
}