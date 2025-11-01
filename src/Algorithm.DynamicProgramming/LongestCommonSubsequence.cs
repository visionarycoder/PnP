namespace Algorithm.DynamicProgramming;

/// <summary>
/// Longest Common Subsequence algorithm implementations.
/// </summary>
public static class LongestCommonSubsequence
{
    /// <summary>
    /// Finds the length of the longest common subsequence between two strings.
    /// </summary>
    public static int GetLength(string text1, string text2)
    {
        var m = text1.Length;
        var n = text2.Length;
        var dp = new int[m + 1, n + 1];
        
        for (var i = 1; i <= m; i++)
        {
            for (var j = 1; j <= n; j++)
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
    
    /// <summary>
    /// Gets the actual longest common subsequence string.
    /// </summary>
    public static string GetSequence(string text1, string text2)
    {
        var m = text1.Length;
        var n = text2.Length;
        var dp = new int[m + 1, n + 1];
        
        // Build the DP table
        for (var i = 1; i <= m; i++)
        {
            for (var j = 1; j <= n; j++)
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
        var lcsLength = dp[m, n];
        var lcs = new char[lcsLength];
        var i2 = m;
        var j2 = n;
        var index = lcsLength - 1;
        
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