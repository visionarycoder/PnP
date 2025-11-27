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