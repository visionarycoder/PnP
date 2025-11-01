# LINQ Extensions

**Description**: Custom LINQ operators and query extensions for advanced data manipulation. Includes batch processing, windowing functions, conditional operations, advanced aggregations, and specialized enumerable operations not available in standard LINQ.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// Batch and Chunking Operations
public static class BatchingExtensions
{
    // Split sequence into batches of specified size
    public static IEnumerable<T[]> Batch<T>(this IEnumerable<T> source, int batchSize)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));

        return BatchIterator(source, batchSize);
    }

    private static IEnumerable<T[]> BatchIterator<T>(IEnumerable<T> source, int batchSize)
    {
        using var enumerator = source.GetEnumerator();
        
        while (enumerator.MoveNext())
        {
            var batch = new List<T>(batchSize) { enumerator.Current };
            
            for (int i = 1; i < batchSize && enumerator.MoveNext(); i++)
            {
                batch.Add(enumerator.Current);
            }
            
            yield return batch.ToArray();
        }
    }

    // Split into chunks with overlap
    public static IEnumerable<T[]> Chunk<T>(this IEnumerable<T> source, int size, int overlap = 0)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size));
        if (overlap < 0) throw new ArgumentOutOfRangeException(nameof(overlap));

        return ChunkIterator(source, size, overlap);
    }

    private static IEnumerable<T[]> ChunkIterator<T>(IEnumerable<T> source, int size, int overlap)
    {
        var buffer = new Queue<T>();
        
        foreach (var item in source)
        {
            buffer.Enqueue(item);
            
            if (buffer.Count == size)
            {
                yield return buffer.ToArray();
                
                // Remove non-overlapping elements
                for (int i = 0; i < size - overlap; i++)
                {
                    if (buffer.Count > 0)
                        buffer.Dequeue();
                }
            }
        }
        
        // Yield remaining elements if any
        if (buffer.Count > 0)
        {
            yield return buffer.ToArray();
        }
    }

    // Split at predicate boundaries
    public static IEnumerable<IEnumerable<T>> SplitAt<T>(
        this IEnumerable<T> source, 
        Func<T, bool> predicate,
        bool includeDelimiter = false)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        return SplitAtIterator(source, predicate, includeDelimiter);
    }

    private static IEnumerable<IEnumerable<T>> SplitAtIterator<T>(
        IEnumerable<T> source, 
        Func<T, bool> predicate,
        bool includeDelimiter)
    {
        var current = new List<T>();
        
        foreach (var item in source)
        {
            if (predicate(item))
            {
                if (includeDelimiter)
                    current.Add(item);
                
                if (current.Any())
                {
                    yield return current;
                    current = new List<T>();
                }
            }
            else
            {
                current.Add(item);
            }
        }
        
        if (current.Any())
        {
            yield return current;
        }
    }
}

// Windowing and Sliding Operations
public static class WindowingExtensions
{
    // Create sliding window of specified size
    public static IEnumerable<T[]> SlidingWindow<T>(this IEnumerable<T> source, int windowSize)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (windowSize <= 0) throw new ArgumentOutOfRangeException(nameof(windowSize));

        return SlidingWindowIterator(source, windowSize);
    }

    private static IEnumerable<T[]> SlidingWindowIterator<T>(IEnumerable<T> source, int windowSize)
    {
        var buffer = new Queue<T>();
        
        foreach (var item in source)
        {
            buffer.Enqueue(item);
            
            if (buffer.Count > windowSize)
            {
                buffer.Dequeue();
            }
            
            if (buffer.Count == windowSize)
            {
                yield return buffer.ToArray();
            }
        }
    }

    // Pairwise operation (sliding window of 2)
    public static IEnumerable<(T Previous, T Current)> Pairwise<T>(this IEnumerable<T> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        return PairwiseIterator(source);
    }

    private static IEnumerable<(T Previous, T Current)> PairwiseIterator<T>(IEnumerable<T> source)
    {
        using var enumerator = source.GetEnumerator();
        
        if (!enumerator.MoveNext())
            yield break;
            
        var previous = enumerator.Current;
        
        while (enumerator.MoveNext())
        {
            var current = enumerator.Current;
            yield return (previous, current);
            previous = current;
        }
    }

    // Group consecutive elements with same key
    public static IEnumerable<IGrouping<TKey, T>> GroupConsecutive<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        IEqualityComparer<TKey>? comparer = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

        comparer ??= EqualityComparer<TKey>.Default;
        return GroupConsecutiveIterator(source, keySelector, comparer);
    }

    private static IEnumerable<IGrouping<TKey, T>> GroupConsecutiveIterator<T, TKey>(
        IEnumerable<T> source,
        Func<T, TKey> keySelector,
        IEqualityComparer<TKey> comparer)
    {
        using var enumerator = source.GetEnumerator();
        
        if (!enumerator.MoveNext())
            yield break;

        var currentKey = keySelector(enumerator.Current);
        var currentGroup = new List<T> { enumerator.Current };

        while (enumerator.MoveNext())
        {
            var itemKey = keySelector(enumerator.Current);
            
            if (comparer.Equals(currentKey, itemKey))
            {
                currentGroup.Add(enumerator.Current);
            }
            else
            {
                yield return new Grouping<TKey, T>(currentKey, currentGroup);
                currentKey = itemKey;
                currentGroup = new List<T> { enumerator.Current };
            }
        }
        
        if (currentGroup.Any())
        {
            yield return new Grouping<TKey, T>(currentKey, currentGroup);
        }
    }
}

