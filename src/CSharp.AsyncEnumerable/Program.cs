using CSharp.AsyncEnumerable;
using System.Text.Json;

namespace CSharp.AsyncEnumerable;

/// <summary>
/// Demonstrates IAsyncEnumerable<T> patterns and async streaming in C#.
/// 
/// IAsyncEnumerable provides asynchronous iteration over collections,
/// enabling efficient streaming of data without blocking threads or
/// consuming excessive memory for large datasets.
/// 
/// Key Features Demonstrated:
/// - Basic async enumerable creation with yield return
/// - Async LINQ-style operations (TakeAsync, SkipAsync, WhereAsync)
/// - Cancellation token support with EnumeratorCancellation
/// - Error handling in async streams
/// - Backpressure and flow control
/// - Real-world scenarios (sensor data, API pagination, file processing)
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== IAsyncEnumerable<T> Pattern Demo ===\n");

        await DemoBasicAsyncEnumerable();
        await DemoAsyncLinqOperations();
        await DemoSensorDataStreaming();
        await DemoApiPagination();
        await DemoFileProcessing();
        await DemoCancellationSupport();
        await DemoErrorHandling();
        await DemoBackpressureControl();

        Console.WriteLine("\n=== Demo Complete ===");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    /// <summary>
    /// Demonstrates basic async enumerable creation and consumption
    /// </summary>
    private static async Task DemoBasicAsyncEnumerable()
    {
        Console.WriteLine("1. Basic Async Enumerable");
        Console.WriteLine("-------------------------");

        // Generate numbers asynchronously
        var numbers = AsyncEnumerableExamples.GenerateNumbersAsync(5, delayMs: 50);

        Console.WriteLine("Consuming async enumerable:");
        await foreach (var number in numbers)
        {
            Console.WriteLine($"  Received: {number}");
        }

        // Demonstrate immediate vs deferred execution
        Console.WriteLine("\nDeferred execution - created but not started:");
        var deferredNumbers = AsyncEnumerableExamples.GenerateNumbersAsync(3, delayMs: 100);
        Console.WriteLine("Numbers created, now iterating...");
        
        await foreach (var number in deferredNumbers)
        {
            Console.WriteLine($"  Deferred: {number}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates async LINQ-style operations
    /// </summary>
    private static async Task DemoAsyncLinqOperations()
    {
        Console.WriteLine("2. Async LINQ Operations");
        Console.WriteLine("------------------------");

        var numbers = AsyncEnumerableExamples.GenerateNumbersAsync(10, delayMs: 30);

        // Chain multiple async operations
        var processedNumbers = numbers
            .SkipAsync(2)
            .TakeAsync(5)
            .WhereAsync(n => n % 2 == 0)
            .SelectAsync(n => n * n);

        Console.WriteLine("Processing: Skip(2) -> Take(5) -> Where(even) -> Select(square)");
        await foreach (var result in processedNumbers)
        {
            Console.WriteLine($"  Result: {result}");
        }

        // Demonstrate aggregation operations
        var sum = await numbers.SumAsync(n => n);
        var count = await numbers.CountAsync();
        var average = await numbers.AverageAsync(n => (double)n);

        Console.WriteLine($"\nAggregation results:");
        Console.WriteLine($"  Sum: {sum}");
        Console.WriteLine($"  Count: {count}");
        Console.WriteLine($"  Average: {average:F2}");

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates real-time sensor data streaming
    /// </summary>
    private static async Task DemoSensorDataStreaming()
    {
        Console.WriteLine("3. Sensor Data Streaming");
        Console.WriteLine("------------------------");

        var sensorData = AsyncEnumerableExamples.GenerateSensorDataAsync(
            duration: TimeSpan.FromSeconds(2),
            intervalMs: 200);

        Console.WriteLine("Streaming sensor readings:");
        await foreach (var reading in sensorData.TakeAsync(8))
        {
            Console.WriteLine($"  {reading.Timestamp:HH:mm:ss.fff} - " +
                            $"Temp: {reading.Temperature:F1}°C, " +
                            $"Humidity: {reading.Humidity:F1}%, " +
                            $"Pressure: {reading.Pressure:F0} hPa");
        }

        // Demonstrate filtering and transformation
        var highTempReadings = sensorData
            .WhereAsync(r => r.Temperature > 22.0)
            .SelectAsync(r => new { r.Timestamp, r.Temperature });

        Console.WriteLine("\nHigh temperature alerts:");
        await foreach (var alert in highTempReadings.TakeAsync(3))
        {
            Console.WriteLine($"  ⚠️ {alert.Timestamp:HH:mm:ss} - High temp: {alert.Temperature:F1}°C");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates paginated API data consumption
    /// </summary>
    private static async Task DemoApiPagination()
    {
        Console.WriteLine("4. API Pagination Streaming");
        Console.WriteLine("---------------------------");

        // Simulate paginated API consumption
        var pagedItems = AsyncEnumerableExamples.FetchPaginatedDataAsync<string>(
            pageSize: 3,
            totalItems: 12);

        Console.WriteLine("Fetching paginated API data:");
        var itemCount = 0;
        await foreach (var item in pagedItems)
        {
            Console.WriteLine($"  Item {++itemCount}: {item}");
            
            // Demonstrate backpressure - slow consumer
            if (itemCount % 5 == 0)
            {
                await Task.Delay(100);
            }
        }

        Console.WriteLine($"Total items processed: {itemCount}");
        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates async file processing scenarios
    /// </summary>
    private static async Task DemoFileProcessing()
    {
        Console.WriteLine("5. Async File Processing");
        Console.WriteLine("------------------------");

        // Create sample data for processing
        var sampleLines = new[]
        {
            "Error: Database connection failed",
            "Info: User logged in successfully", 
            "Warning: High memory usage detected",
            "Error: API timeout occurred",
            "Info: Backup completed successfully",
            "Error: Invalid authentication token",
            "Info: Cache cleared successfully"
        };

        // Simulate async file line processing
        var logLines = AsyncEnumerableExamples.ProcessLinesAsync(sampleLines, delayMs: 80);

        // Filter and process error lines
        var errorLines = logLines
            .WhereAsync(line => line.Contains("Error"))
            .SelectAsync(line => $"🚨 {DateTime.Now:HH:mm:ss} - {line}");

        Console.WriteLine("Processing log file for errors:");
        await foreach (var errorLine in errorLines)
        {
            Console.WriteLine($"  {errorLine}");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates cancellation token support
    /// </summary>
    private static async Task DemoCancellationSupport()
    {
        Console.WriteLine("6. Cancellation Support");
        Console.WriteLine("-----------------------");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        try
        {
            var longRunningStream = AsyncEnumerableExamples.GenerateNumbersAsync(
                count: 20, 
                delayMs: 200, 
                cts.Token);

            Console.WriteLine("Starting long-running stream (will be cancelled):");
            await foreach (var number in longRunningStream)
            {
                Console.WriteLine($"  Processing: {number}");
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("  ⏹️ Stream cancelled as expected");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates error handling in async streams
    /// </summary>
    private static async Task DemoErrorHandling()
    {
        Console.WriteLine("7. Error Handling");
        Console.WriteLine("-----------------");

        var faultyStream = CreateFaultyStream();

        Console.WriteLine("Consuming stream with potential errors:");
        try
        {
            await foreach (var item in faultyStream)
            {
                Console.WriteLine($"  Success: {item}");
            }
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"  ❌ Caught expected error: {ex.Message}");
        }

        // Demonstrate resilient consumption with error recovery
        Console.WriteLine("\nResilient consumption with error recovery:");
        await ConsumeStreamWithErrorRecovery();

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates backpressure control mechanisms
    /// </summary>
    private static async Task DemoBackpressureControl()
    {
        Console.WriteLine("8. Backpressure Control");
        Console.WriteLine("-----------------------");

        var fastProducer = AsyncEnumerableExamples.GenerateNumbersAsync(15, delayMs: 50);
        
        Console.WriteLine("Fast producer with slow consumer (demonstrating natural backpressure):");
        var processed = 0;
        await foreach (var item in fastProducer.TakeAsync(8))
        {
            Console.WriteLine($"  Processing item {item}...");
            
            // Simulate slow processing
            await Task.Delay(120);
            processed++;
            
            if (processed % 3 == 0)
            {
                Console.WriteLine($"    🔄 Processed {processed} items so far");
            }
        }

        Console.WriteLine($"Backpressure naturally handled - processed {processed} items");
        Console.WriteLine();
    }

    // Helper methods for demonstrations

    private static async IAsyncEnumerable<string> CreateFaultyStream()
    {
        yield return "Item 1";
        yield return "Item 2";
        await Task.Delay(50);
        throw new InvalidOperationException("Simulated stream error");
    }

    private static async Task ConsumeStreamWithErrorRecovery()
    {
        var attempts = 0;
        const int maxAttempts = 3;

        while (attempts < maxAttempts)
        {
            attempts++;
            try
            {
                Console.WriteLine($"  Attempt {attempts}:");
                var stream = CreatePartiallyFaultyStream(attempts);
                
                await foreach (var item in stream)
                {
                    Console.WriteLine($"    ✅ {item}");
                }
                
                Console.WriteLine("    Stream completed successfully");
                break;
            }
            catch (Exception ex) when (attempts < maxAttempts)
            {
                Console.WriteLine($"    ❌ Attempt {attempts} failed: {ex.Message}");
                Console.WriteLine("    🔄 Retrying...");
                await Task.Delay(100);
            }
        }
    }

    private static async IAsyncEnumerable<string> CreatePartiallyFaultyStream(int attempt)
    {
        yield return $"Item A (attempt {attempt})";
        
        if (attempt < 2)
        {
            await Task.Delay(30);
            throw new InvalidOperationException($"Simulated failure on attempt {attempt}");
        }
        
        yield return $"Item B (attempt {attempt})";
        yield return $"Item C (attempt {attempt})";
    }
}