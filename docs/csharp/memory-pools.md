# Memory Pools and ArrayPool Strategies

**Description**: Advanced memory pooling techniques using ArrayPool<T>, custom object pools, and memory management strategies to reduce garbage collection pressure and improve performance in high-throughput applications.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// Enhanced ArrayPool extensions
public static class ArrayPoolExtensions
{
    // Rent with automatic return using IDisposable
    public static ArrayPoolRental<T> RentDisposable<T>(this ArrayPool<T> pool, int minimumLength)
    {
        return new ArrayPoolRental<T>(pool, minimumLength);
    }

    // Rent and initialize array
    public static T[] RentAndClear<T>(this ArrayPool<T> pool, int minimumLength)
    {
        var array = pool.Rent(minimumLength);
        Array.Clear(array, 0, minimumLength);
        return array;
    }

    // Safe return that handles null arrays
    public static void SafeReturn<T>(this ArrayPool<T> pool, T[]? array, bool clearArray = false)
    {
        if (array != null)
        {
            pool.Return(array, clearArray);
        }
    }

    // Resize array using pool
    public static T[] Resize<T>(this ArrayPool<T> pool, T[] array, int currentLength, int newLength)
    {
        if (newLength <= array.Length)
        {
            return array; // No need to resize
        }

        var newArray = pool.Rent(newLength);
        Array.Copy(array, 0, newArray, 0, currentLength);
        pool.Return(array);
        
        return newArray;
    }

    // Convert IEnumerable to pooled array
    public static (T[] array, int length) ToPooledArray<T>(
        this IEnumerable<T> source, 
        ArrayPool<T> pool, 
        int initialCapacity = 4)
    {
        if (source is ICollection<T> collection)
        {
            var array = pool.Rent(collection.Count);
            collection.CopyTo(array, 0);
            return (array, collection.Count);
        }

        var buffer = pool.Rent(initialCapacity);
        var count = 0;

        foreach (var item in source)
        {
            if (count >= buffer.Length)
            {
                buffer = pool.Resize(buffer, count, Math.Max(buffer.Length * 2, count + 1));
            }
            
            buffer[count++] = item;
        }

        return (buffer, count);
    }
}

// RAII wrapper for ArrayPool
public readonly struct ArrayPoolRental<T> : IDisposable
{
    private readonly ArrayPool<T> pool;
    public T[] Array { get; }
    public int Length { get; }

    public ArrayPoolRental(ArrayPool<T> pool, int minimumLength)
    {
        pool = pool;
        Array = pool.Rent(minimumLength);
        Length = minimumLength;
    }

    public ArrayPoolRental(ArrayPool<T> pool, T[] array, int length)
    {
        this.pool = pool;
        Array = array;
        Length = length;
    }

    public Span<T> AsSpan() => Array.AsSpan(0, Length);
    public Memory<T> AsMemory() => Array.AsMemory(0, Length);

    public void Dispose()
    {
        pool.SafeReturn(Array, clearArray: true);
    }
}

// Custom object pool for complex objects
public abstract class ObjectPool<T> where T : class
{
    public abstract T Get();
    public abstract void Return(T obj);
    
    // Disposable wrapper for automatic return
    public ObjectPoolRental<T> GetDisposable()
    {
        return new ObjectPoolRental<T>(this, Get());
    }
}

// Default object pool implementation
public class DefaultObjectPool<T> : ObjectPool<T> where T : class, new()
{
    private readonly ConcurrentBag<T> objects = new();
    private readonly Func<T> objectFactory;
    private readonly Action<T>? resetAction;
    private readonly int maxRetainedObjects;
    private int currentCount;

    public DefaultObjectPool(
        Func<T>? objectFactory = null, 
        Action<T>? resetAction = null,
        int maxRetainedObjects = Environment.ProcessorCount * 2)
    {
        this.objectFactory = objectFactory ?? (() => new T());
        this.resetAction = resetAction;
        this.maxRetainedObjects = maxRetainedObjects;
    }

    public override T Get()
    {
        return objects.TryTake(out var obj) ? obj : objectFactory();
    }

