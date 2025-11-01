# Async Enumerable Patterns

**Description**: Asynchronous enumeration patterns using `IAsyncEnumerable<T>` for streaming data, processing large datasets, and handling async operations in sequences. Enables efficient memory usage and responsive applications when dealing with async data sources.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

// Basic async enumerable implementation
public static class AsyncEnumerableExamples
{
    // 1. Simple async enumerable with yield
    public static async IAsyncEnumerable<int> GenerateNumbersAsync(
        int count, 
        int delayMs = 100,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 1; i <= count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            await Task.Delay(delayMs, cancellationToken);
            yield return i;
        }
    }

    // 2. Reading file lines asynchronously
    public static async IAsyncEnumerable<string> ReadLinesAsync(
        string filePath,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var reader = new StreamReader(filePath);
        
        while (!reader.EndOfStream)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            var line = await reader.ReadLineAsync();
            if (line != null)
            {
                yield return line;
            }
        }
    }

    // 3. HTTP API pagination with async enumerable
    public static async IAsyncEnumerable<T> FetchPagedDataAsync<T>(
        HttpClient httpClient,
        string baseUrl,
        int pageSize = 20,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int currentPage = 1;
        bool hasMoreData = true;

        while (hasMoreData)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var url = $"{baseUrl}?page={currentPage}&size={pageSize}";
            var response = await httpClient.GetStringAsync(url);
            var pageData = JsonSerializer.Deserialize<PagedResponse<T>>(response);

            if (pageData?.Items != null)
            {
                foreach (var item in pageData.Items)
                {
                    yield return item;
                }

                hasMoreData = pageData.HasNextPage;
                currentPage++;
            }
            else
            {
                hasMoreData = false;
            }
        }
    }

    // 4. Database records streaming
    public static async IAsyncEnumerable<TResult> StreamQueryResultsAsync<TResult>(
        Func<int, int, Task<IEnumerable<TResult>>> queryFunction,
        int batchSize = 1000,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        int offset = 0;
        bool hasMoreData = true;

        while (hasMoreData)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batch = await queryFunction(offset, batchSize);
            var items = batch.ToList();

            foreach (var item in items)
            {
                yield return item;
            }

            hasMoreData = items.Count == batchSize;
            offset += batchSize;
        }
    }

    // 5. Real-time data stream simulation
    public static async IAsyncEnumerable<SensorReading> SimulateSensorDataAsync(
        TimeSpan interval,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var random = new Random();
        var startTime = DateTime.UtcNow;

        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(interval, cancellationToken);

            yield return new SensorReading
            {
                Timestamp = DateTime.UtcNow,
                Temperature = 20 + random.NextDouble() * 10, // 20-30°C
                Humidity = 40 + random.NextDouble() * 20,    // 40-60%
                Pressure = 1000 + random.NextDouble() * 50   // 1000-1050 hPa
            };
        }
    }
}

