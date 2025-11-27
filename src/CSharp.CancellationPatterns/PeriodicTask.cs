namespace CSharp.CancellationPatterns;

/// <summary>
/// Provides periodic task execution with cancellation support
/// </summary>
public class PeriodicTask : IDisposable
{
    private readonly Timer _timer;
    private readonly Func<CancellationToken, Task> _action;
    private readonly CancellationTokenSource _cancellationTokenSource;
    private volatile bool _isExecuting;
    private volatile bool _isDisposed;

    /// <summary>
    /// Creates a new periodic task
    /// </summary>
    public PeriodicTask(
        Func<CancellationToken, Task> action,
        TimeSpan interval,
        TimeSpan? initialDelay = null)
    {
        _action = action ?? throw new ArgumentNullException(nameof(action));
        _cancellationTokenSource = new CancellationTokenSource();
        
        var delay = initialDelay ?? interval;
        _timer = new Timer(async _ => await ExecuteAsync(), null, delay, interval);
    }

    /// <summary>
    /// Indicates if the task is currently executing
    /// </summary>
    public bool IsExecuting => _isExecuting;

    /// <summary>
    /// Indicates if cancellation was requested
    /// </summary>
    public bool IsCancellationRequested => _cancellationTokenSource.Token.IsCancellationRequested;

    private async Task ExecuteAsync()
    {
        if (_isExecuting || _cancellationTokenSource.Token.IsCancellationRequested || _isDisposed)
            return;

        _isExecuting = true;
        try
        {
            await _action(_cancellationTokenSource.Token);
        }
        catch (OperationCanceledException) when (_cancellationTokenSource.Token.IsCancellationRequested)
        {
            // Expected cancellation
        }
        catch (Exception ex)
        {
            OnException(ex);
        }
        finally
        {
            _isExecuting = false;
        }
    }

    /// <summary>
    /// Called when an exception occurs during task execution
    /// </summary>
    protected virtual void OnException(Exception exception)
    {
        // Default implementation - log to console
        Console.WriteLine($"Periodic task error: {exception.Message}");
    }

    /// <summary>
    /// Stops the periodic task
    /// </summary>
    public void Stop()
    {
        if (_isDisposed) return;

        _cancellationTokenSource.Cancel();
        _timer.Change(Timeout.Infinite, Timeout.Infinite);
    }

    /// <summary>
    /// Disposes the periodic task
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        Stop();
        _timer?.Dispose();
        _cancellationTokenSource?.Dispose();
        _isDisposed = true;
    }
}