namespace CSharp.FunctionalLinq;

/// <summary>
/// Functional pipeline for composing operations in a fluent manner
/// </summary>
public sealed class Pipeline<T>
{
    public T Value { get; }

    public Pipeline(T value)
    {
        Value = value;
    }

    // Map operation
    public Pipeline<TResult> Map<TResult>(Func<T, TResult> func)
    {
        return new Pipeline<TResult>(func(Value));
    }

    // Filter operation (returns Maybe)
    public Maybe<Pipeline<T>> Filter(Func<T, bool> predicate)
    {
        return predicate(Value) 
            ? Maybe<Pipeline<T>>.Some(this)
            : Maybe<Pipeline<T>>.None;
    }

    // FlatMap operation
    public Pipeline<TResult> FlatMap<TResult>(Func<T, Pipeline<TResult>> func)
    {
        return func(Value);
    }

    // Apply side effect
    public Pipeline<T> Tee(Action<T> action)
    {
        action(Value);
        return this;
    }

    // Apply conditional operation
    public Pipeline<T> When(bool condition, Func<T, T> operation)
    {
        return condition ? new Pipeline<T>(operation(Value)) : this;
    }

    public Pipeline<T> Unless(bool condition, Func<T, T> operation)
    {
        return condition ? this : new Pipeline<T>(operation(Value));
    }

    // Execute pipeline and return result
    public T Execute() => Value;

    // Implicit conversion from T to Pipeline<T>
    public static implicit operator Pipeline<T>(T value) => new(value);

    // Implicit conversion from Pipeline<T> to T
    public static implicit operator T(Pipeline<T> pipeline) => pipeline.Value;
}

/// <summary>
/// Static methods for creating pipelines
/// </summary>
public static class Pipeline
{
    public static Pipeline<T> Of<T>(T value) => new(value);

    public static Pipeline<T> Start<T>(T value) => new(value);
}