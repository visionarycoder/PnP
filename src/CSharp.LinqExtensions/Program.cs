using CSharp.LinqExtensions;

namespace CSharp.LinqExtensions;

/// <summary>
/// Demonstrates comprehensive LINQ extensions for advanced data manipulation.
/// Showcases batching, windowing, distinct operations, and statistical functions.
/// </summary>
class Program
{
    static void Main()
    {
        Console.WriteLine("=== LINQ Extensions Demonstration ===\n");
        
        // Sample data
        var numbers = Enumerable.Range(1, 20).ToList();
        var words = new[] { "apple", "banana", "apple", "cherry", "banana", "apple", "date" };
        var sentences = new[] 
        {
            "This is sentence 1.",
            "This is sentence 2.",
            "---",
            "This is sentence 3.",
            "This is sentence 4.",
            "---",
            "This is sentence 5."
        };
        
        DemonstrateBatchingOperations(numbers);
        DemonstrateWindowingOperations(numbers);
        DemonstrateDistinctOperations(words);
        DemonstrateConditionalOperations(numbers);
        DemonstrateAggregationOperations();
        DemonstrateSplitOperations(sentences);
    }

    static void DemonstrateBatchingOperations(List<int> numbers)
    {
        Console.WriteLine("--- Batching Operations ---");
        
        // Basic batching
        var batches = numbers.Batch(5);
        Console.WriteLine("Numbers in batches of 5:");
        foreach (var batch in batches)
        {
            Console.WriteLine($"  [{string.Join(", ", batch)}]");
        }
        
        // Chunking with overlap
        var chunks = numbers.Take(10).Chunk(4, 2);
        Console.WriteLine("\nFirst 10 numbers in chunks of 4 with 2 overlap:");
        foreach (var chunk in chunks)
        {
            Console.WriteLine($"  [{string.Join(", ", chunk)}]");
        }
        
        Console.WriteLine();
    }

    static void DemonstrateWindowingOperations(List<int> numbers)
    {
        Console.WriteLine("--- Windowing Operations ---");
        
        // Sliding window
        var windows = numbers.Take(8).SlidingWindow(3);
        Console.WriteLine("Sliding window of size 3 over first 8 numbers:");
        foreach (var window in windows)
        {
            Console.WriteLine($"  [{string.Join(", ", window)}]");
        }
        
        // Pairwise operations
        var pairs = numbers.Take(5).Pairwise();
        Console.WriteLine("\nPairwise operations on first 5 numbers:");
        foreach (var (prev, curr) in pairs)
        {
            Console.WriteLine($"  {prev} -> {curr} (diff: {curr - prev})");
        }
        
        // Group consecutive
        var data = new[] { 1, 1, 2, 2, 2, 1, 1, 3, 3 };
        var groups = data.GroupConsecutive(x => x);
        Console.WriteLine("\nGroup consecutive identical values:");
        foreach (var group in groups)
        {
            Console.WriteLine($"  Key {group.Key}: [{string.Join(", ", group)}]");
        }
        
        Console.WriteLine();
    }

    static void DemonstrateDistinctOperations(string[] words)
    {
        Console.WriteLine("--- Distinct Operations ---");
        
        // Distinct by length
        var distinctByLength = words.DistinctBy(w => w.Length);
        Console.WriteLine("Words distinct by length:");
        Console.WriteLine($"  [{string.Join(", ", distinctByLength)}]");
        
        // Distinct last occurrence
        var distinctLast = words.DistinctLast().Reverse();
        Console.WriteLine("\nDistinct keeping last occurrence:");
        Console.WriteLine($"  [{string.Join(", ", distinctLast)}]");
        
        // Find duplicates
        var duplicates = words.Duplicates();
        Console.WriteLine("\nDuplicate words:");
        Console.WriteLine($"  [{string.Join(", ", duplicates)}]");
        
        Console.WriteLine();
    }

    static void DemonstrateConditionalOperations(List<int> numbers)
    {
        Console.WriteLine("--- Conditional Operations ---");
        
        bool applyFilter = true;
        var conditionalResult = numbers.Take(10)
            .WhereIf(applyFilter, x => x % 2 == 0);
        Console.WriteLine($"First 10 numbers with conditional even filter (applied: {applyFilter}):");
        Console.WriteLine($"  [{string.Join(", ", conditionalResult)}]");
        
        // With nullables
        var nullableNumbers = new int?[] { 1, null, 3, null, 5, 6 };
        var nonNulls = nullableNumbers.WhereNotNull();
        Console.WriteLine("\nNon-null values from nullable array:");
        Console.WriteLine($"  [{string.Join(", ", nonNulls)}]");
        
        Console.WriteLine();
    }

    static void DemonstrateAggregationOperations()
    {
        Console.WriteLine("--- Aggregation Operations ---");
        
        var values = new double[] { 1, 2, 2, 3, 4, 4, 4, 5, 6 };
        
        Console.WriteLine("Statistical operations on [1, 2, 2, 3, 4, 4, 4, 5, 6]:");
        Console.WriteLine($"  Average: {values.Average():F2}");
        Console.WriteLine($"  Median: {values.Median():F2}");
        Console.WriteLine($"  Standard Deviation: {values.StandardDeviation():F2}");
        Console.WriteLine($"  Mode: {values.Mode()}");
        
        Console.WriteLine();
    }

    static void DemonstrateSplitOperations(string[] sentences)
    {
        Console.WriteLine("--- Split Operations ---");
        
        // Split at delimiter
        var sections = sentences.SplitAt(s => s == "---", includeDelimiter: false);
        Console.WriteLine("Sentences split at '---' delimiter:");
        int sectionNum = 1;
        foreach (var section in sections)
        {
            Console.WriteLine($"  Section {sectionNum++}: [{string.Join(", ", section)}]");
        }
        
        Console.WriteLine();
    }
}