    public override void Return(T obj)
    {
        if (obj == null || currentCount >= maxRetainedObjects)
            return;

        resetAction?.Invoke(obj);

        if (Interlocked.Increment(ref currentCount) <= maxRetainedObjects)
        {
            objects.Add(obj);
        }
        else
        {
            Interlocked.Decrement(ref currentCount);
        }
    }
}

// RAII wrapper for object pool
public readonly struct ObjectPoolRental<T> : IDisposable where T : class
{
    private readonly ObjectPool<T> pool;
    public T Object { get; }

    public ObjectPoolRental(ObjectPool<T> pool, T obj)
    {
        pool = pool;
        Object = obj;
    }

    public void Dispose()
    {
        pool.Return(Object);
    }
}

// StringBuilder pool for string operations
public static class StringBuilderPool
{
    private static readonly DefaultObjectPool<StringBuilder> Pool = new(
        objectFactory: () => new StringBuilder(),
        resetAction: sb => sb.Clear(),
        maxRetainedObjects: 20);

    public static StringBuilder Get() => Pool.Get();
    public static void Return(StringBuilder sb) => Pool.Return(sb);
    
    public static ObjectPoolRental<StringBuilder> GetDisposable() => Pool.GetDisposable();

    // Helper method for string building operations
    public static string Build(Action<StringBuilder> buildAction)
    {
        using var rental = GetDisposable();
        buildAction(rental.Object);
        return rental.Object.ToString();
    }

    // Async version
    public static async Task<string> BuildAsync(Func<StringBuilder, Task> buildAction)
    {
        using var rental = GetDisposable();
        await buildAction(rental.Object);
        return rental.Object.ToString();
    }
}

// Memory stream pool
public static class MemoryStreamPool
{
    private static readonly DefaultObjectPool<MemoryStream> Pool = new(
        objectFactory: () => new MemoryStream(),
        resetAction: ms => 
        {
            ms.SetLength(0);
            ms.Position = 0;
        },
        maxRetainedObjects: 10);

    public static MemoryStream Get() => Pool.Get();
    public static void Return(MemoryStream stream) => Pool.Return(stream);
    
    public static ObjectPoolRental<MemoryStream> GetDisposable() => Pool.GetDisposable();

    // Helper for stream operations
    public static byte[] GetBytes(Action<MemoryStream> writeAction)
    {
        using var rental = GetDisposable();
        writeAction(rental.Object);
        return rental.Object.ToArray();
    }

    public static async Task<byte[]> GetBytesAsync(Func<MemoryStream, Task> writeAction)
    {
        using var rental = GetDisposable();
        await writeAction(rental.Object);
        return rental.Object.ToArray();
    }
}

// Pooled list implementation
public class PooledList<T> : IDisposable, IList<T>
{
    private static readonly ArrayPool<T> Pool = ArrayPool<T>.Shared;
    
    private T[] array;
    private int count;
    private bool disposed;

    public PooledList(int capacity = 4)
    {
        array = Pool.Rent(capacity);
        count = 0;
    }

    public int Count => count;
    public bool IsReadOnly => false;
    public int Capacity => array.Length;

    public T this[int index]
    {
        get
        {
            if (index >= count) throw new ArgumentOutOfRangeException(nameof(index));
            return array[index];
        }
        set
        {
            if (index >= count) throw new ArgumentOutOfRangeException(nameof(index));
            array[index] = value;
        }
    }

    public void Add(T item)
    {
        EnsureCapacity(count + 1);
        array[count++] = item;
    }

    public void AddRange(IEnumerable<T> items)
    {
        foreach (var item in items)
        {
            Add(item);
        }
    }

    public void Insert(int index, T item)
    {
        if (index > count) throw new ArgumentOutOfRangeException(nameof(index));
        
        EnsureCapacity(count + 1);
        Array.Copy(array, index, array, index + 1, count - index);
        array[index] = item;
        count++;
    }

    public bool Remove(T item)
    {
        var index = IndexOf(item);
        if (index >= 0)
        {
            RemoveAt(index);
            return true;
        }
        return false;
    }

    public void RemoveAt(int index)
    {
        if (index >= count) throw new ArgumentOutOfRangeException(nameof(index));
        
        Array.Copy(array, index + 1, array, index, count - index - 1);
        count--;
        array[count] = default!; // Clear reference
    }

