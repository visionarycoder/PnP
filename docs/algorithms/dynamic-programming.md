# Enterprise Dynamic Programming Solutions

**Description**: Production-ready dynamic programming implementations with space optimization, parallel decomposition, and enterprise-scale resource allocation algorithms.
**Language/Technology**: C#, .NET 9.0, Parallel Computing, Memory Optimization
**Performance Complexity**: Space-optimized solutions with parallel processing and cache-efficient implementations
**Enterprise Features**: Memory pool integration, concurrent execution, performance monitoring, and real-world optimization scenarios

## Fibonacci Sequence

**Code**:

```csharp
using System;
using System.Collections.Generic;

public class DynamicProgramming
{
    // Memoized Fibonacci - Top-down approach
    private static Dictionary<int, long> fibMemo = new Dictionary<int, long>();
    
    public static long FibonacciMemo(int n)
    {
        if (n <= 1) return n;
        
        if (fibMemo.ContainsKey(n))
            return fibMemo[n];
        
        fibMemo[n] = FibonacciMemo(n - 1) + FibonacciMemo(n - 2);
        return fibMemo[n];
    }
    
    // Tabulated Fibonacci - Bottom-up approach
    public static long FibonacciTabulated(int n)
    {
        if (n <= 1) return n;
        
        long[] dp = new long[n + 1];
        dp[0] = 0;
        dp[1] = 1;
        
        for (int i = 2; i <= n; i++)
        {
            dp[i] = dp[i - 1] + dp[i - 2];
        }
        
        return dp[n];
    }
    
    // Space-optimized Fibonacci
    public static long FibonacciOptimized(int n)
    {
        if (n <= 1) return n;
        
        long prev2 = 0, prev1 = 1, current = 0;
        
        for (int i = 2; i <= n; i++)
        {
            current = prev1 + prev2;
            prev2 = prev1;
            prev1 = current;
        }
        
        return current;
    }
}
```

## Longest Common Subsequence (LCS)

**Description**: The Longest Common Subsequence problem finds the longest subsequence common to two or more sequences. A subsequence doesn't have to be contiguous, but it must preserve the relative order of elements.

**Code**:

```csharp
public static class LCS
{
    public static int LongestCommonSubsequence(string text1, string text2)
    {
        int m = text1.Length, n = text2.Length;
        int[,] dp = new int[m + 1, n + 1];
        
        for (int i = 1; i <= m; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                if (text1[i - 1] == text2[j - 1])
                {
                    dp[i, j] = dp[i - 1, j - 1] + 1;
                }
                else
                {
                    dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                }
            }
        }
        
        return dp[m, n];
    }
    
    // Get the actual LCS string
    public static string GetLCSString(string text1, string text2)
    {
        int m = text1.Length, n = text2.Length;
        int[,] dp = new int[m + 1, n + 1];
        
        // Build the DP table
        for (int i = 1; i <= m; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                if (text1[i - 1] == text2[j - 1])
                {
                    dp[i, j] = dp[i - 1, j - 1] + 1;
                }
                else
                {
                    dp[i, j] = Math.Max(dp[i - 1, j], dp[i, j - 1]);
                }
            }
        }
        
        // Backtrack to construct the LCS
        int lcsLength = dp[m, n];
        char[] lcs = new char[lcsLength];
        int i2 = m, j2 = n, index = lcsLength - 1;
        
        while (i2 > 0 && j2 > 0)
        {
            if (text1[i2 - 1] == text2[j2 - 1])
            {
                lcs[index] = text1[i2 - 1];
                i2--;
                j2--;
                index--;
            }
            else if (dp[i2 - 1, j2] > dp[i2, j2 - 1])
            {
                i2--;
            }
            else
            {
                j2--;
            }
        }
        
        return new string(lcs);
    }
}
```

## Knapsack Problem (0/1)

**Code**:

