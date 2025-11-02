using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace CSharp.CircuitBreaker;

/// <summary>
/// Registry for managing multiple circuit breakers by name
/// </summary>
public class CircuitBreakerRegistry
{
    private readonly ConcurrentDictionary<string, CircuitBreaker> _circuitBreakers = new();
    private readonly ILoggerFactory? _loggerFactory;

    public CircuitBreakerRegistry(ILoggerFactory? loggerFactory = null)
    {
        _loggerFactory = loggerFactory;
    }

    /// <summary>
    /// Gets or creates a circuit breaker with the specified name and options
    /// </summary>
    public CircuitBreaker GetOrCreate(string name, CircuitBreakerOptions options)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(options);

        return _circuitBreakers.GetOrAdd(name, _ =>
        {
            var logger = _loggerFactory?.CreateLogger<CircuitBreaker>();
            return new CircuitBreaker(options, logger);
        });
    }

    /// <summary>
    /// Gets an existing circuit breaker by name
    /// </summary>
    public CircuitBreaker? Get(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _circuitBreakers.TryGetValue(name, out var circuitBreaker) ? circuitBreaker : null;
    }

    /// <summary>
    /// Removes a circuit breaker from the registry
    /// </summary>
    public bool Remove(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return _circuitBreakers.TryRemove(name, out _);
    }

    /// <summary>
    /// Gets all registered circuit breaker names
    /// </summary>
    public IEnumerable<string> GetNames() => _circuitBreakers.Keys;

    /// <summary>
    /// Gets all circuit breakers with their names
    /// </summary>
    public IEnumerable<(string Name, CircuitBreaker CircuitBreaker)> GetAll()
    {
        return _circuitBreakers.Select(kvp => (kvp.Key, kvp.Value));
    }

    /// <summary>
    /// Clears all circuit breakers from the registry
    /// </summary>
    public void Clear() => _circuitBreakers.Clear();

    /// <summary>
    /// Gets the count of registered circuit breakers
    /// </summary>
    public int Count => _circuitBreakers.Count;

    /// <summary>
    /// Resets all circuit breakers to closed state
    /// </summary>
    public void ResetAll()
    {
        foreach (var circuitBreaker in _circuitBreakers.Values)
        {
            circuitBreaker.Reset();
        }
    }

    /// <summary>
    /// Gets circuit breakers that are currently open
    /// </summary>
    public IEnumerable<(string Name, CircuitBreaker CircuitBreaker)> GetOpenCircuitBreakers()
    {
        return _circuitBreakers
            .Where(kvp => kvp.Value.State == CircuitBreakerState.Open)
            .Select(kvp => (kvp.Key, kvp.Value));
    }

    /// <summary>
    /// Gets circuit breakers that are currently half-open
    /// </summary>
    public IEnumerable<(string Name, CircuitBreaker CircuitBreaker)> GetHalfOpenCircuitBreakers()
    {
        return _circuitBreakers
            .Where(kvp => kvp.Value.State == CircuitBreakerState.HalfOpen)
            .Select(kvp => (kvp.Key, kvp.Value));
    }

    /// <summary>
    /// Gets health status of all circuit breakers
    /// </summary>
    public Dictionary<string, object> GetHealthStatus()
    {
        return _circuitBreakers.ToDictionary(
            kvp => kvp.Key,
            kvp => (object)new
            {
                State = kvp.Value.State.ToString(),
                Metrics = kvp.Value.Metrics.GetSnapshot()
            }
        );
    }
}
