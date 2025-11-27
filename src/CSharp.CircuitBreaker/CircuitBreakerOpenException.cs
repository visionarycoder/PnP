namespace CSharp.CircuitBreaker;

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