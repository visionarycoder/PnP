using Algorithm.StringAlgorithms;

namespace Algorithm.StringAlgorithms;

/// <summary>
/// Demonstrates various string algorithms including pattern matching and text processing.
/// </summary>
internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("=== String Algorithms Demo ===\n");
        
        DemonstratePatternMatching();
        Console.WriteLine();
        
        DemonstrateEditDistance();
        Console.WriteLine();
        
        DemonstratePalindromes();
        Console.WriteLine();
        
        DemonstrateAnagrams();
        Console.WriteLine();
        
        DemonstratePermutations();
    }
    
    private static void DemonstratePatternMatching()
    {
        Console.WriteLine("--- Pattern Matching Algorithms ---");
        
        const string text = "ABABDABACDABABCABCABCABCABC";
        const string pattern = "ABABCAB";
        
        Console.WriteLine($"Text: {text}");
        Console.WriteLine($"Pattern: {pattern}\n");
        
        var naiveMatches = StringAlgorithms.NaiveSearch(text, pattern);
        var kmpMatches = StringAlgorithms.KMPSearch(text, pattern);
        var bmMatches = StringAlgorithms.BoyerMooreSearch(text, pattern);
        var rkMatches = StringAlgorithms.RabinKarpSearch(text, pattern);
        
        Console.WriteLine($"Naive Search matches at positions: [{string.Join(", ", naiveMatches)}]");
        Console.WriteLine($"KMP Search matches at positions: [{string.Join(", ", kmpMatches)}]");
        Console.WriteLine($"Boyer-Moore matches at positions: [{string.Join(", ", bmMatches)}]");
        Console.WriteLine($"Rabin-Karp matches at positions: [{string.Join(", ", rkMatches)}]");
        
        // Verify all algorithms found the same matches
        var allSame = naiveMatches.SequenceEqual(kmpMatches) && 
                      kmpMatches.SequenceEqual(bmMatches) && 
                      bmMatches.SequenceEqual(rkMatches);
        
        Console.WriteLine($"All algorithms agree: {(allSame ? "✓" : "✗")}");
        
        // Show matches in context
        if (naiveMatches.Count > 0)
        {
            Console.WriteLine("\nMatches in context:");
            foreach (var match in naiveMatches)
            {
                var start = Math.Max(0, match - 3);
                var end = Math.Min(text.Length, match + pattern.Length + 3);
                var context = text.Substring(start, end - start);
                var matchPart = pattern;
                
                Console.WriteLine($"Position {match}: ...{context.Replace(pattern, $"[{pattern}]")}...");
            }
        }
    }
    
    private static void DemonstrateEditDistance()
    {
        Console.WriteLine("--- Edit Distance (Levenshtein Distance) ---");
        
        var testPairs = new[]
        {
            ("kitten", "sitting"),
            ("saturday", "sunday"),
            ("intention", "execution"),
            ("algorithm", "altruistic"),
            ("hello", "world")
        };
        
        foreach (var (str1, str2) in testPairs)
        {
            var distance = StringAlgorithms.EditDistance(str1, str2);
            Console.WriteLine($"'{str1}' -> '{str2}': {distance} operations");
        }
        
        // Demonstrate practical use case
        Console.WriteLine("\nSpell checking simulation:");
        var dictionary = new[] { "apple", "apply", "apples", "application" };
        const string misspelled = "aple";
        
        Console.WriteLine($"Misspelled word: '{misspelled}'");
        Console.WriteLine("Suggestions (sorted by edit distance):");
        
        var suggestions = dictionary
            .Select(word => new { Word = word, Distance = StringAlgorithms.EditDistance(misspelled, word) })
            .OrderBy(x => x.Distance)
            .Take(3);
        
        foreach (var suggestion in suggestions)
        {
            Console.WriteLine($"  '{suggestion.Word}' (distance: {suggestion.Distance})");
        }
    }
    
    private static void DemonstratePalindromes()
    {
        Console.WriteLine("--- Palindrome Algorithms ---");
        
        var testStrings = new[]
        {
            "racecar",
            "hello",
            "madam",
            "babad",
            "abcdef",
            "aabbaa"
        };
        
        Console.WriteLine("Palindrome detection:");
        foreach (var str in testStrings)
        {
            var isPalindrome = StringAlgorithms.IsPalindrome(str);
            var longestPalindrome = StringAlgorithms.LongestPalindrome(str);
            
            Console.WriteLine($"'{str}': Is palindrome? {(isPalindrome ? "Yes" : "No")}, " +
                            $"Longest palindromic substring: '{longestPalindrome}'");
        }
        
        // Demonstrate with longer text
        const string longText = "abacabad";
        var longest = StringAlgorithms.LongestPalindrome(longText);
        Console.WriteLine($"\nIn '{longText}', longest palindromic substring: '{longest}'");
    }
    
    private static void DemonstrateAnagrams()
    {
        Console.WriteLine("--- Anagram Detection ---");
        
        const string text = "abab";
        const string pattern = "ab";
        
        Console.WriteLine($"Finding anagrams of '{pattern}' in '{text}':");
        
        var anagramPositions = StringAlgorithms.FindAnagrams(text, pattern);
        
        if (anagramPositions.Count > 0)
        {
            Console.WriteLine($"Anagram positions: [{string.Join(", ", anagramPositions)}]");
            
            foreach (var pos in anagramPositions)
            {
                var anagram = text.Substring(pos, pattern.Length);
                Console.WriteLine($"  Position {pos}: '{anagram}' is an anagram of '{pattern}'");
            }
        }
        else
        {
            Console.WriteLine("No anagrams found.");
        }
        
        // Another example
        const string text2 = "cbaebabacd";
        const string pattern2 = "abc";
        
        Console.WriteLine($"\nFinding anagrams of '{pattern2}' in '{text2}':");
        var anagramPositions2 = StringAlgorithms.FindAnagrams(text2, pattern2);
        
        if (anagramPositions2.Count > 0)
        {
            Console.WriteLine($"Anagram positions: [{string.Join(", ", anagramPositions2)}]");
            
            foreach (var pos in anagramPositions2)
            {
                var anagram = text2.Substring(pos, pattern2.Length);
                Console.WriteLine($"  Position {pos}: '{anagram}' is an anagram of '{pattern2}'");
            }
        }
        else
        {
            Console.WriteLine("No anagrams found.");
        }
    }
    
    private static void DemonstratePermutations()
    {
        Console.WriteLine("--- String Permutations ---");
        
        var testStrings = new[] { "abc", "ab", "xyz" };
        
        foreach (var str in testStrings)
        {
            var permutations = StringAlgorithms.GeneratePermutations(str);
            
            Console.WriteLine($"Permutations of '{str}' ({permutations.Count} total):");
            Console.WriteLine($"  [{string.Join(", ", permutations.Select(p => $"'{p}'"))}]");
            
            // Verify all permutations are unique and have same length
            var uniqueCount = permutations.Distinct().Count();
            var allSameLength = permutations.All(p => p.Length == str.Length);
            
            Console.WriteLine($"  All unique: {(uniqueCount == permutations.Count ? "✓" : "✗")}, " +
                            $"All same length: {(allSameLength ? "✓" : "✗")}");
        }
        
        // Calculate expected number of permutations
        static int Factorial(int n) => n <= 1 ? 1 : n * Factorial(n - 1);
        
        foreach (var str in testStrings)
        {
            var expected = Factorial(str.Length);
            var actual = StringAlgorithms.GeneratePermutations(str).Count;
            Console.WriteLine($"'{str}': Expected {expected}, Got {actual} {(expected == actual ? "✓" : "✗")}");
        }
    }
}