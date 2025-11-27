using Microsoft.Extensions.Logging;

namespace CSharp.CircuitBreaker;

public class TimeoutPolicy
{
    private readonly TimeSpan timeout;
    private readonly ILogger? logger;

    public TimeoutPolicy(TimeSpan timeout, ILogger? logger = null)
    {
        this.timeout = timeout;
        this.logger = logger;
    }

    public async Task<T> ExecuteAsync<T>(Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        using var timeoutCts = new CancellationTokenSource(timeout);
        using var combinedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            return await operation().ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            logger?.LogWarning("Operation timed out after {Timeout}", timeout);
            throw new TimeoutException($"Operation timed out after {timeout}");
        }
    }

    public async Task ExecuteAsync(Func<Task> operation, CancellationToken cancellationToken = default)
    {
        await ExecuteAsync(async () =>
        {
            await operation().ConfigureAwait(false);
            return Task.CompletedTask;
        }, cancellationToken).ConfigureAwait(false);
    }
}