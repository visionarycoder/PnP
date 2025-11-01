# Performance LINQ Extensions

**Description**: High-performance LINQ extensions optimized for memory efficiency, speed, and scalability. Includes memory pooling, span-based operations, vectorization support, and specialized algorithms for large-scale data processing.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

// Memory-efficient enumeration with ArrayPool
public static class PooledLinqExtensions
{
    // Convert to array using ArrayPool for better memory management
    public static T[] ToPooledArray<T>(this IEnumerable<T> source, out ArrayPool<T> pool)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        pool = ArrayPool<T>.Shared;
        
        if (source is ICollection<T> collection)
        {
            var array = pool.Rent(collection.Count);
            collection.CopyTo(array, 0);
            return array;
        }

        var list = new List<T>(source);
        var pooledArray = pool.Rent(list.Count);
        list.CopyTo(pooledArray, 0);
        return pooledArray;
    }

    // Batch processing with memory pooling
    public static IEnumerable<ReadOnlyMemory<T>> BatchPooled<T>(
        this IEnumerable<T> source, 
        int batchSize)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize));

        return BatchPooledIterator(source, batchSize);
    }

    private static IEnumerable<ReadOnlyMemory<T>> BatchPooledIterator<T>(
        IEnumerable<T> source, 
        int batchSize)
    {
        var pool = ArrayPool<T>.Shared;
        var buffer = pool.Rent(batchSize);
        var count = 0;

        try
        {
            foreach (var item in source)
            {
                buffer[count++] = item;
                
                if (count == batchSize)
                {
                    yield return new ReadOnlyMemory<T>(buffer, 0, count);
                    count = 0;
                }
            }

            if (count > 0)
            {
                yield return new ReadOnlyMemory<T>(buffer, 0, count);
            }
        }
        finally
        {
            pool.Return(buffer);
        }
    }

    // Aggregate with memory pooling for intermediate results
    public static TResult AggregatePooled<TSource, TAccumulate, TResult>(
        this IEnumerable<TSource> source,
        TAccumulate seed,
        Func<TAccumulate, TSource, TAccumulate> func,
        Func<TAccumulate, TResult> resultSelector,
        int bufferSize = 1024)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (func == null) throw new ArgumentNullException(nameof(func));
        if (resultSelector == null) throw new ArgumentNullException(nameof(resultSelector));

        var pool = ArrayPool<TSource>.Shared;
        var buffer = pool.Rent(bufferSize);
        var accumulator = seed;

        try
        {
            var bufferCount = 0;
            
            foreach (var item in source)
            {
                buffer[bufferCount++] = item;
                
                if (bufferCount == bufferSize)
                {
                    for (int i = 0; i < bufferCount; i++)
                    {
                        accumulator = func(accumulator, buffer[i]);
                    }
                    bufferCount = 0;
                }
            }

            // Process remaining items
            for (int i = 0; i < bufferCount; i++)
            {
                accumulator = func(accumulator, buffer[i]);
            }

            return resultSelector(accumulator);
        }
        finally
        {
            pool.Return(buffer);
        }
    }
}

// Span-based high-performance operations
public static class SpanLinqExtensions
{
    // Fast sum for numeric spans
    public static long SumFast(this ReadOnlySpan<int> span)
    {
        long sum = 0;
        
        // Process in chunks for better performance
        var chunks = span.Length / 8;
        var remainder = span.Length % 8;

        for (int i = 0; i < chunks; i++)
        {
            var offset = i * 8;
            sum += span[offset] + span[offset + 1] + span[offset + 2] + span[offset + 3] +
                   span[offset + 4] + span[offset + 5] + span[offset + 6] + span[offset + 7];
        }

        // Handle remainder
        var remainderStart = chunks * 8;
        for (int i = 0; i < remainder; i++)
        {
            sum += span[remainderStart + i];
        }

        return sum;
    }

