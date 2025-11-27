namespace CSharp.ExceptionHandling;

/// <summary>
/// Represents the result of an operation that may fail.
/// </summary>
/// <typeparam name="T">The type of the result value.</typeparam>
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T Value { get; }
    public Exception? Exception { get; }
    public string? ErrorMessage { get; }

    private Result(T value)
    {
        IsSuccess = true;
        Value = value;
        Exception = null;
        ErrorMessage = null;
    }

    private Result(Exception exception, string? errorMessage = null)
    {
        IsSuccess = false;
        Value = default(T)!;
        Exception = exception;
        ErrorMessage = errorMessage ?? exception?.Message;
    }

    public static Result<T> Success(T value) => new(value);
    public static Result<T> Failure(Exception exception, string? errorMessage = null) => new(exception, errorMessage);
    public static Result<T> Failure(string errorMessage) => new(new InvalidOperationException(errorMessage), errorMessage);

    public TResult Match<TResult>(Func<T, TResult> onSuccess, Func<Exception, TResult> onFailure)
    {
        return IsSuccess ? onSuccess(Value) : onFailure(Exception!);
    }

    public void Match(Action<T> onSuccess, Action<Exception> onFailure)
    {
        if (IsSuccess)
            onSuccess(Value);
        else
            onFailure(Exception!);
    }

    public Result<TNew> Map<TNew>(Func<T, TNew> transform)
    {
        return IsSuccess ? Result<TNew>.Success(transform(Value)) : Result<TNew>.Failure(Exception!);
    }

    public async Task<Result<TNew>> MapAsync<TNew>(Func<T, Task<TNew>> transform)
    {
        if (IsFailure)
            return Result<TNew>.Failure(Exception!);

        try
        {
            var result = await transform(Value);
            return Result<TNew>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<TNew>.Failure(ex);
        }
    }
}