// Advanced Distinct Operations
public static class DistinctExtensions
{
    // Distinct by property with custom comparer
    public static IEnumerable<T> DistinctBy<T, TKey>(
        this IEnumerable<T> source,
        Func<T, TKey> keySelector,
        IEqualityComparer<TKey>? comparer = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (keySelector == null) throw new ArgumentNullException(nameof(keySelector));

        return DistinctByIterator(source, keySelector, comparer ?? EqualityComparer<TKey>.Default);
    }

    private static IEnumerable<T> DistinctByIterator<T, TKey>(
        IEnumerable<T> source,
        Func<T, TKey> keySelector,
        IEqualityComparer<TKey> comparer)
    {
        var seenKeys = new HashSet<TKey>(comparer);
        
        foreach (var item in source)
        {
            var key = keySelector(item);
            if (seenKeys.Add(key))
            {
                yield return item;
            }
        }
    }

    // Remove duplicates but keep last occurrence
    public static IEnumerable<T> DistinctLast<T>(
        this IEnumerable<T> source,
        IEqualityComparer<T>? comparer = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        comparer ??= EqualityComparer<T>.Default;
        
        var items = source.ToList();
        var seen = new HashSet<T>(comparer);
        
        for (int i = items.Count - 1; i >= 0; i--)
        {
            if (seen.Add(items[i]))
            {
                yield return items[i];
            }
        }
    }

    // Get duplicates instead of distinct values
    public static IEnumerable<T> Duplicates<T>(
        this IEnumerable<T> source,
        IEqualityComparer<T>? comparer = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        comparer ??= EqualityComparer<T>.Default;
        
        var seen = new HashSet<T>(comparer);
        var duplicates = new HashSet<T>(comparer);
        
        foreach (var item in source)
        {
            if (!seen.Add(item))
            {
                duplicates.Add(item);
            }
        }
        
        return duplicates;
    }
}

// Conditional Operations
public static class ConditionalExtensions
{
    // Conditional Where based on predicate
    public static IEnumerable<T> WhereIf<T>(
        this IEnumerable<T> source,
        bool condition,
        Func<T, bool> predicate)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        return condition ? source.Where(predicate) : source;
    }

    // Apply transformation conditionally
    public static IEnumerable<TResult> SelectIf<T, TResult>(
        this IEnumerable<T> source,
        bool condition,
        Func<T, TResult> trueSelector,
        Func<T, TResult> falseSelector)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (trueSelector == null) throw new ArgumentNullException(nameof(trueSelector));
        if (falseSelector == null) throw new ArgumentNullException(nameof(falseSelector));

        var selector = condition ? trueSelector : falseSelector;
        return source.Select(selector);
    }

    // Take elements while condition is true, then skip rest
    public static IEnumerable<T> TakeWhileInclusive<T>(
        this IEnumerable<T> source,
        Func<T, bool> predicate)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        foreach (var item in source)
        {
            yield return item;
            
            if (!predicate(item))
                break;
        }
    }

    // Skip elements until condition is met (inclusive)
    public static IEnumerable<T> SkipUntil<T>(
        this IEnumerable<T> source,
        Func<T, bool> predicate)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        var found = false;
        
        foreach (var item in source)
        {
            if (!found && predicate(item))
            {
                found = true;
            }
            
            if (found)
            {
                yield return item;
            }
        }
    }

    // Take every nth element
    public static IEnumerable<T> TakeEvery<T>(this IEnumerable<T> source, int step)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (step <= 0) throw new ArgumentOutOfRangeException(nameof(step));

        return source.Where((item, index) => index % step == 0);
    }
}

