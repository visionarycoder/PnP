namespace CSharp.CircuitBreaker;

/// <summary>
/// Circuit breaker states for managing service fault tolerance
/// </summary>
public enum CircuitBreakerState
{
    /// <summary>Normal operation - requests flow through</summary>
    Closed,
    /// <summary>Failures detected - requests fail fast</summary>
    Open,
    /// <summary>Testing if service has recovered</summary>
    HalfOpen
}

/// <summary>
/// Configuration options for circuit breaker behavior
/// </summary>
public class CircuitBreakerOptions
{
    /// <summary>Number of failures before opening the circuit</summary>
    public int FailureThreshold { get; set; } = 5;
    
    /// <summary>Duration to keep the circuit open before trying again</summary>
    public TimeSpan OpenTimeout { get; set; } = TimeSpan.FromSeconds(30);
    
    /// <summary>Maximum number of calls allowed in half-open state</summary>
    public int HalfOpenMaxCalls { get; set; } = 3;
    
    /// <summary>Time window for sampling failure rates</summary>
    public TimeSpan SamplingDuration { get; set; } = TimeSpan.FromSeconds(60);
    
    /// <summary>Failure rate threshold (0.0 to 1.0) to open the circuit</summary>
    public double FailureRateThreshold { get; set; } = 0.5;
    
    /// <summary>Minimum number of calls before considering failure rate</summary>
    public int MinimumThroughput { get; set; } = 10;
}

/// <summary>
/// Exception thrown when circuit breaker is open
/// </summary>
public class CircuitBreakerOpenException : Exception
{
    public CircuitBreakerState State { get; }
    public TimeSpan RetryAfter { get; }

    public CircuitBreakerOpenException(CircuitBreakerState state, TimeSpan retryAfter)
        : base($"Circuit breaker is {state}. Retry after {retryAfter}")
    {
        State = state;
        RetryAfter = retryAfter;
    }
}