    public void Clear()
    {
        Array.Clear(array, 0, count);
        count = 0;
    }

    public bool Contains(T item)
    {
        return IndexOf(item) >= 0;
    }

    public int IndexOf(T item)
    {
        return Array.IndexOf(array, item, 0, count);
    }

    public void CopyTo(T[] array, int arrayIndex)
    {
        Array.Copy(array, 0, array, arrayIndex, count);
    }

    public Span<T> AsSpan() => array.AsSpan(0, count);
    public ReadOnlySpan<T> AsReadOnlySpan() => array.AsSpan(0, count);

    private void EnsureCapacity(int capacity)
    {
        if (array.Length < capacity)
        {
            var newSize = Math.Max(array.Length * 2, capacity);
            var newArray = Pool.Rent(newSize);
            Array.Copy(array, 0, newArray, 0, count);
            Pool.Return(array);
            array = newArray;
        }
    }

    public IEnumerator<T> GetEnumerator()
    {
        for (int i = 0; i < count; i++)
        {
            yield return array[i];
        }
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Dispose()
    {
        if (!disposed)
        {
            Pool.SafeReturn(array, clearArray: true);
            array = null!;
            disposed = true;
        }
    }
}

// Pooled dictionary for temporary dictionaries
public class PooledDictionary<TKey, TValue> : IDisposable where TKey : notnull
{
    private static readonly ObjectPool<Dictionary<TKey, TValue>> Pool = 
        new DefaultObjectPool<Dictionary<TKey, TValue>>(
            objectFactory: () => new Dictionary<TKey, TValue>(),
            resetAction: dict => dict.Clear());

    private readonly Dictionary<TKey, TValue> dictionary;
    private bool disposed;

    public PooledDictionary()
    {
        dictionary = Pool.Get();
    }

    public TValue this[TKey key]
    {
        get => dictionary[key];
        set => dictionary[key] = value;
    }

    public int Count => dictionary.Count;
    public Dictionary<TKey, TValue>.KeyCollection Keys => dictionary.Keys;
    public Dictionary<TKey, TValue>.ValueCollection Values => dictionary.Values;

    public void Add(TKey key, TValue value) => dictionary.Add(key, value);
    public bool ContainsKey(TKey key) => dictionary.ContainsKey(key);
    public bool TryGetValue(TKey key, out TValue value) => dictionary.TryGetValue(key, out value!);
    public bool Remove(TKey key) => dictionary.Remove(key);
    public void Clear() => dictionary.Clear();

    public void Dispose()
    {
        if (!disposed)
        {
            Pool.Return(dictionary);
            disposed = true;
        }
    }
}

// Memory-efficient buffer writer
public class PooledBufferWriter<T> : IBufferWriter<T>, IDisposable
{
    private readonly ArrayPool<T> pool;
    private T[] buffer;
    private int index;

    public PooledBufferWriter(ArrayPool<T>? pool = null, int initialCapacity = 256)
    {
        this.pool = pool ?? ArrayPool<T>.Shared;
        buffer = pool.Rent(initialCapacity);
        index = 0;
    }

    public ReadOnlyMemory<T> WrittenMemory => buffer.AsMemory(0, index);
    public ReadOnlySpan<T> WrittenSpan => buffer.AsSpan(0, index);
    public int WrittenCount => index;

    public void Advance(int count)
    {
        if (count < 0 || index + count > buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(count));
            
        index += count;
    }

    public Memory<T> GetMemory(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return buffer.AsMemory(index);
    }

    public Span<T> GetSpan(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return buffer.AsSpan(index);
    }

    public void Write(ReadOnlySpan<T> value)
    {
        EnsureCapacity(value.Length);
        value.CopyTo(buffer.AsSpan(index));
        index += value.Length;
    }

    public void Write(T value)
    {
        EnsureCapacity(1);
        buffer[index++] = value;
    }

    public void Reset()
    {
        index = 0;
        Array.Clear(buffer, 0, buffer.Length);
    }

