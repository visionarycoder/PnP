# String Algorithms

**Description**: Collection of string processing and pattern matching algorithms including Knuth-Morris-Pratt (KMP), Rabin-Karp, and various string manipulation techniques
**Language/Technology**: C# / String Processing

## Pattern Matching - Knuth-Morris-Pratt (KMP) Algorithm

**Description**: The KMP algorithm searches for occurrences of a pattern within a text by utilizing information from previous match attempts to avoid redundant character comparisons. It preprocesses the pattern to create a failure function (LPS - Longest Proper Prefix which is also Suffix).

**Code**:

```csharp
using System;

public static class StringAlgorithms
{
    // Knuth-Morris-Pratt (KMP) pattern matching algorithm
    // Time Complexity: O(n + m) where n is text length, m is pattern length
    public static int KMPSearch(string text, string pattern)
    {
        if (string.IsNullOrEmpty(pattern)) return 0;
        if (string.IsNullOrEmpty(text)) return -1;
        
        int[] lps = ComputeLPSArray(pattern);
        int textIndex = 0, patternIndex = 0;
        
        while (textIndex < text.Length)
        {
            if (pattern[patternIndex] == text[textIndex])
            {
                textIndex++;
                patternIndex++;
            }
            
            if (patternIndex == pattern.Length)
            {
                return textIndex - patternIndex; // Found at this position
            }
            else if (textIndex < text.Length && pattern[patternIndex] != text[textIndex])
            {
                if (patternIndex != 0)
                {
                    patternIndex = lps[patternIndex - 1];
                }
                else
                {
                    textIndex++;
                }
            }
        }
        
        return -1; // Pattern not found
    }
    
    // Compute Longest Proper Prefix which is also Suffix array
    private static int[] ComputeLPSArray(string pattern)
    {
        int[] lps = new int[pattern.Length];
        int length = 0; // Length of previous longest prefix suffix
        int i = 1;
        
        while (i < pattern.Length)
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
                {
                    length = lps[length - 1];
                }
                else
                {
                    lps[i] = 0;
                    i++;
                }
            }
        }
        
        return lps;
    }
}
```

## String Matching - Rabin-Karp Algorithm

**Description**: The Rabin-Karp algorithm uses rolling hash function to find pattern occurrences in text. It's particularly useful for finding multiple patterns or when the pattern length is large.

**Code**:

```csharp
public static class RabinKarp
{
    private const int Prime = 101; // A prime number for hashing
    
    public static int RabinKarpSearch(string text, string pattern)
    {
        int n = text.Length;
        int m = pattern.Length;
        int patternHash = 0; // Hash value for pattern
        int textHash = 0;    // Hash value for text window
        int h = 1;
        
        // Calculate h = pow(256, m-1) % Prime
        for (int i = 0; i < m - 1; i++)
        {
            h = (h * 256) % Prime;
        }
        
        // Calculate hash value of pattern and first window of text
        for (int i = 0; i < m; i++)
        {
            patternHash = (256 * patternHash + pattern[i]) % Prime;
            textHash = (256 * textHash + text[i]) % Prime;
        }
        
        // Slide the pattern over text one by one
        for (int i = 0; i <= n - m; i++)
        {
            // Check if hash values match
            if (patternHash == textHash)
            {
                // Check characters one by one for spurious hits
                int j;
                for (j = 0; j < m; j++)
                {
                    if (text[i + j] != pattern[j])
                        break;
                }
                
                if (j == m) // Pattern found
                    return i;
            }
            
            // Calculate hash value for next window
            if (i < n - m)
            {
                textHash = (256 * (textHash - text[i] * h) + text[i + m]) % Prime;
                
                // Handle negative values
                if (textHash < 0)
                    textHash += Prime;
            }
        }
        
        return -1; // Pattern not found
    }
}
```

## Longest Palindromic Substring

**Code**:

