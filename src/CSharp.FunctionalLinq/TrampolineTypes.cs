namespace CSharp.FunctionalLinq;

/// <summary>
/// Simple Trampoline implementation for stack-safe recursion demonstration
/// </summary>
public abstract class Trampoline<T>
{
    public static Trampoline<T> Return(T value) => new Return<T>(value);
    public static Trampoline<T> Suspend(Func<Trampoline<T>> continuation) => new Suspend<T>(continuation);
    
    public T Run()
    {
        var current = this;
        while (current is Suspend<T> suspend)
            current = suspend.Continuation();
        return ((Return<T>)current).Value;
    }
}

internal class Return<T>(T value) : Trampoline<T>
{
    public T Value { get; } = value;
}

internal class Suspend<T>(Func<Trampoline<T>> continuation) : Trampoline<T>
{
    public Func<Trampoline<T>> Continuation { get; } = continuation;
}