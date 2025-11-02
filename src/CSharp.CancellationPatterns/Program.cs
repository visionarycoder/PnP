using CSharp.CancellationPatterns;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CSharp.CancellationPatterns;

/// <summary>
/// Demonstrates comprehensive cancellation patterns in C# for robust async operations.
/// 
/// Proper cancellation handling is crucial for responsive applications, resource cleanup,
/// and graceful shutdown scenarios. This demo covers various cancellation patterns
/// from basic timeout handling to complex coordination scenarios.
/// 
/// Key Features Demonstrated:
/// - CancellationToken propagation and chaining
/// - Timeout-based cancellation with combining tokens
/// - Graceful shutdown patterns for background services
/// - Parallel operation cancellation and coordination
/// - Retry patterns with cancellation support
/// - Resource cleanup and proper disposal
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Cancellation Patterns Demo ===\n");

        var host = CreateHost();
        
        await DemoBasicCancellation();
        await DemoTimeoutPatterns();
        await DemoLinkedTokenSources();
        await DemoParallelCancellation();
        await DemoRetryWithCancellation();
        await DemoGracefulShutdown(host.Services);
        await DemoBackgroundServiceCancellation(host.Services);
        await DemoCancellationCoordination();

        Console.WriteLine("\n=== Demo Complete ===");
        Console.WriteLine("Press any key to exit...");
        Console.ReadKey();
    }

    /// <summary>
    /// Demonstrates basic cancellation token usage
    /// </summary>
    private static async Task DemoBasicCancellation()
    {
        Console.WriteLine("1. Basic Cancellation Patterns");
        Console.WriteLine("------------------------------");

        // Manual cancellation after delay
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(TimeSpan.FromSeconds(2));

        try
        {
            Console.WriteLine("Starting long-running operation (will be cancelled)...");
            await CancellationExamples.SimulateLongRunningOperationAsync(
                durationSeconds: 5,
                cts.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("✅ Operation cancelled as expected");
        }

        // Immediate cancellation
        using var immediateCancel = new CancellationTokenSource();
        immediateCancel.Cancel();

        try
        {
            await CancellationExamples.CheckCancellationAsync(immediateCancel.Token);
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("✅ Immediate cancellation handled");
        }

        // Cooperative cancellation
        Console.WriteLine("\nCooperative cancellation demo:");
        using var cooperativeCts = new CancellationTokenSource();
        cooperativeCts.CancelAfter(TimeSpan.FromMilliseconds(800));

        var result = await CancellationExamples.ProcessItemsWithCancellationAsync(
            itemCount: 20,
            processingDelayMs: 100,
            cooperativeCts.Token);

        Console.WriteLine($"Processed {result.ItemsProcessed} items before cancellation");
        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates various timeout patterns
    /// </summary>
    private static async Task DemoTimeoutPatterns()
    {
        Console.WriteLine("2. Timeout Patterns");
        Console.WriteLine("-------------------");

        // Fixed timeout
        try
        {
            Console.WriteLine("Testing HTTP request with 1-second timeout...");
            var result = await CancellationExamples.FetchDataWithTimeoutAsync(
                "https://httpbin.org/delay/2", // This will timeout
                timeoutSeconds: 1);
        }
        catch (TimeoutException ex)
        {
            Console.WriteLine($"✅ Timeout handled: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠️ Network error (expected in demo): {ex.Message}");
        }

        // Dynamic timeout based on operation complexity
        var operations = new[] { 100, 500, 1200, 2000 }; // milliseconds
        
        foreach (var duration in operations)
        {
            var timeout = TimeSpan.FromMilliseconds(duration + 200); // 200ms buffer
            
            try
            {
                Console.WriteLine($"Operation ({duration}ms) with {timeout.TotalMilliseconds}ms timeout...");
                await TimeoutUtility.WithTimeoutAsync(
                    SimulateWork(duration),
                    timeout);
                Console.WriteLine("  ✅ Completed within timeout");
            }
            catch (TimeoutException)
            {
                Console.WriteLine("  ⏰ Timed out");
            }
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates linked token sources for complex cancellation scenarios
    /// </summary>
    private static async Task DemoLinkedTokenSources()
    {
        Console.WriteLine("3. Linked Token Sources");
        Console.WriteLine("-----------------------");

        // Create parent cancellation sources
        using var userCancelCts = new CancellationTokenSource();
        using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        using var systemShutdownCts = new CancellationTokenSource();

        // Create linked token that responds to any cancellation
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
            userCancelCts.Token,
            timeoutCts.Token, 
            systemShutdownCts.Token);

        // Start background task that monitors the linked token
        var monitorTask = Task.Run(async () =>
        {
            try
            {
                while (!linkedCts.Token.IsCancellationRequested)
                {
                    Console.WriteLine("  🔄 Background operation running...");
                    await Task.Delay(200, linkedCts.Token);
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("  ⏹️ Background operation cancelled");
            }
        });

        // Simulate different cancellation triggers
        await Task.Delay(600); // Let it run for a bit
        
        // Determine cancellation reason
        await monitorTask;
        
        if (timeoutCts.Token.IsCancellationRequested)
        {
            Console.WriteLine("✅ Cancelled due to timeout");
        }
        else if (userCancelCts.Token.IsCancellationRequested)
        {
            Console.WriteLine("✅ Cancelled by user request");
        }
        else if (systemShutdownCts.Token.IsCancellationRequested)
        {
            Console.WriteLine("✅ Cancelled due to system shutdown");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates parallel operation cancellation
    /// </summary>
    private static async Task DemoParallelCancellation()
    {
        Console.WriteLine("4. Parallel Operation Cancellation");
        Console.WriteLine("----------------------------------");

        using var cts = new CancellationTokenSource();
        var processor = new ParallelProcessor();

        // Start parallel processing
        var processingTask = processor.ProcessInParallelAsync(
            itemCount: 20,
            maxConcurrency: 4,
            cts.Token);

        // Cancel after allowing some processing
        await Task.Delay(800);
        cts.Cancel();

        var result = await processingTask;
        
        Console.WriteLine($"Parallel processing results:");
        Console.WriteLine($"  Items completed: {result.CompletedItems}");
        Console.WriteLine($"  Items failed: {result.FailedItems}");
        Console.WriteLine($"  Items cancelled: {result.CancelledItems}");
        Console.WriteLine($"  Total processing time: {result.TotalTime.TotalMilliseconds:F0}ms");

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates retry patterns with cancellation support
    /// </summary>
    private static async Task DemoRetryWithCancellation()
    {
        Console.WriteLine("5. Retry with Cancellation");
        Console.WriteLine("-------------------------");

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        var retryService = new RetryWithCancellation();

        // Simulate an operation that fails a few times then succeeds
        var attempt = 0;
        Func<CancellationToken, Task<string>> flakyOperation = async (token) =>
        {
            attempt++;
            Console.WriteLine($"  Attempt {attempt}...");
            
            if (attempt < 3)
            {
                throw new InvalidOperationException($"Simulated failure on attempt {attempt}");
            }
            
            await Task.Delay(100, token);
            return $"Success on attempt {attempt}";
        };

        try
        {
            var result = await retryService.ExecuteWithRetryAsync(
                flakyOperation,
                maxAttempts: 5,
                baseDelay: TimeSpan.FromMilliseconds(200),
                cts.Token);

            Console.WriteLine($"✅ Retry succeeded: {result}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("⏰ Retry operation cancelled due to timeout");
        }

        // Demonstrate retry with exponential backoff
        Console.WriteLine("\nRetry with exponential backoff:");
        attempt = 0;
        using var backoffCts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        try
        {
            await retryService.ExecuteWithExponentialBackoffAsync(
                async (token) =>
                {
                    attempt++;
                    Console.WriteLine($"  Backoff attempt {attempt}...");
                    throw new Exception("Always fails for demo");
                },
                maxAttempts: 4,
                baseDelay: TimeSpan.FromMilliseconds(100),
                backoffCts.Token);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            Console.WriteLine($"❌ All retry attempts exhausted");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("⏰ Retry cancelled due to timeout");
        }

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates graceful shutdown patterns
    /// </summary>
    private static async Task DemoGracefulShutdown(IServiceProvider services)
    {
        Console.WriteLine("6. Graceful Shutdown Patterns");
        Console.WriteLine("-----------------------------");

        var logger = services.GetRequiredService<ILogger<Program>>();
        var shutdownService = new GracefulShutdownService(logger);

        // Start the service
        using var serviceLifetimeCts = new CancellationTokenSource();
        var serviceTask = shutdownService.StartAsync(serviceLifetimeCts.Token);

        Console.WriteLine("Service started, running for 1 second...");
        await Task.Delay(1000);

        // Initiate graceful shutdown
        Console.WriteLine("Initiating graceful shutdown...");
        serviceLifetimeCts.Cancel();

        // Wait for service to complete shutdown
        await serviceTask;
        Console.WriteLine("✅ Service shut down gracefully");

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates background service cancellation patterns
    /// </summary>
    private static async Task DemoBackgroundServiceCancellation(IServiceProvider services)
    {
        Console.WriteLine("7. Background Service Cancellation");
        Console.WriteLine("----------------------------------");

        var logger = services.GetRequiredService<ILogger<Program>>();
        var backgroundService = new CancellableBackgroundService(logger);

        using var serviceCts = new CancellationTokenSource();
        
        // Start background service
        var serviceTask = backgroundService.StartAsync(serviceCts.Token);
        
        Console.WriteLine("Background service started...");
        await Task.Delay(1500);

        // Stop the service
        await backgroundService.StopAsync(CancellationToken.None);
        Console.WriteLine("✅ Background service stopped gracefully");

        Console.WriteLine();
    }

    /// <summary>
    /// Demonstrates coordination of multiple cancellation operations
    /// </summary>
    private static async Task DemoCancellationCoordination()
    {
        Console.WriteLine("8. Cancellation Coordination");
        Console.WriteLine("---------------------------");

        var coordinator = new CancellationCoordinator();

        // Register multiple operations with the coordinator
        coordinator.RegisterOperation("DataProcessor", TimeSpan.FromSeconds(2));
        coordinator.RegisterOperation("FileUploader", TimeSpan.FromSeconds(1.5));
        coordinator.RegisterOperation("CacheUpdater", TimeSpan.FromSeconds(1));

        // Start coordinated cancellation
        Console.WriteLine("Starting coordinated operations...");
        var coordinationTask = coordinator.StartCoordinatedOperationsAsync();

        // Wait a bit then trigger coordinated shutdown
        await Task.Delay(800);
        Console.WriteLine("Triggering coordinated shutdown...");
        
        coordinator.InitiateShutdown();
        
        var results = await coordinationTask;
        
        Console.WriteLine("Coordination results:");
        foreach (var (operationName, completed, duration) in results)
        {
            var status = completed ? "✅ Completed" : "⏹️ Cancelled";
            Console.WriteLine($"  {operationName}: {status} ({duration.TotalMilliseconds:F0}ms)");
        }

        Console.WriteLine();
    }

    private static IHost CreateHost()
    {
        return Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddLogging(builder =>
                    builder.AddConsole().SetMinimumLevel(LogLevel.Information));
            })
            .Build();
    }

    private static async Task SimulateWork(int durationMs)
    {
        await Task.Delay(durationMs);
    }
}