```csharp
public static class PalindromeAlgorithms
{
    // Expand around center approach
    public static string LongestPalindrome(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        
        int start = 0, maxLength = 1;
        
        for (int i = 0; i < s.Length; i++)
        {
            // Check for odd-length palindromes (center at i)
            int len1 = ExpandAroundCenter(s, i, i);
            
            // Check for even-length palindromes (center between i and i+1)
            int len2 = ExpandAroundCenter(s, i, i + 1);
            
            int currentMax = Math.Max(len1, len2);
            
            if (currentMax > maxLength)
            {
                maxLength = currentMax;
                start = i - (currentMax - 1) / 2;
            }
        }
        
        return s.Substring(start, maxLength);
    }
    
    private static int ExpandAroundCenter(string s, int left, int right)
    {
        while (left >= 0 && right < s.Length && s[left] == s[right])
        {
            left--;
            right++;
        }
        return right - left - 1;
    }
    
    // Manacher's algorithm for O(n) solution
    public static string LongestPalindromeManacher(string s)
    {
        if (string.IsNullOrEmpty(s)) return "";
        
        // Transform string to handle even-length palindromes
        string transformed = "#" + string.Join("#", s.ToCharArray()) + "#";
        int n = transformed.Length;
        int[] p = new int[n]; // Array to store palindrome lengths
        int center = 0, right = 0; // Center and right boundary of rightmost palindrome
        
        int maxLen = 0, centerIndex = 0;
        
        for (int i = 0; i < n; i++)
        {
            int mirror = 2 * center - i; // Mirror of i with respect to center
            
            if (i < right)
            {
                p[i] = Math.Min(right - i, p[mirror]);
            }
            
            // Try to expand palindrome centered at i
            try
            {
                while (i + p[i] + 1 < n && i - p[i] - 1 >= 0 &&
                       transformed[i + p[i] + 1] == transformed[i - p[i] - 1])
                {
                    p[i]++;
                }
            }
            catch { }
            
            // If palindrome centered at i extends past right, adjust center and right
            if (i + p[i] > right)
            {
                center = i;
                right = i + p[i];
            }
            
            // Update maximum length palindrome found so far
            if (p[i] > maxLen)
            {
                maxLen = p[i];
                centerIndex = i;
            }
        }
        
        // Extract the longest palindrome from original string
        int originalCenter = (centerIndex - 1) / 2;
        int start = originalCenter - maxLen / 2;
        return s.Substring(start, maxLen);
    }
}
```

## String Reversal and Rotation

**Code**:

```csharp
public static class StringManipulation
{
    // Check if s2 is rotation of s1
    public static bool IsRotation(string s1, string s2)
    {
        if (s1.Length != s2.Length) return false;
        if (string.IsNullOrEmpty(s1)) return string.IsNullOrEmpty(s2);
        
        // If s2 is rotation of s1, then s2 will be substring of s1+s1
        string concatenated = s1 + s1;
        return concatenated.Contains(s2);
    }
    
    // Reverse words in a string
    public static string ReverseWords(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;
        
        // Split by spaces and reverse array
        string[] words = s.Split(new char[] { ' ' }, 
            StringSplitOptions.RemoveEmptyEntries);
        Array.Reverse(words);
        
        return string.Join(" ", words);
    }
    
    // Reverse string in place (char array)
    public static void ReverseString(char[] s)
    {
        int left = 0, right = s.Length - 1;
        
        while (left < right)
        {
            char temp = s[left];
            s[left] = s[right];
            s[right] = temp;
            left++;
            right--;
        }
    }
    
    // Check if string is palindrome (ignore case and non-alphanumeric)
    public static bool IsPalindrome(string s)
    {
        if (string.IsNullOrEmpty(s)) return true;
        
        int left = 0, right = s.Length - 1;
        
        while (left < right)
        {
            // Skip non-alphanumeric characters
            while (left < right && !char.IsLetterOrDigit(s[left]))
                left++;
            while (left < right && !char.IsLetterOrDigit(s[right]))
                right--;
            
            // Compare characters (case insensitive)
            if (char.ToLower(s[left]) != char.ToLower(s[right]))
                return false;
                
            left++;
            right--;
        }
        
        return true;
    }
}
```

