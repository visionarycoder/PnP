using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CSharp.SpanOperations;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== Span and Memory Operations Examples ===\n");

        // Example 1: Zero-allocation string splitting
        Console.WriteLine("1. Zero-allocation string splitting:");
        DemoStringSplitting();

        // Example 2: String manipulation with spans
        Console.WriteLine("\n2. String manipulation with spans:");
        DemoStringManipulation();

        // Example 3: High-performance numerical operations
        Console.WriteLine("\n3. High-performance numerical operations:");
        DemoNumericalOperations();

        // Example 4: Span-based algorithms
        Console.WriteLine("\n4. Span-based sorting algorithms:");
        DemoSortingAlgorithms();

        // Example 5: Predicate-based operations
        Console.WriteLine("\n5. Predicate operations:");
        DemoPredicateOperations();

        // Example 6: Memory operations for async scenarios
        Console.WriteLine("\n6. Memory operations:");
        DemoMemoryOperations();

        // Example 7: CSV parsing without allocations
        Console.WriteLine("\n7. Zero-allocation CSV parsing:");
        DemoSpanParsers();

        // Example 8: High-performance formatting
        Console.WriteLine("\n8. High-performance formatting:");
        DemoHighPerformanceFormatting();

        // Example 9: SpanStringBuilder for efficient string construction
        Console.WriteLine("\n9. SpanStringBuilder usage:");
        DemoSpanStringBuilder();

        // Example 10: Statistical operations on spans
        Console.WriteLine("\n10. Statistical operations:");
        DemoStatisticalOperations();

        // Example 11: Remove duplicates in-place
        Console.WriteLine("\n11. Remove duplicates in-place:");
        DemoRemoveDuplicates();

        // Example 12: Joining strings with spans
        Console.WriteLine("\n12. Joining strings with spans:");
        DemoJoiningStrings();

        // Example 13: Performance benchmarking
        Console.WriteLine("\n13. Performance benchmarking:");
        DemoPerformanceBenchmarking();

        // Example 14: Memory allocation comparison
        Console.WriteLine("\n14. Memory allocation comparison:");
        DemoMemoryAllocation();

        // Example 15: Async file operations with Memory<T>
        Console.WriteLine("\n15. Async file operations:");
        await DemoAsyncFileOperations();

        // Example 16: Advanced span operations
        Console.WriteLine("\n16. Advanced span operations:");
        DemoAdvancedOperations();

        Console.WriteLine("\nSpan operations completed!");
    }

    static void DemoStringSplitting()
    {
        var csvLine = "apple,banana,cherry,date,elderberry";
        Console.WriteLine($"Original: {csvLine}");
        Console.WriteLine("Split using span (no allocations):");

        foreach (var part in csvLine.AsSpan().Split(','))
        {
            Console.WriteLine($"  Part: '{part.ToString()}'");
        }
    }

    static void DemoStringManipulation()
    {
        var text = "  Hello, World!  ";
        var span = text.AsSpan();

        var trimmed = span.TrimFast();
        Console.WriteLine($"Trimmed: '{trimmed.ToString()}'");

        Console.WriteLine($"Contains 'World': {trimmed.ContainsFast("World".AsSpan())}");
        Console.WriteLine($"Comma count: {trimmed.CountOccurrences(',')}");

        // In-place modifications using mutable span
        var mutableText = "hello world".ToCharArray();
        var mutableSpan = mutableText.AsSpan();

        mutableSpan.ReplaceInPlace('l', 'L');
        mutableSpan.ToUpperInPlace();
        Console.WriteLine($"Modified: '{new string(mutableSpan)}'");
    }

    static void DemoNumericalOperations()
    {
        var numbers = new int[] { 1, 5, 3, 9, 2, 8, 4, 7, 6 };
        var numberSpan = numbers.AsSpan();

        Console.WriteLine($"Numbers: [{string.Join(", ", numbers)}]");
        Console.WriteLine($"Sum: {SpanNumerics.Sum(numberSpan)}");
        Console.WriteLine($"Min: {SpanNumerics.Min<int>(numberSpan)}");
        Console.WriteLine($"Max: {SpanNumerics.Max<int>(numberSpan)}");
        Console.WriteLine($"Average: {SpanNumerics.Average(numberSpan):F2}");
        Console.WriteLine($"Min index: {SpanNumerics.IndexOfMin<int>(numberSpan)}");
        Console.WriteLine($"Max index: {SpanNumerics.IndexOfMax<int>(numberSpan)}");
    }

    static void DemoSortingAlgorithms()
    {
        var unsorted = new int[] { 64, 34, 25, 12, 22, 11, 90 };
        Console.WriteLine($"Original: [{string.Join(", ", unsorted)}]");

        var forQuickSort = (int[])unsorted.Clone();
        SpanAlgorithms.QuickSort(forQuickSort.AsSpan());
        Console.WriteLine($"Quick sort: [{string.Join(", ", forQuickSort)}]");

        var forInsertionSort = (int[])unsorted.Clone();
        SpanAlgorithms.InsertionSort(forInsertionSort.AsSpan());
        Console.WriteLine($"Insertion sort: [{string.Join(", ", forInsertionSort)}]");

        var forHybridSort = (int[])unsorted.Clone();
        SpanAlgorithms.HybridSort(forHybridSort.AsSpan());
        Console.WriteLine($"Hybrid sort: [{string.Join(", ", forHybridSort)}]");

        // Binary search
        var sortedNumbers = new int[] { 1, 3, 5, 7, 9, 11, 13, 15 };
        int searchValue = 7;
        int index = SpanAlgorithms.BinarySearch(sortedNumbers.AsSpan(), searchValue);
        Console.WriteLine($"Binary search for {searchValue}: index {index}");
    }

    static void DemoPredicateOperations()
    {
        var testData = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var testSpan = testData.AsSpan();

        var evenCount = SpanAlgorithms.Count<int>(testSpan, x => x % 2 == 0);
        Console.WriteLine($"Even numbers count: {evenCount}");

        var hasLargeNumber = SpanAlgorithms.Any<int>(testSpan, x => x > 8);
        Console.WriteLine($"Has number > 8: {hasLargeNumber}");

        var allPositive = SpanAlgorithms.All<int>(testSpan, x => x > 0);
        Console.WriteLine($"All positive: {allPositive}");

        // Find indices of even numbers
        var indices = new int[10];
        SpanAlgorithms.FindIndices<int>(testSpan, x => x % 2 == 0, indices.AsSpan(), out int evenIndicesCount);
        Console.WriteLine($"Even number indices: [{string.Join(", ", indices.AsSpan(0, evenIndicesCount).ToArray())}]");
    }

    static void DemoMemoryOperations()
    {
        var dataArray = Enumerable.Range(1, 12).ToArray();
        var memory = dataArray.AsMemory();

        // Split into chunks for parallel processing
        var chunks = MemoryOperations.SplitIntoChunks(memory, 3);
        Console.WriteLine($"Split into {chunks.Length} chunks:");

        for (int i = 0; i < chunks.Length; i++)
        {
            var chunkArray = chunks[i].ToArray();
            Console.WriteLine($"  Chunk {i + 1}: [{string.Join(", ", chunkArray)}]");
        }
    }

    static void DemoSpanParsers()
    {
        var csvData = "John,25,Engineer,New York";
        var fields = new List<string>();

        Console.WriteLine($"Parsing CSV data: {csvData}");
        SpanParsers.ParseCsvLine(csvData.AsSpan(), (field, index) => 
        {
            var fieldValue = field.ToString();
            fields.Add(fieldValue);
            Console.WriteLine($"  Field {index + 1}: '{fieldValue}'");
        });

        Console.WriteLine($"Total fields parsed: {fields.Count}");

        // Parse key-value pairs
        var kvData = "name=Alice, age=30, city=Boston";
        Console.WriteLine($"\nParsing key-value pairs from: {kvData}");

        foreach (var pair in kvData.AsSpan().Split(','))
        {
            if (SpanParsers.TryParseKeyValue(pair.TrimFast(), out var key, out var value))
            {
                Console.WriteLine($"  {key.ToString()} = {value.ToString()}");
            }
        }

        // Parse numbers from delimited string
        var numberString = "10,20,30,40,50";
        var parsedNumbers = new int[10];
        SpanParsers.ParseIntegers(numberString.AsSpan(), ',', parsedNumbers.AsSpan(), out int numberCount);

        Console.WriteLine($"Parsed {numberCount} integers: [{string.Join(", ", parsedNumbers.AsSpan(0, numberCount).ToArray())}]");
    }

    static void DemoHighPerformanceFormatting()
    {
        var buffer = new char[100];
        var bufferSpan = buffer.AsSpan();

        // Format integers
        if (SpanFormatters.TryFormat(12345, bufferSpan, out int written1))
        {
            Console.WriteLine($"Formatted integer: '{new string(bufferSpan.Slice(0, written1))}'");
        }

        // Format double with precision
        if (SpanFormatters.TryFormat(3.14159, bufferSpan, out int written2, precision: 3))
        {
            Console.WriteLine($"Formatted double: '{new string(bufferSpan.Slice(0, written2))}'");
        }

        // Format DateTime
        if (SpanFormatters.TryFormat(DateTime.Now, bufferSpan, out int written3))
        {
            Console.WriteLine($"Formatted DateTime: '{new string(bufferSpan.Slice(0, written3))}'");
        }

        // Format byte array as hex
        var bytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
        if (SpanFormatters.TryFormatHex(bytes.AsSpan(), bufferSpan, out int written4, lowercase: true))
        {
            Console.WriteLine($"Hex format: '{new string(bufferSpan.Slice(0, written4))}'");
        }
    }

    static void DemoSpanStringBuilder()
    {
        var result = SpanFormatters.BuildString(200, builder =>
        {
            builder.TryAppend("Building a string: ");
            builder.TryAppend(DateTime.Now.Year);
            builder.TryAppend(" - ");
            builder.TryAppend(3.14159);
            builder.TryAppendLine(" (π)");
            
            for (int i = 1; i <= 5; i++)
            {
                builder.TryAppend("Item ");
                builder.TryAppend(i);
                if (i < 5) builder.TryAppend(", ");
            }
        });

        Console.WriteLine($"Built string: {result}");
    }

    static void DemoStatisticalOperations()
    {
        var samples = new double[] { 1.5, 2.3, 3.7, 2.8, 4.1, 3.2, 2.9, 3.5, 4.0, 2.1 };
        var sampleSpan = samples.AsSpan();

        Console.WriteLine($"Samples: [{string.Join(", ", samples.Select(x => x.ToString("F1")))}]");
        Console.WriteLine($"Average: {SpanNumerics.Average(sampleSpan):F2}");
        Console.WriteLine($"Variance: {SpanNumerics.Variance(sampleSpan):F3}");
        Console.WriteLine($"Std Dev: {SpanNumerics.StandardDeviation(sampleSpan):F3}");
    }

    static void DemoRemoveDuplicates()
    {
        var duplicateData = new int[] { 1, 1, 2, 2, 2, 3, 4, 4, 5 };
        Console.WriteLine($"Original: [{string.Join(", ", duplicateData)}]");

        var uniqueCount = SpanAlgorithms.RemoveDuplicates(duplicateData.AsSpan());
        Console.WriteLine($"After removing duplicates: [{string.Join(", ", duplicateData.AsSpan(0, uniqueCount).ToArray())}]");
        Console.WriteLine($"Unique count: {uniqueCount}");
    }

    static void DemoJoiningStrings()
    {
        var words = new string[] { "apple", "banana", "cherry" };

        var joinBuffer = new char[100];
        var joinResult = SpanStringExtensions.Join(words, " | ".AsSpan(), joinBuffer.AsSpan());

        if (joinResult > 0)
        {
            Console.WriteLine($"Joined: '{new string(joinBuffer.AsSpan(0, joinResult))}'");
        }
    }

    static void DemoPerformanceBenchmarking()
    {
        var testString = string.Join(",", Enumerable.Range(1, 100).Select(i => $"item{i}"));
        SpanPerformanceUtils.BenchmarkStringSplit(testString, 1000);

        var largeArray = Enumerable.Range(1, 10000).ToArray();
        SpanPerformanceUtils.BenchmarkNumericOperations(largeArray, 100);
    }

    static void DemoMemoryAllocation()
    {
        SpanPerformanceUtils.CompareAllocations();
    }

    static async Task DemoAsyncFileOperations()
    {
        // Create a temporary file for demonstration
        var tempFile = Path.GetTempFileName();
        var testData = "Line 1: Hello\nLine 2: World\nLine 3: Span operations\nLine 4: Memory<T>";
        await File.WriteAllTextAsync(tempFile, testData);

        Console.WriteLine("Processing file line by line:");
        int lineNumber = 0;
        await SpanFileOperations.ProcessTextFileAsync(tempFile, line =>
        {
            lineNumber++;
            Console.WriteLine($"  Line {lineNumber}: '{line.ToString()}'");
        });

        // Read file in chunks
        Console.WriteLine("\nReading file in chunks:");
        int chunkNumber = 0;
        await foreach (var chunk in SpanFileOperations.ReadFileChunksAsync(tempFile, 10))
        {
            chunkNumber++;
            var chunkText = Encoding.UTF8.GetString(chunk.Span);
            Console.WriteLine($"  Chunk {chunkNumber}: '{chunkText.Replace('\n', '\\')}'");
        }

        // Cleanup
        File.Delete(tempFile);
    }

    static void DemoAdvancedOperations()
    {
        // Safe memory copy with overlap detection
        var sourceData = new int[] { 1, 2, 3, 4, 5 };
        var destData = new int[7];

        MemoryOperations.SafeCopy(sourceData.AsMemory(), destData.AsMemory(2, 5));
        Console.WriteLine($"Safe copy result: [{string.Join(", ", destData)}]");

        // Vectorized sum demonstration
        var largeNumbers = Enumerable.Range(1, 1000).ToArray();
        var vectorSum = SpanNumerics.Sum(largeNumbers.AsSpan());
        Console.WriteLine($"Vectorized sum of 1-1000: {vectorSum} (expected: {1000 * 1001 / 2})");
    }
}