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
public enum RetryStrategy
{
    FixedInterval,
    ExponentialBackoff,
    LinearBackoff,
    Jitter
}

public class RetryOptions
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(100);
    public RetryStrategy Strategy { get; set; } = RetryStrategy.ExponentialBackoff;
    public double BackoffMultiplier { get; set; } = 2.0;
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
    public Func<Exception, bool> RetryPredicate { get; set; } = ex => true;
    public bool UseJitter { get; set; } = true;
}

public class RetryPolicy
{
    private readonly RetryOptions options;
    private readonly ILogger? logger;
    private readonly Random jitterRandom = new();

    public RetryPolicy(RetryOptions options, ILogger? logger = null)
    {
        this.options = options ?? throw new ArgumentNullException(nameof(options));
        this.logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= options.MaxRetries)
        {
            try
            {
                var result = await operation().ConfigureAwait(false);
                
                if (attempt > 0)
                {
                    logger?.LogInformation("Retry succeeded on attempt {Attempt}", attempt + 1);
                }
                
                return result;
            }
            catch (Exception ex) when (options.RetryPredicate(ex))
            {
                lastException = ex;
                attempt++;

                if (attempt > options.MaxRetries)
                {
                    logger?.LogError(ex, "All {MaxRetries} retry attempts failed", options.MaxRetries);
                    break;
                }

                var delay = CalculateDelay(attempt);
                
                logger?.LogWarning(ex, "Operation failed on attempt {Attempt}. Retrying in {Delay}ms", 
                    attempt, delay.TotalMilliseconds);

                await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
            }
        }

        throw lastException ?? new InvalidOperationException("Retry failed without exception");
    }

    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await operation().ConfigureAwait(false);
            return Task.CompletedTask;
        }, cancellationToken).ConfigureAwait(false);
    }

    private TimeSpan CalculateDelay(int attempt)
    {
        TimeSpan delay = options.Strategy switch
        {
            RetryStrategy.FixedInterval => options.BaseDelay,
            RetryStrategy.LinearBackoff => TimeSpan.FromMilliseconds(options.BaseDelay.TotalMilliseconds * attempt),
            RetryStrategy.ExponentialBackoff => TimeSpan.FromMilliseconds(
                options.BaseDelay.TotalMilliseconds * Math.Pow(options.BackoffMultiplier, attempt - 1)),
            RetryStrategy.Jitter => CalculateJitteredDelay(attempt),
            _ => options.BaseDelay
        };

        // Apply max delay cap
        if (delay > options.MaxDelay)
            delay = options.MaxDelay;

        // Apply jitter if enabled
        if (options.UseJitter && options.Strategy != RetryStrategy.Jitter)
        {
            var jitterRange = delay.TotalMilliseconds * 0.1; // 10% jitter
            var jitter = (jitterRandom.NextDouble() - 0.5) * 2 * jitterRange;
            delay = TimeSpan.FromMilliseconds(Math.Max(0, delay.TotalMilliseconds + jitter));
        }

        return delay;
    }

    private TimeSpan CalculateJitteredDelay(int attempt)
    {
        // Full jitter strategy
        var exponentialDelay = options.BaseDelay.TotalMilliseconds * Math.Pow(options.BackoffMultiplier, attempt - 1);
        var jitteredDelay = jitterRandom.NextDouble() * exponentialDelay;
        return TimeSpan.FromMilliseconds(jitteredDelay);
    }
}

// Timeout policy
public class TimeoutPolicy
{
    private readonly TimeSpan timeout;
    private readonly ILogger? logger;

    public TimeoutPolicy(TimeSpan timeout, ILogger? logger = null)
    {
        this.timeout = timeout;
        this.logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            logger?.LogWarning("Operation timed out after {Timeout}", timeout);
            throw new TimeoutException($"Operation timed out after {timeout}");
        }
    }

    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await operation().ConfigureAwait(false);
            return Task.CompletedTask;
        }, cancellationToken).ConfigureAwait(false);
    }
}