    private void EnsureCapacity(int sizeHint)
    {
        var availableSpace = buffer.Length - index;
        if (availableSpace >= sizeHint)
            return;

        var growBy = Math.Max(sizeHint, buffer.Length);
        var newSize = buffer.Length + growBy;
        
        var newBuffer = pool.Rent(newSize);
        Array.Copy(buffer, 0, newBuffer, 0, index);
        
        pool.Return(buffer);
        buffer = newBuffer;
    }

    public void Dispose()
    {
        pool.SafeReturn(buffer, clearArray: true);
        buffer = null!;
    }
}

// High-performance string operations with pooling
public static class PooledStringOperations
{
    // Join strings with pooled StringBuilder
    public static string Join<T>(IEnumerable<T> values, string separator)
    {
        return StringBuilderPool.Build(sb =>
        {
            using var enumerator = values.GetEnumerator();
            if (enumerator.MoveNext())
            {
                sb.Append(enumerator.Current);
                while (enumerator.MoveNext())
                {
                    sb.Append(separator).Append(enumerator.Current);
                }
            }
        });
    }

    // Format strings with pooled arrays
    public static string FormatWith(string template, params object[] args)
    {
        var pool = ArrayPool<char>.Shared;
        var buffer = pool.RentAndClear(template.Length + args.Length * 20); // Estimated size

        try
        {
            var result = string.Format(template, args);
            return result;
        }
        finally
        {
            pool.SafeReturn(buffer, clearArray: true);
        }
    }

    // Concatenate many strings efficiently
    public static string Concat(IEnumerable<string> strings)
    {
        return StringBuilderPool.Build(sb =>
        {
            foreach (var str in strings)
            {
                sb.Append(str);
            }
        });
    }

    // Split string with pooled result array
    public static string[] SplitPooled(string input, char separator, StringSplitOptions options = StringSplitOptions.None)
    {
        using var list = new PooledList<string>();
        
        var span = input.AsSpan();
        int start = 0;
        
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] == separator)
            {
                if (start != i || options != StringSplitOptions.RemoveEmptyEntries)
                {
                    list.Add(span.Slice(start, i - start).ToString());
                }
                start = i + 1;
            }
        }
        
        // Add final segment
        if (start < span.Length || options != StringSplitOptions.RemoveEmptyEntries)
        {
            list.Add(span.Slice(start).ToString());
        }
        
        var result = new string[list.Count];
        list.CopyTo(result, 0);
        return result;
    }
}

// Performance monitoring for memory pools
public class PoolPerformanceMonitor
{
    private readonly ConcurrentDictionary<string, PoolStats> stats = new();

    public void RecordRent(string poolName, int size)
    {
        stats.AddOrUpdate(poolName, 
            new PoolStats { RentCount = 1, TotalRentedBytes = size, MaxRentSize = size },
            (_, existing) =>
            {
                existing.RentCount++;
                existing.TotalRentedBytes += size;
                if (size > existing.MaxRentSize)
                    existing.MaxRentSize = size;
                return existing;
            });
    }

    public void RecordReturn(string poolName, int size)
    {
        stats.AddOrUpdate(poolName,
            new PoolStats { ReturnCount = 1 },
            (_, existing) =>
            {
                existing.ReturnCount++;
                return existing;
            });
    }

    public PoolStats GetStats(string poolName)
    {
        return stats.TryGetValue(poolName, out var stats) ? stats : new PoolStats();
    }

    public string GenerateReport()
    {
        return StringBuilderPool.Build(sb =>
        {
            sb.AppendLine("Pool Performance Report");
            sb.AppendLine("======================");
            
            foreach (var kvp in stats)
            {
                var stats = kvp.Value;
                sb.AppendLine($"\nPool: {kvp.Key}");
                sb.AppendLine($"  Rents: {stats.RentCount:N0}");
                sb.AppendLine($"  Returns: {stats.ReturnCount:N0}");
                sb.AppendLine($"  Outstanding: {stats.RentCount - stats.ReturnCount:N0}");
                sb.AppendLine($"  Total Bytes Rented: {stats.TotalRentedBytes:N0}");
                sb.AppendLine($"  Max Rent Size: {stats.MaxRentSize:N0}");
                sb.AppendLine($"  Average Rent Size: {(stats.RentCount > 0 ? stats.TotalRentedBytes / stats.RentCount : 0):F1}");
            }
        });
    }

