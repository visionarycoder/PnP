namespace CSharp.CancellationPatterns;

/// <summary>
/// Extension methods for enhanced cancellation token functionality
/// </summary>
public static class CancellationExtensions
{
    /// <summary>
    /// Creates a cancellation token that cancels after a delay
    /// </summary>
    public static CancellationToken WithTimeout(this CancellationToken cancellationToken, TimeSpan timeout)
    {
        if (timeout == Timeout.InfiniteTimeSpan || timeout == TimeSpan.Zero)
            return cancellationToken;

        var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(timeout);
        return cts.Token;
    }

    /// <summary>
    /// Combines multiple cancellation tokens
    /// </summary>
    public static CancellationToken CombineWith(this CancellationToken token, params CancellationToken[] otherTokens)
    {
        if (otherTokens == null || otherTokens.Length == 0)
            return token;

        var allTokens = new[] { token }.Concat(otherTokens).ToArray();
        var cts = CancellationTokenSource.CreateLinkedTokenSource(allTokens);
        return cts.Token;
    }

    /// <summary>
    /// Checks if an OperationCanceledException was caused by a specific timeout token
    /// </summary>
    public static bool WasCancelledDueToTimeout(this OperationCanceledException ex, CancellationToken timeoutToken)
    {
        return timeoutToken.IsCancellationRequested;
    }

    /// <summary>
    /// Safely attempts to cancel a task after a timeout
    /// </summary>
    public static async Task<bool> TryCancelAfterAsync(this Task task, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        
        try
        {
            await task.WaitAsync(cts.Token);
            return true; // Completed before timeout
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            return false; // Timed out
        }
    }

    /// <summary>
    /// Registers a callback that executes when cancellation is requested
    /// </summary>
    public static CancellationTokenRegistration RegisterSafe(
        this CancellationToken cancellationToken, 
        Action callback)
    {
        return cancellationToken.Register(() =>
        {
            try
            {
                callback();
            }
            catch (Exception ex)
            {
                // Log exception but don't let it escape
                Console.WriteLine($"Cancellation callback error: {ex.Message}");
            }
        });
    }

    /// <summary>
    /// Creates a cancellation token that cancels when any of the provided tokens cancel
    /// </summary>
    public static CancellationToken WhenAny(params CancellationToken[] tokens)
    {
        if (tokens == null || tokens.Length == 0)
            return CancellationToken.None;

        if (tokens.Length == 1)
            return tokens[0];

        return CancellationTokenSource.CreateLinkedTokenSource(tokens).Token;
    }

    /// <summary>
    /// Creates a delay that can be cancelled
    /// </summary>
    public static Task Delay(this CancellationToken cancellationToken, TimeSpan delay)
    {
        return Task.Delay(delay, cancellationToken);
    }

    /// <summary>
    /// Executes an action if the cancellation token is not cancelled
    /// </summary>
    public static void ExecuteIfNotCancelled(this CancellationToken cancellationToken, Action action)
    {
        if (!cancellationToken.IsCancellationRequested)
        {
            action();
        }
    }

    /// <summary>
    /// Executes an async function if the cancellation token is not cancelled
    /// </summary>
    public static async Task<T?> ExecuteIfNotCancelledAsync<T>(
        this CancellationToken cancellationToken, 
        Func<Task<T>> asyncFunc)
    {
        if (cancellationToken.IsCancellationRequested)
            return default(T);

        return await asyncFunc();
    }

    /// <summary>
    /// Creates a progress reporter that respects cancellation
    /// </summary>
    public static IProgress<T> CreateCancellableProgress<T>(
        this CancellationToken cancellationToken,
        Action<T> handler)
    {
        return new CancellableProgress<T>(handler, cancellationToken);
    }

    /// <summary>
    /// Waits for cancellation with a timeout
    /// </summary>
    public static async Task<bool> WaitForCancellationAsync(
        this CancellationToken cancellationToken, 
        TimeSpan timeout)
    {
        try
        {
            await Task.Delay(timeout, cancellationToken);
            return false; // Timeout occurred
        }
        catch (OperationCanceledException)
        {
            return true; // Cancellation occurred
        }
    }
}

/// <summary>
/// Progress reporter that respects cancellation tokens
/// </summary>
public class CancellableProgress<T> : IProgress<T>
{
    private readonly Action<T> _handler;
    private readonly CancellationToken _cancellationToken;
    private readonly SynchronizationContext? _synchronizationContext;

    /// <summary>
    /// Creates a new cancellable progress reporter
    /// </summary>
    public CancellableProgress(Action<T> handler, CancellationToken cancellationToken = default)
    {
        _handler = handler ?? throw new ArgumentNullException(nameof(handler));
        _cancellationToken = cancellationToken;
        _synchronizationContext = SynchronizationContext.Current;
    }

    /// <summary>
    /// Reports progress if not cancelled
    /// </summary>
    public void Report(T value)
    {
        if (_cancellationToken.IsCancellationRequested)
            return;

        if (_synchronizationContext != null)
        {
            _synchronizationContext.Post(_ =>
            {
                if (!_cancellationToken.IsCancellationRequested)
                    _handler(value);
            }, null);
        }
        else
        {
            _handler(value);
        }
    }
}

/// <summary>
/// Utility class for creating and managing cancellation tokens
/// </summary>
public static class CancellationTokenFactory
{
    /// <summary>
    /// Creates a cancellation token that cancels after the specified timeout
    /// </summary>
    public static (CancellationToken Token, CancellationTokenSource Source) CreateWithTimeout(TimeSpan timeout)
    {
        var cts = new CancellationTokenSource(timeout);
        return (cts.Token, cts);
    }

    /// <summary>
    /// Creates a cancellation token that combines multiple sources
    /// </summary>
    public static (CancellationToken Token, CancellationTokenSource Source) CreateCombined(
        params CancellationToken[] tokens)
    {
        var cts = CancellationTokenSource.CreateLinkedTokenSource(tokens);
        return (cts.Token, cts);
    }

    /// <summary>
    /// Creates a cancellation token with both timeout and external cancellation
    /// </summary>
    public static (CancellationToken Token, CancellationTokenSource Source) CreateWithTimeoutAndCancellation(
        TimeSpan timeout, 
        CancellationToken externalToken = default)
    {
        var timeoutCts = new CancellationTokenSource(timeout);
        
        if (externalToken == default || externalToken == CancellationToken.None)
        {
            return (timeoutCts.Token, timeoutCts);
        }

        var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken, timeoutCts.Token);
        timeoutCts.Dispose(); // The combined source will manage the timeout
        
        return (combinedCts.Token, combinedCts);
    }
}