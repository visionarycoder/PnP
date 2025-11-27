namespace CSharp.AsyncLazyLoading;

/// <summary>
/// AsyncLazy with expiration support.
/// </summary>
/// <typeparam name="T">The type of the value to be lazily initialized.</typeparam>
public class AsyncLazyWithExpiration<T>(Func<Task<T>> taskFactory, TimeSpan expiration)
{
    private readonly Func<Task<T>> taskFactory = taskFactory ?? throw new ArgumentNullException(nameof(taskFactory));
    private readonly TimeSpan expiration = expiration;
    private readonly object lockObj = new();
    private Task<T>? cachedTask;
    private DateTime creationTime;

    public Task<T> GetValueAsync()
    {
        lock (lockObj)
        {
            var now = DateTime.UtcNow;

            if (cachedTask == null || 
                cachedTask.IsFaulted || 
                now - creationTime > expiration)
            {
                cachedTask = taskFactory();
                creationTime = now;
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

    public bool IsExpired
    {
        get
        {
            lock (lockObj)
            {
                return DateTime.UtcNow - creationTime > expiration;
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