    public class PoolStats
    {
        public long RentCount { get; set; }
        public long ReturnCount { get; set; }
        public long TotalRentedBytes { get; set; }
        public int MaxRentSize { get; set; }
    }
}

// Monitored ArrayPool wrapper
public class MonitoredArrayPool<T>(ArrayPool<T> innerPool, PoolPerformanceMonitor monitor, string poolName) : ArrayPool<T>
{

    public override T[] Rent(int minimumLength)
    {
        var array = innerPool.Rent(minimumLength);
        monitor.RecordRent(poolName, array.Length * Unsafe.SizeOf<T>());
        return array;
    }

    public override void Return(T[] array, bool clearArray = false)
    {
        if (array != null)
        {
            monitor.RecordReturn(poolName, array.Length * Unsafe.SizeOf<T>());
            innerPool.Return(array, clearArray);
        }
    }
}

// Batch processing with memory pools
public static class PooledBatchProcessor
{
    public static async Task ProcessBatchesAsync<T, TResult>(
        IEnumerable<T> source,
        Func<T[], Task<TResult[]>> processor,
        int batchSize,
        Action<TResult[]>? resultHandler = null)
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
                    var batch = new T[count];
                    Array.Copy(buffer, 0, batch, 0, count);
                    
                    var results = await processor(batch);
                    resultHandler?.Invoke(results);
                    
                    count = 0;
                    Array.Clear(buffer, 0, batchSize);
                }
            }
            
            // Process remaining items
            if (count > 0)
            {
                var finalBatch = new T[count];
                Array.Copy(buffer, 0, finalBatch, 0, count);
                
                var results = await processor(finalBatch);
                resultHandler?.Invoke(results);
            }
        }
        finally
        {
            pool.SafeReturn(buffer, clearArray: true);
        }
    }

    public static IEnumerable<TResult> ProcessBatches<T, TResult>(
        IEnumerable<T> source,
        Func<T[], TResult[]> processor,
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
                    var batch = new T[count];
                    Array.Copy(buffer, 0, batch, 0, count);
                    
                    var results = processor(batch);
                    foreach (var result in results)
                    {
                        yield return result;
                    }
                    
                    count = 0;
                    Array.Clear(buffer, 0, batchSize);
                }
            }
            
            // Process remaining items
            if (count > 0)
            {
                var finalBatch = new T[count];
                Array.Copy(buffer, 0, finalBatch, 0, count);
                
                var results = processor(finalBatch);
                foreach (var result in results)
                {
                    yield return result;
                }
            }
        }
        finally
        {
            pool.SafeReturn(buffer, clearArray: true);
        }
    }
}

// Memory-efficient CSV reader using pools
public class PooledCsvReader : IDisposable
{
    private readonly TextReader reader;
    private readonly PooledList<string> fields;
    private readonly ArrayPool<char> charPool;
    private char[]? buffer;

    public PooledCsvReader(TextReader reader)
    {
        reader = reader;
        fields = new PooledList<string>();
        charPool = ArrayPool<char>.Shared;
        buffer = charPool.Rent(1024);
    }

    public IEnumerable<string[]> ReadRecords()
    {
        string? line;
        while ((line = reader.ReadLine()) != null)
        {
            fields.Clear();
            ParseCsvLine(line);
            
            var record = new string[fields.Count];
            fields.CopyTo(record, 0);
            yield return record;
        }
    }

    private void ParseCsvLine(string line)
    {
        var span = line.AsSpan();
        var start = 0;
        var inQuotes = false;
        
        for (int i = 0; i < span.Length; i++)
        {
            var ch = span[i];
            
            if (ch == '"')
            {
                inQuotes = !inQuotes;
            }
            else if (ch == ',' && !inQuotes)
            {
                fields.Add(span.Slice(start, i - start).ToString());
                start = i + 1;
            }
        }
        
        // Add final field
        fields.Add(span.Slice(start).ToString());
    }

    public void Dispose()
    {
        fields?.Dispose();
        charPool.SafeReturn(buffer, clearArray: true);
        buffer = null;
    }
}

