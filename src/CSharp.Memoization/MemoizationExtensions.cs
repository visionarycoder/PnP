namespace CSharp.Memoization;

/// <summary>
/// Extension methods for easy memoization of functions.
/// </summary>
public static class MemoizationExtensions
{
    /// <summary>
    /// Creates a memoized version of a function.
    /// </summary>
    /// <typeparam name="TArg">The type of the function argument.</typeparam>
    /// <typeparam name="TResult">The type of the function result.</typeparam>
    /// <param name="function">The function to memoize.</param>
    /// <param name="options">Optional memoization options.</param>
    /// <returns>A memoized version of the function.</returns>
    public static Func<TArg, TResult> Memoize<TArg, TResult>(
        this Func<TArg, TResult> function, 
        MemoizationOptions? options = null) where TArg : notnull
    {
        var memoizer = new Memoizer<TArg, TResult>(options);
        return arg => memoizer.GetOrCompute(arg, function);
    }

    /// <summary>
    /// Creates a memoized version of an async function.
    /// </summary>
    /// <typeparam name="TArg">The type of the function argument.</typeparam>
    /// <typeparam name="TResult">The type of the function result.</typeparam>
    /// <param name="function">The async function to memoize.</param>
    /// <param name="options">Optional memoization options.</param>
    /// <returns>A memoized version of the async function.</returns>
    public static Func<TArg, Task<TResult>> MemoizeAsync<TArg, TResult>(
        this Func<TArg, Task<TResult>> function, 
        MemoizationOptions? options = null) where TArg : notnull
    {
        var memoizer = new Memoizer<TArg, TResult>(options);
        return arg => memoizer.GetOrComputeAsync(arg, function);
    }
}