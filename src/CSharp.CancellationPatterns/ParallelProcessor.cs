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