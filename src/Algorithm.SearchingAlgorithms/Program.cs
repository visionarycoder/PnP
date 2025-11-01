using Algorithm.SearchingAlgorithms;

namespace Algorithm.SearchingAlgorithms;

/// <summary>
/// Demonstrates various searching algorithms with performance comparisons.
/// </summary>
internal class Program
{
    private static void Main(string[] args)
    {
        Console.WriteLine("=== Searching Algorithms Demo ===\n");
        
        DemonstrateBasicSearches();
        Console.WriteLine();
        
        DemonstrateAdvancedSearches();
        Console.WriteLine();
        
        DemonstrateSearchInDuplicates();
        Console.WriteLine();
        
        DemonstratePerformanceComparison();
    }
    
    private static void DemonstrateBasicSearches()
    {
        Console.WriteLine("--- Basic Search Algorithms ---");
        
        var unsortedArray = new[] { 5, 2, 8, 1, 9, 3, 7, 4, 6 };
        var sortedArray = new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 };
        const int target = 7;
        
        Console.WriteLine($"Unsorted array: [{string.Join(", ", unsortedArray)}]");
        Console.WriteLine($"Sorted array: [{string.Join(", ", sortedArray)}]");
        Console.WriteLine($"Target: {target}\n");
        
        // Linear search (works on both sorted and unsorted)
        var linearResult1 = SearchAlgorithms.LinearSearch(unsortedArray, target);
        var linearResult2 = SearchAlgorithms.LinearSearch(sortedArray, target);
        
        Console.WriteLine($"Linear Search (unsorted): Index {linearResult1}");
        Console.WriteLine($"Linear Search (sorted): Index {linearResult2}");
        
        // Binary search (only works on sorted arrays)
        var binaryResult = SearchAlgorithms.BinarySearch(sortedArray, target);
        var binaryRecursiveResult = SearchAlgorithms.BinarySearchRecursive(sortedArray, target);
        
        Console.WriteLine($"Binary Search (iterative): Index {binaryResult}");
        Console.WriteLine($"Binary Search (recursive): Index {binaryRecursiveResult}");
    }
    
    private static void DemonstrateAdvancedSearches()
    {
        Console.WriteLine("--- Advanced Search Algorithms ---");
        
        var largeArray = Enumerable.Range(1, 100).ToArray();
        const int target = 67;
        
        Console.WriteLine($"Large sorted array: [1, 2, 3, ..., 100]");
        Console.WriteLine($"Target: {target}\n");
        
        var jumpResult = SearchAlgorithms.JumpSearch(largeArray, target);
        var exponentialResult = SearchAlgorithms.ExponentialSearch(largeArray, target);
        var interpolationResult = SearchAlgorithms.InterpolationSearch(largeArray, target);
        
        Console.WriteLine($"Jump Search: Index {jumpResult}");
        Console.WriteLine($"Exponential Search: Index {exponentialResult}");
        Console.WriteLine($"Interpolation Search: Index {interpolationResult}");
        
        // Test with string array
        var stringArray = new[] { "apple", "banana", "cherry", "date", "elderberry", "fig", "grape" };
        const string stringTarget = "date";
        
        Console.WriteLine($"\nString array: [{string.Join(", ", stringArray)}]");
        Console.WriteLine($"Target: {stringTarget}");
        
        var stringBinaryResult = SearchAlgorithms.BinarySearch(stringArray, stringTarget);
        var stringJumpResult = SearchAlgorithms.JumpSearch(stringArray, stringTarget);
        
        Console.WriteLine($"Binary Search (strings): Index {stringBinaryResult}");
        Console.WriteLine($"Jump Search (strings): Index {stringJumpResult}");
    }
    
    private static void DemonstrateSearchInDuplicates()
    {
        Console.WriteLine("--- Search in Arrays with Duplicates ---");
        
        var duplicateArray = new[] { 1, 2, 2, 2, 3, 4, 4, 5, 5, 5, 5, 6 };
        const int duplicateTarget = 5;
        
        Console.WriteLine($"Array with duplicates: [{string.Join(", ", duplicateArray)}]");
        Console.WriteLine($"Target: {duplicateTarget}\n");
        
        var firstOccurrence = SearchAlgorithms.FindFirstOccurrence(duplicateArray, duplicateTarget);
        var lastOccurrence = SearchAlgorithms.FindLastOccurrence(duplicateArray, duplicateTarget);
        var anyOccurrence = SearchAlgorithms.BinarySearch(duplicateArray, duplicateTarget);
        
        Console.WriteLine($"First occurrence of {duplicateTarget}: Index {firstOccurrence}");
        Console.WriteLine($"Last occurrence of {duplicateTarget}: Index {lastOccurrence}");
        Console.WriteLine($"Any occurrence of {duplicateTarget}: Index {anyOccurrence}");
        
        if (firstOccurrence != -1 && lastOccurrence != -1)
        {
            var count = lastOccurrence - firstOccurrence + 1;
            Console.WriteLine($"Total occurrences of {duplicateTarget}: {count}");
        }
    }
    
    private static void DemonstratePerformanceComparison()
    {
        Console.WriteLine("--- Performance Comparison ---");
        
        // Create a large sorted array for performance testing
        const int size = 100000;
        var largeArray = Enumerable.Range(1, size).ToArray();
        var random = new Random(42); // Fixed seed for reproducible results
        
        // Test multiple random targets
        var targets = new int[10];
        for (var i = 0; i < targets.Length; i++)
        {
            targets[i] = random.Next(1, size + 1);
        }
        
        Console.WriteLine($"Testing with array of size {size:N0}");
        Console.WriteLine($"Test targets: [{string.Join(", ", targets)}]\n");
        
        // Measure Linear Search
        var linearTime = MeasureSearchTime(() =>
        {
            foreach (var target in targets)
                SearchAlgorithms.LinearSearch(largeArray, target);
        });
        
        // Measure Binary Search
        var binaryTime = MeasureSearchTime(() =>
        {
            foreach (var target in targets)
                SearchAlgorithms.BinarySearch(largeArray, target);
        });
        
        // Measure Jump Search
        var jumpTime = MeasureSearchTime(() =>
        {
            foreach (var target in targets)
                SearchAlgorithms.JumpSearch(largeArray, target);
        });
        
        // Measure Interpolation Search
        var interpolationTime = MeasureSearchTime(() =>
        {
            foreach (var target in targets)
                SearchAlgorithms.InterpolationSearch(largeArray, target);
        });
        
        Console.WriteLine($"Linear Search: {linearTime:F4} ms");
        Console.WriteLine($"Binary Search: {binaryTime:F4} ms");
        Console.WriteLine($"Jump Search: {jumpTime:F4} ms");
        Console.WriteLine($"Interpolation Search: {interpolationTime:F4} ms");
        
        // Calculate speedup
        Console.WriteLine($"\nSpeedup compared to Linear Search:");
        Console.WriteLine($"Binary Search: {linearTime / binaryTime:F1}x faster");
        Console.WriteLine($"Jump Search: {linearTime / jumpTime:F1}x faster");
        Console.WriteLine($"Interpolation Search: {linearTime / interpolationTime:F1}x faster");
    }
    
    private static double MeasureSearchTime(Action searchAction)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Run multiple times for more accurate measurement
        for (var i = 0; i < 100; i++)
        {
            searchAction();
        }
        
        stopwatch.Stop();
        return stopwatch.Elapsed.TotalMilliseconds / 100.0; // Average time
    }
}