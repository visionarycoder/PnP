namespace CSharp.CancellationPatterns;

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