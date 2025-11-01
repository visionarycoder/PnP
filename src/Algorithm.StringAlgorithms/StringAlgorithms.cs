namespace Algorithm.StringAlgorithms;

/// <summary>
/// Collection of string algorithms including pattern matching and string processing.
/// </summary>
public static class StringAlgorithms
{
    /// <summary>
    /// Naive pattern matching algorithm.
    /// Time Complexity: O(n * m) where n is text length, m is pattern length
    /// </summary>
    public static List<int> NaiveSearch(string text, string pattern)
    {
        var matches = new List<int>();
        var n = text.Length;
        var m = pattern.Length;
        
        for (var i = 0; i <= n - m; i++)
        {
            var j = 0;
            while (j < m && text[i + j] == pattern[j])
                j++;
            
            if (j == m)
                matches.Add(i);
        }
        
        return matches;
    }
    
    /// <summary>
    /// KMP (Knuth-Morris-Pratt) pattern matching algorithm.
    /// Time Complexity: O(n + m)
    /// </summary>
    public static List<int> KMPSearch(string text, string pattern)
    {
        var matches = new List<int>();
        var n = text.Length;
        var m = pattern.Length;
        
        if (m == 0) return matches;
        
        var lps = ComputeLPSArray(pattern);
        
        int i = 0, j = 0;
        while (i < n)
        {
            if (text[i] == pattern[j])
            {
                i++;
                j++;
            }
            
            if (j == m)
            {
                matches.Add(i - j);
                j = lps[j - 1];
            }
            else if (i < n && text[i] != pattern[j])
            {
                if (j != 0)
                    j = lps[j - 1];
                else
                    i++;
            }
        }
        
        return matches;
    }
    
    /// <summary>
    /// Computes the Longest Prefix Suffix (LPS) array for KMP algorithm.
    /// </summary>
    private static int[] ComputeLPSArray(string pattern)
    {
        var m = pattern.Length;
        var lps = new int[m];
        var length = 0;
        var i = 1;
        
        while (i < m)
        {
            if (pattern[i] == pattern[length])
            {
                length++;
                lps[i] = length;
                i++;
            }
            else
            {
                if (length != 0)
                    length = lps[length - 1];
                else
                {
                    lps[i] = 0;
                    i++;
                }
            }
        }
        
        return lps;
    }
    
    /// <summary>
    /// Boyer-Moore pattern matching algorithm with bad character heuristic.
    /// Time Complexity: O(n * m) worst case, O(n / m) best case
    /// </summary>
    public static List<int> BoyerMooreSearch(string text, string pattern)
    {
        var matches = new List<int>();
        var n = text.Length;
        var m = pattern.Length;
        
        if (m == 0) return matches;
        
        var badChar = ComputeBadCharTable(pattern);
        
        var shift = 0;
        while (shift <= n - m)
        {
            var j = m - 1;
            
            while (j >= 0 && pattern[j] == text[shift + j])
                j--;
            
            if (j < 0)
            {
                matches.Add(shift);
                shift += shift + m < n ? m - badChar.GetValueOrDefault(text[shift + m], -1) : 1;
            }
            else
            {
                shift += Math.Max(1, j - badChar.GetValueOrDefault(text[shift + j], -1));
            }
        }
        
        return matches;
    }
    
    /// <summary>
    /// Computes the bad character table for Boyer-Moore algorithm.
    /// </summary>
    private static Dictionary<char, int> ComputeBadCharTable(string pattern)
    {
        var badChar = new Dictionary<char, int>();
        var m = pattern.Length;
        
        for (var i = 0; i < m; i++)
        {
            badChar[pattern[i]] = i;
        }
        
        return badChar;
    }
    
    /// <summary>
    /// Rabin-Karp pattern matching algorithm using rolling hash.
    /// Time Complexity: O(n + m) average case, O(n * m) worst case
    /// </summary>
    public static List<int> RabinKarpSearch(string text, string pattern)
    {
        var matches = new List<int>();
        var n = text.Length;
        var m = pattern.Length;
        
        if (m == 0 || m > n) return matches;
        
        const int prime = 101; // A prime number for hashing
        const int baseValue = 256;  // Number of characters in alphabet
        
        var patternHash = 0;
        var textHash = 0;
        var h = 1;
        
        // Calculate h = baseValue^(m-1) % prime
        for (var i = 0; i < m - 1; i++)
            h = (h * baseValue) % prime;
        
        // Calculate hash for pattern and first window of text
        for (var i = 0; i < m; i++)
        {
            patternHash = (baseValue * patternHash + pattern[i]) % prime;
            textHash = (baseValue * textHash + text[i]) % prime;
        }
        
        // Slide the pattern over text one by one
        for (var i = 0; i <= n - m; i++)
        {
            if (patternHash == textHash)
            {
                // Check characters one by one to avoid spurious hits
                var j = 0;
                while (j < m && text[i + j] == pattern[j])
                    j++;
                
                if (j == m)
                    matches.Add(i);
            }
            
            // Calculate hash for next window
            if (i < n - m)
            {
                textHash = (baseValue * (textHash - text[i] * h) + text[i + m]) % prime;
                
                // Convert negative hash to positive
                if (textHash < 0)
                    textHash += prime;
            }
        }
        
        return matches;
    }
    
