# Cancellation Patterns

**Description**: Comprehensive patterns for cancellation token handling in async operations. Includes timeout management, cooperative cancellation, linked tokens, and graceful shutdown patterns for building responsive and controllable async applications.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO;
using System.Diagnostics;

// Basic cancellation patterns
public class CancellationExamples
{
    // Simple cancellation with timeout
    public static async Task<string> FetchDataWithTimeoutAsync(
        string url, 
        int timeoutSeconds = 30)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        
        try
        {
            using var httpClient = new HttpClient();
            return await httpClient.GetStringAsync(url, cts.Token);
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"Operation timed out after {timeoutSeconds} seconds");
        }
    }

    // Cancellation with progress reporting
    public static async Task<byte[]> DownloadFileWithProgressAsync(
        string url,
        IProgress<DownloadProgress> progress = null,
        CancellationToken cancellationToken = default)
    {
        using var httpClient = new HttpClient();
        using var response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        
        response.EnsureSuccessStatusCode();
        
        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        var buffer = new byte[8192];
        var totalRead = 0L;
        
        using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var memoryStream = new MemoryStream();
        
        int bytesRead;
        while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
        {
            await memoryStream.WriteAsync(buffer, 0, bytesRead, cancellationToken);
            totalRead += bytesRead;
            
            progress?.Report(new DownloadProgress(totalRead, totalBytes));
            
            // Check for cancellation periodically
            cancellationToken.ThrowIfCancellationRequested();
        }
        
        return memoryStream.ToArray();
    }

    // Cooperative cancellation in CPU-bound work
    public static async Task<long> CalculatePrimesAsync(
        int maxNumber,
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() =>
        {
            long primeCount = 0;
            
            for (int number = 2; number <= maxNumber; number++)
            {
                // Check for cancellation every 1000 iterations
                if (number % 1000 == 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
                
                if (IsPrime(number))
                {
                    primeCount++;
                }
            }
            
            return primeCount;
        }, cancellationToken);
    }
    
    private static bool IsPrime(int number)
    {
        if (number < 2) return false;
        for (int i = 2; i <= Math.Sqrt(number); i++)
        {
            if (number % i == 0) return false;
        }
        return true;
    }
}

// Advanced cancellation coordinator
public class CancellationCoordinator : IDisposable
{
    private readonly CancellationTokenSource masterCts;
    private readonly List<CancellationTokenSource> childSources;
    private readonly object lockObj = new();
    private bool disposed;

    public CancellationCoordinator()
    {
        masterCts = new CancellationTokenSource();
        childSources = new();
    }

    public CancellationToken MasterToken => masterCts.Token;

    // Create a linked token that cancels when master cancels OR when timeout occurs
    public CancellationToken CreateLinkedToken(TimeSpan timeout)
    {
        lock (lockObj)
        {
            if (disposed) throw new ObjectDisposedException(nameof(CancellationCoordinator));
            
            var childCts = CancellationTokenSource.CreateLinkedTokenSource(masterCts.Token);
            childCts.CancelAfter(timeout);
            
            childSources.Add(childCts);
            return childCts.Token;
        }
    }

    // Create a linked token that cancels when master cancels OR when external token cancels
    public CancellationToken CreateLinkedToken(CancellationToken externalToken)
    {
        lock (lockObj)
        {
            if (disposed) throw new ObjectDisposedException(nameof(CancellationCoordinator));
            
            var childCts = CancellationTokenSource.CreateLinkedTokenSource(masterCts.Token, externalToken);
            childSources.Add(childCts);
            
            return childCts.Token;
        }
    }

    // Cancel all operations
    public void CancelAll()
    {
        lock (lockObj)
        {
            if (!disposed)
            {
                masterCts.Cancel();
            }
        }
    }

    public void Dispose()
    {
        lock (lockObj)
        {
            if (!disposed)
            {
                masterCts.Cancel();
                masterCts.Dispose();
                
                foreach (var childCts in childSources)
                {
                    childCts.Dispose();
                }
                
                childSources.Clear();
                disposed = true;
            }
        }
    }
}

