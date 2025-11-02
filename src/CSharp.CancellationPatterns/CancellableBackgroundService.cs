using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CSharp.CancellationPatterns;

/// <summary>
/// Abstract base class for background services with cancellation support
/// </summary>
public abstract class CancellableBackgroundService : BackgroundService, IDisposable
{
    private readonly ILogger? _logger;
    protected CancellationTokenSource? _stoppingCts;

    protected CancellableBackgroundService(ILogger? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Starts the background service
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _stoppingCts = new CancellationTokenSource();
        
        _logger?.LogInformation("Starting {ServiceName}", GetType().Name);
        
        // Start the background execution
        _ = Task.Run(() => ExecuteAsync(_stoppingCts.Token), cancellationToken);
        
        await Task.CompletedTask;
    }

    /// <summary>
    /// Stops the background service gracefully
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        if (_stoppingCts == null) return;

        _logger?.LogInformation("Stopping {ServiceName}", GetType().Name);
        
        try
        {
            _stoppingCts.Cancel();
            
            // Wait for the service to stop with a timeout
            using var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);
            
            // Give the service time to stop gracefully
            await Task.Delay(100, combinedCts.Token);
        }
        catch (OperationCanceledException)
        {
            _logger?.LogWarning("Stop operation was cancelled for {ServiceName}", GetType().Name);
        }
        finally
        {
            _logger?.LogInformation("Stopped {ServiceName}", GetType().Name);
        }
    }

    /// <summary>
    /// Executes the background work - must be implemented by derived classes
    /// </summary>
    protected abstract override Task ExecuteAsync(CancellationToken stoppingToken);

    /// <summary>
    /// Called when the service encounters an unhandled exception
    /// </summary>
    protected virtual void OnExecutionException(Exception exception)
    {
        _logger?.LogError(exception, "Unhandled exception in {ServiceName}", GetType().Name);
    }

    /// <summary>
    /// Determines if the service should continue after an exception
    /// </summary>
    protected virtual bool ShouldContinueOnException(Exception exception)
    {
        return !(exception is OutOfMemoryException || exception is StackOverflowException);
    }

    /// <summary>
    /// Gets the delay before restarting after an exception
    /// </summary>
    protected virtual TimeSpan GetExceptionDelay(Exception exception, int attemptCount)
    {
        // Exponential backoff with jitter
        var baseDelay = TimeSpan.FromSeconds(Math.Min(5 * Math.Pow(2, attemptCount), 300));
        var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));
        return baseDelay + jitter;
    }

    /// <summary>
    /// Disposes the service
    /// </summary>
    public override void Dispose()
    {
        _stoppingCts?.Cancel();
        _stoppingCts?.Dispose();
        base.Dispose();
    }
}

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