// Examples and benchmarking
public static class PoolingExamples
{
    // Example: String processing with pooling
    public static string ProcessLargeText(string input)
    {
        return StringBuilderPool.Build(sb =>
        {
            var lines = PooledStringOperations.SplitPooled(input, '\n');
            
            foreach (var line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    sb.AppendLine($"Processed: {line.Trim()}");
                }
            }
        });
    }

    // Example: Batch processing with monitoring
    public static async Task DemonstrateBatchProcessing()
    {
        var monitor = new PoolPerformanceMonitor();
        var monitoredPool = new MonitoredArrayPool<int>(ArrayPool<int>.Shared, monitor, "BatchProcessing");
        
        var data = Enumerable.Range(1, 10000);
        
        await PooledBatchProcessor.ProcessBatchesAsync(
            data,
            async batch =>
            {
                // Simulate processing
                await Task.Delay(1);
                return batch.Select(x => x * x).ToArray();
            },
            batchSize: 100,
            resultHandler: results =>
            {
                Console.WriteLine($"Processed batch of {results.Length} items");
            });
        
        Console.WriteLine(monitor.GenerateReport());
    }

    // Example: CSV processing with memory efficiency
    public static void ProcessCsvData(string csvContent)
    {
        using var reader = new StringReader(csvContent);
        using var csvReader = new PooledCsvReader(reader);
        
        var recordCount = 0;
        foreach (var record in csvReader.ReadRecords())
        {
            recordCount++;
            // Process record without additional allocations
            if (recordCount % 1000 == 0)
            {
                Console.WriteLine($"Processed {recordCount} records");
            }
        }
    }
}
```

**Usage**:

```csharp
// Example 1: Basic ArrayPool usage with automatic disposal
Console.WriteLine("ArrayPool with RAII pattern:");

using (var rental = ArrayPool<int>.Shared.RentDisposable(1000))
{
    var array = rental.Array;
    var span = rental.AsSpan();
    
    // Fill array with data
    for (int i = 0; i < rental.Length; i++)
    {
        array[i] = i * i;
    }
    
    Console.WriteLine($"Filled array of {rental.Length} elements");
    Console.WriteLine($"First 10: [{string.Join(", ", span.Slice(0, 10).ToArray())}]");
} // Array automatically returned to pool here

// Example 2: StringBuilder pooling for string operations
Console.WriteLine("\nStringBuilder pooling:");

var result = StringBuilderPool.Build(sb =>
{
    sb.AppendLine("Building a complex string");
    for (int i = 0; i < 5; i++)
    {
        sb.AppendLine($"Line {i + 1}: Some content here");
    }
    sb.AppendLine("End of string");
});

Console.WriteLine($"Built string:\n{result}");

// Demonstrate reuse
var result2 = StringBuilderPool.Build(sb =>
{
    sb.Append("Reused StringBuilder: ");
    sb.Append(DateTime.Now.ToString("HH:mm:ss"));
});

Console.WriteLine($"Second use: {result2}");

// Example 3: PooledList for temporary collections
Console.WriteLine("\nPooledList usage:");

using (var list = new PooledList<string>(10))
{
    list.AddRange(new[] { "Apple", "Banana", "Cherry", "Date", "Elderberry" });
    
    Console.WriteLine($"PooledList contains {list.Count} items:");
    foreach (var item in list)
    {
        Console.WriteLine($"  - {item}");
    }
    
    // Use as span for high-performance operations
    var span = list.AsSpan();
    Console.WriteLine($"As span: Length = {span.Length}");
} // Memory automatically returned to pool

// Example 4: MemoryStream pooling for binary operations
Console.WriteLine("\nMemoryStream pooling:");

var binaryData = MemoryStreamPool.GetBytes(stream =>
{
    using var writer = new BinaryWriter(stream);
    writer.Write("Hello, World!");
    writer.Write(42);
    writer.Write(3.14159);
});

Console.WriteLine($"Generated {binaryData.Length} bytes of binary data");

// Example 5: Custom object pooling
Console.WriteLine("\nCustom object pooling:");