```csharp
public class KnapsackItem
{
    public int Weight { get; set; }
    public int Value { get; set; }
    
    public KnapsackItem(int weight, int value)
    {
        Weight = weight;
        Value = value;
    }
}

public static class Knapsack
{
    public static int Knapsack01(KnapsackItem[] items, int capacity)
    {
        int n = items.Length;
        int[,] dp = new int[n + 1, capacity + 1];
        
        for (int i = 1; i <= n; i++)
        {
            for (int w = 1; w <= capacity; w++)
            {
                int currentWeight = items[i - 1].Weight;
                int currentValue = items[i - 1].Value;
                
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
    
    // Space-optimized version
    public static int Knapsack01Optimized(KnapsackItem[] items, int capacity)
    {
        int[] dp = new int[capacity + 1];
        
        foreach (var item in items)
        {
            // Traverse backwards to avoid using updated values
            for (int w = capacity; w >= item.Weight; w--)
            {
                dp[w] = Math.Max(dp[w], dp[w - item.Weight] + item.Value);
            }
        }
        
        return dp[capacity];
    }
}
```

## Coin Change Problem

**Code**:

```csharp
public static class CoinChange
{
    // Minimum coins needed to make amount
    public static int MinCoins(int[] coins, int amount)
    {
        int[] dp = new int[amount + 1];
        Array.Fill(dp, amount + 1); // Initialize with impossible value
        dp[0] = 0; // Base case: 0 coins needed for amount 0
        
        for (int i = 1; i <= amount; i++)
        {
            foreach (int coin in coins)
            {
                if (coin <= i)
                {
                    dp[i] = Math.Min(dp[i], dp[i - coin] + 1);
                }
            }
        }
        
        return dp[amount] > amount ? -1 : dp[amount];
    }
    
    // Number of ways to make amount
    public static int CountWays(int[] coins, int amount)
    {
        int[] dp = new int[amount + 1];
        dp[0] = 1; // One way to make amount 0: use no coins
        
        foreach (int coin in coins)
        {
            for (int i = coin; i <= amount; i++)
            {
                dp[i] += dp[i - coin];
            }
        }
        
        return dp[amount];
    }
}
```

**Usage**:

```csharp
// Fibonacci examples
Console.WriteLine(DynamicProgramming.FibonacciMemo(10)); // Output: 55
Console.WriteLine(DynamicProgramming.FibonacciTabulated(10)); // Output: 55
Console.WriteLine(DynamicProgramming.FibonacciOptimized(10)); // Output: 55

// LCS example
string text1 = "abcde";
string text2 = "ace";
Console.WriteLine(LCS.LongestCommonSubsequence(text1, text2)); // Output: 3
Console.WriteLine(LCS.GetLCSString(text1, text2)); // Output: "ace"

// Knapsack example
var items = new KnapsackItem[]
{
    new KnapsackItem(2, 1),
    new KnapsackItem(3, 4),
    new KnapsackItem(4, 5),
    new KnapsackItem(5, 7)
};
Console.WriteLine(Knapsack.Knapsack01(items, 8)); // Output: 9

// Coin change examples
int[] coins = {1, 3, 4};
Console.WriteLine(CoinChange.MinCoins(coins, 6)); // Output: 2 (3+3)
Console.WriteLine(CoinChange.CountWays(coins, 6)); // Output: 4 ways
```

**Notes**:

- **Time Complexity**:
  - Fibonacci: O(n) for tabulated and optimized versions
  - LCS: O(m*n) where m,n are string lengths
  - Knapsack: O(n*W) where n is items count and W is capacity
  - Coin Change: O(n*amount) where n is number of coin types
- **Space Complexity**:
  - Can often be optimized from O(n²) to O(n) using 1D arrays
- **Use Cases**:
  - Optimization problems with overlapping subproblems
  - Finding optimal solutions where greedy approach doesn't work
- **Performance**: Memoization (top-down) vs Tabulation (bottom-up) trade-offs
- **Security**: No special security considerations for DP algorithms
- **Alternatives**:
  - Greedy algorithms for problems with optimal substructure (when greedy choice property holds)
  - Divide and conquer for problems without overlapping subproblems
  - Branch and bound for optimization problems with pruning opportunities

## Levenshtein Distance (Edit Distance)