// Advanced Aggregation Operations
public static class AggregationExtensions
{
    // Running sum/aggregation
    public static IEnumerable<TResult> Scan<T, TResult>(
        this IEnumerable<T> source,
        TResult seed,
        Func<TResult, T, TResult> accumulator)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (accumulator == null) throw new ArgumentNullException(nameof(accumulator));

        var result = seed;
        
        foreach (var item in source)
        {
            result = accumulator(result, item);
            yield return result;
        }
    }

    // Running aggregation without seed
    public static IEnumerable<T> Scan<T>(
        this IEnumerable<T> source,
        Func<T, T, T> accumulator)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (accumulator == null) throw new ArgumentNullException(nameof(accumulator));

        using var enumerator = source.GetEnumerator();
        
        if (!enumerator.MoveNext())
            yield break;
            
        var result = enumerator.Current;
        yield return result;
        
        while (enumerator.MoveNext())
        {
            result = accumulator(result, enumerator.Current);
            yield return result;
        }
    }

    // Variance calculation
    public static double Variance<T>(this IEnumerable<T> source, Func<T, double> selector)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (selector == null) throw new ArgumentNullException(nameof(selector));

        var values = source.Select(selector).ToArray();
        if (values.Length == 0) return 0;
        
        var mean = values.Average();
        return values.Sum(x => Math.Pow(x - mean, 2)) / values.Length;
    }

    // Standard deviation
    public static double StandardDeviation<T>(this IEnumerable<T> source, Func<T, double> selector)
    {
        return Math.Sqrt(source.Variance(selector));
    }

    // Mode (most frequent value)
    public static T Mode<T>(this IEnumerable<T> source, IEqualityComparer<T>? comparer = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        comparer ??= EqualityComparer<T>.Default;
        
        var frequencies = source
            .GroupBy(x => x, comparer)
            .ToDictionary(g => g.Key, g => g.Count());
            
        if (frequencies.Count == 0)
            throw new InvalidOperationException("Source sequence is empty");
            
        var maxFrequency = frequencies.Values.Max();
        return frequencies.First(kvp => kvp.Value == maxFrequency).Key;
    }

    // Percentile calculation
    public static double Percentile<T>(
        this IEnumerable<T> source,
        double percentile,
        Func<T, double> selector)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (selector == null) throw new ArgumentNullException(nameof(selector));
        if (percentile < 0 || percentile > 100) 
            throw new ArgumentOutOfRangeException(nameof(percentile));

        var values = source.Select(selector).OrderBy(x => x).ToArray();
        if (values.Length == 0) return 0;
        
        var index = percentile / 100.0 * (values.Length - 1);
        var lower = (int)Math.Floor(index);
        var upper = (int)Math.Ceiling(index);
        
        if (lower == upper)
            return values[lower];
            
        var weight = index - lower;
        return values[lower] * (1 - weight) + values[upper] * weight;
    }
}

// Specialized Set Operations
public static class SetExtensions
{
    // Symmetric difference (XOR)
    public static IEnumerable<T> SymmetricDifference<T>(
        this IEnumerable<T> first,
        IEnumerable<T> second,
        IEqualityComparer<T>? comparer = null)
    {
        if (first == null) throw new ArgumentNullException(nameof(first));
        if (second == null) throw new ArgumentNullException(nameof(second));

        comparer ??= EqualityComparer<T>.Default;
        
        var firstSet = new HashSet<T>(first, comparer);
        var secondSet = new HashSet<T>(second, comparer);
        
        return firstSet.Except(secondSet, comparer)
                     .Concat(secondSet.Except(firstSet, comparer));
    }

    // Check if sequences have any common elements
    public static bool Overlaps<T>(
        this IEnumerable<T> first,
        IEnumerable<T> second,
        IEqualityComparer<T>? comparer = null)
    {
        if (first == null) throw new ArgumentNullException(nameof(first));
        if (second == null) throw new ArgumentNullException(nameof(second));

        comparer ??= EqualityComparer<T>.Default;
        
        var firstSet = new HashSet<T>(first, comparer);
        return second.Any(item => firstSet.Contains(item));
    }