    // Vectorized operations where supported
    public static void AddVectorized(this Span<float> destination, ReadOnlySpan<float> source)
    {
        if (destination.Length != source.Length)
            throw new ArgumentException("Spans must have the same length");

        if (Vector.IsHardwareAccelerated && destination.Length >= Vector<float>.Count)
        {
            var vectorLength = Vector<float>.Count;
            var vectorCount = destination.Length / vectorLength;

            for (int i = 0; i < vectorCount; i++)
            {
                var offset = i * vectorLength;
                var destVector = new Vector<float>(destination.Slice(offset, vectorLength));
                var srcVector = new Vector<float>(source.Slice(offset, vectorLength));
                var result = destVector + srcVector;
                result.CopyTo(destination.Slice(offset, vectorLength));
            }

            // Handle remainder
            var remaining = destination.Length % vectorLength;
            if (remaining > 0)
            {
                var remainderStart = vectorCount * vectorLength;
                var destRemainder = destination.Slice(remainderStart);
                var srcRemainder = source.Slice(remainderStart);
                
                for (int i = 0; i < remaining; i++)
                {
                    destRemainder[i] += srcRemainder[i];
                }
            }
        }
        else
        {
            // Fallback to scalar operations
            for (int i = 0; i < destination.Length; i++)
            {
                destination[i] += source[i];
            }
        }
    }

    // Fast binary search on sorted spans
    public static int BinarySearchFast<T>(this ReadOnlySpan<T> span, T value) 
        where T : IComparable<T>
    {
        var left = 0;
        var right = span.Length - 1;

        while (left <= right)
        {
            var mid = left + (right - left) / 2;
            var comparison = span[mid].CompareTo(value);

            if (comparison == 0)
                return mid;
            else if (comparison < 0)
                left = mid + 1;
            else
                right = mid - 1;
        }

        return ~left; // Return bitwise complement of insertion point
    }

    // Memory-efficient string operations
    public static bool ContainsAnyFast(this ReadOnlySpan<char> span, ReadOnlySpan<char> values)
    {
        foreach (var value in values)
        {
            if (span.Contains(value))
                return true;
        }
        return false;
    }

    // Fast equality comparison
    public static bool SequenceEqualFast<T>(this ReadOnlySpan<T> first, ReadOnlySpan<T> second) 
        where T : IEquatable<T>
    {
        return first.SequenceEqual(second);
    }
}

// Lock-free and thread-safe operations
public static class ConcurrentLinqExtensions
{
    // Parallel aggregation with partitioning
    public static TResult ParallelAggregate<T, TResult>(
        this IEnumerable<T> source,
        TResult seed,
        Func<TResult, T, TResult> func,
        Func<TResult, TResult, TResult> combiner,
        int? maxDegreeOfParallelism = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (func == null) throw new ArgumentNullException(nameof(func));
        if (combiner == null) throw new ArgumentNullException(nameof(combiner));

        var parallelOptions = new ParallelQuery<T>(source);
        
        if (maxDegreeOfParallelism.HasValue)
        {
            parallelOptions = parallelOptions.WithDegreeOfParallelism(maxDegreeOfParallelism.Value);
        }

        return parallelOptions.Aggregate(seed, func, combiner);
    }

    // Thread-safe counting with atomic operations
    public static long CountAtomic<T>(this IEnumerable<T> source, Func<T, bool> predicate)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (predicate == null) throw new ArgumentNullException(nameof(predicate));

        long count = 0;

        Parallel.ForEach(source, item =>
        {
            if (predicate(item))
            {
                Interlocked.Increment(ref count);
            }
        });

        return count;
    }

    // Lock-free parallel processing with partitioner
    public static void ParallelForEachPartitioned<T>(
        this IEnumerable<T> source,
        Action<T> action,
        int partitionSize = 1000)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (action == null) throw new ArgumentNullException(nameof(action));

        var partitioner = Partitioner.Create(source, true);
        
        Parallel.ForEach(partitioner, action);
    }

    // Concurrent collection building
    public static ConcurrentBag<TResult> SelectConcurrent<T, TResult>(
        this IEnumerable<T> source,
        Func<T, TResult> selector,
        int maxDegreeOfParallelism = -1)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (selector == null) throw new ArgumentNullException(nameof(selector));

        var results = new System.Collections.Concurrent.ConcurrentBag<TResult>();
        var options = new ParallelOptions();
        
        if (maxDegreeOfParallelism > 0)
            options.MaxDegreeOfParallelism = maxDegreeOfParallelism;

        Parallel.ForEach(source, options, item =>
        {
            results.Add(selector(item));
        });

        return results;
    }
}

