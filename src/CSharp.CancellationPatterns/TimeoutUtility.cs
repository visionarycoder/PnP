namespace CSharp.CancellationPatterns;

/// <summary>
/// Provides timeout utilities for tasks and operations with cancellation support
/// </summary>
public static class TimeoutUtility
{
    /// <summary>
    /// Adds a timeout to a task with custom timeout exception
    /// </summary>
    public static async Task<T> WithTimeoutAsync<T>(
        this Task<T> task,
        TimeSpan timeout,
        CancellationToken cancellationToken = default,
        string? timeoutMessage = null)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        try
        {
            return await task.WaitAsync(combinedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            var message = timeoutMessage ?? $"Operation timed out after {timeout}";
            throw new TimeoutException(message);
        }
    }

    /// <summary>
    /// Adds a timeout to a void task
    /// </summary>
    public static async Task WithTimeoutAsync(
        this Task task,
        TimeSpan timeout,
        CancellationToken cancellationToken = default,
        string? timeoutMessage = null)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        try
        {
            await task.WaitAsync(combinedCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            var message = timeoutMessage ?? $"Operation timed out after {timeout}";
            throw new TimeoutException(message);
        }
    }

    /// <summary>
    /// Tries to complete a task within the timeout, returning success/failure
    /// </summary>
    public static async Task<(bool Success, T? Result)> TryWithTimeoutAsync<T>(
        this Task<T> task,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        try
        {
            var result = await task.WaitAsync(combinedCts.Token);
            return (true, result);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            return (false, default(T));
        }
    }

    /// <summary>
    /// Tries to complete a void task within the timeout
    /// </summary>
    public static async Task<bool> TryWithTimeoutAsync(
        this Task task,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, timeoutCts.Token);

        try
        {
            await task.WaitAsync(combinedCts.Token);
            return true;
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            return false;
        }
    }

    /// <summary>
    /// Creates a task that completes when any of the provided tasks complete or timeout occurs
    /// </summary>
    public static async Task<T> WhenAnyWithTimeoutAsync<T>(
        TimeSpan timeout,
        params Task<T>[] tasks)
    {
        if (tasks == null || tasks.Length == 0)
            throw new ArgumentException("At least one task must be provided", nameof(tasks));

        using var timeoutCts = new CancellationTokenSource(timeout);
        var timeoutTask = Task.Delay(timeout, timeoutCts.Token);

        var allTasks = tasks.Cast<Task>().Append(timeoutTask).ToArray();
        var completedTask = await Task.WhenAny(allTasks);

        if (completedTask == timeoutTask)
        {
            throw new TimeoutException($"No task completed within {timeout}");
        }

        timeoutCts.Cancel(); // Cancel the timeout task
        return await (Task<T>)completedTask;
    }

    /// <summary>
    /// Creates a task that completes when all tasks complete or timeout occurs
    /// </summary>
    public static async Task<T[]> WhenAllWithTimeoutAsync<T>(
        TimeSpan timeout,
        params Task<T>[] tasks)
    {
        if (tasks == null || tasks.Length == 0)
            return Array.Empty<T>();

        var allTask = Task.WhenAll(tasks);
        
        try
        {
            return await allTask.WithTimeoutAsync(timeout);
        }
        catch (TimeoutException)
        {
            // Cancel individual tasks that are still running
            foreach (var task in tasks.Where(t => !t.IsCompleted))
            {
                // Note: We can't directly cancel tasks, but this gives them a chance to observe cancellation
                // if they're implemented with cancellation support
            }
            throw;
        }
    }

    /// <summary>
    /// Executes an operation with a timeout and returns default value on timeout
    /// </summary>
    public static async Task<T> WithTimeoutOrDefaultAsync<T>(
        this Task<T> task,
        TimeSpan timeout,
        T defaultValue = default(T)!,
        CancellationToken cancellationToken = default)
    {
        var (success, result) = await task.TryWithTimeoutAsync(timeout, cancellationToken);
        return success ? result! : defaultValue;
    }

    /// <summary>
    /// Executes multiple operations with individual timeouts
    /// </summary>
    public static async Task<T[]> ExecuteWithIndividualTimeoutsAsync<T>(
        IEnumerable<Func<CancellationToken, Task<T>>> operations,
        TimeSpan individualTimeout,
        CancellationToken cancellationToken = default)
    {
        var tasks = operations.Select(async operation =>
        {
            using var timeoutCts = new CancellationTokenSource(individualTimeout);
            using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                cancellationToken, timeoutCts.Token);

            return await operation(combinedCts.Token);
        });

        return await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Executes an operation with progressive timeout (timeout increases with retries)
    /// </summary>
    public static async Task<T> WithProgressiveTimeoutAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        TimeSpan baseTimeout,
        int maxAttempts = 3,
        double timeoutMultiplier = 1.5,
        CancellationToken cancellationToken = default)
    {
        var currentTimeout = baseTimeout;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                using var timeoutCts = new CancellationTokenSource(currentTimeout);
                using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    cancellationToken, timeoutCts.Token);

                return await operation(combinedCts.Token);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                // External cancellation - don't retry
                throw;
            }
            catch (TimeoutException) when (attempt < maxAttempts - 1)
            {
                // Increase timeout for next attempt
                currentTimeout = TimeSpan.FromTicks((long)(currentTimeout.Ticks * timeoutMultiplier));
                continue;
            }
            // Last attempt or other exception - let it bubble up
        }

        // This should never be reached due to the logic above
        throw new InvalidOperationException("Progressive timeout logic error");
    }
}