    /// <summary>
    /// Calculates the edit distance (Levenshtein distance) between two strings.
    /// Time Complexity: O(m * n)
    /// </summary>
    public static int EditDistance(string str1, string str2)
    {
        var m = str1.Length;
        var n = str2.Length;
        var dp = new int[m + 1, n + 1];
        
        // Initialize base cases
        for (var i = 0; i <= m; i++)
            dp[i, 0] = i;
            
        for (var j = 0; j <= n; j++)
            dp[0, j] = j;
        
        // Fill the DP table
        for (var i = 1; i <= m; i++)
        {
            for (var j = 1; j <= n; j++)
            {
                if (str1[i - 1] == str2[j - 1])
                {
                    dp[i, j] = dp[i - 1, j - 1];
                }
                else
                {
                    dp[i, j] = 1 + Math.Min(
                        Math.Min(dp[i - 1, j], dp[i, j - 1]),
                        dp[i - 1, j - 1]
                    );
                }
            }
        }
        
        return dp[m, n];
    }
    
    /// <summary>
    /// Finds the longest palindromic substring using expand around centers approach.
    /// Time Complexity: O(n²)
    /// </summary>
    public static string LongestPalindrome(string s)
    {
        if (string.IsNullOrEmpty(s))
            return string.Empty;
        
        var start = 0;
        var maxLength = 1;
        
        for (var i = 0; i < s.Length; i++)
        {
            // Check for odd length palindromes
            var len1 = ExpandAroundCenter(s, i, i);
            
            // Check for even length palindromes
            var len2 = ExpandAroundCenter(s, i, i + 1);
            
            var currentMax = Math.Max(len1, len2);
            
            if (currentMax > maxLength)
            {
                maxLength = currentMax;
                start = i - (currentMax - 1) / 2;
            }
        }
        
        return s.Substring(start, maxLength);
    }
    
    /// <summary>
    /// Helper method to expand around center and find palindrome length.
    /// </summary>
    private static int ExpandAroundCenter(string s, int left, int right)
    {
        while (left >= 0 && right < s.Length && s[left] == s[right])
        {
            left--;
            right++;
        }
        
        return right - left - 1;
    }
    
    /// <summary>
    /// Checks if a string is a palindrome.
    /// Time Complexity: O(n)
    /// </summary>
    public static bool IsPalindrome(string s)
    {
        var left = 0;
        var right = s.Length - 1;
        
        while (left < right)
        {
            if (s[left] != s[right])
                return false;
                
            left++;
            right--;
        }
        
        return true;
    }
    
    /// <summary>
    /// Finds all anagrams of a pattern in a text using sliding window.
    /// Time Complexity: O(n)
    /// </summary>
    public static List<int> FindAnagrams(string text, string pattern)
    {
        var result = new List<int>();
        
        if (text.Length < pattern.Length)
            return result;
        
        var patternCount = new int[26];
        var windowCount = new int[26];
        
        // Count characters in pattern
        foreach (var c in pattern)
            patternCount[c - 'a']++;
        
        var windowSize = pattern.Length;
        
        // Initialize first window
        for (var i = 0; i < windowSize; i++)
            windowCount[text[i] - 'a']++;
        
        // Check first window
        if (AreEqual(patternCount, windowCount))
            result.Add(0);
        
        // Slide the window
        for (var i = windowSize; i < text.Length; i++)
        {
            // Add new character
            windowCount[text[i] - 'a']++;
            
            // Remove old character
            windowCount[text[i - windowSize] - 'a']--;
            
            // Check if current window is an anagram
            if (AreEqual(patternCount, windowCount))
                result.Add(i - windowSize + 1);
        }
        
        return result;
    }
    
    /// <summary>
    /// Helper method to compare two character count arrays.
    /// </summary>
    private static bool AreEqual(int[] arr1, int[] arr2)
    {
        for (var i = 0; i < arr1.Length; i++)
        {
            if (arr1[i] != arr2[i])
                return false;
        }
        return true;
    }
    
    /// <summary>
    /// Generates all permutations of a string.
    /// Time Complexity: O(n! * n)
    /// </summary>
    public static List<string> GeneratePermutations(string s)
    {
        var result = new List<string>();
        var chars = s.ToCharArray();
        GeneratePermutationsHelper(chars, 0, result);
        return result;
    }
    
    /// <summary>
    /// Helper method for generating permutations using backtracking.
    /// </summary>
    private static void GeneratePermutationsHelper(char[] chars, int index, List<string> result)
    {
        if (index == chars.Length)
        {
            result.Add(new string(chars));
            return;
        }
        
        for (var i = index; i < chars.Length; i++)
        {
            // Swap
            (chars[index], chars[i]) = (chars[i], chars[index]);
            
            // Recurse
            GeneratePermutationsHelper(chars, index + 1, result);
            
            // Backtrack
            (chars[index], chars[i]) = (chars[i], chars[index]);
        }
    }
}