// Extension methods for async enumerables
public static class AsyncEnumerableExtensions
{
    // Take first N items
    public static async IAsyncEnumerable<T> TakeAsync<T>(
        this IAsyncEnumerable<T> source, 
        int count,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (count <= 0) yield break;

        var taken = 0;
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            if (taken >= count) break;
            
            yield return item;
            taken++;
        }
    }

    // Skip first N items
    public static async IAsyncEnumerable<T> SkipAsync<T>(
        this IAsyncEnumerable<T> source, 
        int count,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var skipped = 0;
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            if (skipped < count)
            {
                skipped++;
                continue;
            }
            
            yield return item;
        }
    }

    // Where filter
    public static async IAsyncEnumerable<T> WhereAsync<T>(
        this IAsyncEnumerable<T> source, 
        Func<T, bool> predicate,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            if (predicate(item))
            {
                yield return item;
            }
        }
    }

    // Select transformation
    public static async IAsyncEnumerable<TResult> SelectAsync<T, TResult>(
        this IAsyncEnumerable<T> source, 
        Func<T, TResult> selector,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            yield return selector(item);
        }
    }

    // Async select transformation
    public static async IAsyncEnumerable<TResult> SelectAsync<T, TResult>(
        this IAsyncEnumerable<T> source, 
        Func<T, Task<TResult>> selector,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            var result = await selector(item);
            yield return result;
        }
    }

    // Buffer items into batches
    public static async IAsyncEnumerable<IList<T>> BufferAsync<T>(
        this IAsyncEnumerable<T> source, 
        int batchSize,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0) throw new ArgumentException("Batch size must be positive", nameof(batchSize));

        var buffer = new List<T>(batchSize);

        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            buffer.Add(item);

            if (buffer.Count >= batchSize)
            {
                yield return buffer.ToList();
                buffer.Clear();
            }
        }

        // Yield remaining items
        if (buffer.Count > 0)
        {
            yield return buffer;
        }
    }

    // Convert to regular enumerable (materialize)
    public static async Task<List<T>> ToListAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        var list = new List<T>();
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            list.Add(item);
        }
        return list;
    }

    // Count items
    public static async Task<int> CountAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        var count = 0;
        await foreach (var _ in source.WithCancellation(cancellationToken))
        {
            count++;
        }
        return count;
    }

    // Check if any items match predicate
    public static async Task<bool> AnyAsync<T>(
        this IAsyncEnumerable<T> source,
        Func<T, bool> predicate,
        CancellationToken cancellationToken = default)
    {
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            if (predicate(item))
            {
                return true;
            }
        }
        return false;
    }

    // Get first item or default
    public static async Task<T?> FirstOrDefaultAsync<T>(
        this IAsyncEnumerable<T> source,
        CancellationToken cancellationToken = default)
    {
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            return item;
        }
        return default(T);
    }

    // Merge multiple async enumerables
    public static async IAsyncEnumerable<T> MergeAsync<T>(
        params IAsyncEnumerable<T>[] sources)
    {
        var tasks = sources.Select(async source => await source.ToListAsync()).ToArray();
        var results = await Task.WhenAll(tasks);
        
        foreach (var result in results)
        {
            foreach (var item in result)
            {
                yield return item;
            }
        }
    }
}

// Advanced async enumerable patterns
public class AsyncDataProcessor<T>
{
    private readonly Func<T, Task<bool>> _filter;
    private readonly Func<T, Task<T>> _transform;
    
    public AsyncDataProcessor(
        Func<T, Task<bool>>? filter = null,
        Func<T, Task<T>>? transform = null)
    {
        _filter = filter ?? (_ => Task.FromResult(true));
        _transform = transform ?? (x => Task.FromResult(x));
    }

    public async IAsyncEnumerable<T> ProcessAsync(
        IAsyncEnumerable<T> source,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            if (await _filter(item))
            {
                var transformed = await _transform(item);
                yield return transformed;
            }
        }
    }
}

// Parallel processing with async enumerable
public static class ParallelAsyncProcessor
{
    public static async IAsyncEnumerable<TResult> ProcessInParallelAsync<T, TResult>(
        IAsyncEnumerable<T> source,
        Func<T, Task<TResult>> processor,
        int maxConcurrency = Environment.ProcessorCount,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        var tasks = new List<Task<TResult>>();

        await foreach (var item in source.WithCancellation(cancellationToken))
        {
            await semaphore.WaitAsync(cancellationToken);

            var task = ProcessItemAsync(item, processor, semaphore, cancellationToken);
            tasks.Add(task);

            // Yield completed results
            var completedTasks = tasks.Where(t => t.IsCompleted).ToList();
            foreach (var completedTask in completedTasks)
            {
                tasks.Remove(completedTask);
                yield return await completedTask;
            }
        }

        // Wait for remaining tasks
        while (tasks.Count > 0)
        {
            var completed = await Task.WhenAny(tasks);
            tasks.Remove(completed);
            yield return await completed;
        }
    }

    private static async Task<TResult> ProcessItemAsync<T, TResult>(
        T item,
        Func<T, Task<TResult>> processor,
        SemaphoreSlim semaphore,
        CancellationToken cancellationToken)
    {
        try
        {
            return await processor(item);
        }
        finally
        {
            semaphore.Release();
        }
    }
}

// Supporting data models
public class PagedResponse<T>
{
    public List<T>? Items { get; set; }
    public bool HasNextPage { get; set; }
    public int TotalCount { get; set; }
    public int PageSize { get; set; }
    public int CurrentPage { get; set; }
}

