namespace CSharp.FunctionalLinq;

/// <summary>
/// Lazy computation wrapper for deferred evaluation
/// </summary>
public sealed class Thunk<T>
{
    private readonly Lazy<T> lazy;

    public Thunk(Func<T> computation)
    {
        lazy = new Lazy<T>(computation);
    }

    // Force evaluation and get the value
    public T Force() => lazy.Value;

    // Check if value has been computed
    public bool IsEvaluated => lazy.IsValueCreated;

    // Map over the thunk (preserves laziness)
    public Thunk<TResult> Map<TResult>(Func<T, TResult> func)
    {
        return new Thunk<TResult>(() => func(Force()));
    }

    // FlatMap for thunks
    public Thunk<TResult> FlatMap<TResult>(Func<T, Thunk<TResult>> func)
    {
        return new Thunk<TResult>(() => func(Force()).Force());
    }

    // Apply function with side effects
    public Thunk<T> Tee(Action<T> action)
    {
        return new Thunk<T>(() =>
        {
            var value = Force();
            action(value);
            return value;
        });
    }

    // Implicit conversion from function to thunk
    public static implicit operator Thunk<T>(Func<T> computation) => new(computation);

    // Implicit conversion from thunk to value (forces evaluation)
    public static implicit operator T(Thunk<T> thunk) => thunk.Force();
}

/// <summary>
/// Static methods for creating thunks
/// </summary>
public static class Thunk
{
    public static Thunk<T> Of<T>(Func<T> computation) => new(computation);

    public static Thunk<T> Delay<T>(Func<T> computation) => new(computation);

    // Create a thunk from a value (already computed)
    public static Thunk<T> Return<T>(T value) => new(() => value);
}