// Specialized high-performance algorithms
public static class OptimizedAlgorithmExtensions
{
    // Fast median calculation using Quickselect algorithm
    public static T QuickSelectMedian<T>(this IEnumerable<T> source) where T : IComparable<T>
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var array = source.ToArray();
        if (array.Length == 0) throw new InvalidOperationException("Source is empty");

        var medianIndex = array.Length / 2;
        return QuickSelect(array, 0, array.Length - 1, medianIndex);
    }

    private static T QuickSelect<T>(T[] array, int left, int right, int k) where T : IComparable<T>
    {
        if (left == right) return array[left];

        var pivotIndex = Partition(array, left, right);

        if (k == pivotIndex)
            return array[k];
        else if (k < pivotIndex)
            return QuickSelect(array, left, pivotIndex - 1, k);
        else
            return QuickSelect(array, pivotIndex + 1, right, k);
    }

    private static int Partition<T>(T[] array, int left, int right) where T : IComparable<T>
    {
        var pivot = array[right];
        var i = left;

        for (int j = left; j < right; j++)
        {
            if (array[j].CompareTo(pivot) <= 0)
            {
                (array[i], array[j]) = (array[j], array[i]);
                i++;
            }
        }

        (array[i], array[right]) = (array[right], array[i]);
        return i;
    }

    // Optimized top-K selection using min-heap
    public static IEnumerable<T> TopK<T>(this IEnumerable<T> source, int k, IComparer<T>? comparer = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (k <= 0) throw new ArgumentOutOfRangeException(nameof(k));

        comparer ??= Comparer<T>.Default;
        var heap = new SortedSet<T>(comparer);

        foreach (var item in source)
        {
            if (heap.Count < k)
            {
                heap.Add(item);
            }
            else if (comparer.Compare(item, heap.Min) > 0)
            {
                heap.Remove(heap.Min);
                heap.Add(item);
            }
        }

        return heap.Reverse();
    }

    // Reservoir sampling for random selection
    public static T[] ReservoirSample<T>(this IEnumerable<T> source, int sampleSize, Random? random = null)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (sampleSize <= 0) throw new ArgumentOutOfRangeException(nameof(sampleSize));

        random ??= new Random();
        var reservoir = new T[sampleSize];
        var count = 0;

        foreach (var item in source)
        {
            if (count < sampleSize)
            {
                reservoir[count] = item;
            }
            else
            {
                var randomIndex = random.Next(0, count + 1);
                if (randomIndex < sampleSize)
                {
                    reservoir[randomIndex] = item;
                }
            }
            count++;
        }

        // If we have fewer items than sample size, return smaller array
        if (count < sampleSize)
        {
            var result = new T[count];
            Array.Copy(reservoir, result, count);
            return result;
        }

        return reservoir;
    }

    // Bloom filter for membership testing
    public static BloomFilter<T> ToBloomFilter<T>(
        this IEnumerable<T> source,
        int expectedElements,
        double falsePositiveRate = 0.01)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (expectedElements <= 0) throw new ArgumentOutOfRangeException(nameof(expectedElements));
        if (falsePositiveRate <= 0 || falsePositiveRate >= 1) 
            throw new ArgumentOutOfRangeException(nameof(falsePositiveRate));

        var bloomFilter = new BloomFilter<T>(expectedElements, falsePositiveRate);
        
        foreach (var item in source)
        {
            bloomFilter.Add(item);
        }

        return bloomFilter;
    }
}

// Streaming and iterator optimizations
public static class StreamingExtensions
{
    // Memory-efficient streaming operations
    public static IEnumerable<TResult> SelectStreaming<T, TResult>(
        this IEnumerable<T> source,
        Func<T, TResult> selector,
        int bufferSize = 1024)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (selector == null) throw new ArgumentNullException(nameof(selector));

