namespace CSharp.FunctionalLinq;

internal class Suspend<T>(Func<Trampoline<T>> continuation) : Trampoline<T>
{
    public Func<Trampoline<T>> Continuation { get; } = continuation;
}