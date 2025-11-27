namespace CSharp.ExceptionHandling;

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