// Graceful shutdown service
public class GracefulShutdownService
{
    private readonly CancellationTokenSource shutdownCts;
    private readonly List<Func<CancellationToken, Task>> shutdownTasks;
    private readonly object lockObj = new();

    public GracefulShutdownService()
    {
        shutdownCts = new CancellationTokenSource();
        shutdownTasks = new List<Func<CancellationToken, Task>>();
    }

    public CancellationToken ShutdownToken => shutdownCts.Token;

    // Register a task to run during shutdown
    public void RegisterShutdownTask(Func<CancellationToken, Task> shutdownTask)
    {
        lock (lockObj)
        {
            shutdownTasks.Add(shutdownTask);
        }
    }

    // Initiate graceful shutdown
    public async Task ShutdownAsync(TimeSpan timeout = default)
    {
        if (timeout == default)
            timeout = TimeSpan.FromSeconds(30);

        shutdownCts.Cancel();

        var tasks = new();
        lock (lockObj)
        {
            foreach (var shutdownTask in shutdownTasks)
            {
                tasks.Add(shutdownTask(shutdownCts.Token));
            }
        }

        try
        {
            using var timeoutCts = new CancellationTokenSource(timeout);
            await Task.WhenAll(tasks).WaitAsync(timeoutCts.Token);
        }
        catch (OperationCanceledException)
        {
            // Log that some shutdown tasks didn't complete in time
            Console.WriteLine($"Some shutdown tasks didn't complete within {timeout}");
        }
    }
}

// Cancellation-aware background service
public abstract class CancellableBackgroundService : IDisposable
{
    private readonly CancellationTokenSource stoppingCts = new CancellationTokenSource();
    private Task? executingTask;

    protected CancellationToken StoppingToken => stoppingCts.Token;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        executingTask = ExecuteAsync(stoppingCts.Token);
        
        if (executingTask.IsCompleted)
        {
            return executingTask;
        }

        return Task.CompletedTask;
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (executingTask == null)
            return;

        try
        {
            stoppingCts.Cancel();
        }
        finally
        {
            await Task.WhenAny(executingTask, Task.Delay(Timeout.Infinite, cancellationToken));
        }
    }

    protected abstract Task ExecuteAsync(CancellationToken stoppingToken);

    public virtual void Dispose()
    {
        stoppingCts.Cancel();
        stoppingCts.Dispose();
    }
}

// Retry with cancellation pattern
public class RetryWithCancellation
{
    public static async Task<T> ExecuteWithRetryAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        int maxRetries = 3,
        TimeSpan delay = default,
        CancellationToken cancellationToken = default)
    {
        if (delay == default)
            delay = TimeSpan.FromSeconds(1);

        Exception? lastException = null;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await operation(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw; // Don't retry if operation was cancelled
            }
            catch (Exception ex)
            {
                lastException = ex;
                
                if (attempt == maxRetries)
                    break;

                // Wait before retry, but respect cancellation
                try
                {
                    await Task.Delay(delay, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
            }
        }

        throw lastException ?? new InvalidOperationException("Operation failed after all retries");
    }

    // Exponential backoff retry
    public static async Task<T> ExecuteWithExponentialBackoffAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        int maxRetries = 3,
        TimeSpan initialDelay = default,
        double backoffMultiplier = 2.0,
        CancellationToken cancellationToken = default)
    {
        if (initialDelay == default)
            initialDelay = TimeSpan.FromSeconds(1);

        Exception? lastException = null;
        var currentDelay = initialDelay;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                return await operation(cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                lastException = ex;
                
                if (attempt == maxRetries)
                    break;

                try
                {
                    await Task.Delay(currentDelay, cancellationToken);
                    currentDelay = TimeSpan.FromMilliseconds(currentDelay.TotalMilliseconds * backoffMultiplier);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
            }
        }

        throw lastException ?? new InvalidOperationException("Operation failed after all retries");
    }
}

// Timeout utility with cancellation
public static class TimeoutUtility
{
    // Add timeout to any task
    public static async Task<T> WithTimeoutAsync<T>(
        this Task<T> task, 
        TimeSpan timeout, 
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        try
        {
            return await task.WaitAsync(combinedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"Operation timed out after {timeout}");
        }
    }