**Usage**:

```csharp
// KMP Pattern Matching
string text = "ABABDABACDABABCABCABCABCABC";
string pattern = "ABABCABCAB";
int position = StringAlgorithms.KMPSearch(text, pattern);
Console.WriteLine($"Pattern found at position: {position}"); // Output: 15

// Rabin-Karp Pattern Matching
position = RabinKarp.RabinKarpSearch(text, pattern);
Console.WriteLine($"Pattern found at position: {position}"); // Output: 15

// Longest Palindrome
string s = "babad";
string palindrome = PalindromeAlgorithms.LongestPalindrome(s);
Console.WriteLine($"Longest palindrome: {palindrome}"); // Output: "bab" or "aba"

// String rotation check
bool isRotation = StringManipulation.IsRotation("abcdef", "defabc");
Console.WriteLine($"Is rotation: {isRotation}"); // Output: True

// Reverse words
string reversed = StringManipulation.ReverseWords("The quick brown fox");
Console.WriteLine($"Reversed words: {reversed}"); // Output: "fox brown quick The"

// Palindrome check
bool isPalindrome = StringManipulation.IsPalindrome("A man, a plan, a canal: Panama");
Console.WriteLine($"Is palindrome: {isPalindrome}"); // Output: True
```

## Boyer-Moore String Search Algorithm

**Description**: The Boyer-Moore algorithm searches for pattern occurrences by scanning from right to left within the pattern and using two heuristics: bad character rule and good suffix rule. It can skip many characters during search, making it very efficient for large texts.

**Code**:

```csharp
public static class BoyerMoore
{
    public static int BoyerMooreSearch(string text, string pattern)
    {
        if (string.IsNullOrEmpty(pattern) || string.IsNullOrEmpty(text))
            return -1;
        
        var badCharTable = BuildBadCharTable(pattern);
        int skip = 0;
        
        while (skip <= text.Length - pattern.Length)
        {
            int j = pattern.Length - 1;
            
            // Compare pattern from right to left
            while (j >= 0 && pattern[j] == text[skip + j])
                j--;
            
            if (j < 0) // Pattern found
            {
                return skip;
            }
            else
            {
                // Apply bad character rule
                char badChar = text[skip + j];
                int badCharShift = j - (badCharTable.ContainsKey(badChar) ? badCharTable[badChar] : -1);
                skip += Math.Max(1, badCharShift);
            }
        }
        
        return -1; // Pattern not found
    }
    
    private static Dictionary<char, int> BuildBadCharTable(string pattern)
    {
        var table = new Dictionary<char, int>();
        
        for (int i = 0; i < pattern.Length; i++)
        {
            table[pattern[i]] = i;
        }
        
        return table;
    }
}
```

## Aho-Corasick Algorithm (Multiple Pattern Matching)

**Description**: The Aho-Corasick algorithm efficiently finds all occurrences of multiple patterns in a text simultaneously. It builds a trie of patterns with failure links, allowing linear-time search for all patterns.

**Code**:

```csharp
public class AhoCorasickNode
{
    public Dictionary<char, AhoCorasickNode> Children { get; set; }
    public AhoCorasickNode Failure { get; set; }
    public List<string> Outputs { get; set; }
    public bool IsEndOfWord { get; set; }
    
    public AhoCorasickNode()
    {
        Children = new Dictionary<char, AhoCorasickNode>();
        Outputs = new List<string>();
        Failure = null;
        IsEndOfWord = false;
    }
}

public static class AhoCorasick
{
    public static List<(int position, string pattern)> SearchMultiplePatterns(string text, string[] patterns)
    {
        var root = BuildTrie(patterns);
        BuildFailureLinks(root);
        return Search(text, root);
    }
    
    private static AhoCorasickNode BuildTrie(string[] patterns)
    {
        var root = new AhoCorasickNode();
        
        foreach (string pattern in patterns)
        {
            var current = root;
            
            foreach (char c in pattern)
            {
                if (!current.Children.ContainsKey(c))
                {
                    current.Children[c] = new AhoCorasickNode();
                }
                current = current.Children[c];
            }
            
            current.IsEndOfWord = true;
            current.Outputs.Add(pattern);
        }
        
        return root;
    }
    
    private static void BuildFailureLinks(AhoCorasickNode root)
    {
        var queue = new Queue<AhoCorasickNode>();
        
        // Initialize failure links for level 1 nodes
        foreach (var child in root.Children.Values)
        {
            child.Failure = root;
            queue.Enqueue(child);
        }
        
        // Build failure links for deeper levels
        while (queue.Count > 0)
        {
            var current = queue.Dequeue();
            
            foreach (var kvp in current.Children)
            {
                char c = kvp.Key;
                var child = kvp.Value;
                queue.Enqueue(child);
                
                // Find failure link
                var failureNode = current.Failure;
                while (failureNode != null && !failureNode.Children.ContainsKey(c))
                {
                    failureNode = failureNode.Failure;
                }
                
                child.Failure = failureNode?.Children.ContainsKey(c) == true 
                    ? failureNode.Children[c] 
                    : root;
                
                // Add outputs from failure node
                child.Outputs.AddRange(child.Failure.Outputs);
            }
        }
    }
    
    private static List<(int position, string pattern)> Search(string text, AhoCorasickNode root)
    {
        var results = new List<(int position, string pattern)>();
        var current = root;
        
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            
            // Follow failure links until we find a valid transition
            while (current != null && !current.Children.ContainsKey(c))
            {
                current = current.Failure;
            }
            
            if (current == null)
            {
                current = root;
                continue;
            }
            
            current = current.Children[c];
            
            // Add all matching patterns at this position
            foreach (string pattern in current.Outputs)
            {
                results.Add((i - pattern.Length + 1, pattern));
            }
        }
        
        return results;
    }
}
```

## Z-Algorithm (Linear Pattern Matching)

**Description**: The Z-algorithm finds all occurrences of a pattern in a text in linear time by computing the Z-array, which contains the length of the longest substring starting from each position that matches a prefix of the string.

**Code**:

```csharp
public static class ZAlgorithm
{
    public static List<int> ZSearch(string text, string pattern)
    {
        string combined = pattern + "$" + text; // $ is a separator
        int[] zArray = ComputeZArray(combined);
        var positions = new List<int>();
        
        for (int i = pattern.Length + 1; i < combined.Length; i++)
        {
            if (zArray[i] == pattern.Length)
            {
                positions.Add(i - pattern.Length - 1);
            }
        }
        
        return positions;
    }
    
    public static int[] ComputeZArray(string s)
    {
        int n = s.Length;
        int[] z = new int[n];
        int left = 0, right = 0;
        
        for (int i = 1; i < n; i++)
        {
            if (i > right)
            {
                left = right = i;
                while (right < n && s[right - left] == s[right])
                    right++;
                z[i] = right - left;
                right--;
            }
            else
            {
                int k = i - left;
                if (z[k] < right - i + 1)
                {
                    z[i] = z[k];
                }
                else
                {
                    left = i;
                    while (right < n && s[right - left] == s[right])
                        right++;
                    z[i] = right - left;
                    right--;
                }
            }
        }
        
        return z;
    }
}
```

## Suffix Array and Suffix Tree Applications

**Description**: Suffix arrays and suffix trees are powerful data structures for various string problems including pattern matching, longest common substring, and string compression.

**Code**:

