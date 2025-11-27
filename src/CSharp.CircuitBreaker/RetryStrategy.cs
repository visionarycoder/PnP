namespace CSharp.CircuitBreaker;

public enum RetryStrategy
{
    FixedInterval,
    ExponentialBackoff,
    LinearBackoff,
    Jitter
}