    // Add timeout to void task
    public static async Task WithTimeoutAsync(
        this Task task, 
        TimeSpan timeout, 
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        try
        {
            await task.WaitAsync(combinedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"Operation timed out after {timeout}");
        }
    }
}

// Cancellation-aware parallel processing
public class ParallelProcessor<T>
{
    public static async Task ProcessInParallelAsync<TResult>(
        IEnumerable<T> items,
        Func<T, CancellationToken, Task<TResult>> processor,
        int maxConcurrency = Environment.ProcessorCount,
        CancellationToken cancellationToken = default)
    {
        using var semaphore = new(maxConcurrency, maxConcurrency);
        var tasks = items.Select(async item =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                return await processor(item, cancellationToken);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }

    // Process with early cancellation on first failure
    public static async Task<TResult[]> ProcessWithFailFastAsync<TResult>(
        IEnumerable<T> items,
        Func<T, CancellationToken, Task<TResult>> processor,
        CancellationToken cancellationToken = default)
    {
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        var tasks = items.Select(async item =>
        {
            try
            {
                return await processor(item, linkedCts.Token);
            }
            catch
            {
                linkedCts.Cancel(); // Cancel all other operations on first failure
                throw;
            }
        }).ToArray();

        return await Task.WhenAll(tasks);
    }
}

// Periodic task with cancellation
public class PeriodicTask : IDisposable
{
    private readonly Timer timer;
    private readonly Func<CancellationToken, Task> action;
    private readonly CancellationTokenSource cancellationTokenSource;
    private volatile bool isExecuting;

    public PeriodicTask(
        Func<CancellationToken, Task> action,
        TimeSpan interval,
        TimeSpan? initialDelay = null)
    {
        this.action = action ?? throw new ArgumentNullException(nameof(action));
        cancellationTokenSource = new CancellationTokenSource();
        
        var delay = initialDelay ?? interval;
        timer = new Timer(async _ => await ExecuteAsync(), null, delay, interval);
    }

    private async Task ExecuteAsync()
    {
        if (isExecuting || cancellationTokenSource.Token.IsCancellationRequested)
            return;

        isExecuting = true;
        try
        {
            await action(cancellationTokenSource.Token);
        }
        catch (OperationCanceledException) when (cancellationTokenSource.Token.IsCancellationRequested)
        {
            // Expected cancellation
        }
        catch (Exception ex)
        {
            // Log exception
            Console.WriteLine($"Periodic task error: {ex.Message}");
        }
        finally
        {
            isExecuting = false;
        }
    }

    public void Stop()
    {
        cancellationTokenSource.Cancel();
        timer?.Change(Timeout.Infinite, Timeout.Infinite);
    }

    public void Dispose()
    {
        Stop();
        timer?.Dispose();
        cancellationTokenSource?.Dispose();
    }
}

// Progress reporting with cancellation
public class ProgressReporter<T> : IProgress<T>
{
    private readonly Action<T> handler;
    private readonly CancellationToken cancellationToken;
    private readonly SynchronizationContext? context;

    public ProgressReporter(Action<T> handler, CancellationToken cancellationToken = default)
    {
        this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
        this.cancellationToken = cancellationToken;
        context = SynchronizationContext.Current;
    }

    public void Report(T value)
    {
        if (cancellationToken.IsCancellationRequested)
            return;

        if (context != null)
        {
            context.Post(_ => handler(value), null);
        }
        else
        {
            handler(value);
        }
    }
}

// Real-world examples
public class FileProcessingService : CancellableBackgroundService
{
    private readonly string watchDirectory;
    private readonly int processingDelayMs;

