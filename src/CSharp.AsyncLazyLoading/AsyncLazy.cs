using System.Runtime.CompilerServices;

namespace CSharp.AsyncLazyLoading;

/// <summary>
/// Basic AsyncLazy implementation for asynchronous lazy initialization.
/// </summary>
/// <typeparam name="T">The type of the value to be lazily initialized.</typeparam>
public class AsyncLazy<T>(Func<Task<T>> taskFactory)
{
    private readonly Lazy<Task<T>> lazy = new(taskFactory);

    public AsyncLazy(Func<T> valueFactory) : this(() => Task.FromResult(valueFactory()))
    {
    }

    public Task<T> Value => lazy.Value;
    
    public bool IsValueCreated => lazy.IsValueCreated;

    public TaskAwaiter<T> GetAwaiter() => Value.GetAwaiter();
    
    public ConfiguredTaskAwaitable<T> ConfigureAwait(bool continueOnCapturedContext) =>
        Value.ConfigureAwait(continueOnCapturedContext);
}

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