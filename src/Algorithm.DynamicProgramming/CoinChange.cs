namespace Algorithm.DynamicProgramming;

/// <summary>
/// Coin change problem implementations using dynamic programming.
/// </summary>
public static class CoinChange
{
    /// <summary>
    /// Finds the minimum number of coins needed to make the given amount.
    /// Returns -1 if it's impossible to make the amount.
    /// </summary>
    public static int MinCoins(int[] coins, int amount)
    {
        var dp = new int[amount + 1];
        Array.Fill(dp, amount + 1); // Initialize with impossible value
        dp[0] = 0; // Base case: 0 coins needed for amount 0
        
        for (var i = 1; i <= amount; i++)
        {
            foreach (var coin in coins)
            {
                if (coin <= i)
                {
                    dp[i] = Math.Min(dp[i], dp[i - coin] + 1);
                }
            }
        }
        
        return dp[amount] > amount ? -1 : dp[amount];
    }
    
    /// <summary>
    /// Counts the number of different ways to make the given amount using the available coins.
    /// </summary>
    public static int CountWays(int[] coins, int amount)
    {
        var dp = new int[amount + 1];
        dp[0] = 1; // One way to make amount 0: use no coins
        
        foreach (var coin in coins)
        {
            for (var i = coin; i <= amount; i++)
            {
                dp[i] += dp[i - coin];
            }
        }
        
        return dp[amount];
    }
    
    /// <summary>
    /// Gets the actual coins used to make the minimum change for the given amount.
    /// Returns null if it's impossible to make the amount.
    /// </summary>
    public static int[]? GetCoinsUsed(int[] coins, int amount)
    {
        var dp = new int[amount + 1];
        var coinUsed = new int[amount + 1];
        Array.Fill(dp, amount + 1);
        dp[0] = 0;
        
        for (var i = 1; i <= amount; i++)
        {
            foreach (var coin in coins)
            {
                if (coin <= i && dp[i - coin] + 1 < dp[i])
                {
                    dp[i] = dp[i - coin] + 1;
                    coinUsed[i] = coin;
                }
            }
        }
        
        if (dp[amount] > amount)
            return null; // Impossible to make the amount
        
        // Reconstruct the solution
        var result = new List<int>();
        var currentAmount = amount;
        
        while (currentAmount > 0)
        {
            var coin = coinUsed[currentAmount];
            result.Add(coin);
            currentAmount -= coin;
        }
        
        return result.ToArray();
    }
}