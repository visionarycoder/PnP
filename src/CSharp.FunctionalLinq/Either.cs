namespace CSharp.FunctionalLinq;

/// <summary>
/// Either monad for error handling
/// </summary>
/// <typeparam name="TLeft">The type of the left (error) value</typeparam>
/// <typeparam name="TRight">The type of the right (success) value</typeparam>
public abstract class Either<TLeft, TRight>
{
    public abstract bool IsLeft { get; }
    public abstract bool IsRight { get; }

    public abstract TResult Match<TResult>(
        Func<TLeft, TResult> leftFunc,
        Func<TRight, TResult> rightFunc);

    public Either<TLeft, TResult> Map<TResult>(Func<TRight, TResult> func)
    {
        return Match<Either<TLeft, TResult>>(
            left => Either<TLeft, TResult>.Left(left),
            right => Either<TLeft, TResult>.Right(func(right)));
    }

    public Either<TLeft, TResult> FlatMap<TResult>(Func<TRight, Either<TLeft, TResult>> func)
    {
        return Match(
            left => Either<TLeft, TResult>.Left(left),
            func);
    }

    public static Either<TLeft, TRight> Left(TLeft value) => new LeftImpl(value);
    public static Either<TLeft, TRight> Right(TRight value) => new RightImpl(value);

    private class LeftImpl : Either<TLeft, TRight>
    {
        public TLeft Value { get; }
        public LeftImpl(TLeft value) => Value = value;
        public override bool IsLeft => true;
        public override bool IsRight => false;

        public override TResult Match<TResult>(
            Func<TLeft, TResult> leftFunc,
            Func<TRight, TResult> rightFunc) => leftFunc(Value);
    }

    private class RightImpl : Either<TLeft, TRight>
    {
        public TRight Value { get; }
        public RightImpl(TRight value) => Value = value;
        public override bool IsLeft => false;
        public override bool IsRight => true;

        public override TResult Match<TResult>(
            Func<TLeft, TResult> leftFunc,
            Func<TRight, TResult> rightFunc) => rightFunc(Value);
    }
}