var stringPool = new DefaultObjectPool<StringBuilder>(
    objectFactory: () => new StringBuilder(100),
    resetAction: sb => sb.Clear(),
    maxRetainedObjects: 5);

using (var rental = stringPool.GetDisposable())
{
    rental.Object.Append("Pooled StringBuilder: ");
    rental.Object.Append(Guid.NewGuid());
    Console.WriteLine($"Result: {rental.Object}");
}

// Example 6: PooledDictionary for temporary mappings
Console.WriteLine("\nPooledDictionary usage:");

using (var dict = new PooledDictionary<string, int>())
{
    dict["apple"] = 1;
    dict["banana"] = 2;
    dict["cherry"] = 3;
    
    Console.WriteLine($"Dictionary has {dict.Count} entries:");
    foreach (var key in dict.Keys)
    {
        Console.WriteLine($"  {key} = {dict[key]}");
    }
}

// Example 7: High-performance buffer writer
Console.WriteLine("\nPooledBufferWriter usage:");

using (var writer = new PooledBufferWriter<byte>())
{
    // Write some data
    var data1 = "Hello, "u8;
    writer.Write(data1);
    
    var data2 = "World!"u8;
    writer.Write(data2);
    
    // Get the result
    var resultBytes = writer.WrittenSpan.ToArray();
    var text = System.Text.Encoding.UTF8.GetString(resultBytes);
    
    Console.WriteLine($"BufferWriter result: '{text}' ({resultBytes.Length} bytes)");
}

// Example 8: String operations with pooling
Console.WriteLine("\nPooled string operations:");

var words = new[] { "functional", "programming", "with", "memory", "pooling" };
var joined = PooledStringOperations.Join(words, " | ");
Console.WriteLine($"Joined string: {joined}");

var concatenated = PooledStringOperations.Concat(words.Select(w => w.ToUpper()));
Console.WriteLine($"Concatenated: {concatenated}");

var split = PooledStringOperations.SplitPooled("apple,banana,cherry,date", ',');
Console.WriteLine($"Split result: [{string.Join(", ", split)}]");

// Example 9: Performance monitoring
Console.WriteLine("\nPerformance monitoring:");

var monitor = new PoolPerformanceMonitor();
var monitoredPool = new MonitoredArrayPool<int>(ArrayPool<int>.Shared, monitor, "TestPool");

// Simulate some operations
for (int i = 0; i < 10; i++)
{
    var array = monitoredPool.Rent(100 * (i + 1));
    // Simulate work
    Array.Fill(array, i, 0, Math.Min(100, array.Length));
    monitoredPool.Return(array, clearArray: true);
}

Console.WriteLine(monitor.GenerateReport());

// Example 10: Batch processing with pools
Console.WriteLine("\nBatch processing with memory pools:");

var largeDataset = Enumerable.Range(1, 1000);

var processedResults = PooledBatchProcessor.ProcessBatches(
    largeDataset,
    batch => batch.Select(x => x * x).ToArray(),
    batchSize: 50);

var firstResults = processedResults.Take(20).ToArray();
Console.WriteLine($"First 20 processed results: [{string.Join(", ", firstResults)}]");

// Example 11: CSV processing with pooling
Console.WriteLine("\nCSV processing with memory efficiency:");

var csvData = """
    Name,Age,City
    Alice,25,New York
    Bob,30,San Francisco
    Charlie,35,Chicago
    Diana,28,Boston
    """;

using var csvReader = new PooledCsvReader(new StringReader(csvData));
var recordCount = 0;

foreach (var record in csvReader.ReadRecords())
{
    recordCount++;
    Console.WriteLine($"Record {recordCount}: [{string.Join(", ", record)}]");
}

// Example 12: Memory-efficient text processing
Console.WriteLine("\nMemory-efficient text processing:");

var largeText = string.Join('\n', Enumerable.Range(1, 100).Select(i => $"Line {i}: Some sample content here"));

var processed = PoolingExamples.ProcessLargeText(largeText);
var lines = processed.Split('\n');

Console.WriteLine($"Processed {lines.Length} lines");
Console.WriteLine("First 5 processed lines:");
foreach (var line in lines.Take(5))
{
    Console.WriteLine($"  {line}");
}