```csharp
public static class SuffixArray
{
    public class Suffix
    {
        public int Index { get; set; }
        public string Text { get; set; }
        
        public Suffix(int index, string text)
        {
            Index = index;
            Text = text.Substring(index);
        }
    }
    
    public static int[] BuildSuffixArray(string text)
    {
        var suffixes = new List<Suffix>();
        
        for (int i = 0; i < text.Length; i++)
        {
            suffixes.Add(new Suffix(i, text));
        }
        
        suffixes.Sort((a, b) => string.Compare(a.Text, b.Text, StringComparison.Ordinal));
        
        return suffixes.Select(s => s.Index).ToArray();
    }
    
    public static List<int> SearchWithSuffixArray(string text, string pattern)
    {
        var suffixArray = BuildSuffixArray(text);
        var results = new List<int>();
        
        // Binary search for pattern in suffix array
        int left = 0, right = suffixArray.Length - 1;
        
        while (left <= right)
        {
            int mid = (left + right) / 2;
            string suffix = text.Substring(suffixArray[mid]);
            
            if (suffix.StartsWith(pattern))
            {
                // Found one occurrence, search for others
                results.Add(suffixArray[mid]);
                
                // Search left for more occurrences
                int temp = mid - 1;
                while (temp >= 0 && text.Substring(suffixArray[temp]).StartsWith(pattern))
                {
                    results.Add(suffixArray[temp]);
                    temp--;
                }
                
                // Search right for more occurrences
                temp = mid + 1;
                while (temp < suffixArray.Length && text.Substring(suffixArray[temp]).StartsWith(pattern))
                {
                    results.Add(suffixArray[temp]);
                    temp++;
                }
                
                break;
            }
            else if (string.Compare(suffix, pattern, StringComparison.Ordinal) < 0)
            {
                left = mid + 1;
            }
            else
            {
                right = mid - 1;
            }
        }
        
        results.Sort();
        return results;
    }
    
    public static string LongestCommonSubstring(string str1, string str2)
    {
        string combined = str1 + "#" + str2 + "$";
        var suffixArray = BuildSuffixArray(combined);
        
        int maxLength = 0;
        string result = "";
        
        for (int i = 0; i < suffixArray.Length - 1; i++)
        {
            int pos1 = suffixArray[i];
            int pos2 = suffixArray[i + 1];
            
            // Check if suffixes are from different strings
            bool fromDifferentStrings = (pos1 < str1.Length) != (pos2 < str1.Length);
            
            if (fromDifferentStrings)
            {
                string suffix1 = combined.Substring(pos1);
                string suffix2 = combined.Substring(pos2);
                
                int commonLength = GetCommonPrefixLength(suffix1, suffix2);
                
                if (commonLength > maxLength)
                {
                    maxLength = commonLength;
                    result = suffix1.Substring(0, commonLength);
                }
            }
        }
        
        return result;
    }
    
    private static int GetCommonPrefixLength(string str1, string str2)
    {
        int length = 0;
        int minLength = Math.Min(str1.Length, str2.Length);
        
        while (length < minLength && str1[length] == str2[length])
        {
            length++;
        }
        
        return length;
    }
}
```

**Extended Usage Examples**:

```csharp
// Boyer-Moore Algorithm
string text = "ABAAABCDABABCABCABCABC";
string pattern = "ABABCABCAB";
int position = BoyerMoore.BoyerMooreSearch(text, pattern);
Console.WriteLine($"Boyer-Moore found pattern at: {position}");

// Aho-Corasick Multiple Pattern Matching
string[] patterns = {"he", "she", "his", "hers"};
string searchText = "ushers";
var matches = AhoCorasick.SearchMultiplePatterns(searchText, patterns);
foreach (var (pos, pat) in matches)
{
    Console.WriteLine($"Pattern '{pat}' found at position {pos}");
}

// Z-Algorithm
var positions = ZAlgorithm.ZSearch("aabaacaadaabaaba", "aaba");
Console.WriteLine($"Z-Algorithm found pattern at positions: {string.Join(", ", positions)}");

// Suffix Array Applications
string text1 = "banana", text2 = "ananas";
string lcs = SuffixArray.LongestCommonSubstring(text1, text2);
Console.WriteLine($"Longest common substring: '{lcs}'");

var suffixPositions = SuffixArray.SearchWithSuffixArray("banana", "ana");
Console.WriteLine($"Suffix array search found 'ana' at: {string.Join(", ", suffixPositions)}");
```

