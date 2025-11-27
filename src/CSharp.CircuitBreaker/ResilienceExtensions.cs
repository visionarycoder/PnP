using Microsoft.Extensions.Logging;

namespace CSharp.CircuitBreaker;

// Predefined resilience policies for common scenarios
public static class ResiliencePolicies
{
    public static CircuitBreakerOptions WebServiceDefaults => new()
    {
        FailureThreshold = 5,
        OpenTimeout = TimeSpan.FromSeconds(30),
        HalfOpenMaxCalls = 3,
        SamplingDuration = TimeSpan.FromMinutes(1),
        FailureRateThreshold = 0.5,
        MinimumThroughput = 10
    };

    public static CircuitBreakerOptions DatabaseDefaults => new()
    {
        FailureThreshold = 3,
        OpenTimeout = TimeSpan.FromSeconds(60),
        HalfOpenMaxCalls = 2,
        SamplingDuration = TimeSpan.FromMinutes(2),
        FailureRateThreshold = 0.3,
        MinimumThroughput = 5
    };

    public static RetryOptions ExponentialBackoffDefaults => new()
    {
        MaxRetries = 3,
        BaseDelay = TimeSpan.FromMilliseconds(100),
        Strategy = RetryStrategy.ExponentialBackoff,
        BackoffMultiplier = 2.0,
        MaxDelay = TimeSpan.FromSeconds(10),
        UseJitter = true
    };

    public static RetryOptions QuickRetryDefaults => new()
    {
        MaxRetries = 2,
        BaseDelay = TimeSpan.FromMilliseconds(50),
        Strategy = RetryStrategy.FixedInterval,
        MaxDelay = TimeSpan.FromSeconds(1),
        UseJitter = false
    };
}

// Extension methods for easier usage
public static class ResilienceExtensions
{
    public static CircuitBreaker CreateCircuitBreaker(this CircuitBreakerRegistry registry, string name)
    {
        return registry.GetOrCreate(name, new CircuitBreakerOptions());
    }

    public static async Task<T> WithCircuitBreaker<T>(this Task<T> task, CircuitBreaker circuitBreaker)
    {
        return await circuitBreaker.ExecuteAsync(() => task).ConfigureAwait(false);
    }

    public static async Task WithCircuitBreaker(this Task task, CircuitBreaker circuitBreaker)
    {
        await circuitBreaker.ExecuteAsync(() => task).ConfigureAwait(false);
    }

    public static async Task<T> WithRetry<T>(this Func<Task<T>> operation, RetryOptions options, ILogger? logger = null)
    {
        var retryPolicy = new RetryPolicy(options, logger);
        return await retryPolicy.ExecuteAsync(operation).ConfigureAwait(false);
    }

    public static async Task<T> WithTimeout<T>(this Func<Task<T>> operation, TimeSpan timeout, ILogger? logger = null)
    {
        var timeoutPolicy = new TimeoutPolicy(timeout, logger);
        return await timeoutPolicy.ExecuteAsync(operation).ConfigureAwait(false);
    }

    public static async Task<T> WithBulkhead<T>(this Func<Task<T>> operation, BulkheadIsolation bulkhead)
    {
        return await bulkhead.ExecuteAsync(operation).ConfigureAwait(false);
    }
}

// Health monitoring and diagnostics