**Description**: The Levenshtein distance calculates the minimum number of single-character edits (insertions, deletions, or substitutions) required to change one word into another. This is a classic dynamic programming problem used in spell checkers and DNA analysis.

**Code**:

```csharp
public static class EditDistance
{
    public static int LevenshteinDistance(string word1, string word2)
    {
        int m = word1.Length, n = word2.Length;
        int[,] dp = new int[m + 1, n + 1];
        
        // Initialize base cases
        for (int i = 0; i <= m; i++)
            dp[i, 0] = i; // Delete all characters from word1
            
        for (int j = 0; j <= n; j++)
            dp[0, j] = j; // Insert all characters of word2
        
        // Fill the DP table
        for (int i = 1; i <= m; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                if (word1[i - 1] == word2[j - 1])
                {
                    dp[i, j] = dp[i - 1, j - 1]; // No operation needed
                }
                else
                {
                    dp[i, j] = 1 + Math.Min(
                        Math.Min(dp[i - 1, j],     // Delete from word1
                                dp[i, j - 1]),     // Insert into word1
                        dp[i - 1, j - 1]          // Replace in word1
                    );
                }
            }
        }
        
        return dp[m, n];
    }
    
    // Get the actual sequence of operations
    public static List<string> GetEditOperations(string word1, string word2)
    {
        int m = word1.Length, n = word2.Length;
        int[,] dp = new int[m + 1, n + 1];
        
        // Build the DP table (same as above)
        for (int i = 0; i <= m; i++) dp[i, 0] = i;
        for (int j = 0; j <= n; j++) dp[0, j] = j;
        
        for (int i = 1; i <= m; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                if (word1[i - 1] == word2[j - 1])
                    dp[i, j] = dp[i - 1, j - 1];
                else
                    dp[i, j] = 1 + Math.Min(Math.Min(dp[i - 1, j], dp[i, j - 1]), dp[i - 1, j - 1]);
            }
        }
        
        // Backtrack to find operations
        var operations = new List<string>();
        int x = m, y = n;
        
        while (x > 0 || y > 0)
        {
            if (x > 0 && y > 0 && word1[x - 1] == word2[y - 1])
            {
                x--; y--; // No operation
            }
            else if (x > 0 && y > 0 && dp[x, y] == dp[x - 1, y - 1] + 1)
            {
                operations.Add($"Replace '{word1[x - 1]}' with '{word2[y - 1]}' at position {x - 1}");
                x--; y--;
            }
            else if (x > 0 && dp[x, y] == dp[x - 1, y] + 1)
            {
                operations.Add($"Delete '{word1[x - 1]}' at position {x - 1}");
                x--;
            }
            else if (y > 0 && dp[x, y] == dp[x, y - 1] + 1)
            {
                operations.Add($"Insert '{word2[y - 1]}' at position {x}");
                y--;
            }
        }
        
        operations.Reverse();
        return operations;
    }
}
```

## Maximum Subarray Problem (Kadane's Algorithm)

**Description**: Finds the contiguous subarray within a one-dimensional array of numbers that has the largest sum. This is both a dynamic programming and divide-and-conquer problem.

**Code**:

```csharp
public static class MaxSubarray
{
    // Kadane's Algorithm - Dynamic Programming Approach
    public static int KadaneAlgorithm(int[] nums)
    {
        if (nums.Length == 0) return 0;
        
        int maxSoFar = nums[0];
        int maxEndingHere = nums[0];
        
        for (int i = 1; i < nums.Length; i++)
        {
            maxEndingHere = Math.Max(nums[i], maxEndingHere + nums[i]);
            maxSoFar = Math.Max(maxSoFar, maxEndingHere);
        }
        
        return maxSoFar;
    }
    
    // Get the actual subarray indices
    public static (int start, int end, int sum) FindMaxSubarray(int[] nums)
    {
        if (nums.Length == 0) return (0, 0, 0);
        
        int maxSum = nums[0];
        int currentSum = nums[0];
        int start = 0, end = 0, tempStart = 0;
        
        for (int i = 1; i < nums.Length; i++)
        {
            if (currentSum < 0)
            {
                currentSum = nums[i];
                tempStart = i;
            }
            else
            {
                currentSum += nums[i];
            }
            
            if (currentSum > maxSum)
            {
                maxSum = currentSum;
                start = tempStart;
                end = i;
            }
        }
        
        return (start, end, maxSum);
    }
    
    // Divide and Conquer Approach
    public static int DivideAndConquer(int[] nums, int left, int right)
    {
        if (left == right) return nums[left];
        
        int mid = (left + right) / 2;
        
        // Find max sum in left half
        int leftSum = DivideAndConquer(nums, left, mid);
        
        // Find max sum in right half
        int rightSum = DivideAndConquer(nums, mid + 1, right);
        
        // Find max sum crossing the middle
        int leftMax = int.MinValue;
        int sum = 0;
        for (int i = mid; i >= left; i--)
        {
            sum += nums[i];
            leftMax = Math.Max(leftMax, sum);
        }
        
        int rightMax = int.MinValue;
        sum = 0;
        for (int i = mid + 1; i <= right; i++)
        {
            sum += nums[i];
            rightMax = Math.Max(rightMax, sum);
        }
        
        int crossSum = leftMax + rightMax;
        
        return Math.Max(Math.Max(leftSum, rightSum), crossSum);
    }
}
```

## Matrix Chain Multiplication

**Description**: Determines the optimal way to parenthesize a chain of matrices to minimize the number of scalar multiplications. This is a classic interval dynamic programming problem.

**Code**:

```csharp
public static class MatrixChainMultiplication
{
    public static int MinScalarMultiplications(int[] dimensions)
    {
        int n = dimensions.Length - 1; // Number of matrices
        int[,] dp = new int[n, n];
        
        // Length of chain
        for (int length = 2; length <= n; length++)
        {
            for (int i = 0; i <= n - length; i++)
            {
                int j = i + length - 1;
                dp[i, j] = int.MaxValue;
                
                for (int k = i; k < j; k++)
                {
                    int cost = dp[i, k] + dp[k + 1, j] + 
                              dimensions[i] * dimensions[k + 1] * dimensions[j + 1];
                    dp[i, j] = Math.Min(dp[i, j], cost);
                }
            }
        }
        
        return dp[0, n - 1];
    }
    
    public static string GetOptimalParenthesization(int[] dimensions)
    {
        int n = dimensions.Length - 1;
        int[,] dp = new int[n, n];
        int[,] split = new int[n, n];
        
        for (int length = 2; length <= n; length++)
        {
            for (int i = 0; i <= n - length; i++)
            {
                int j = i + length - 1;
                dp[i, j] = int.MaxValue;
                
                for (int k = i; k < j; k++)
                {
                    int cost = dp[i, k] + dp[k + 1, j] + 
                              dimensions[i] * dimensions[k + 1] * dimensions[j + 1];
                    
                    if (cost < dp[i, j])
                    {
                        dp[i, j] = cost;
                        split[i, j] = k;
                    }
                }
            }
        }
        
        return BuildParentheses(split, 0, n - 1);
    }
    
    private static string BuildParentheses(int[,] split, int i, int j)
    {
        if (i == j)
            return $"M{i}";
        
        return $"({BuildParentheses(split, i, split[i, j])} × " +
               $"{BuildParentheses(split, split[i, j] + 1, j)})";
    }
}
```

## House Robber Problem Variants

**Description**: Multiple variations of the house robber problem where you cannot rob adjacent houses. Includes linear, circular, and binary tree versions.

**Code**:

