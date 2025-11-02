namespace CSharp.FunctionalLinq;

/// <summary>
/// Maybe/Option monad for null-safe operations
/// </summary>
/// <typeparam name="T">The type of the contained value</typeparam>
public readonly struct Maybe<T>
{
    private readonly T value;
    private readonly bool hasValue;

    private Maybe(T value)
    {
        this.value = value;
        hasValue = value != null;
    }

    public static Maybe<T> Some(T value) => 
        value != null ? new Maybe<T>(value) : None;

    public static Maybe<T> None => default;

    public bool HasValue => hasValue;
    
    public T Value => hasValue ? value : throw new InvalidOperationException("Maybe has no value");

    // Functor: map function over Maybe
    public Maybe<TResult> Map<TResult>(Func<T, TResult> func)
    {
        return hasValue ? Maybe<TResult>.Some(func(value)) : Maybe<TResult>.None;
    }

    // Monad: flatMap for chaining Maybe operations
    public Maybe<TResult> FlatMap<TResult>(Func<T, Maybe<TResult>> func)
    {
        return hasValue ? func(value) : Maybe<TResult>.None;
    }

    // Filter: conditional Maybe
    public Maybe<T> Filter(Func<T, bool> predicate)
    {
        return hasValue && predicate(value) ? this : None;
    }

    // GetOrElse: provide default value
    public T GetOrElse(T defaultValue)
    {
        return hasValue ? value : defaultValue;
    }

    public T GetOrElse(Func<T> defaultFactory)
    {
        return hasValue ? value : defaultFactory();
    }

    // Fold: reduce Maybe to single value
    public TResult Fold<TResult>(TResult noneValue, Func<T, TResult> someFunc)
    {
        return hasValue ? someFunc(value) : noneValue;
    }

    public override string ToString()
    {
        return hasValue ? $"Some({value})" : "None";
    }

    public static implicit operator Maybe<T>(T value) => Some(value);
}