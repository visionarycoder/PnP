namespace CSharp.CancellationPatterns;

/// <summary>
/// Provides retry functionality with exponential backoff and cancellation support
/// </summary>
public static class RetryWithCancellation
{
    /// <summary>
    /// Executes an async function with exponential backoff retry policy
    /// </summary>
    public static async Task<T> ExecuteWithExponentialBackoffAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        int maxRetries = 3,
        TimeSpan? initialDelay = null,
        double backoffMultiplier = 2.0,
        TimeSpan? maxDelay = null,
        Func<Exception, bool>? shouldRetry = null,
        CancellationToken cancellationToken = default)
    {
        initialDelay ??= TimeSpan.FromSeconds(1);
        maxDelay ??= TimeSpan.FromMinutes(1);
        shouldRetry ??= DefaultShouldRetry;

        var attempt = 0;
        var delay = initialDelay.Value;

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await operation(cancellationToken);
            }
            catch (Exception ex) when (attempt < maxRetries && shouldRetry(ex))
            {
                attempt++;
                
                if (attempt >= maxRetries)
                    throw;

                // Add jitter to prevent thundering herd
                var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, (int)delay.TotalMilliseconds / 10));
                var actualDelay = delay + jitter;
                
                await Task.Delay(actualDelay, cancellationToken);
                
                // Calculate next delay with backoff
                delay = TimeSpan.FromTicks((long)(delay.Ticks * backoffMultiplier));
                if (delay > maxDelay.Value)
                    delay = maxDelay.Value;
            }
        }
    }

    /// <summary>
    /// Executes an async action with exponential backoff retry policy
    /// </summary>
    public static async Task ExecuteWithExponentialBackoffAsync(
        Func<CancellationToken, Task> operation,
        int maxRetries = 3,
        TimeSpan? initialDelay = null,
        double backoffMultiplier = 2.0,
        TimeSpan? maxDelay = null,
        Func<Exception, bool>? shouldRetry = null,
        CancellationToken cancellationToken = default)
    {
        await ExecuteWithExponentialBackoffAsync(
            async token =>
            {
                await operation(token);
                return true; // Dummy return value
            },
            maxRetries,
            initialDelay,
            backoffMultiplier,
            maxDelay,
            shouldRetry,
            cancellationToken);
    }

    /// <summary>
    /// Executes an operation with linear retry policy
    /// </summary>
    public static async Task<T> ExecuteWithLinearBackoffAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        int maxRetries = 3,
        TimeSpan? retryDelay = null,
        Func<Exception, bool>? shouldRetry = null,
        CancellationToken cancellationToken = default)
    {
        retryDelay ??= TimeSpan.FromSeconds(1);
        shouldRetry ??= DefaultShouldRetry;

        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await operation(cancellationToken);
            }
            catch (Exception ex) when (attempt < maxRetries && shouldRetry(ex))
            {
                // Add small jitter
                var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 100));
                await Task.Delay(retryDelay.Value + jitter, cancellationToken);
            }
        }

        // This should never be reached due to the exception handling above
        throw new InvalidOperationException("Retry logic error");
    }

    /// <summary>
    /// Executes an operation with custom retry delays
    /// </summary>
    public static async Task<T> ExecuteWithCustomDelaysAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        TimeSpan[] retryDelays,
        Func<Exception, bool>? shouldRetry = null,
        CancellationToken cancellationToken = default)
    {
        shouldRetry ??= DefaultShouldRetry;

        for (int attempt = 0; attempt <= retryDelays.Length; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                return await operation(cancellationToken);
            }
            catch (Exception ex) when (attempt < retryDelays.Length && shouldRetry(ex))
            {
                await Task.Delay(retryDelays[attempt], cancellationToken);
            }
        }

        // This should never be reached due to the exception handling above
        throw new InvalidOperationException("Retry logic error");
    }

    /// <summary>
    /// Default retry policy - retries on transient exceptions
    /// </summary>
    public static bool DefaultShouldRetry(Exception exception)
    {
        return exception switch
        {
            OperationCanceledException => false, // Never retry cancelled operations
            ArgumentException => false,          // Never retry argument errors
            ArgumentNullException => false,      // Never retry null argument errors
            TimeoutException => true,            // Retry timeouts
            HttpRequestException => true,        // Retry HTTP errors
            TaskCanceledException => true,       // Retry task cancellations (often timeouts)
            _ => true                           // Retry other exceptions by default
        };
    }

    /// <summary>
    /// Conservative retry policy - only retries known transient exceptions
    /// </summary>
    public static bool ConservativeRetryPolicy(Exception exception)
    {
        return exception switch
        {
            TimeoutException => true,
            HttpRequestException httpEx when IsTransientHttpError(httpEx) => true,
            TaskCanceledException => true,
            _ => false
        };
    }

    /// <summary>
    /// Aggressive retry policy - retries all exceptions except critical ones
    /// </summary>
    public static bool AggressiveRetryPolicy(Exception exception)
    {
        return exception switch
        {
            OutOfMemoryException => false,
            StackOverflowException => false,
            AccessViolationException => false,
            ArgumentException => false,
            ArgumentNullException => false,
            OperationCanceledException => false,
            _ => true
        };
    }

    private static bool IsTransientHttpError(HttpRequestException httpEx)
    {
        var message = httpEx.Message.ToLowerInvariant();
        return message.Contains("timeout") || 
               message.Contains("connection") || 
               message.Contains("network") ||
               message.Contains("502") ||
               message.Contains("503") ||
               message.Contains("504");
    }
}