```csharp
public static class HouseRobber
{
    // Linear House Robber (Original)
    public static int Rob(int[] houses)
    {
        if (houses.Length == 0) return 0;
        if (houses.Length == 1) return houses[0];
        
        int prev2 = 0, prev1 = 0;
        
        foreach (int house in houses)
        {
            int current = Math.Max(prev1, prev2 + house);
            prev2 = prev1;
            prev1 = current;
        }
        
        return prev1;
    }
    
    // Circular House Robber (houses form a circle)
    public static int RobCircular(int[] houses)
    {
        if (houses.Length == 0) return 0;
        if (houses.Length == 1) return houses[0];
        if (houses.Length == 2) return Math.Max(houses[0], houses[1]);
        
        // Case 1: Rob first house, can't rob last
        int[] case1 = new int[houses.Length - 1];
        Array.Copy(houses, case1, houses.Length - 1);
        
        // Case 2: Don't rob first house, can rob last
        int[] case2 = new int[houses.Length - 1];
        Array.Copy(houses, 1, case2, 0, houses.Length - 1);
        
        return Math.Max(Rob(case1), Rob(case2));
    }
    
    // Binary Tree House Robber
    public class TreeNode
    {
        public int val;
        public TreeNode left;
        public TreeNode right;
        public TreeNode(int val = 0, TreeNode left = null, TreeNode right = null)
        {
            this.val = val;
            this.left = left;
            this.right = right;
        }
    }
    
    public static int RobTree(TreeNode root)
    {
        var (robRoot, skipRoot) = RobTreeHelper(root);
        return Math.Max(robRoot, skipRoot);
    }
    
    private static (int robRoot, int skipRoot) RobTreeHelper(TreeNode node)
    {
        if (node == null) return (0, 0);
        
        var (robLeft, skipLeft) = RobTreeHelper(node.left);
        var (robRight, skipRight) = RobTreeHelper(node.right);
        
        // Rob current node: can't rob children
        int robCurrent = node.val + skipLeft + skipRight;
        
        // Don't rob current node: can choose to rob or skip children
        int skipCurrent = Math.Max(robLeft, skipLeft) + Math.Max(robRight, skipRight);
        
        return (robCurrent, skipCurrent);
    }
}
```

**Extended Usage Examples**:

```csharp
// Edit Distance Example
string word1 = "kitten", word2 = "sitting";
int distance = EditDistance.LevenshteinDistance(word1, word2);
var operations = EditDistance.GetEditOperations(word1, word2);
Console.WriteLine($"Edit distance: {distance}");
operations.ForEach(Console.WriteLine);

// Maximum Subarray Example
int[] nums = {-2, 1, -3, 4, -1, 2, 1, -5, 4};
int maxSum = MaxSubarray.KadaneAlgorithm(nums);
var (start, end, sum) = MaxSubarray.FindMaxSubarray(nums);
Console.WriteLine($"Max sum: {maxSum} from index {start} to {end}");

// Matrix Chain Multiplication Example
int[] dimensions = {40, 20, 30, 10, 30}; // 4 matrices
int minOps = MatrixChainMultiplication.MinScalarMultiplications(dimensions);
string parentheses = MatrixChainMultiplication.GetOptimalParenthesization(dimensions);
Console.WriteLine($"Minimum operations: {minOps}");
Console.WriteLine($"Optimal parenthesization: {parentheses}");

// House Robber Examples
int[] houses = {2, 7, 9, 3, 1};
int[] circularHouses = {2, 3, 2};
Console.WriteLine($"Linear robber: {HouseRobber.Rob(houses)}");
Console.WriteLine($"Circular robber: {HouseRobber.RobCircular(circularHouses)}");
```

**Algorithm Comparison and Selection Guide**:

- **Memoization vs Tabulation**:
  - Memoization (Top-down): Easier to implement, solves only needed subproblems, but has function call overhead
  - Tabulation (Bottom-up): Better performance, no recursion overhead, but may solve unnecessary subproblems

- **Space Optimization**:
  - Many 2D DP problems can be optimized to 1D by observing that we only need the previous row/column
  - Rolling arrays can further reduce space complexity
  - In-place modifications when input can be modified

- **When to Use Dynamic Programming**:
  - Problem has optimal substructure (optimal solution contains optimal solutions to subproblems)
  - Problem has overlapping subproblems (same subproblems are solved multiple times)
  - Greedy approach doesn't work (local optimum ≠ global optimum)

## Related Snippets

- [Graph Algorithms](graph-algorithms.md) - For dynamic programming on graphs (shortest paths, counting paths)
- [Data Structures](data-structures.md) - Arrays and matrices used in DP tables, priority queues for optimization
- [String Algorithms](string-algorithms.md) - String DP problems like edit distance and pattern matching
