using Microsoft.Extensions.Logging;

namespace CSharp.CancellationPatterns;

/// <summary>
/// A resilient background service that automatically restarts on exceptions
/// </summary>
public abstract class ResilientBackgroundService : CancellableBackgroundService
{
    private readonly int _maxConsecutiveFailures;
    private int _consecutiveFailures;

    protected ResilientBackgroundService(
        ILogger? logger = null, 
        int maxConsecutiveFailures = 5) 
        : base(logger)
    {
        _maxConsecutiveFailures = maxConsecutiveFailures;
    }

    protected sealed override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ExecuteResilientAsync(stoppingToken);
                _consecutiveFailures = 0; // Reset failure count on success
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Expected cancellation
                break;
            }
            catch (Exception ex)
            {
                _consecutiveFailures++;
                OnExecutionException(ex);

                if (_consecutiveFailures >= _maxConsecutiveFailures)
                {
                    OnMaxConsecutiveFailuresReached(ex);
                    break;
                }

                if (!ShouldContinueOnException(ex))
                {
                    break;
                }

                try
                {
                    var delay = GetExceptionDelay(ex, _consecutiveFailures);
                    await Task.Delay(delay, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Executes the resilient background work - must be implemented by derived classes
    /// </summary>
    protected abstract Task ExecuteResilientAsync(CancellationToken stoppingToken);

    /// <summary>
    /// Called when the maximum number of consecutive failures is reached
    /// </summary>
    protected virtual void OnMaxConsecutiveFailuresReached(Exception lastException)
    {
        // Override to implement custom behavior
    }
}