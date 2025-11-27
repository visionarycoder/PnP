namespace CSharp.AsyncLazyLoading;

/// <summary>
/// Thread-safe AsyncLazy with cancellation support.
/// </summary>
/// <typeparam name="T">The type of the value to be lazily initialized.</typeparam>
public class AsyncLazyCancellable<T>(Func<CancellationToken, Task<T>> taskFactory)
{
    private readonly Func<CancellationToken, Task<T>> taskFactory = taskFactory ?? throw new ArgumentNullException(nameof(taskFactory));
    private readonly object lockObj = new();
    private Task<T>? cachedTask;

    public Task<T> GetValueAsync(CancellationToken cancellationToken = default)
    {
        lock (lockObj)
        {
            if (cachedTask == null)
            {
                cachedTask = taskFactory(cancellationToken);
            }
            else if (cachedTask.IsCanceled && !cancellationToken.IsCancellationRequested)
            {
                // Previous task was cancelled, but new request isn't - retry
                cachedTask = taskFactory(cancellationToken);
            }

            return cachedTask;
        }
    }

    public bool IsValueCreated
    {
        get
        {
            lock (lockObj)
            {
                return cachedTask?.IsCompletedSuccessfully == true;
            }
        }
    }

    public void Reset()
    {
        lock (lockObj)
        {
            cachedTask = null;
        }
    }
}