    // Power set (all subsets)
    public static IEnumerable<IEnumerable<T>> PowerSet<T>(this IEnumerable<T> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var list = source.ToList();
        var powerSetSize = 1 << list.Count;
        
        for (int mask = 0; mask < powerSetSize; mask++)
        {
            yield return list.Where((item, index) => (mask & (1 << index)) != 0);
        }
    }

    // Cartesian product
    public static IEnumerable<(T1, T2)> CartesianProduct<T1, T2>(
        this IEnumerable<T1> first,
        IEnumerable<T2> second)
    {
        if (first == null) throw new ArgumentNullException(nameof(first));
        if (second == null) throw new ArgumentNullException(nameof(second));

        return from item1 in first
               from item2 in second
               select (item1, item2);
    }
}

// String-specific LINQ Extensions
public static class StringLinqExtensions
{
    // Join with different separators
    public static string JoinWith<T>(
        this IEnumerable<T> source,
        string separator,
        Func<T, string>? selector = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (separator == null) throw new ArgumentNullException(nameof(separator));

        selector ??= item => item?.ToString() ?? "";
        return string.Join(separator, source.Select(selector));
    }

    // Join with different separators for last element
    public static string JoinWithOxfordComma<T>(
        this IEnumerable<T> source,
        string separator = ", ",
        string lastSeparator = ", and ",
        Func<T, string>? selector = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        selector ??= item => item?.ToString() ?? "";
        var items = source.Select(selector).ToArray();
        
        return items.Length switch
        {
            0 => "",
            1 => items[0],
            2 => $"{items[0]} and {items[1]}",
            _ => string.Join(separator, items.Take(items.Length - 1)) + lastSeparator + items.Last()
        };
    }

    // Convert to dictionary with case-insensitive keys
    public static Dictionary<string, T> ToCaseInsensitiveDictionary<T>(
        this IEnumerable<KeyValuePair<string, T>> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        return source.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value,
            StringComparer.OrdinalIgnoreCase);
    }
}

// Async LINQ Extensions
public static class AsyncLinqExtensions
{
    // Async Where
    public static async IAsyncEnumerable<T> WhereAsync<T>(
        this IAsyncEnumerable<T> source,
        Func<T, Task<bool>> predicate,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            if (await predicate(item))
            {
                yield return item;
            }
        }
    }

    // Async Select
    public static async IAsyncEnumerable<TResult> SelectAsync<T, TResult>(
        this IAsyncEnumerable<T> source,
        Func<T, Task<TResult>> selector,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (selector == null) throw new ArgumentNullException(nameof(selector));

        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            yield return await selector(item);
        }
    }

    // Async batch processing
    public static async IAsyncEnumerable<T[]> BatchAsync<T>(
        this IAsyncEnumerable<T> source,
        int batchSize,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));

        var batch = new List<T>(batchSize);
        
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            batch.Add(item);
            
            if (batch.Count == batchSize)
            {
                yield return batch.ToArray();
                batch.Clear();
            }
        }
        
        if (batch.Count > 0)
        {
            yield return batch.ToArray();
        }
    }

    // Convert async enumerable to list
    public static async Task<List<T>> ToListAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var list = new List<T>();
        
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            list.Add(item);
        }
        
        return list;
    }
}

// Performance-optimized extensions
public static class PerformanceExtensions
{
    // Fast count for collections
    public static int FastCount<T>(this IEnumerable<T> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        return source switch
        {
            ICollection<T> collection => collection.Count,
            ICollection nonGenericCollection => nonGenericCollection.Count,
            _ => source.Count()
        };
    }

    // Check if sequence has exactly n elements
    public static bool HasExactly<T>(this IEnumerable<T> source, int count)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

        if (source is ICollection<T> collection)
            return collection.Count == count;

        return source.Take(count + 1).Count() == count;
    }

    // Check if sequence has at least n elements
    public static bool HasAtLeast<T>(this IEnumerable<T> source, int count)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (count < 0) throw new ArgumentOutOfRangeException(nameof(count));

        if (source is ICollection<T> collection)
            return collection.Count >= count;

        return source.Take(count).Count() == count;
    }

    // Memory-efficient range generation
    public static IEnumerable<int> RangeFromTo(int start, int end, int step = 1)
    {
        if (step == 0) throw new ArgumentException("Step cannot be zero", nameof(step));

        if (step > 0)
        {
            for (int i = start; i <= end; i += step)
                yield return i;
        }
        else
        {
            for (int i = start; i >= end; i += step)
                yield return i;
        }
    }
}