        return SelectStreamingIterator(source, selector, bufferSize);
    }

    private static IEnumerable<TResult> SelectStreamingIterator<T, TResult>(
        IEnumerable<T> source,
        Func<T, TResult> selector,
        int bufferSize)
    {
        var buffer = new List<T>(bufferSize);

        foreach (var item in source)
        {
            buffer.Add(item);
            
            if (buffer.Count == bufferSize)
            {
                foreach (var bufferedItem in buffer)
                {
                    yield return selector(bufferedItem);
                }
                buffer.Clear();
            }
        }

        // Process remaining items
        foreach (var remainingItem in buffer)
        {
            yield return selector(remainingItem);
        }
    }

    // Lazy evaluation with caching for expensive operations
    public static IEnumerable<T> CachedEnumerable<T>(this IEnumerable<T> source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        return new CachedEnumerable<T>(source);
    }

    // Efficient paging without loading all data
    public static IEnumerable<T> Page<T>(this IEnumerable<T> source, int pageNumber, int pageSize)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (pageNumber < 0) throw new ArgumentOutOfRangeException(nameof(pageNumber));
        if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));

        return source.Skip(pageNumber * pageSize).Take(pageSize);
    }

    // Interleaved enumeration of multiple sequences
    public static IEnumerable<T> Interleave<T>(this IEnumerable<IEnumerable<T>> sources)
    {
        if (sources == null) throw new ArgumentNullException(nameof(sources));

        var enumerators = sources.Select(s => s.GetEnumerator()).ToList();
        
        try
        {
            bool hasItems;
            do
            {
                hasItems = false;
                
                for (int i = enumerators.Count - 1; i >= 0; i--)
                {
                    if (enumerators[i].MoveNext())
                    {
                        yield return enumerators[i].Current;
                        hasItems = true;
                    }
                    else
                    {
                        enumerators[i].Dispose();
                        enumerators.RemoveAt(i);
                    }
                }
            } while (hasItems);
        }
        finally
        {
            foreach (var enumerator in enumerators)
            {
                enumerator.Dispose();
            }
        }
    }
}

// Supporting classes and data structures
public class CachedEnumerable<T> : IEnumerable<T>
{
    private readonly IEnumerable<T> source;
    private readonly List<T> cache;
    private IEnumerator<T>? enumerator;
    private bool isFullyCached;
    private readonly object lockObj = new();

    public CachedEnumerable(IEnumerable<T> source)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        cache = new();
        isFullyCached = false;
    }

    public IEnumerator<T> GetEnumerator()
    {
        return new CachedEnumerator(this);
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    private class CachedEnumerator : IEnumerator<T>
    {
        private readonly CachedEnumerable<T> parent;
        private int index;

        public CachedEnumerator(CachedEnumerable<T> parent)
        {
            parent = parent;
            index = -1;
        }

        public T Current { get; private set; } = default!;

        object? System.Collections.IEnumerator.Current => Current;

        public bool MoveNext()
        {
            index++;

            lock (parent.lockObj)
            {
                // If we already have this item cached, return it
                if (index < parent.cache.Count)
                {
                    Current = parent.cache[index];
                    return true;
                }

                // If we've fully cached, no more items
                if (parent.isFullyCached)
                {
                    return false;
                }

                // Initialize enumerator if needed
                parent.enumerator ??= parent.source.GetEnumerator();

                // Try to get next item from source
                if (parent.enumerator.MoveNext())
                {
                    Current = parent.enumerator.Current;
                    parent.cache.Add(Current);
                    return true;
                }

                // No more items, mark as fully cached
                parent.isFullyCached = true;
                parent.enumerator.Dispose();
                parent.enumerator = null;
                return false;
            }
        }

        public void Reset()
        {
            index = -1;
        }

        public void Dispose()
        {
            // Nothing to dispose
        }
    }
}

public class BloomFilter<T>
{
    private readonly BitArray bits;
    private readonly int hashFunctions;
    private readonly int bitArraySize;

    public BloomFilter(int expectedElements, double falsePositiveRate)
    {
        bitArraySize = (int)Math.Ceiling(-expectedElements * Math.Log(falsePositiveRate) / (Math.Log(2) * Math.Log(2)));
        hashFunctions = (int)Math.Ceiling(bitArraySize / (double)expectedElements * Math.Log(2));
        bits = new BitArray(bitArraySize);
    }

    public void Add(T item)
    {
        var hashes = GetHashes(item);
        
        for (int i = 0; i < hashFunctions; i++)
        {
            var index = Math.Abs((hashes[0] + i * hashes[1]) % bitArraySize);
            bits[index] = true;
        }
    }

