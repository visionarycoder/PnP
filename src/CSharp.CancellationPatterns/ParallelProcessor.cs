namespace CSharp.CancellationPatterns;

/// <summary>
/// Provides cancellation-aware parallel processing utilities
/// </summary>
public static class ParallelProcessor
{
    /// <summary>
    /// Processes items in parallel with cancellation support and concurrency control
    /// </summary>
    public static async Task<TResult[]> ProcessInParallelAsync<TItem, TResult>(
        IEnumerable<TItem> items,
        Func<TItem, CancellationToken, Task<TResult>> processor,
        int maxConcurrency = 0,
        CancellationToken cancellationToken = default)
    {
        if (maxConcurrency <= 0)
            maxConcurrency = Environment.ProcessorCount;

        using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
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

        return await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Processes items with early cancellation on first failure
    /// </summary>
    public static async Task<TResult[]> ProcessWithFailFastAsync<TItem, TResult>(
        IEnumerable<TItem> items,
        Func<TItem, CancellationToken, Task<TResult>> processor,
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

    /// <summary>
    /// Processes items in batches with cancellation support
    /// </summary>
    public static async Task<TResult[]> ProcessInBatchesAsync<TItem, TResult>(
        IEnumerable<TItem> items,
        Func<TItem, CancellationToken, Task<TResult>> processor,
        int batchSize,
        int maxConcurrency = 0,
        CancellationToken cancellationToken = default)
    {
        if (maxConcurrency <= 0)
            maxConcurrency = Environment.ProcessorCount;

        var itemList = items.ToList();
        var results = new List<TResult>();

        for (int i = 0; i < itemList.Count; i += batchSize)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var batch = itemList.Skip(i).Take(batchSize);
            var batchResults = await ProcessInParallelAsync(
                batch, processor, maxConcurrency, cancellationToken);
            
            results.AddRange(batchResults);
        }

        return results.ToArray();
    }

    /// <summary>
    /// Processes items with progress reporting
    /// </summary>
    public static async Task<TResult[]> ProcessWithProgressAsync<TItem, TResult>(
        IEnumerable<TItem> items,
        Func<TItem, CancellationToken, Task<TResult>> processor,
        IProgress<ProcessingProgress>? progress = null,
        int maxConcurrency = 0,
        CancellationToken cancellationToken = default)
    {
        if (maxConcurrency <= 0)
            maxConcurrency = Environment.ProcessorCount;

        var itemList = items.ToList();
        var completedCount = 0;
        var results = new TResult[itemList.Count];

        using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        
        var tasks = itemList.Select(async (item, index) =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                results[index] = await processor(item, cancellationToken);
                
                var completed = Interlocked.Increment(ref completedCount);
                progress?.Report(new ProcessingProgress(completed, itemList.Count));
                
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

/// <summary>
/// Represents processing progress information
/// </summary>
public record ProcessingProgress(int CompletedItems, int TotalItems)
{
    /// <summary>
    /// Gets the completion percentage (0-100)
    /// </summary>
    public double ProgressPercentage => TotalItems > 0 ? (double)CompletedItems / TotalItems * 100 : 0;

    /// <summary>
    /// Gets a formatted progress string
    /// </summary>
    public string ProgressText => $"{CompletedItems}/{TotalItems} ({ProgressPercentage:F1}%)";
}

/// <summary>
/// Provides periodic task execution with cancellation support
/// </summary>
public class PeriodicTask : IDisposable
{
    private readonly Timer _timer;
    private readonly Func<CancellationToken, Task> _action;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private volatile bool _isExecuting;
    private volatile bool _isDisposed;

    /// <summary>
    /// Creates a new periodic task
    /// </summary>
    public PeriodicTask(
        Func<CancellationToken, Task> action,
        TimeSpan interval,
        TimeSpan? initialDelay = null)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _cancellationTokenSource = new CancellationTokenSource();
        
        var delay = initialDelay ?? interval;
        _timer = new Timer(async _ => await ExecuteAsync(), null, delay, interval);
    }

    /// <summary>
    /// Indicates if the task is currently executing
    /// </summary>
    public bool IsExecuting => _isExecuting;

    /// <summary>
    /// Indicates if cancellation was requested
    /// </summary>
    public bool IsCancellationRequested => _cancellationTokenSource.Token.IsCancellationRequested;

    private async Task ExecuteAsync()
    {
        if (_isExecuting || _cancellationTokenSource.Token.IsCancellationRequested || _isDisposed)
            return;

        _isExecuting = true;
        try
        {
            await _action(_cancellationTokenSource.Token);
        }
        catch (OperationCanceledException) when (_cancellationTokenSource.Token.IsCancellationRequested)
        {
            // Expected cancellation
        }
        catch (Exception ex)
        {
            OnException(ex);
        }
        finally
        {
            _isExecuting = false;
        }
    }

    /// <summary>
    /// Called when an exception occurs during task execution
    /// </summary>
    protected virtual void OnException(Exception exception)
    {
        // Default implementation - log to console
        Console.WriteLine($"Periodic task error: {exception.Message}");
    }

    /// <summary>
    /// Stops the periodic task
    /// </summary>
    public void Stop()
    {
        if (_isDisposed) return;

        _cancellationTokenSource.Cancel();
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>
    /// Disposes the periodic task
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        Stop();
        _timer?.Dispose();
        _cancellationTokenSource?.Dispose();
        _isDisposed = true;
    }
}