// Supporting classes
public class Grouping<TKey, TElement> : IGrouping<TKey, TElement>
{
    private readonly List<TElement> _elements;

    public Grouping(TKey key, IEnumerable<TElement> elements)
    {
        Key = key;
        _elements = elements.ToList();
    }

    public TKey Key { get; }

    public IEnumerator<TElement> GetEnumerator() => _elements.GetEnumerator();

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
}

// Real-world usage examples
public static class LinqExamples
{
    public static void DemonstrateExtensions()
    {
        // Sample data
        var numbers = Enumerable.Range(1, 20);
        var words = new[] { "apple", "banana", "cherry", "date", "elderberry", "fig", "grape" };
        var people = new[]
        {
            new { Name = "John", Age = 25, City = "New York" },
            new { Name = "Jane", Age = 30, City = "Los Angeles" },
            new { Name = "Bob", Age = 25, City = "New York" },
            new { Name = "Alice", Age = 35, City = "Chicago" }
        };

        // Batching examples
        var numberBatches = numbers.Batch(5);
        Console.WriteLine("Number batches:");
        foreach (var batch in numberBatches)
        {
            Console.WriteLine($"  [{string.Join(", ", batch)}]");
        }

        // Sliding window
        var slidingWindows = numbers.Take(10).SlidingWindow(3);
        Console.WriteLine("\nSliding windows:");
        foreach (var window in slidingWindows)
        {
            Console.WriteLine($"  [{string.Join(", ", window)}]");
        }

        // Pairwise operations
        var differences = numbers.Take(10).Pairwise()
            .Select(pair => pair.Current - pair.Previous);
        Console.WriteLine($"\nPairwise differences: [{string.Join(", ", differences)}]");

        // Distinct by property
        var uniquePeopleByAge = people.DistinctBy(p => p.Age);
        Console.WriteLine("\nUnique people by age:");
        foreach (var person in uniquePeopleByAge)
        {
            Console.WriteLine($"  {person.Name} (Age: {person.Age})");
        }

        // Conditional operations
        var filteredWords = words.WhereIf(true, w => w.Length > 5);
        Console.WriteLine($"\nLong words: [{string.Join(", ", filteredWords)}]");

        // Running aggregation
        var runningSums = numbers.Take(5).Scan(0, (sum, x) => sum + x);
        Console.WriteLine($"\nRunning sums: [{string.Join(", ", runningSums)}]");

        // Statistical operations
        var ages = people.Select(p => (double)p.Age);
        Console.WriteLine($"\nAge statistics:");
        Console.WriteLine($"  Variance: {ages.Variance(x => x):F2}");
        Console.WriteLine($"  Standard Deviation: {ages.StandardDeviation(x => x):F2}");
        Console.WriteLine($"  Mode: {people.Select(p => p.Age).Mode()}");
        Console.WriteLine($"  75th Percentile: {ages.Percentile(75, x => x):F1}");

        // String operations
        var joinedWords = words.JoinWith(" | ");
        Console.WriteLine($"\nJoined words: {joinedWords}");
        
        var oxfordJoined = words.Take(4).JoinWithOxfordComma();
        Console.WriteLine($"Oxford comma: {oxfordJoined}");
    }
}
```

**Usage**:

```csharp
// Example 1: Batch processing large datasets
var largeDataset = Enumerable.Range(1, 1000);

foreach (var batch in largeDataset.Batch(100))
{
    // Process each batch of 100 items
    Console.WriteLine($"Processing batch with {batch.Length} items");
    
    // Simulate batch processing
    var batchSum = batch.Sum();
    Console.WriteLine($"Batch sum: {batchSum}");
}

// Example 2: Sliding window analysis for time series data
var temperatures = new[] { 20.5, 21.0, 22.5, 24.0, 23.5, 22.0, 20.5, 19.0, 18.5, 20.0 };

var temperatureTrends = temperatures
    .SlidingWindow(3)
    .Select(window => new
    {
        Average = window.Average(),
        Min = window.Min(),
        Max = window.Max(),
        Trend = window[2] - window[0] // Compare last with first
    });

