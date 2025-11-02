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
public class ResilienceHealthMonitor : IDisposable
{
    private readonly CircuitBreakerRegistry registry;
    private readonly ILogger? logger;
    private readonly Timer healthCheckTimer;

    public ResilienceHealthMonitor(CircuitBreakerRegistry registry, ILogger? logger = null)
    {
        this.registry = registry;
        this.logger = logger;
        
        // Check health every 30 seconds
        this.healthCheckTimer = new Timer(CheckHealth, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
    }

    private void CheckHealth(object? state)
    {
        try
        {
            var allBreakers = registry.GetAll().ToList();
            var openBreakers = allBreakers.Where(cb => cb.CircuitBreaker.State == CircuitBreakerState.Open).ToList();
            var halfOpenBreakers = allBreakers.Where(cb => cb.CircuitBreaker.State == CircuitBreakerState.HalfOpen).ToList();

            if (openBreakers.Count != 0)
            {
                logger?.LogWarning("Health Check: {Count} circuit breakers are OPEN: {Names}", 
                    openBreakers.Count, string.Join(", ", openBreakers.Select(cb => cb.Name)));
            }

            if (halfOpenBreakers.Count != 0)
            {
                logger?.LogInformation("Health Check: {Count} circuit breakers are HALF-OPEN: {Names}", 
                    halfOpenBreakers.Count, string.Join(", ", halfOpenBreakers.Select(cb => cb.Name)));
            }

            // Log metrics for each circuit breaker
            foreach (var (name, circuitBreaker) in allBreakers)
            {
                var metrics = circuitBreaker.Metrics;
                logger?.LogDebug("Circuit Breaker {Name}: State={State}, Total={Total}, Success={Success}, Failed={Failed}", 
                    name, circuitBreaker.State, metrics.TotalCalls, metrics.SuccessfulCalls, metrics.FailedCalls);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error during resilience health check");
        }
    }

    public void Dispose()
    {
        healthCheckTimer?.Dispose();
        GC.SuppressFinalize(this);
    }
}