namespace CSharp.ExceptionHandling;

/// <summary>
/// Exception transformation utilities for converting between exception types.
/// </summary>
public static class ExceptionTransformer
{
    /// <summary>
    /// Transforms a general exception into a domain-specific exception.
    /// </summary>
    public static DomainException ToDomainException(Exception exception, string? context = null)
    {
        return exception switch
        {
            DomainException domain => domain,
            ArgumentException arg => new ValidationException(
                $"Invalid argument: {arg.Message}",
                new[] { new ValidationError(arg.ParamName ?? "Unknown", arg.Message, null, "INVALID_ARGUMENT") }),
            InvalidOperationException invalid => new BusinessRuleException(
                "InvalidOperation", 
                invalid.Message, 
                invalid),
            TimeoutException timeout => new ExternalServiceException(
                context ?? "Unknown Service",
                $"Operation timed out: {timeout.Message}",
                408, // Request Timeout
                null,
                timeout),
            HttpRequestException http => new ExternalServiceException(
                context ?? "HTTP Service",
                http.Message,
                null,
                null,
                http),
            _ => new BusinessRuleException(
                "UnhandledException",
                $"An unexpected error occurred: {exception.Message}",
                exception)
        };
    }

    /// <summary>
    /// Flattens nested exceptions for easier analysis.
    /// </summary>
    public static IEnumerable<Exception> FlattenExceptions(Exception exception)
    {
        var current = exception;
        
        while (current != null)
        {
            yield return current;
            
            if (current is AggregateException aggregate)
            {
                foreach (var inner in aggregate.InnerExceptions.SelectMany(FlattenExceptions))
                {
                    yield return inner;
                }
                break;
            }
            
            current = current.InnerException;
        }
    }
}