    public FileProcessingService(string watchDirectory, int processingDelayMs = 1000)
    {
        this.watchDirectory = watchDirectory;
        this.processingDelayMs = processingDelayMs;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var files = Directory.GetFiles(watchDirectory, "*.txt");
                
                foreach (var file in files)
                {
                    if (stoppingToken.IsCancellationRequested)
                        break;

                    await ProcessFileAsync(file, stoppingToken);
                }

                await Task.Delay(processingDelayMs, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected cancellation
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"File processing error: {ex.Message}");
                await Task.Delay(5000, stoppingToken); // Wait before retrying
            }
        }
    }

    private async Task ProcessFileAsync(string filePath, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Processing file: {filePath}");
        
        // Simulate file processing with cancellation support
        var content = await File.ReadAllTextAsync(filePath, cancellationToken);
        
        // Process content (simulate work)
        await Task.Delay(2000, cancellationToken);
        
        // Move processed file
        var processedPath = Path.ChangeExtension(filePath, ".processed");
        File.Move(filePath, processedPath);
        
        Console.WriteLine($"Completed processing: {filePath}");
    }
}

public class ApiService
{
    private readonly HttpClient httpClient;
    private readonly CancellationCoordinator cancellationCoordinator;

    public ApiService()
    {
        httpClient = new HttpClient { Timeout = Timeout.InfiniteTimeSpan }; // We'll handle timeouts manually
        cancellationCoordinator = new CancellationCoordinator();
    }

    public async Task<string> GetDataAsync(
        string endpoint, 
        TimeSpan timeout = default, 
        CancellationToken cancellationToken = default)
    {
        if (timeout == default)
            timeout = TimeSpan.FromSeconds(30);

        var linkedToken = cancellationCoordinator.CreateLinkedToken(cancellationToken);
        
        return await RetryWithCancellation.ExecuteWithExponentialBackoffAsync(
            async token =>
            {
                using var timeoutCts = new CancellationTokenSource(timeout);
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(token, timeoutCts.Token);
                
                try
                {
                    return await httpClient.GetStringAsync(endpoint, combinedCts.Token);
                }
                catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
                {
                    throw new TimeoutException($"Request to {endpoint} timed out after {timeout}");
                }
            },
            maxRetries: 3,
            initialDelay: TimeSpan.FromSeconds(1),
            cancellationToken: linkedToken
        );
    }

    public void CancelAllRequests()
    {
        cancellationCoordinator.CancelAll();
    }

    public void Dispose()
    {
        cancellationCoordinator?.Dispose();
        httpClient?.Dispose();
    }
}

public class BatchJobProcessor
{
    public async Task<ProcessingResult[]> ProcessBatchAsync<TItem>(
        IEnumerable<TItem> items,
        Func<TItem, CancellationToken, Task<ProcessingResult>> processor,
        BatchProcessingOptions options,
        CancellationToken cancellationToken = default)
    {
        var progress = new Progress<BatchProgress>(p => 
            options.ProgressCallback?.Invoke(p));

        var semaphore = new(options.MaxConcurrency, options.MaxConcurrency);
        var itemList = items.ToList();
        var results = new ProcessingResult[itemList.Count];
        var completedCount = 0;

        var tasks = itemList.Select(async (item, index) =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                using var itemCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                itemCts.CancelAfter(options.ItemTimeout);

                results[index] = await processor(item, itemCts.Token);
                
                var completed = Interlocked.Increment(ref completedCount);
                progress.Report(new BatchProgress(completed, itemList.Count));
                
                return results[index];
            }
            catch (Exception ex)
            {
                results[index] = new ProcessingResult { Success = false, Error = ex.Message };
                return results[index];
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        return results;
    }
}

// Supporting data structures
public record DownloadProgress(long BytesDownloaded, long TotalBytes)
{
    public double ProgressPercentage => TotalBytes > 0 ? (double)BytesDownloaded / TotalBytes * 100 : 0;
}

public record BatchProgress(int CompletedItems, int TotalItems)
{
    public double ProgressPercentage => TotalItems > 0 ? (double)CompletedItems / TotalItems * 100 : 0;
}

public class BatchProcessingOptions
{
    public int MaxConcurrency { get; set; } = Environment.ProcessorCount;
    public TimeSpan ItemTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public Action<BatchProgress>? ProgressCallback { get; set; }
}

public class ProcessingResult
{
    public bool Success { get; set; }
    public string? Error { get; set; }
    public object? Data { get; set; }
}

// Extension methods for easier cancellation handling
public static class CancellationExtensions
{
    // Create a cancellation token that cancels after a delay
    public static CancellationToken WithTimeout(this CancellationToken cancellationToken, TimeSpan timeout)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        return cts.Token;
    }