Console.WriteLine("Temperature trends (3-point window):");
foreach (var trend in temperatureTrends)
{
    Console.WriteLine($"Avg: {trend.Average:F1}°C, " +
                     $"Min: {trend.Min:F1}°C, " +
                     $"Max: {trend.Max:F1}°C, " +
                     $"Trend: {trend.Trend:+0.0;-0.0}°C");
}

// Example 3: Data analysis with grouping and statistics
var salesData = new[]
{
    new { Product = "Laptop", Category = "Electronics", Sales = 15000, Month = "Jan" },
    new { Product = "Mouse", Category = "Electronics", Sales = 2500, Month = "Jan" },
    new { Product = "Desk", Category = "Furniture", Sales = 8000, Month = "Jan" },
    new { Product = "Chair", Category = "Furniture", Sales = 5500, Month = "Jan" },
    new { Product = "Laptop", Category = "Electronics", Sales = 16000, Month = "Feb" },
    new { Product = "Mouse", Category = "Electronics", Sales = 2700, Month = "Feb" }
};

// Group consecutive items by category and calculate statistics
var categoryAnalysis = salesData
    .OrderBy(s => s.Category)
    .GroupConsecutive(s => s.Category)
    .Select(group => new
    {
        Category = group.Key,
        TotalSales = group.Sum(s => s.Sales),
        AverageSales = group.Average(s => (double)s.Sales),
        ProductCount = group.Count(),
        TopProduct = group.OrderByDescending(s => s.Sales).First().Product
    });

Console.WriteLine("\nCategory Analysis:");
foreach (var analysis in categoryAnalysis)
{
    Console.WriteLine($"{analysis.Category}:");
    Console.WriteLine($"  Total Sales: ${analysis.TotalSales:N0}");
    Console.WriteLine($"  Average Sales: ${analysis.AverageSales:N0}");
    Console.WriteLine($"  Products: {analysis.ProductCount}");
    Console.WriteLine($"  Top Product: {analysis.TopProduct}");
}

// Example 4: Advanced filtering with conditional operations
var inventory = new[]
{
    new { Item = "Widget A", Stock = 150, Price = 25.50m, Category = "Tools" },
    new { Item = "Widget B", Stock = 75, Price = 15.25m, Category = "Parts" },
    new { Item = "Gadget X", Stock = 200, Price = 45.00m, Category = "Tools" },
    new { Item = "Component Y", Stock = 50, Price = 8.75m, Category = "Parts" }
};

bool lowStockAlert = true;
bool expensiveItemsOnly = false;

var filteredInventory = inventory
    .WhereIf(lowStockAlert, item => item.Stock < 100)
    .WhereIf(expensiveItemsOnly, item => item.Price > 30m)
    .OrderBy(item => item.Stock);

Console.WriteLine($"\nFiltered Inventory (Low Stock: {lowStockAlert}, Expensive Only: {expensiveItemsOnly}):");
foreach (var item in filteredInventory)
{
    Console.WriteLine($"  {item.Item}: {item.Stock} units @ ${item.Price}");
}

// Example 5: Text processing with string extensions
var sentences = new[]
{
    "The quick brown fox jumps over the lazy dog",
    "Pack my box with five dozen liquor jugs",
    "How vexingly quick daft zebras jump"
};

var wordAnalysis = sentences
    .SelectMany(sentence => sentence.Split(' '))
    .Select(word => word.ToLowerInvariant().Trim('.', ',', '!', '?'))
    .GroupBy(word => word)
    .Select(group => new { Word = group.Key, Count = group.Count() })
    .OrderByDescending(x => x.Count)
    .ThenBy(x => x.Word);

Console.WriteLine("\nWord frequency analysis:");
foreach (var word in wordAnalysis.Take(10))
{
    Console.WriteLine($"  '{word.Word}': {word.Count} occurrences");
}

// Join words with custom formatting
var topWords = wordAnalysis.Take(5).Select(w => w.Word);
var formattedList = topWords.JoinWithOxfordComma(", ", ", and ");
Console.WriteLine($"\nTop 5 words: {formattedList}");

// Example 6: Statistical analysis of numeric data
var testScores = new[] { 85, 92, 78, 96, 88, 76, 94, 89, 82, 91, 87, 93, 79, 86, 90 };

// Running statistics
var runningAverages = testScores
    .Scan((sum: 0, count: 0), (acc, score) => (acc.sum + score, acc.count + 1))
    .Select(acc => (double)acc.sum / acc.count);

