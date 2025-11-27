using Microsoft.Extensions.Logging;

namespace CSharp.CircuitBreaker;

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