    // Combine multiple cancellation tokens
    public static CancellationToken CombineWith(this CancellationToken token, params CancellationToken[] otherTokens)
    {
        var allTokens = new[] { token }.Concat(otherTokens).ToArray();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(allTokens);
        return cts.Token;
    }

    // Check if task was cancelled due to timeout
    public static bool WasCancelledDueToTimeout(this OperationCanceledException ex, CancellationToken timeoutToken)
    {
        return timeoutToken.IsCancellationRequested;
    }

    // Safe task cancellation check
    public static async Task<bool> TryCancelAfterAsync(this Task task, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        
        try
        {
            await task.WaitAsync(cts.Token);
            return true; // Completed before timeout
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            return false; // Timed out
        }
    }
}
```

**Usage**:

```csharp
// Example 1: Basic timeout with cancellation
try
{
    var result = await CancellationExamples.FetchDataWithTimeoutAsync(
        "https://api.example.com/data", 
        timeoutSeconds: 10);
    Console.WriteLine($"Data received: {result.Substring(0, Math.Min(50, result.Length))}...");
}
catch (TimeoutException ex)
{
    Console.WriteLine($"Request timed out: {ex.Message}");
}
catch (HttpRequestException ex)
{
    Console.WriteLine($"HTTP error: {ex.Message}");
}

// Example 2: Download with progress and cancellation
using var cts = new CancellationTokenSource();

var progress = new Progress<DownloadProgress>(p =>
{
    Console.WriteLine($"Downloaded: {p.BytesDownloaded:N0} / {p.TotalBytes:N0} bytes " +
                     $"({p.ProgressPercentage:F1}%)");
});

// Cancel after 30 seconds
cts.CancelAfter(TimeSpan.FromSeconds(30));

try
{
    var fileData = await CancellationExamples.DownloadFileWithProgressAsync(
        "https://example.com/largefile.zip",
        progress,
        cts.Token);
    
    Console.WriteLine($"Download completed: {fileData.Length:N0} bytes");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Download was cancelled");
}

// Example 3: CPU-bound work with cooperative cancellation
using var primeCts = new CancellationTokenSource();

// Cancel after 10 seconds
primeCts.CancelAfter(TimeSpan.FromSeconds(10));

try
{
    var primeCount = await CancellationExamples.CalculatePrimesAsync(
        maxNumber: 1_000_000, 
        primeCts.Token);
    
    Console.WriteLine($"Found {primeCount:N0} prime numbers");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Prime calculation was cancelled");
}

// Example 4: Coordinated cancellation
using var coordinator = new CancellationCoordinator();

// Start multiple operations with different timeouts
var task1 = DoLongRunningWorkAsync("Task 1", coordinator.CreateLinkedToken(TimeSpan.FromSeconds(5)));
var task2 = DoLongRunningWorkAsync("Task 2", coordinator.CreateLinkedToken(TimeSpan.FromSeconds(10)));
var task3 = DoLongRunningWorkAsync("Task 3", coordinator.CreateLinkedToken(TimeSpan.FromSeconds(15)));

// Let them run for a bit
await Task.Delay(3000);

// Cancel all remaining operations
coordinator.CancelAll();

try
{
    await Task.WhenAll(task1, task2, task3);
}
catch (OperationCanceledException)
{
    Console.WriteLine("Some operations were cancelled");
}

static async Task DoLongRunningWorkAsync(string taskName, CancellationToken cancellationToken)
{
    try
    {
        for (int i = 0; i < 20; i++)
        {
            await Task.Delay(1000, cancellationToken);
            Console.WriteLine($"{taskName}: Step {i + 1}");
        }
        Console.WriteLine($"{taskName}: Completed successfully");
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine($"{taskName}: Was cancelled");
        throw;
    }
}

// Example 5: Graceful shutdown service
var shutdownService = new GracefulShutdownService();

// Register cleanup tasks
shutdownService.RegisterShutdownTask(async token =>
{
    Console.WriteLine("Cleaning up database connections...");
    await Task.Delay(2000, token);
    Console.WriteLine("Database connections cleaned up");
});

