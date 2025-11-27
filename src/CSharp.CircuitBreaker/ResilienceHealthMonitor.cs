using Microsoft.Extensions.Logging;

namespace CSharp.CircuitBreaker;

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