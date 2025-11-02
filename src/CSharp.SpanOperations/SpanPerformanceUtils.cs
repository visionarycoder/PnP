using System;
using System.Diagnostics;
using System.Linq;

namespace Snippets.CSharp.SpanOperations;

/// <summary>
/// Performance comparison utilities for span operations
/// </summary>
public static class SpanPerformanceUtils
{
    /// <summary>
    /// Benchmark span operations vs traditional approaches
    /// </summary>
    public static void BenchmarkStringSplit(string testString, int iterations = 10000)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // Traditional string.Split
        stopwatch.Restart();
        for (int i = 0; i < iterations; i++)
        {
            var parts = testString.Split(',');
            // Consume results to prevent optimization
            _ = parts.Length;
        }
        stopwatch.Stop();
        Console.WriteLine($"String.Split: {stopwatch.ElapsedMilliseconds}ms");
        
        // Span-based split
        stopwatch.Restart();
        for (int i = 0; i < iterations; i++)
        {
            int count = 0;
            foreach (var part in testString.AsSpan().Split(','))
            {
                count++;
            }
            // Consume results
            _ = count;
        }
        stopwatch.Stop();
        Console.WriteLine($"Span.Split: {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Benchmark numeric operations
    /// </summary>
    public static void BenchmarkNumericOperations(int[] data, int iterations = 1000)
    {
        var stopwatch = Stopwatch.StartNew();
        
        // LINQ Sum
        stopwatch.Restart();
        for (int i = 0; i < iterations; i++)
        {
            _ = data.Sum();
        }
        stopwatch.Stop();
        Console.WriteLine($"LINQ Sum: {stopwatch.ElapsedMilliseconds}ms");
        
        // Span Sum
        stopwatch.Restart();
        for (int i = 0; i < iterations; i++)
        {
            _ = SpanNumerics.Sum(data.AsSpan());
        }
        stopwatch.Stop();
        Console.WriteLine($"Span Sum: {stopwatch.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// Memory allocation comparison
    /// </summary>
    public static void CompareAllocations()
    {
        const int iterations = 10000;
        
        Console.WriteLine("Allocation Comparison:");
        
        // Measure before
        var before = GC.GetTotalMemory(true);
        
        // Traditional approach (allocates strings)
        for (int i = 0; i < iterations; i++)
        {
            var text = $"Item {i}";
            var parts = text.Split(' ');
            var trimmed = parts[1].Trim();
        }
        
        var afterTraditional = GC.GetTotalMemory(false);
        
        // Force GC
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var afterGC = GC.GetTotalMemory(true);
        
        // Span approach (minimal allocations)
        for (int i = 0; i < iterations; i++)
        {
            var text = $"Item {i}".AsSpan();
            foreach (var part in text.Split(' '))
            {
                var trimmed = part.TrimFast();
                break; // Just process first part for comparison
            }
        }
        
        var afterSpan = GC.GetTotalMemory(false);
        
        Console.WriteLine($"Traditional allocated: {afterTraditional - before:N0} bytes");
        Console.WriteLine($"Span allocated: {afterSpan - afterGC:N0} bytes");
        Console.WriteLine($"Reduction: {((double)(afterTraditional - before) / (afterSpan - afterGC)):F1}x less allocation");
    }
}