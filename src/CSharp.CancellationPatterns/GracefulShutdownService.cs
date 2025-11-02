namespace CSharp.CancellationPatterns;

/// <summary>
/// Provides graceful shutdown coordination for multiple services
/// </summary>
public class GracefulShutdownService : IDisposable
{
    private readonly List<Func<CancellationToken, Task>> _shutdownTasks = new();
    private readonly object _lock = new();
    private volatile bool _isShuttingDown;
    private volatile bool _isDisposed;

    /// <summary>
    /// Registers a task to be executed during shutdown
    /// </summary>
    public void RegisterShutdownTask(Func<CancellationToken, Task> shutdownTask)
    {
        ThrowIfDisposed();
        
        if (_isShuttingDown)
            throw new InvalidOperationException("Cannot register shutdown tasks while shutdown is in progress");

        lock (_lock)
        {
            _shutdownTasks.Add(shutdownTask ?? throw new ArgumentNullException(nameof(shutdownTask)));
        }
    }

    /// <summary>
    /// Registers multiple shutdown tasks
    /// </summary>
    public void RegisterShutdownTasks(params Func<CancellationToken, Task>[] shutdownTasks)
    {
        foreach (var task in shutdownTasks)
        {
            RegisterShutdownTask(task);
        }
    }

    /// <summary>
    /// Executes all registered shutdown tasks with a timeout
    /// </summary>
    public async Task ShutdownAsync(TimeSpan timeout = default)
    {
        ThrowIfDisposed();
        
        if (_isShuttingDown)
            return; // Shutdown already in progress

        _isShuttingDown = true;

        if (timeout == default)
            timeout = TimeSpan.FromSeconds(30);

        using var cts = new CancellationTokenSource(timeout);
        
        List<Func<CancellationToken, Task>> tasks;
        lock (_lock)
        {
            tasks = new List<Func<CancellationToken, Task>>(_shutdownTasks);
        }

        var shutdownTasks = tasks.Select(async task =>
        {
            try
            {
                await task(cts.Token);
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
            {
                // Timeout occurred during shutdown
                throw new TimeoutException($"Shutdown task timed out after {timeout}");
            }
            catch (Exception ex)
            {
                // Log shutdown task exception but don't let it fail the entire shutdown
                Console.WriteLine($"Shutdown task failed: {ex.Message}");
            }
        });

        try
        {
            await Task.WhenAll(shutdownTasks);
        }
        catch (TimeoutException)
        {
            Console.WriteLine($"Some shutdown tasks did not complete within {timeout}");
            // Continue with shutdown even if some tasks timed out
        }
    }

    /// <summary>
    /// Clears all registered shutdown tasks
    /// </summary>
    public void ClearShutdownTasks()
    {
        ThrowIfDisposed();
        
        if (_isShuttingDown)
            throw new InvalidOperationException("Cannot clear shutdown tasks while shutdown is in progress");

        lock (_lock)
        {
            _shutdownTasks.Clear();
        }
    }

    /// <summary>
    /// Gets the number of registered shutdown tasks
    /// </summary>
    public int ShutdownTaskCount
    {
        get
        {
            lock (_lock)
            {
                return _shutdownTasks.Count;
            }
        }
    }

    /// <summary>
    /// Indicates whether shutdown is currently in progress
    /// </summary>
    public bool IsShuttingDown => _isShuttingDown;

    /// <summary>
    /// Disposes the service and executes shutdown if not already done
    /// </summary>
    public void Dispose()
    {
        if (_isDisposed) return;

        if (!_isShuttingDown)
        {
            // Execute synchronous shutdown with a reasonable timeout
            try
            {
                ShutdownAsync(TimeSpan.FromSeconds(10)).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error during dispose shutdown: {ex.Message}");
            }
        }

        _isDisposed = true;
    }

    private void ThrowIfDisposed()
    {
        if (_isDisposed)
            throw new ObjectDisposedException(nameof(GracefulShutdownService));
    }
}