shutdownService.RegisterShutdownTask(async token =>
{
    Console.WriteLine("Flushing log buffers...");
    await Task.Delay(1000, token);
    Console.WriteLine("Log buffers flushed");
});

shutdownService.RegisterShutdownTask(async token =>
{
    Console.WriteLine("Notifying external services...");
    await Task.Delay(3000, token);
    Console.WriteLine("External services notified");
});

// Simulate application running
Console.WriteLine("Application running... Press any key to shutdown");
Console.ReadKey();

// Initiate graceful shutdown
await shutdownService.ShutdownAsync(TimeSpan.FromSeconds(10));
Console.WriteLine("Graceful shutdown completed");

// Example 6: Background service with cancellation
var processingService = new FileProcessingService("C:\\temp\\input");

await processingService.StartAsync();
Console.WriteLine("File processing service started. Press any key to stop...");
Console.ReadKey();

await processingService.StopAsync();
processingService.Dispose();
Console.WriteLine("File processing service stopped");

// Example 7: Retry with cancellation
using var retryCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

try
{
    var result = await RetryWithCancellation.ExecuteWithExponentialBackoffAsync(
        async token =>
        {
            // Simulate flaky operation
            var random = new Random();
            if (random.NextDouble() < 0.7) // 70% chance of failure
            {
                throw new Exception("Simulated transient error");
            }
            
            await Task.Delay(1000, token);
            return "Success!";
        },
        maxRetries: 5,
        initialDelay: TimeSpan.FromMilliseconds(500),
        backoffMultiplier: 2.0,
        cancellationToken: retryCts.Token
    );
    
    Console.WriteLine($"Retry operation succeeded: {result}");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Retry operation was cancelled due to timeout");
}
catch (Exception ex)
{
    Console.WriteLine($"Retry operation failed after all attempts: {ex.Message}");
}

// Example 8: Timeout utility usage
var slowTask = SimulateSlowWorkAsync();

try
{
    var result = await slowTask.WithTimeoutAsync(TimeSpan.FromSeconds(5));
    Console.WriteLine($"Slow work completed: {result}");
}
catch (TimeoutException ex)
{
    Console.WriteLine($"Slow work timed out: {ex.Message}");
}

static async Task<string> SimulateSlowWorkAsync()
{
    await Task.Delay(10000); // 10 seconds of work
    return "Slow work completed";
}

// Example 9: Parallel processing with cancellation
var items = Enumerable.Range(1, 100).ToList();

using var parallelCts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

try
{
    await ParallelProcessor<int>.ProcessInParallelAsync(
        items,
        async (item, token) =>
        {
            await Task.Delay(Random.Shared.Next(100, 1000), token);
            Console.WriteLine($"Processed item: {item}");
            return item * 2;
        },
        maxConcurrency: 5,
        cancellationToken: parallelCts.Token
    );
    
    Console.WriteLine("All items processed successfully");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Parallel processing was cancelled");
}

// Example 10: Periodic task with cancellation
using var periodicTask = new PeriodicTask(
    async token =>
    {
        Console.WriteLine($"Periodic task executing at {DateTime.Now:HH:mm:ss}");
        await Task.Delay(2000, token); // Simulate work
        Console.WriteLine("Periodic task work completed");
    },
    interval: TimeSpan.FromSeconds(5),
    initialDelay: TimeSpan.FromSeconds(1)
);

Console.WriteLine("Periodic task started. Press any key to stop...");
Console.ReadKey();

periodicTask.Stop();
Console.WriteLine("Periodic task stopped");

// Example 11: API service with coordinated cancellation
using var apiService = new ApiService();

var apiTasks = new[]
{
    apiService.GetDataAsync("https://api.example.com/endpoint1"),
    apiService.GetDataAsync("https://api.example.com/endpoint2"),
    apiService.GetDataAsync("https://api.example.com/endpoint3")
};

// Let them run for a few seconds
await Task.Delay(3000);

// Cancel all API requests
apiService.CancelAllRequests();

try
{
    var results = await Task.WhenAll(apiTasks);
    Console.WriteLine($"All API calls completed: {results.Length} results");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Some API calls were cancelled");
}