    public bool Contains(T item)
    {
        var hashes = GetHashes(item);
        
        for (int i = 0; i < hashFunctions; i++)
        {
            var index = Math.Abs((hashes[0] + i * hashes[1]) % bitArraySize);
            if (!bits[index])
                return false;
        }
        
        return true;
    }

    private int[] GetHashes(T item)
    {
        var hash1 = item?.GetHashCode() ?? 0;
        var hash2 = hash1.ToString().GetHashCode();
        
        return new[] { hash1, hash2 };
    }
}

// BitArray for bloom filter
public class BitArray
{
    private readonly uint[] array;
    private readonly int length;

    public BitArray(int length)
    {
        length = length;
        array = new uint[(length + 31) / 32];
    }

    public bool this[int index]
    {
        get
        {
            if (index < 0 || index >= length)
                throw new ArgumentOutOfRangeException(nameof(index));
                
            var arrayIndex = index / 32;
            var bitIndex = index % 32;
            return (array[arrayIndex] & (1u << bitIndex)) != 0;
        }
        set
        {
            if (index < 0 || index >= length)
                throw new ArgumentOutOfRangeException(nameof(index));
                
            var arrayIndex = index / 32;
            var bitIndex = index % 32;
            
            if (value)
                array[arrayIndex] |= (1u << bitIndex);
            else
                array[arrayIndex] &= ~(1u << bitIndex);
        }
    }
}

// Performance measurement utilities
public static class PerformanceMeasurement
{
    public static (TResult Result, TimeSpan Elapsed, long MemoryAllocated) MeasurePerformance<TResult>(
        Func<TResult> operation)
    {
        var initialMemory = GC.GetTotalMemory(true);
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        var result = operation();
        
        stopwatch.Stop();
        var finalMemory = GC.GetTotalMemory(false);
        
        return (result, stopwatch.Elapsed, finalMemory - initialMemory);
    }

    public static IEnumerable<T> WithPerformanceLogging<T>(
        this IEnumerable<T> source,
        string operationName = "Operation")
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        var count = 0;
        var initialMemory = GC.GetTotalMemory(false);

        foreach (var item in source)
        {
            count++;
            yield return item;
            
            // Log progress every 10000 items
            if (count % 10000 == 0)
            {
                var currentMemory = GC.GetTotalMemory(false);
                Console.WriteLine($"{operationName}: Processed {count:N0} items in {stopwatch.Elapsed.TotalSeconds:F2}s, " +
                                $"Memory: {(currentMemory - initialMemory) / 1024 / 1024:F2}MB");
            }
        }

        stopwatch.Stop();
        var finalMemory = GC.GetTotalMemory(false);
        Console.WriteLine($"{operationName} completed: {count:N0} items in {stopwatch.Elapsed.TotalSeconds:F2}s, " +
                         $"Final memory: {(finalMemory - initialMemory) / 1024 / 1024:F2}MB");
    }
}

// Real-world performance examples
public static class PerformanceExamples
{
    public static void DemonstrateLargeDataProcessing()
    {
        // Generate large dataset
        var largeDataset = Enumerable.Range(1, 10_000_000);

        // Demonstrate different approaches and their performance
        Console.WriteLine("Performance Comparison for Large Dataset Processing:");

        // Standard LINQ
        var (standardResult, standardTime, standardMemory) = PerformanceMeasurement.MeasurePerformance(() =>
            largeDataset.Where(x => x % 2 == 0).Select(x => x * x).Sum());

        Console.WriteLine($"Standard LINQ: {standardTime.TotalMilliseconds:F2}ms, " +
                         $"Memory: {standardMemory / 1024 / 1024:F2}MB, Result: {standardResult}");

        // Parallel LINQ
        var (parallelResult, parallelTime, parallelMemory) = PerformanceMeasurement.MeasurePerformance(() =>
            largeDataset.AsParallel().Where(x => x % 2 == 0).Select(x => x * x).Sum());

        Console.WriteLine($"Parallel LINQ: {parallelTime.TotalMilliseconds:F2}ms, " +
                         $"Memory: {parallelMemory / 1024 / 1024:F2}MB, Result: {parallelResult}");

        // Pooled aggregation
        var (pooledResult, pooledTime, pooledMemory) = PerformanceMeasurement.MeasurePerformance(() =>
            largeDataset.AggregatePooled(
                0L,
                (acc, x) => x % 2 == 0 ? acc + (long)x * x : acc,
                x => x));

        Console.WriteLine($"Pooled Aggregation: {pooledTime.TotalMilliseconds:F2}ms, " +
                         $"Memory: {pooledMemory / 1024 / 1024:F2}MB, Result: {pooledResult}");
    }

