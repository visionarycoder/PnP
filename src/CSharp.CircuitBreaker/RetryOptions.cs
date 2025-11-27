namespace CSharp.CircuitBreaker;

public class RetryOptions
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan BaseDelay { get; set; } = TimeSpan.FromMilliseconds(100);
    public RetryStrategy Strategy { get; set; } = RetryStrategy.ExponentialBackoff;
    public double BackoffMultiplier { get; set; } = 2.0;
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
    public Func<Exception, bool> RetryPredicate { get; set; } = ex => true;
    public bool UseJitter { get; set; } = true;
}