public class SensorReading
{
    public DateTime Timestamp { get; set; }
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public double Pressure { get; set; }

    public override string ToString()
    {
        return $"[{Timestamp:HH:mm:ss}] T:{Temperature:F1}°C H:{Humidity:F1}% P:{Pressure:F1}hPa";
    }
}

// Real-world example: Log file processor
public class LogFileProcessor
{
    public async IAsyncEnumerable<LogEntry> ProcessLogFilesAsync(
        IEnumerable<string> filePaths,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var filePath in filePaths)
        {
            await foreach (var line in AsyncEnumerableExamples.ReadLinesAsync(filePath, cancellationToken))
            {
                if (TryParseLogEntry(line, out var logEntry))
                {
                    yield return logEntry;
                }
            }
        }
    }

    public async IAsyncEnumerable<LogSummary> GenerateHourlySummariesAsync(
        IAsyncEnumerable<LogEntry> logEntries,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var hourlyBatches = logEntries
            .BufferAsync(1000, cancellationToken)
            .SelectAsync(async batch =>
            {
                var groups = batch.GroupBy(entry => new DateTime(
                    entry.Timestamp.Year,
                    entry.Timestamp.Month,
                    entry.Timestamp.Day,
                    entry.Timestamp.Hour, 0, 0));

                return groups.Select(group => new LogSummary
                {
                    Hour = group.Key,
                    EntryCount = group.Count(),
                    ErrorCount = group.Count(e => e.Level == LogLevel.Error),
                    WarningCount = group.Count(e => e.Level == LogLevel.Warning)
                });
            });

        await foreach (var summaryBatch in hourlyBatches)
        {
            foreach (var summary in summaryBatch)
            {
                yield return summary;
            }
        }
    }

    private bool TryParseLogEntry(string line, out LogEntry logEntry)
    {
        logEntry = new LogEntry();
        
        // Simplified log parsing - in reality would be more robust
        if (line.Length > 20 && DateTime.TryParse(line.Substring(0, 19), out var timestamp))
        {
            logEntry.Timestamp = timestamp;
            logEntry.Message = line.Substring(20);
            
            if (line.Contains("ERROR"))
                logEntry.Level = LogLevel.Error;
            else if (line.Contains("WARN"))
                logEntry.Level = LogLevel.Warning;
            else
                logEntry.Level = LogLevel.Info;
                
            return true;
        }
        
        return false;
    }
}

public enum LogLevel { Info, Warning, Error }

public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public LogLevel Level { get; set; }
    public string Message { get; set; } = "";
}

public class LogSummary
{
    public DateTime Hour { get; set; }
    public int EntryCount { get; set; }
    public int ErrorCount { get; set; }
    public int WarningCount { get; set; }

    public override string ToString()
    {
        return $"{Hour:yyyy-MM-dd HH:00} - Entries: {EntryCount}, Errors: {ErrorCount}, Warnings: {WarningCount}";
    }
}
```

**Usage**:

```csharp
using System;
using System.Net.Http;
using System.Threading;

// Example 1: Basic async enumerable consumption
await foreach (var number in AsyncEnumerableExamples.GenerateNumbersAsync(5, 500))
{
    Console.WriteLine($"Generated: {number}");
}
// Output: Generated: 1, Generated: 2, etc. (with 500ms delays)

// Example 2: File processing with cancellation
var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
await foreach (var line in AsyncEnumerableExamples.ReadLinesAsync("large-file.txt", cts.Token))
{
    Console.WriteLine($"Line: {line}");
    // Processing will stop after 10 seconds
}

// Example 3: HTTP API pagination
using var httpClient = new HttpClient();
var apiData = AsyncEnumerableExamples.FetchPagedDataAsync<Product>(
    httpClient, 
    "https://api.example.com/products");

await foreach (var product in apiData.TakeAsync(100)) // Only take first 100 items
{
    Console.WriteLine($"Product: {product.Name}");
}

// Example 4: Real-time sensor data
var sensorStream = AsyncEnumerableExamples.SimulateSensorDataAsync(TimeSpan.FromSeconds(1));
var filteredData = sensorStream
    .WhereAsync(reading => reading.Temperature > 25.0)
    .TakeAsync(10);