    public static void DemonstrateVectorization()
    {
        Console.WriteLine($"\nVectorization Support: {Vector.IsHardwareAccelerated}");
        Console.WriteLine($"Vector<float> Count: {Vector<float>.Count}");

        var size = 1_000_000;
        var array1 = new float[size];
        var array2 = new float[size];
        var result = new float[size];

        // Initialize arrays
        for (int i = 0; i < size; i++)
        {
            array1[i] = i;
            array2[i] = i * 0.5f;
        }

        // Scalar addition
        var (_, scalarTime, _) = PerformanceMeasurement.MeasurePerformance(() =>
        {
            for (int i = 0; i < size; i++)
            {
                result[i] = array1[i] + array2[i];
            }
        });

        // Vectorized addition
        var (_, vectorTime, _) = PerformanceMeasurement.MeasurePerformance(() =>
        {
            result.AsSpan().AddVectorized(array2);
        });

        Console.WriteLine($"Scalar Addition: {scalarTime.TotalMilliseconds:F2}ms");
        Console.WriteLine($"Vectorized Addition: {vectorTime.TotalMilliseconds:F2}ms");
        Console.WriteLine($"Speedup: {scalarTime.TotalMilliseconds / vectorTime.TotalMilliseconds:F2}x");
    }
}
```

**Usage**:

```csharp
// Example 1: Memory-efficient batch processing with ArrayPool
var largeDataset = Enumerable.Range(1, 1_000_000);

Console.WriteLine("Memory-Efficient Batch Processing:");
var batchCount = 0;
var totalSum = 0L;

foreach (var batch in largeDataset.BatchPooled(10000))
{
    batchCount++;
    var batchSum = batch.Span.SumFast();
    totalSum += batchSum;
    
    if (batchCount % 10 == 0)
    {
        Console.WriteLine($"Processed {batchCount} batches, running total: {totalSum:N0}");
    }
}

Console.WriteLine($"Final result: {batchCount} batches, total sum: {totalSum:N0}");

// Example 2: High-performance span operations
var numbers = new int[1000];
for (int i = 0; i < numbers.Length; i++)
{
    numbers[i] = i + 1;
}

var numberSpan = numbers.AsSpan();

// Fast sum using optimized algorithm
var fastSum = numberSpan.SumFast();
Console.WriteLine($"\nFast span sum: {fastSum:N0}");

// Binary search on sorted data
var sortedNumbers = numbers.OrderBy(x => x).ToArray();
var searchValue = 500;
var index = sortedNumbers.AsSpan().BinarySearchFast(searchValue);
Console.WriteLine($"Binary search for {searchValue}: index {index}");

// Example 3: Vectorized operations for numerical computing
if (Vector.IsHardwareAccelerated)
{
    var floatArray1 = new float[1000];
    var floatArray2 = new float[1000];
    
    for (int i = 0; i < floatArray1.Length; i++)
    {
        floatArray1[i] = i * 1.5f;
        floatArray2[i] = i * 0.5f;
    }

    Console.WriteLine($"\nVectorization (Vector<float>.Count = {Vector<float>.Count}):");
    
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    floatArray1.AsSpan().AddVectorized(floatArray2);
    stopwatch.Stop();
    
    Console.WriteLine($"Vectorized addition of 1000 floats: {stopwatch.Elapsed.TotalMicroseconds:F2} microseconds");
    Console.WriteLine($"First few results: [{string.Join(", ", floatArray1.Take(5).Select(x => x.ToString("F1")))}]");
}

// Example 4: Concurrent processing with thread-safe operations
var dataset = Enumerable.Range(1, 100_000);

Console.WriteLine("\nConcurrent Processing:");

// Thread-safe counting
var evenCount = dataset.CountAtomic(x => x % 2 == 0);
Console.WriteLine($"Even numbers (atomic count): {evenCount:N0}");

