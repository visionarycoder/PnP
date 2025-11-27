using Microsoft.Extensions.Logging;

namespace CSharp.CircuitBreaker;

// Bulkhead isolation pattern
public class BulkheadIsolation : IDisposable
{
    private readonly SemaphoreSlim semaphore;
    private readonly string name;
    private readonly ILogger? logger;

    public BulkheadIsolation(string name, int maxConcurrency, ILogger? logger = null)
    {
        this.name = name ?? throw new ArgumentNullException(nameof(name));
        this.semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);
        this.logger = logger;
        
        MaxConcurrency = maxConcurrency;
    }

    public int MaxConcurrency { get; }
    public int CurrentCount => semaphore.CurrentCount;
    public int AvailableCount => MaxConcurrency - CurrentCount;

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        return await ExecuteAsync(operation, TimeSpan.FromMilliseconds(-1), cancellationToken).ConfigureAwait(false);
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var acquired = false;
        
        try
        {
            acquired = await semaphore.WaitAsync(timeout, cancellationToken).ConfigureAwait(false);
            
            if (!acquired)
            {
                throw new TimeoutException($"Bulkhead {name}: Unable to acquire slot within timeout {timeout}");
            }

            logger?.LogDebug("Bulkhead {Name}: Acquired slot. Available: {Available}/{Max}", 
                name, CurrentCount, MaxConcurrency);

            return await operation().ConfigureAwait(false);
        }
        finally
        {
            if (acquired)
            {
                semaphore.Release();
                
                logger?.LogDebug("Bulkhead {Name}: Released slot. Available: {Available}/{Max}", 
                    name, CurrentCount, MaxConcurrency);
            }
        }
    }

    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync<object>(async () =>
        {
            await operation().ConfigureAwait(false);
            return null!;
        }, cancellationToken).ConfigureAwait(false);
    }

    public void Dispose()
    {
        semaphore?.Dispose();
        GC.SuppressFinalize(this);
    }
}

// Retry policy with various strategies

// Timeout policy