// Example 12: Batch processing with progress and cancellation
var batchProcessor = new BatchJobProcessor();
var itemsToProcess = Enumerable.Range(1, 50).Select(i => new { Id = i, Data = $"Item {i}" });

var options = new BatchProcessingOptions
{
    MaxConcurrency = 3,
    ItemTimeout = TimeSpan.FromSeconds(10),
    ProgressCallback = progress =>
    {
        Console.WriteLine($"Batch progress: {progress.CompletedItems}/{progress.TotalItems} " +
                         $"({progress.ProgressPercentage:F1}%)");
    }
};

using var batchCts = new CancellationTokenSource(TimeSpan.FromMinutes(2));

try
{
    var results = await batchProcessor.ProcessBatchAsync(
        itemsToProcess,
        async (item, token) =>
        {
            // Simulate processing
            await Task.Delay(Random.Shared.Next(500, 2000), token);
            return new ProcessingResult
            {
                Success = true,
                Data = $"Processed {item.Data}"
            };
        },
        options,
        batchCts.Token
    );
    
    var successCount = results.Count(r => r.Success);
    Console.WriteLine($"Batch processing completed: {successCount}/{results.Length} successful");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Batch processing was cancelled");
}

// Example 13: Custom cancellation token combinations
var userCts = new CancellationTokenSource();
var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
var systemCts = new CancellationTokenSource();

// Combine multiple cancellation sources
var combinedToken = userCts.Token.CombineWith(timeoutCts.Token, systemCts.Token);

// Use combined token in operations
try
{
    await SomeOperationAsync(combinedToken);
}
catch (OperationCanceledException ex)
{
    if (userCts.Token.IsCancellationRequested)
        Console.WriteLine("Operation cancelled by user");
    else if (timeoutCts.Token.IsCancellationRequested)
        Console.WriteLine("Operation timed out");
    else if (systemCts.Token.IsCancellationRequested)
        Console.WriteLine("Operation cancelled by system");
}

static async Task SomeOperationAsync(CancellationToken cancellationToken)
{
    for (int i = 0; i < 20; i++)
    {
        await Task.Delay(500, cancellationToken);
        Console.WriteLine($"Operation step {i + 1}");
    }
}

// Example 14: Safe cancellation check
var longRunningTask = Task.Run(async () =>
{
    await Task.Delay(15000); // 15 seconds
    return "Long running task completed";
});

var completed = await longRunningTask.TryCancelAfterAsync(TimeSpan.FromSeconds(5));

if (completed)
{
    Console.WriteLine("Task completed within timeout");
}
else
{
    Console.WriteLine("Task did not complete within timeout");
}
```

**Notes**:

- Always respect cancellation tokens in long-running operations by checking `IsCancellationRequested` or calling `ThrowIfCancellationRequested()`
- Use `CancellationTokenSource.CreateLinkedTokenSource()` to combine multiple cancellation conditions
- Set reasonable timeouts for network operations and provide user-friendly timeout messages
- In CPU-bound work, check for cancellation periodically (every 1000 iterations is often good)
- Distinguish between user-requested cancellation and timeout cancellation in exception handling
- Dispose of `CancellationTokenSource` instances to prevent memory leaks
- Use cooperative cancellation patterns rather than forceful thread termination
- Consider implementing graceful shutdown sequences for complex applications
- Progress reporting should respect cancellation tokens to avoid unnecessary work
- Retry mechanisms should not retry operations that were explicitly cancelled by users

**Prerequisites**:

- .NET Framework 4.0+ or .NET Core for CancellationToken support
- Understanding of async/await patterns and Task-based programming
- Knowledge of thread safety and synchronization concepts
- Familiarity with exception handling in async contexts

**Related Snippets**:

- [Async Lazy Loading](async-lazy-loading.md) - Lazy initialization with cancellation support
- [Task Combinators](task-combinators.md) - Advanced task coordination patterns
- [Retry Pattern](retry-pattern.md) - Basic retry implementation patterns
- [Async Enumerable](async-enumerable.md) - Streaming operations with cancellation
