using System.Runtime.ExceptionServices;

namespace CSharp.ExceptionHandling;

/// <summary>
/// Options for configuring error boundary behavior.
/// </summary>
public class ErrorBoundaryOptions
{
    public bool LogErrors { get; set; } = true;
    public bool RetryOnTransientErrors { get; set; } = false;
    public int MaxRetryAttempts { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(1000);
    public Func<Exception, bool> IsTransientError { get; set; } = _ => false;
    public Func<Exception, Exception> TransformException { get; set; } = ex => ex;
}

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

/// <summary>
/// Error boundary for containing and handling exceptions in a controlled manner.
/// </summary>
public class ErrorBoundary
{
    private readonly ErrorBoundaryOptions options;

    public ErrorBoundary(ErrorBoundaryOptions? options = null)
    {
        this.options = options ?? new ErrorBoundaryOptions();
    }

    /// <summary>
    /// Executes an operation within an error boundary, returning a Result.
    /// </summary>
    public async Task<Result<T>> ExecuteAsync<T>(Func<Task<T>> operation)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= options.MaxRetryAttempts)
        {
            try
            {
                var result = await operation();
                return Result<T>.Success(result);
            }
            catch (Exception ex)
            {
                lastException = ex;
                
                if (options.LogErrors)
                {
                    Console.WriteLine($"Error in ErrorBoundary (attempt {attempt + 1}): {ex.Message}");
                }

                // Check if we should retry
                if (attempt < options.MaxRetryAttempts && 
                    options.RetryOnTransientErrors && 
                    options.IsTransientError(ex))
                {
                    attempt++;
                    if (options.RetryDelay > TimeSpan.Zero)
                    {
                        await Task.Delay(options.RetryDelay);
                    }
                    continue;
                }

                // Transform exception if configured
                var transformedException = options.TransformException(ex);
                return Result<T>.Failure(transformedException);
            }
        }

        return Result<T>.Failure(lastException!);
    }

    /// <summary>
    /// Executes a synchronous operation within an error boundary.
    /// </summary>
    public Result<T> Execute<T>(Func<T> operation)
    {
        var attempt = 0;
        Exception? lastException = null;

        while (attempt <= options.MaxRetryAttempts)
        {
            try
            {
                var result = operation();
                return Result<T>.Success(result);
            }
            catch (Exception ex)
            {
                lastException = ex;
                
                if (options.LogErrors)
                {
                    Console.WriteLine($"Error in ErrorBoundary (attempt {attempt + 1}): {ex.Message}");
                }

                // Check if we should retry
                if (attempt < options.MaxRetryAttempts && 
                    options.RetryOnTransientErrors && 
                    options.IsTransientError(ex))
                {
                    attempt++;
                    if (options.RetryDelay > TimeSpan.Zero)
                    {
                        Thread.Sleep(options.RetryDelay);
                    }
                    continue;
                }

                // Transform exception if configured
                var transformedException = options.TransformException(ex);
                return Result<T>.Failure(transformedException);
            }
        }

        return Result<T>.Failure(lastException!);
    }
}

/// <summary>
/// Exception aggregator for collecting multiple exceptions during batch operations.
/// </summary>
public class ExceptionAggregator
{
    private readonly List<Exception> exceptions = new();
    private readonly object lockObject = new();

    public bool HasExceptions => exceptions.Count > 0;
    public int ExceptionCount => exceptions.Count;
    public IReadOnlyList<Exception> Exceptions => exceptions.AsReadOnly();

    public void Add(Exception exception)
    {
        if (exception == null) return;

        lock (lockObject)
        {
            exceptions.Add(exception);
        }
    }

    public void AddRange(IEnumerable<Exception> exceptions)
    {
        if (exceptions == null) return;

        lock (lockObject)
        {
            this.exceptions.AddRange(exceptions.Where(ex => ex != null));
        }
    }

    public void ThrowIfAny()
    {
        if (HasExceptions)
        {
            throw new AggregateException("One or more errors occurred during batch operation.", exceptions);
        }
    }

    public AggregateException? ToAggregateException()
    {
        return HasExceptions ? new AggregateException(exceptions) : null;
    }

    public void Clear()
    {
        lock (lockObject)
        {
            exceptions.Clear();
        }
    }
}

/// <summary>
/// Preserves the original stack trace when re-throwing exceptions.
/// </summary>
public static class ExceptionDispatchHelper
{
    /// <summary>
    /// Re-throws an exception while preserving the original stack trace.
    /// </summary>
    public static void RethrowWithStackTrace(Exception exception)
    {
        ExceptionDispatchInfo.Capture(exception).Throw();
    }

    /// <summary>
    /// Transforms an exception while preserving stack trace information.
    /// </summary>
    public static T TransformException<T>(Exception originalException, Func<Exception, T> transform) where T : Exception
    {
        var transformed = transform(originalException);
        
        // Preserve original exception as inner exception if not already set
        if (transformed.InnerException == null && transformed != originalException)
        {
            // Create new instance with original exception as inner exception
            var constructors = typeof(T).GetConstructors();
            var messageInnerConstructor = constructors.FirstOrDefault(c => 
            {
                var parameters = c.GetParameters();
                return parameters.Length == 2 && 
                       parameters[0].ParameterType == typeof(string) && 
                       parameters[1].ParameterType == typeof(Exception);
            });

            if (messageInnerConstructor != null)
            {
                return (T)messageInnerConstructor.Invoke(new object[] { transformed.Message, originalException });
            }
        }

        return transformed;
    }
}