// Parallel selection with concurrent collection
var squaredEvens = dataset
    .Where(x => x % 2 == 0)
    .SelectConcurrent(x => x * x, maxDegreeOfParallelism: Environment.ProcessorCount);

var first10Squares = squaredEvens.Take(10).OrderBy(x => x);
Console.WriteLine($"First 10 squared evens: [{string.Join(", ", first10Squares)}]");

// Parallel aggregation with custom combiner
var parallelSum = dataset.ParallelAggregate(
    seed: 0L,
    func: (acc, x) => acc + x,
    combiner: (acc1, acc2) => acc1 + acc2,
    maxDegreeOfParallelism: 4);

Console.WriteLine($"Parallel sum: {parallelSum:N0}");

// Example 5: Advanced algorithms for large-scale analysis
var randomData = Enumerable.Range(1, 10000)
    .Select(_ => new Random().Next(1, 1000))
    .ToArray();

Console.WriteLine("\nAdvanced Algorithm Performance:");

// Quick-select median (O(n) average case vs O(n log n) for sorting)
var median = randomData.QuickSelectMedian();
Console.WriteLine($"Median using QuickSelect: {median}");

// Top-K selection without full sorting
var topNumbers = randomData.TopK(10);
Console.WriteLine($"Top 10 numbers: [{string.Join(", ", topNumbers)}]");

// Reservoir sampling for random subset
var randomSample = randomData.ReservoirSample(20);
Console.WriteLine($"Random sample of 20: [{string.Join(", ", randomSample.OrderBy(x => x).Take(10))}...]");

// Bloom filter for membership testing
var bloomFilter = randomData.ToBloomFilter(randomData.Length, falsePositiveRate: 0.01);
var testValue = randomData[100];
var mightContain = bloomFilter.Contains(testValue);
var definitelyNotContain = bloomFilter.Contains(-1);

Console.WriteLine($"Bloom filter test - {testValue} might be present: {mightContain}");
Console.WriteLine($"Bloom filter test - -1 definitely not present: {!definitelyNotContain}");

// Example 6: Streaming operations with memory management
var infiniteSequence = GenerateInfiniteSequence().Take(1_000_000);

static IEnumerable<int> GenerateInfiniteSequence()
{
    var i = 0;
    while (true)
    {
        yield return ++i;
    }
}

Console.WriteLine("\nStreaming Operations:");

// Process large sequence with memory-efficient streaming
var processedCount = 0;
foreach (var value in infiniteSequence
    .SelectStreaming(x => x * x, bufferSize: 5000)
    .Where(x => x % 7 == 0)
    .WithPerformanceLogging("Square and Filter"))
{
    processedCount++;
    if (processedCount >= 1000) break;
}

Console.WriteLine($"Processed {processedCount} items that are squares divisible by 7");

// Example 7: Cached enumerable for expensive operations
var expensiveSequence = Enumerable.Range(1, 1000)
    .Select(x =>
    {
        // Simulate expensive computation
        Thread.Sleep(1);
        return x * x * x;
    })
    .CachedEnumerable();

Console.WriteLine("\nCached Enumerable Performance:");

// First enumeration - computes values
var stopwatch1 = System.Diagnostics.Stopwatch.StartNew();
var sum1 = expensiveSequence.Take(100).Sum();
stopwatch1.Stop();
Console.WriteLine($"First enumeration (computed): {stopwatch1.ElapsedMilliseconds}ms, sum: {sum1}");

// Second enumeration - uses cached values
var stopwatch2 = System.Diagnostics.Stopwatch.StartNew();
var sum2 = expensiveSequence.Take(100).Sum();
stopwatch2.Stop();
Console.WriteLine($"Second enumeration (cached): {stopwatch2.ElapsedMilliseconds}ms, sum: {sum2}");

Console.WriteLine($"Speedup factor: {(double)stopwatch1.ElapsedMilliseconds / stopwatch2.ElapsedMilliseconds:F1}x");

// Example 8: Interleaved processing of multiple data sources
var source1 = Enumerable.Range(1, 10).Select(x => $"A{x}");
var source2 = Enumerable.Range(1, 8).Select(x => $"B{x}");
var source3 = Enumerable.Range(1, 12).Select(x => $"C{x}");