Console.WriteLine("\nRunning averages:");
var scoreIndex = 0;
foreach (var average in runningAverages)
{
    Console.WriteLine($"  After score {scoreIndex + 1}: {average:F2}");
    scoreIndex++;
}

// Comprehensive statistics
var doubleScores = testScores.Select(s => (double)s);
Console.WriteLine($"\nTest Score Statistics:");
Console.WriteLine($"  Mean: {doubleScores.Average():F2}");
Console.WriteLine($"  Variance: {doubleScores.Variance(x => x):F2}");
Console.WriteLine($"  Standard Deviation: {doubleScores.StandardDeviation(x => x):F2}");
Console.WriteLine($"  Mode: {testScores.Mode()}");
Console.WriteLine($"  25th Percentile: {doubleScores.Percentile(25, x => x):F1}");
Console.WriteLine($"  Median (50th): {doubleScores.Percentile(50, x => x):F1}");
Console.WriteLine($"  75th Percentile: {doubleScores.Percentile(75, x => x):F1}");

// Example 7: Set operations and combinatorics
var set1 = new[] { 1, 2, 3, 4, 5 };
var set2 = new[] { 4, 5, 6, 7, 8 };

Console.WriteLine($"\nSet Operations:");
Console.WriteLine($"Set 1: [{set1.JoinWith(", ")}]");
Console.WriteLine($"Set 2: [{set2.JoinWith(", ")}]");
Console.WriteLine($"Symmetric Difference: [{set1.SymmetricDifference(set2).JoinWith(", ")}]");
Console.WriteLine($"Overlaps: {set1.Overlaps(set2)}");

// Power set of small set
var smallSet = new[] { "A", "B", "C" };
var powerSet = smallSet.PowerSet();

Console.WriteLine($"\nPower set of {{{smallSet.JoinWith(", ")}}}:");
foreach (var subset in powerSet)
{
    Console.WriteLine($"  {{{subset.JoinWith(", ")}}}");
}

// Cartesian product
var colors = new[] { "Red", "Green", "Blue" };
var sizes = new[] { "Small", "Large" };
var combinations = colors.CartesianProduct(sizes);

Console.WriteLine($"\nCartesian Product (Colors × Sizes):");
foreach (var combination in combinations)
{
    Console.WriteLine($"  {combination.Item1} {combination.Item2}");
}

// Example 8: Performance optimizations
var largeCollection = Enumerable.Range(1, 1_000_000);

// Fast count checking
Console.WriteLine($"\nPerformance Examples:");
var hasManyItems = largeCollection.HasAtLeast(500_000);
Console.WriteLine($"Has at least 500K items: {hasManyItems}");

var hasExactly = Enumerable.Range(1, 100).HasExactly(100);
Console.WriteLine($"Range 1-100 has exactly 100 items: {hasExactly}");

// Memory-efficient range generation
var customRange = PerformanceExtensions.RangeFromTo(10, 1, -2);
Console.WriteLine($"Custom range (10 to 1, step -2): [{customRange.JoinWith(", ")}]");

// Example 9: Async LINQ operations
async Task AsyncLinqDemo()
{
    // Simulate async data source
    async IAsyncEnumerable<int> GenerateNumbersAsync()
    {
        for (int i = 1; i <= 10; i++)
        {
            await Task.Delay(100); // Simulate async work
            yield return i;
        }
    }

    Console.WriteLine("\nAsync LINQ Operations:");
    
    // Async filtering and transformation
    var evenDoubled = GenerateNumbersAsync()
        .WhereAsync(async x => 
        {
            await Task.Delay(10); // Simulate async validation
            return x % 2 == 0;
        })
        .SelectAsync(async x =>
        {
            await Task.Delay(10); // Simulate async transformation
            return x * 2;
        });

    await foreach (var number in evenDoubled)
    {
        Console.WriteLine($"  Processed: {number}");
    }

    // Async batching
    var batches = GenerateNumbersAsync().BatchAsync(3);
    
    await foreach (var batch in batches)
    {
        Console.WriteLine($"  Batch: [{batch.JoinWith(", ")}]");
    }
}

await AsyncLinqDemo();

// Example 10: Complex data analysis pipeline
var customerData = new[]
{
    new { Id = 1, Name = "Alice Johnson", Age = 28, City = "Seattle", Orders = 15, TotalSpent = 2500m },
    new { Id = 2, Name = "Bob Smith", Age = 35, City = "Portland", Orders = 8, TotalSpent = 1200m },
    new { Id = 3, Name = "Carol Davis", Age = 42, City = "Seattle", Orders = 22, TotalSpent = 3800m },
    new { Id = 4, Name = "David Wilson", Age = 29, City = "Vancouver", Orders = 12, TotalSpent = 1800m },
    new { Id = 5, Name = "Eva Brown", Age = 33, City = "Portland", Orders = 18, TotalSpent = 2900m }
};

