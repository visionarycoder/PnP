namespace CSharp.ExceptionHandling;

/// <summary>
/// Options for configuring error boundary behavior.
/// </summary>
public class ErrorBoundaryOptions
{
    public bool LogErrors { get; set; } = true;
    public bool RetryOnTransientErrors { get; set; } = false;
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(1000);
    public Func<Exception, bool> IsTransientError { get; set; } = _ => false;
    public Func<Exception, Exception> TransformException { get; set; } = ex => ex;
}