await foreach (var reading in filteredData)
{
    Console.WriteLine($"High temp reading: {reading}");
}

// Example 5: Buffering and batch processing
var numbers = AsyncEnumerableExamples.GenerateNumbersAsync(20);
var batches = numbers.BufferAsync(5);

await foreach (var batch in batches)
{
    Console.WriteLine($"Batch: [{string.Join(", ", batch)}]");
}
// Output: Batch: [1, 2, 3, 4, 5], Batch: [6, 7, 8, 9, 10], etc.

// Example 6: Async transformation pipeline
var processor = new AsyncDataProcessor<int>(
    filter: async x => await Task.FromResult(x % 2 == 0), // Even numbers only
    transform: async x => await Task.FromResult(x * x)     // Square them
);

var evenSquares = processor.ProcessAsync(AsyncEnumerableExamples.GenerateNumbersAsync(10));
await foreach (var square in evenSquares)
{
    Console.WriteLine($"Even square: {square}");
}
// Output: Even square: 4, Even square: 16, Even square: 36, Even square: 64, Even square: 100

// Example 7: Parallel processing
var source = AsyncEnumerableExamples.GenerateNumbersAsync(10);
var processed = ParallelAsyncProcessor.ProcessInParallelAsync(
    source,
    async x => 
    {
        await Task.Delay(100); // Simulate work
        return x * 2;
    },
    maxConcurrency: 3
);

await foreach (var result in processed)
{
    Console.WriteLine($"Processed: {result}");
}

// Example 8: Log file processing
var logProcessor = new LogFileProcessor();
var logFiles = new[] { "app.log", "error.log", "debug.log" };

var logEntries = logProcessor.ProcessLogFilesAsync(logFiles);
var summaries = logProcessor.GenerateHourlySummariesAsync(logEntries);

await foreach (var summary in summaries.TakeAsync(24)) // Last 24 hours
{
    Console.WriteLine(summary);
}

// Example 9: Error handling in async enumerables
try
{
    await foreach (var item in GenerateWithErrorsAsync())
    {
        Console.WriteLine($"Item: {item}");
    }
}
catch (Exception ex)
{
    Console.WriteLine($"Error occurred: {ex.Message}");
}

static async IAsyncEnumerable<int> GenerateWithErrorsAsync()
{
    for (int i = 1; i <= 5; i++)
    {
        if (i == 3)
            throw new InvalidOperationException("Something went wrong!");
            
        await Task.Delay(100);
        yield return i;
    }
}

// Example 10: Combining multiple streams
var stream1 = AsyncEnumerableExamples.GenerateNumbersAsync(5);
var stream2 = AsyncEnumerableExamples.GenerateNumbersAsync(5).SelectAsync(x => x + 10);
var merged = AsyncEnumerableExtensions.MergeAsync(stream1, stream2);

var allItems = await merged.ToListAsync();
Console.WriteLine($"All items: [{string.Join(", ", allItems)}]");
```

**Notes**:

- Use `[EnumeratorCancellation]` parameter attribute for proper cancellation token propagation
- `IAsyncEnumerable<T>` is ideal for streaming large datasets without loading everything into memory
- Always consider cancellation tokens for responsive applications and resource cleanup
- Async enumerables integrate well with LINQ-style operations through extension methods
- Be careful with exception handling - exceptions in async enumerables propagate to consumers
- Use buffering for batch processing scenarios to improve throughput
- Consider `ConfigureAwait(false)` in library code to avoid deadlocks
- Parallel processing with async enumerables requires careful coordination with semaphores

**Prerequisites**:

- .NET Core 3.0+ or .NET Framework with C# 8.0+ for `IAsyncEnumerable<T>` support
- Understanding of async/await patterns and Task-based programming
- Familiarity with LINQ and enumerable operations
- Knowledge of cancellation tokens and cooperative cancellation

**Related Snippets**:

- [Task Combinators](task-combinators.md) - Combining multiple async operations
- [Cancellation Patterns](cancellation-patterns.md) - Advanced cancellation scenarios
- [LINQ Extensions](linq-extensions.md) - Synchronous enumerable extensions