var customerAnalysis = customerData
    .GroupBy(c => c.City)
    .Select(cityGroup => new
    {
        City = cityGroup.Key,
        CustomerCount = cityGroup.Count(),
        AverageAge = cityGroup.Average(c => (double)c.Age),
        TotalRevenue = cityGroup.Sum(c => c.TotalSpent),
        AverageOrderValue = cityGroup.Average(c => c.TotalSpent / c.Orders),
        TopCustomer = cityGroup.OrderByDescending(c => c.TotalSpent).First(),
        AgeDistribution = new
        {
            Under30 = cityGroup.Count(c => c.Age < 30),
            Between30And40 = cityGroup.Count(c => c.Age >= 30 && c.Age < 40),
            Over40 = cityGroup.Count(c => c.Age >= 40)
        }
    })
    .OrderByDescending(analysis => analysis.TotalRevenue);

Console.WriteLine("\nCustomer Analysis by City:");
foreach (var analysis in customerAnalysis)
{
    Console.WriteLine($"\n{analysis.City}:");
    Console.WriteLine($"  Customers: {analysis.CustomerCount}");
    Console.WriteLine($"  Average Age: {analysis.AverageAge:F1} years");
    Console.WriteLine($"  Total Revenue: ${analysis.TotalRevenue:N0}");
    Console.WriteLine($"  Avg Order Value: ${analysis.AverageOrderValue:F0}");
    Console.WriteLine($"  Top Customer: {analysis.TopCustomer.Name} (${analysis.TopCustomer.TotalSpent:N0})");
    Console.WriteLine($"  Age Distribution: <30: {analysis.AgeDistribution.Under30}, " +
                     $"30-39: {analysis.AgeDistribution.Between30And40}, " +
                     $"40+: {analysis.AgeDistribution.Over40}");
}

// Find patterns in customer behavior
var behaviorPatterns = customerData
    .DistinctBy(c => new { AgeGroup = c.Age / 10 * 10, SpendingTier = c.TotalSpent / 1000m })
    .Select(c => new
    {
        Pattern = $"Age {c.Age / 10 * 10}s, ${c.TotalSpent / 1000m:F0}K+ spending",
        Example = c.Name,
        Frequency = customerData.Count(other => 
            other.Age / 10 == c.Age / 10 && 
            other.TotalSpent / 1000m == c.TotalSpent / 1000m)
    })
    .Where(p => p.Frequency > 0)
    .OrderByDescending(p => p.Frequency);

Console.WriteLine("\nCustomer Behavior Patterns:");
foreach (var pattern in behaviorPatterns)
{
    Console.WriteLine($"  {pattern.Pattern}: {pattern.Frequency} customers (e.g., {pattern.Example})");
}
```

**Notes**:

- These extensions follow LINQ conventions and integrate seamlessly with existing LINQ methods
- Most extensions use deferred execution (yield return) for memory efficiency
- Batch operations are essential for processing large datasets without memory issues
- Statistical functions provide comprehensive data analysis capabilities
- Async extensions support modern async/await patterns with IAsyncEnumerable
- Performance extensions optimize common operations like counting and range generation
- Set operations extend beyond basic Union/Intersect to include symmetric difference and overlap detection
- String extensions provide specialized formatting and case-insensitive operations
- All extensions include proper null checking and argument validation
- Consider memory implications when using operations that materialize sequences (ToArray, ToList)

**Prerequisites**:

- .NET Framework 4.0+ or .NET Core for basic LINQ support
- .NET Core 3.0+ or .NET 5+ for IAsyncEnumerable support
- C# 8.0+ for async streams and nullable reference types
- Understanding of LINQ principles, deferred execution, and enumerable patterns
- Familiarity with generic types and extension methods

**Related Snippets**:

- [Async Enumerable](async-enumerable.md) - Streaming async operations
- [Performance Optimization](performance-optimization.md) - Memory and speed optimization techniques
- [Data Structures](data-structures.md) - Custom collection implementations
- [String Manipulation](string-manipulation.md) - Advanced string processing patterns