// Example 13: Async batch processing with monitoring
Console.WriteLine("\nAsync batch processing:");

await PoolingExamples.DemonstrateBatchProcessing();

// Example 14: Converting collections to pooled arrays
Console.WriteLine("\nConverting collections with pooling:");

var pool = ArrayPool<string>.Shared;
var sourceData = new[] { "alpha", "beta", "gamma", "delta", "epsilon" };

var (pooledArray, length) = sourceData.ToPooledArray(pool);

try
{
    Console.WriteLine($"Converted to pooled array: Length = {length}");
    Console.WriteLine($"Array capacity: {pooledArray.Length}");
    Console.WriteLine($"Contents: [{string.Join(", ", pooledArray.AsSpan(0, length).ToArray())}]");
}
finally
{
    pool.SafeReturn(pooledArray, clearArray: true);
}

// Example 15: Resizing pooled arrays
Console.WriteLine("\nPooled array resizing:");

var resizePool = ArrayPool<int>.Shared;
var initialArray = resizePool.Rent(10);

try
{
    // Fill initial array
    for (int i = 0; i < 10; i++)
    {
        initialArray[i] = i;
    }
    
    Console.WriteLine($"Initial array (capacity {initialArray.Length}): [{string.Join(", ", initialArray.AsSpan(0, 10).ToArray())}]");
    
    // Resize to larger array
    var resizedArray = resizePool.Resize(initialArray, 10, 20);
    initialArray = resizedArray; // Update reference
    
    // Add more data
    for (int i = 10; i < 20; i++)
    {
        initialArray[i] = i;
    }
    
    Console.WriteLine($"Resized array (capacity {initialArray.Length}): [{string.Join(", ", initialArray.AsSpan(0, 20).ToArray())}]");
}
finally
{
    resizePool.SafeReturn(initialArray, clearArray: true);
}

// Example 16: Comparing allocation patterns
Console.WriteLine("\nAllocation comparison:");

// Without pooling (creates garbage)
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
for (int i = 0; i < 1000; i++)
{
    var sb = new StringBuilder();
    sb.Append("Test string ");
    sb.Append(i);
    var resultUnpooled = sb.ToString();
}
stopwatch.Stop();
Console.WriteLine($"Without pooling: {stopwatch.ElapsedMilliseconds}ms");

// With pooling (minimal garbage)
stopwatch.Restart();
for (int i = 0; i < 1000; i++)
{
    var resultPooled = StringBuilderPool.Build(sb =>
    {
        sb.Append("Test string ");
        sb.Append(i);
    });
}
stopwatch.Stop();
Console.WriteLine($"With pooling: {stopwatch.ElapsedMilliseconds}ms");
```

**Notes**:

- ArrayPool<T> reduces garbage collection pressure by reusing arrays instead of allocating new ones
- RAII patterns with IDisposable ensure automatic return of pooled resources
- Object pools work best for expensive-to-create objects like StringBuilder, MemoryStream, etc.
- Pooled collections (PooledList, PooledDictionary) provide temporary high-performance collections
- Buffer writers enable efficient sequential writing without pre-allocating large buffers
- Performance monitoring helps identify pool usage patterns and optimize pool configurations
- Batch processing with pools minimizes allocations during large-scale data operations
- Custom pools can be tuned for specific object lifecycles and usage patterns
- Clear arrays when returning to pools if they contain sensitive data or references
- Pool sizes should be tuned based on application load and memory constraints

**Prerequisites**:

- .NET Core 2.1+ or .NET Framework 4.7.1+ for ArrayPool<T> and Span<T> support
- Understanding of memory management and garbage collection in .NET
- Knowledge of IDisposable pattern and resource management
- Familiarity with performance profiling tools to measure allocation reduction
- Understanding of concurrent programming for thread-safe pool implementations

**Related Snippets**:

- [Span Operations](span-operations.md) - High-performance memory operations with Span<T>
- [Performance LINQ](performance-linq.md) - Memory-efficient LINQ operations
- [Vectorization](vectorization.md) - SIMD operations for numerical computations
- [Micro Optimizations](micro-optimizations.md) - Low-level performance techniques

