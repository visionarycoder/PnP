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