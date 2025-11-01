using Algorithm.DynamicProgramming;

namespace Algorithm.DynamicProgramming;

/// <summary>
/// Demonstrates dynamic programming algorithms with practical examples.
/// </summary>
internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("=== Dynamic Programming Algorithms Demo ===\n");
        
        DemonstrateFibonacci();
        Console.WriteLine();
        
        DemonstrateLCS();
        Console.WriteLine();
        
        DemonstrateKnapsack();
        Console.WriteLine();
        
        DemonstrateCoinChange();
    }
    
    private static void DemonstrateFibonacci()
    {
        Console.WriteLine("--- Fibonacci Sequence Demo ---");
        
        const int n = 20;
        
        // Clear cache for fair timing comparison
        FibonacciCalculator.ClearCache();
        
        var memoResult = FibonacciCalculator.FibonacciMemo(n);
        var tabulatedResult = FibonacciCalculator.FibonacciTabulated(n);
        var optimizedResult = FibonacciCalculator.FibonacciOptimized(n);
        
        Console.WriteLine($"Fibonacci({n}):");
        Console.WriteLine($"  Memoization: {memoResult}");
        Console.WriteLine($"  Tabulation: {tabulatedResult}");
        Console.WriteLine($"  Optimized: {optimizedResult}");
        
        // Show sequence
        Console.Write("First 10 Fibonacci numbers: ");
        for (var i = 0; i < 10; i++)
        {
            Console.Write($"{FibonacciCalculator.FibonacciOptimized(i)} ");
        }
        Console.WriteLine();
    }
    
    private static void DemonstrateLCS()
    {
        Console.WriteLine("--- Longest Common Subsequence Demo ---");
        
        const string text1 = "ABCDGH";
        const string text2 = "AEDFHR";
        
        var lcsLength = LongestCommonSubsequence.GetLength(text1, text2);
        var lcsSequence = LongestCommonSubsequence.GetSequence(text1, text2);
        
        Console.WriteLine($"Text 1: {text1}");
        Console.WriteLine($"Text 2: {text2}");
        Console.WriteLine($"LCS Length: {lcsLength}");
        Console.WriteLine($"LCS Sequence: {lcsSequence}");
        
        // Another example
        const string str1 = "programming";
        const string str2 = "algorithm";
        var length = LongestCommonSubsequence.GetLength(str1, str2);
        var sequence = LongestCommonSubsequence.GetSequence(str1, str2);
        
        Console.WriteLine($"\nText 1: {str1}");
        Console.WriteLine($"Text 2: {str2}");
        Console.WriteLine($"LCS Length: {length}");
        Console.WriteLine($"LCS Sequence: {sequence}");
    }
    
    private static void DemonstrateKnapsack()
    {
        Console.WriteLine("--- 0/1 Knapsack Problem Demo ---");
        
        var items = new KnapsackItem[]
        {
            new(10, 60),  // Weight: 10, Value: 60
            new(20, 100), // Weight: 20, Value: 100
            new(30, 120)  // Weight: 30, Value: 120
        };
        
        const int capacity = 50;
        
        var maxValue = KnapsackSolver.Solve(items, capacity);
        var maxValueOptimized = KnapsackSolver.SolveOptimized(items, capacity);
        
        Console.WriteLine("Items:");
        for (var i = 0; i < items.Length; i++)
        {
            Console.WriteLine($"  Item {i + 1}: {items[i]}");
        }
        
        Console.WriteLine($"Knapsack Capacity: {capacity}");
        Console.WriteLine($"Maximum Value (Standard): {maxValue}");
        Console.WriteLine($"Maximum Value (Optimized): {maxValueOptimized}");
    }
    
    private static void DemonstrateCoinChange()
    {
        Console.WriteLine("--- Coin Change Problem Demo ---");
        
        var coins = new[] { 1, 3, 4 };
        const int amount = 6;
        
        var minCoins = CoinChange.MinCoins(coins, amount);
        var ways = CoinChange.CountWays(coins, amount);
        var coinsUsed = CoinChange.GetCoinsUsed(coins, amount);
        
        Console.WriteLine($"Available coins: [{string.Join(", ", coins)}]");
        Console.WriteLine($"Target amount: {amount}");
        Console.WriteLine($"Minimum coins needed: {minCoins}");
        Console.WriteLine($"Number of ways to make change: {ways}");
        
        if (coinsUsed != null)
        {
            Console.WriteLine($"Coins used for minimum change: [{string.Join(", ", coinsUsed)}]");
        }
        else
        {
            Console.WriteLine("Cannot make the exact amount with given coins.");
        }
        
        // Another example
        var coins2 = new[] { 2, 3, 5 };
        const int amount2 = 9;
        
        var minCoins2 = CoinChange.MinCoins(coins2, amount2);
        var ways2 = CoinChange.CountWays(coins2, amount2);
        var coinsUsed2 = CoinChange.GetCoinsUsed(coins2, amount2);
        
        Console.WriteLine($"\nAvailable coins: [{string.Join(", ", coins2)}]");
        Console.WriteLine($"Target amount: {amount2}");
        Console.WriteLine($"Minimum coins needed: {minCoins2}");
        Console.WriteLine($"Number of ways to make change: {ways2}");
        
        if (coinsUsed2 != null)
        {
            Console.WriteLine($"Coins used for minimum change: [{string.Join(", ", coinsUsed2)}]");
        }
        else
        {
            Console.WriteLine("Cannot make the exact amount with given coins.");
        }
    }
}