**Algorithm Comparison and Performance Analysis**:

- **Time Complexity Comparison**:
  - Naive/Brute Force: O(nm) where n is text length, m is pattern length
  - KMP (Knuth-Morris-Pratt): O(n + m) - consistent linear time
  - Boyer-Moore: O(n + m) best case, O(nm) worst case, but very fast in practice
  - Rabin-Karp: O(n + m) average case, O(nm) worst case due to hash collisions
  - Z-Algorithm: O(n + m) - linear time with simple implementation
  - Aho-Corasick: O(n + m + z) where z is number of pattern occurrences
  - Suffix Array construction: O(n log n) with sorting, O(n) with advanced algorithms

- **Space Complexity**:
  - KMP: O(m) for failure function
  - Boyer-Moore: O(σ) where σ is alphabet size
  - Rabin-Karp: O(1) additional space
  - Aho-Corasick: O(total length of all patterns)
  - Suffix Array: O(n) for the array itself

- **When to Use Each Algorithm**:
  - **Single Pattern Search**:
    - Small patterns: Boyer-Moore (fastest in practice)
    - Large patterns or need worst-case guarantee: KMP
    - Rolling hash applications: Rabin-Karp
    - Simple implementation: Z-Algorithm
  
  - **Multiple Pattern Search**:
    - Multiple patterns simultaneously: Aho-Corasick
    - Many queries on same text: Build suffix array once, then use binary search
  
  - **String Analysis**:
    - Longest common substring: Suffix array or suffix tree
    - All substring searches: Suffix array/tree
    - Pattern matching with wildcards: Modified KMP or regex engines

**Notes**:

- **Time Complexity**:
  - KMP (Knuth-Morris-Pratt): O(n + m) where n is text length, m is pattern length
  - Rabin-Karp: O(n + m) average case, O(nm) worst case
  - Boyer-Moore: O(n + m) best case, O(nm) worst case, but typically very fast
  - Z-Algorithm: O(n + m) linear time complexity
  - Aho-Corasick: O(n + m + z) where z is the number of pattern occurrences
  - Manacher's Algorithm: O(n) for longest palindrome
  - String reversal/rotation: O(n)

- **Space Complexity**:
  - Generally O(m) for pattern preprocessing
  - Aho-Corasick: O(total length of all patterns)
  - Suffix arrays: O(n) for the array structure

- **Use Cases**:
  - **Text Editors**: Pattern search and replace (Boyer-Moore for single patterns)
  - **Bioinformatics**: DNA/RNA sequence analysis (KMP, Aho-Corasick for motif finding)
  - **Information Retrieval**: Document search engines (suffix arrays for indexing)
  - **Security**: Intrusion detection systems (Aho-Corasick for signature matching)
  - **Data Validation**: Input validation and string processing
  - **Plagiarism Detection**: Longest common substring algorithms

- **Performance Considerations**:
  - Boyer-Moore is fastest for most practical applications with large alphabets
  - KMP provides consistent worst-case performance
  - Aho-Corasick is essential when searching for multiple patterns simultaneously
  - Suffix arrays excel when multiple queries need to be performed on the same text

- **Implementation Tips**:
  - Use built-in string search methods for simple cases
  - Implement Boyer-Moore for performance-critical applications
  - Consider Aho-Corasick for virus scanning, spam detection, etc.
  - Preprocessing time is crucial - choose algorithm based on query frequency

- **Security Considerations**:
  - Hash collision attacks in Rabin-Karp (use strong hash functions)
  - Denial of service through algorithmic complexity attacks
  - Input validation for pattern and text lengths

## Related Snippets

- [Data Structures](data-structures.md) - Arrays, tries, and hashing used in string algorithms
- [Dynamic Programming Algorithms](dynamic-programming.md) - For longest common subsequence and edit distance problems
- [Graph Algorithms](graph-algorithms.md) - Suffix trees can be viewed as compressed tries with graph properties
