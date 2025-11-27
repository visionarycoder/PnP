namespace CSharp.CircuitBreaker;

/// <summary>
/// Tracks circuit breaker metrics and statistics
/// </summary>
public class CircuitBreakerMetrics
{
    private readonly object _lockObj = new();
    private readonly Queue<DateTime> _recentCalls = new();
    private readonly Queue<DateTime> _recentFailures = new();
    
    public int TotalCalls { get; private set; }
    public int FailedCalls { get; private set; }
    public int SuccessfulCalls { get; private set; }
    public DateTime LastFailureTime { get; private set; }
    public DateTime LastSuccessTime { get; private set; }

    /// <summary>
    /// Records a new call attempt
    /// </summary>
    public void RecordCall()
    {
        lock (_lockObj)
        {
            TotalCalls++;
            _recentCalls.Enqueue(DateTime.UtcNow);
        }
    }

    /// <summary>
    /// Records a successful call
    /// </summary>
    public void RecordSuccess()
    {
        lock (_lockObj)
        {
            SuccessfulCalls++;
            LastSuccessTime = DateTime.UtcNow;
        }
    }

    /// <summary>
    /// Records a failed call
    /// </summary>
    public void RecordFailure()
    {
        lock (_lockObj)
        {
            FailedCalls++;
            LastFailureTime = DateTime.UtcNow;
            _recentFailures.Enqueue(DateTime.UtcNow);
        }
    }

    /// <summary>
    /// Gets recent call and failure statistics within a time window
    /// </summary>
    public (int calls, int failures) GetRecentStats(TimeSpan window)
    {
        lock (_lockObj)
        {
            var cutoff = DateTime.UtcNow - window;
            
            // Clean old entries
            while (_recentCalls.Count > 0 && _recentCalls.Peek() < cutoff)
                _recentCalls.Dequeue();
            
            while (_recentFailures.Count > 0 && _recentFailures.Peek() < cutoff)
                _recentFailures.Dequeue();

            return (_recentCalls.Count, _recentFailures.Count);
        }
    }

    /// <summary>
    /// Calculates failure rate within a time window
    /// </summary>
    public double GetFailureRate(TimeSpan window)
    {
        var (calls, failures) = GetRecentStats(window);
        return calls > 0 ? (double)failures / calls : 0;
    }

    /// <summary>
    /// Resets all metrics
    /// </summary>
    public void Reset()
    {
        lock (_lockObj)
        {
            TotalCalls = 0;
            FailedCalls = 0;
            SuccessfulCalls = 0;
            _recentCalls.Clear();
            _recentFailures.Clear();
        }
    }

    /// <summary>
    /// Gets a snapshot of current metrics
    /// </summary>
    public MetricsSnapshot GetSnapshot(TimeSpan? window = null)
    {
        lock (_lockObj)
        {
            var (recentCalls, recentFailures) = window.HasValue 
                ? GetRecentStats(window.Value) 
                : (TotalCalls, FailedCalls);

            return new MetricsSnapshot
            {
                TotalCalls = TotalCalls,
                SuccessfulCalls = SuccessfulCalls,
                FailedCalls = FailedCalls,
                RecentCalls = recentCalls,
                RecentFailures = recentFailures,
                FailureRate = recentCalls > 0 ? (double)recentFailures / recentCalls : 0,
                LastFailureTime = LastFailureTime,
                LastSuccessTime = LastSuccessTime
            };
        }
    }
}