var sources = new[] { source1, source2, source3 };
var interleaved = sources.Interleave();

Console.WriteLine("\nInterleaved sequences:");
Console.WriteLine($"Result: [{string.Join(", ", interleaved)}]");

// Example 9: Efficient paging for large datasets
var customers = Enumerable.Range(1, 1000)
    .Select(i => new { Id = i, Name = $"Customer {i}", Score = i * 3.14 });

Console.WriteLine("\nEfficient Paging:");
for (int page = 0; page < 3; page++)
{
    var pageData = customers.Page(page, 5);
    Console.WriteLine($"Page {page + 1}: [{string.Join(", ", pageData.Select(c => c.Name))}]");
}

// Example 10: Performance comparison demonstration
Console.WriteLine("\n=== Large Dataset Performance Comparison ===");
PerformanceExamples.DemonstrateLargeDataProcessing();

Console.WriteLine("\n=== Vectorization Performance ===");
PerformanceExamples.DemonstrateVectorization();

// Example 11: Memory usage optimization
Console.WriteLine("\n=== Memory Usage Optimization ===");

// Using ArrayPool for temporary arrays
var pool = ArrayPool<int>.Shared;
var tempArray = pool.Rent(1000);

try
{
    // Use the array for computations
    for (int i = 0; i < 1000; i++)
    {
        tempArray[i] = i * i;
    }
    
    var tempSum = tempArray.AsSpan(0, 1000).SumFast();
    Console.WriteLine($"Computed sum using pooled array: {tempSum:N0}");
}
finally
{
    pool.Return(tempArray);
}

// Demonstrate pooled enumeration
var pooledData = Enumerable.Range(1, 10000);
var pooledArray = pooledData.ToPooledArray(out var returnPool);

try
{
    var pooledSum = pooledArray.AsSpan(0, pooledData.Count()).SumFast();
    Console.WriteLine($"Pooled array sum: {pooledSum:N0}");
}
finally
{
    returnPool.Return(pooledArray);
}

// Example 12: String processing optimizations
var longText = "The quick brown fox jumps over the lazy dog. " +
               "Pack my box with five dozen liquor jugs. " +
               "How vexingly quick daft zebras jump!";

var textSpan = longText.AsSpan();
var searchChars = "aeiou".AsSpan();

Console.WriteLine("\nString Processing Optimizations:");
Console.WriteLine($"Text contains vowels: {textSpan.ContainsAnyFast(searchChars)}");

// Fast string equality
var text1 = "Hello World";
var text2 = "Hello World";
var areEqual = text1.AsSpan().SequenceEqualFast(text2.AsSpan());
Console.WriteLine($"Strings are equal (fast): {areEqual}");
```

**Notes**:

- ArrayPool usage significantly reduces garbage collection pressure for temporary arrays
- Span<T> and ReadOnlySpan<T> provide zero-allocation slicing and high-performance operations
- Vectorization can provide 4x-8x performance improvements for numerical computations when hardware supports it
- Parallel operations should be used judiciously - they have overhead and may not benefit small datasets
- Bloom filters are memory-efficient for large-scale membership testing with acceptable false positive rates
- Cached enumerables are useful for expensive computations that may be enumerated multiple times
- Quick-select algorithm provides O(n) average-case performance for finding medians and k-th elements
- Reservoir sampling provides uniform random sampling from streams of unknown size
- Memory pooling is essential for high-performance, low-allocation code in hot paths
- Performance measurement should always be done in release builds with realistic data sizes

**Prerequisites**:

- .NET Core 2.1+ or .NET Framework 4.7.1+ for Span<T> support
- .NET Core 3.0+ for hardware intrinsics and advanced vectorization
- Understanding of memory management, garbage collection, and performance profiling
- Knowledge of parallel programming concepts and thread safety
- Familiarity with SIMD (Single Instruction, Multiple Data) concepts for vectorization

**Related Snippets**:

- [LINQ Extensions](linq-extensions.md) - Standard LINQ extension methods
- [Memory Management](memory-management.md) - Advanced memory optimization techniques
- [Parallel Processing](parallel-processing.md) - Concurrent and parallel programming patterns
- [Algorithm Optimization](algorithm-optimization.md) - Optimized data structure and algorithm implementations

