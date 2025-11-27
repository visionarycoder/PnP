namespace CSharp.FunctionalLinq;

internal class Return<T>(T value) : Trampoline<T>
{
    public T Value { get; } = value;
}