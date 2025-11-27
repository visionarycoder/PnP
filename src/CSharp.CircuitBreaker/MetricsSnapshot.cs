namespace CSharp.CircuitBreaker;

/// <summary>
/// Snapshot of circuit breaker metrics at a point in time
/// </summary>
public record MetricsSnapshot
{
    public int TotalCalls { get; init; }
    public int SuccessfulCalls { get; init; }
    public int FailedCalls { get; init; }
    public int RecentCalls { get; init; }
    public int RecentFailures { get; init; }
    public double FailureRate { get; init; }
    public DateTime LastFailureTime { get; init; }
    public DateTime LastSuccessTime { get; init; }
}