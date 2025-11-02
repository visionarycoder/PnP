# GraphQL Error Handling Patterns

**Description**: Comprehensive error handling strategies for HotChocolate GraphQL applications including structured error responses, exception mapping, and error monitoring.

**Language/Technology**: C# / HotChocolate

## Code

### Custom Error Types and Extensions

```csharp
namespace DocumentProcessor.GraphQL.Errors;

using HotChocolate;

// Base error interface for consistent error structure
public interface IDocumentProcessorError
{
    string Code { get; }
    string Message { get; }
    Dictionary<string, object?> Extensions { get; }
}

// Business logic errors
public class BusinessLogicError : Exception, IDocumentProcessorError
{
    public string Code { get; }
    public Dictionary<string, object?> Extensions { get; }

    public BusinessLogicError(string code, string message, Dictionary<string, object?>? extensions = null) 
        : base(message)
    {
        Code = code;
        Extensions = extensions ?? new Dictionary<string, object?>();
    }
}

// Validation errors with field-specific details
public class ValidationError : Exception, IDocumentProcessorError
{
    public string Code => "VALIDATION_ERROR";
    public Dictionary<string, object?> Extensions { get; }
    public List<FieldValidationError> FieldErrors { get; }

    public ValidationError(string message, List<FieldValidationError> fieldErrors) : base(message)
    {
        FieldErrors = fieldErrors;
        Extensions = new Dictionary<string, object?>
        {
            ["fieldErrors"] = fieldErrors.Select(e => new
            {
                field = e.FieldName,
                message = e.Message,
                code = e.Code
            }).ToArray()
        };
    }
}

public class FieldValidationError
{
    public string FieldName { get; set; } = "";
    public string Message { get; set; } = "";
    public string Code { get; set; } = "";
}

// Resource not found error
public class ResourceNotFoundError : Exception, IDocumentProcessorError
{
    public string Code => "RESOURCE_NOT_FOUND";
    public Dictionary<string, object?> Extensions { get; }

    public ResourceNotFoundError(string resourceType, string resourceId) 
        : base($"{resourceType} with ID '{resourceId}' was not found")
    {
        Extensions = new Dictionary<string, object?>
        {
            ["resourceType"] = resourceType,
            ["resourceId"] = resourceId
        };
    }
}

// Authorization error
public class AuthorizationError : Exception, IDocumentProcessorError
{
    public string Code => "AUTHORIZATION_ERROR";
    public Dictionary<string, object?> Extensions { get; }

    public AuthorizationError(string action, string resource) 
        : base($"Insufficient permissions to {action} {resource}")
    {
        Extensions = new Dictionary<string, object?>
        {
            ["action"] = action,
            ["resource"] = resource,
            ["requiredPermissions"] = GetRequiredPermissions(action, resource)
        };
    }

    private string[] GetRequiredPermissions(string action, string resource)
    {
        return action.ToLower() switch
        {
            "read" => new[] { $"read:{resource}" },
            "write" => new[] { $"write:{resource}", $"read:{resource}" },
            "delete" => new[] { $"delete:{resource}", $"write:{resource}" },
            _ => new[] { $"{action}:{resource}" }
        };
    }
}

// Processing error with detailed context
public class ProcessingError : Exception, IDocumentProcessorError
{
    public string Code => "PROCESSING_ERROR";
    public Dictionary<string, object?> Extensions { get; }

    public ProcessingError(
        string message, 
        string? pipelineId = null, 
        string? stageId = null, 
        Exception? innerException = null) : base(message, innerException)
    {
        Extensions = new Dictionary<string, object?>
        {
            ["pipelineId"] = pipelineId,
            ["stageId"] = stageId,
            ["timestamp"] = DateTime.UtcNow.ToString("O")
        };

        if (innerException != null)
        {
            Extensions["innerError"] = new
            {
                type = innerException.GetType().Name,
                message = innerException.Message,
                stackTrace = innerException.StackTrace
            };
        }
    }
}

// Rate limiting error
public class RateLimitError : Exception, IDocumentProcessorError
{
    public string Code => "RATE_LIMIT_EXCEEDED";
    public Dictionary<string, object?> Extensions { get; }

    public RateLimitError(string operation, int limit, TimeSpan window) 
        : base($"Rate limit exceeded for {operation}")
    {
        Extensions = new Dictionary<string, object?>
        {
            ["operation"] = operation,
            ["limit"] = limit,
            ["window"] = window.TotalSeconds,
            ["retryAfter"] = CalculateRetryAfter(window)
        };
    }

    private int CalculateRetryAfter(TimeSpan window)
    {
        return (int)Math.Ceiling(window.TotalSeconds);
    }
}
```

### Error Filter and Handler

```csharp
// Global error filter for GraphQL
public class GraphQLErrorFilter : IErrorFilter
{
    private readonly ILogger<GraphQLErrorFilter> _logger;
    private readonly IHostEnvironment _environment;

    public GraphQLErrorFilter(ILogger<GraphQLErrorFilter> logger, IHostEnvironment environment)
    {
        _logger = logger;
        _environment = environment;
    }

    public IError OnError(IError error)
    {
        var exception = error.Exception;
        
        // Log the error
        LogError(error, exception);

        // Transform the error based on type
        return exception switch
        {
            BusinessLogicError businessError => CreateBusinessError(error, businessError),
            ValidationError validationError => CreateValidationError(error, validationError),
            ResourceNotFoundError notFoundError => CreateNotFoundError(error, notFoundError),
            AuthorizationError authError => CreateAuthorizationError(error, authError),
            ProcessingError processingError => CreateProcessingError(error, processingError),
            RateLimitError rateLimitError => CreateRateLimitError(error, rateLimitError),
            ArgumentException argException => CreateArgumentError(error, argException),
            UnauthorizedAccessException => CreateUnauthorizedError(error),
            TimeoutException => CreateTimeoutError(error),
            _ => CreateGenericError(error, exception)
        };
    }

    private void LogError(IError error, Exception? exception)
    {
        var context = new
        {
            Path = error.Path?.ToString(),
            Code = error.Code,
            Message = error.Message,
            Extensions = error.Extensions
        };

        if (exception != null)
        {
            _logger.LogError(exception, "GraphQL error occurred: {@ErrorContext}", context);
        }
        else
        {
            _logger.LogWarning("GraphQL error occurred: {@ErrorContext}", context);
        }
    }

    private IError CreateBusinessError(IError error, BusinessLogicError businessError)
    {
        var builder = ErrorBuilder.FromError(error)
            .SetCode(businessError.Code)
            .SetMessage(businessError.Message);

        foreach (var (key, value) in businessError.Extensions)
        {
            builder.SetExtension(key, value);
        }

        return builder.Build();
    }

    private IError CreateValidationError(IError error, ValidationError validationError)
    {
        return ErrorBuilder.FromError(error)
            .SetCode("VALIDATION_ERROR")
            .SetMessage(validationError.Message)
            .SetExtension("fieldErrors", validationError.Extensions["fieldErrors"])
            .Build();
    }

    private IError CreateNotFoundError(IError error, ResourceNotFoundError notFoundError)
    {
        return ErrorBuilder.FromError(error)
            .SetCode("RESOURCE_NOT_FOUND")
            .SetMessage(notFoundError.Message)
            .SetExtension("resourceType", notFoundError.Extensions["resourceType"])
            .SetExtension("resourceId", notFoundError.Extensions["resourceId"])
            .Build();
    }

    private IError CreateAuthorizationError(IError error, AuthorizationError authError)
    {
        return ErrorBuilder.FromError(error)
            .SetCode("AUTHORIZATION_ERROR")
            .SetMessage("Access denied")
            .SetExtension("action", authError.Extensions["action"])
            .SetExtension("resource", authError.Extensions["resource"])
            .SetExtension("requiredPermissions", authError.Extensions["requiredPermissions"])
            .Build();
    }

    private IError CreateProcessingError(IError error, ProcessingError processingError)
    {
        var builder = ErrorBuilder.FromError(error)
            .SetCode("PROCESSING_ERROR")
            .SetMessage(processingError.Message);

        foreach (var (key, value) in processingError.Extensions)
        {
            builder.SetExtension(key, value);
        }

        return builder.Build();
    }

    private IError CreateRateLimitError(IError error, RateLimitError rateLimitError)
    {
        return ErrorBuilder.FromError(error)
            .SetCode("RATE_LIMIT_EXCEEDED")
            .SetMessage(rateLimitError.Message)
            .SetExtension("limit", rateLimitError.Extensions["limit"])
            .SetExtension("window", rateLimitError.Extensions["window"])
            .SetExtension("retryAfter", rateLimitError.Extensions["retryAfter"])
            .Build();
    }

    private IError CreateArgumentError(IError error, ArgumentException argException)
    {
        return ErrorBuilder.FromError(error)
            .SetCode("INVALID_ARGUMENT")
            .SetMessage("Invalid argument provided")
            .SetExtension("parameterName", argException.ParamName)
            .SetExtension("details", argException.Message)
            .Build();
    }

    private IError CreateUnauthorizedError(IError error)
    {
        return ErrorBuilder.FromError(error)
            .SetCode("UNAUTHORIZED")
            .SetMessage("Authentication required")
            .SetExtension("hint", "Please provide a valid authentication token")
            .Build();
    }

    private IError CreateTimeoutError(IError error)
    {
        return ErrorBuilder.FromError(error)
            .SetCode("TIMEOUT")
            .SetMessage("Operation timed out")
            .SetExtension("hint", "Please try again or reduce the complexity of your request")
            .Build();
    }

    private IError CreateGenericError(IError error, Exception? exception)
    {
        var builder = ErrorBuilder.FromError(error)
            .SetCode("INTERNAL_ERROR")
            .SetMessage("An internal error occurred");

        // In development, include more details
        if (_environment.IsDevelopment() && exception != null)
        {
            builder
                .SetExtension("exceptionType", exception.GetType().Name)
                .SetExtension("exceptionMessage", exception.Message)
                .SetExtension("stackTrace", exception.StackTrace);
        }

        return builder.Build();
    }
}
```

### Result Types for Error Handling

```csharp
// Union types for operation results
[UnionType("DocumentResult")]
public abstract class DocumentResult
{
    public static DocumentResult Success(Document document) => new DocumentSuccess(document);
    public static DocumentResult Error(IDocumentProcessorError error) => new DocumentError(error);
}

public class DocumentSuccess : DocumentResult
{
    public Document Document { get; }
    
    public DocumentSuccess(Document document)
    {
        Document = document;
    }
}

public class DocumentError : DocumentResult
{
    public string Code { get; }
    public string Message { get; }
    public Dictionary<string, object?> Extensions { get; }
    
    public DocumentError(IDocumentProcessorError error)
    {
        Code = error.Code;
        Message = error.Message;
        Extensions = error.Extensions;
    }
}

// Processing result with detailed error information
[UnionType("ProcessingResult")]
public abstract class ProcessingResult
{
    public static ProcessingResult Success(ProcessingJob job) => new ProcessingSuccess(job);
    public static ProcessingResult Error(ProcessingError error) => new ProcessingFailure(error);
}

public class ProcessingSuccess : ProcessingResult
{
    public ProcessingJob Job { get; }
    
    public ProcessingSuccess(ProcessingJob job)
    {
        Job = job;
    }
}

public class ProcessingFailure : ProcessingResult
{
    public string Code { get; }
    public string Message { get; }
    public string? PipelineId { get; }
    public string? StageId { get; }
    public DateTime Timestamp { get; }
    
    public ProcessingFailure(ProcessingError error)
    {
        Code = error.Code;
        Message = error.Message;
        PipelineId = error.Extensions.GetValueOrDefault("pipelineId")?.ToString();
        StageId = error.Extensions.GetValueOrDefault("stageId")?.ToString();
        Timestamp = DateTime.UtcNow;
    }
}

// Batch operation results
public class BatchOperationResult<T>
{
    public List<T> Successful { get; set; } = new();
    public List<BatchError> Failed { get; set; } = new();
    public BatchOperationSummary Summary { get; set; } = new();
}

public class BatchError
{
    public string Id { get; set; } = "";
    public string Code { get; set; } = "";
    public string Message { get; set; } = "";
    public Dictionary<string, object?> Extensions { get; set; } = new();
}

public class BatchOperationSummary
{
    public int Total { get; set; }
    public int Successful { get; set; }
    public int Failed { get; set; }
    public TimeSpan Duration { get; set; }
}
```

### Error-Safe Resolvers

```csharp
// Document mutations with comprehensive error handling
[MutationType]
public class DocumentMutationsWithErrorHandling
{
    public async Task<DocumentResult> CreateDocumentAsync(
        CreateDocumentInput input,
        [Service] IDocumentService documentService,
        [Service] IValidator<CreateDocumentInput> validator,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate input
            var validationResult = await validator.ValidateAsync(input, cancellationToken);
            if (!validationResult.IsValid)
            {
                var fieldErrors = validationResult.Errors.Select(e => new FieldValidationError
                {
                    FieldName = e.PropertyName,
                    Message = e.ErrorMessage,
                    Code = e.ErrorCode
                }).ToList();
                
                return DocumentResult.Error(new ValidationError("Input validation failed", fieldErrors));
            }

            // Check authorization
            var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return DocumentResult.Error(new AuthorizationError("create", "document"));
            }

            // Create document
            var document = await documentService.CreateAsync(input, userId, cancellationToken);
            return DocumentResult.Success(document);
        }
        catch (BusinessLogicError businessError)
        {
            return DocumentResult.Error(businessError);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Log unexpected errors
            return DocumentResult.Error(new BusinessLogicError(
                "CREATION_FAILED", 
                "Failed to create document",
                new Dictionary<string, object?> { ["originalError"] = ex.Message }));
        }
    }

    public async Task<BatchOperationResult<Document>> CreateDocumentsBatchAsync(
        List<CreateDocumentInput> inputs,
        [Service] IDocumentService documentService,
        [Service] IValidator<CreateDocumentInput> validator,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken)
    {
        var result = new BatchOperationResult<Document>();
        var stopwatch = Stopwatch.StartNew();
        
        var userId = currentUser.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            result.Failed.Add(new BatchError
            {
                Id = "all",
                Code = "AUTHORIZATION_ERROR",
                Message = "User not authenticated"
            });
            
            result.Summary = new BatchOperationSummary
            {
                Total = inputs.Count,
                Failed = inputs.Count,
                Duration = stopwatch.Elapsed
            };
            
            return result;
        }

        // Process each input
        for (int i = 0; i < inputs.Count; i++)
        {
            var input = inputs[i];
            var inputId = input.Id ?? i.ToString();
            
            try
            {
                // Validate individual input
                var validationResult = await validator.ValidateAsync(input, cancellationToken);
                if (!validationResult.IsValid)
                {
                    result.Failed.Add(new BatchError
                    {
                        Id = inputId,
                        Code = "VALIDATION_ERROR",
                        Message = "Input validation failed",
                        Extensions = new Dictionary<string, object?>
                        {
                            ["fieldErrors"] = validationResult.Errors.Select(e => new
                            {
                                field = e.PropertyName,
                                message = e.ErrorMessage,
                                code = e.ErrorCode
                            }).ToArray()
                        }
                    });
                    continue;
                }

                // Create document
                var document = await documentService.CreateAsync(input, userId, cancellationToken);
                result.Successful.Add(document);
            }
            catch (BusinessLogicError businessError)
            {
                result.Failed.Add(new BatchError
                {
                    Id = inputId,
                    Code = businessError.Code,
                    Message = businessError.Message,
                    Extensions = businessError.Extensions
                });
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                result.Failed.Add(new BatchError
                {
                    Id = inputId,
                    Code = "CREATION_FAILED",
                    Message = "Unexpected error during creation",
                    Extensions = new Dictionary<string, object?> { ["originalError"] = ex.Message }
                });
            }
        }

        stopwatch.Stop();
        result.Summary = new BatchOperationSummary
        {
            Total = inputs.Count,
            Successful = result.Successful.Count,
            Failed = result.Failed.Count,
            Duration = stopwatch.Elapsed
        };

        return result;
    }

    [Error<ResourceNotFoundError>]
    [Error<AuthorizationError>]
    [Error<ValidationError>]
    public async Task<Document> UpdateDocumentAsync(
        string id,
        UpdateDocumentInput input,
        [Service] IDocumentService documentService,
        [Service] IValidator<UpdateDocumentInput> validator,
        [Service] IAuthorizationService authorizationService,
        ClaimsPrincipal currentUser,
        CancellationToken cancellationToken)
    {
        // Validate input
        var validationResult = await validator.ValidateAsync(input, cancellationToken);
        if (!validationResult.IsValid)
        {
            var fieldErrors = validationResult.Errors.Select(e => new FieldValidationError
            {
                FieldName = e.PropertyName,
                Message = e.ErrorMessage,
                Code = e.ErrorCode
            }).ToList();
            
            throw new ValidationError("Input validation failed", fieldErrors);
        }

        // Check if document exists
        var existingDocument = await documentService.GetByIdAsync(id, cancellationToken);
        if (existingDocument == null)
        {
            throw new ResourceNotFoundError("Document", id);
        }

        // Check authorization
        var authResult = await authorizationService.AuthorizeAsync(
            currentUser, existingDocument, "ModifyDocument");
            
        if (!authResult.Succeeded)
        {
            throw new AuthorizationError("modify", "document");
        }

        // Update document
        return await documentService.UpdateAsync(id, input, cancellationToken);
    }
}
```

### Error Monitoring and Reporting

```csharp
// Error tracking and reporting service
public interface IErrorReportingService
{
    Task ReportErrorAsync(IError error, IRequestContext context, CancellationToken cancellationToken = default);
    Task<ErrorReport> GetErrorReportAsync(DateTime from, DateTime to, CancellationToken cancellationToken = default);
}

public class ErrorReportingService : IErrorReportingService
{
    private readonly ILogger<ErrorReportingService> _logger;
    private readonly IErrorRepository _errorRepository;
    private readonly IMetrics _metrics;

    public ErrorReportingService(
        ILogger<ErrorReportingService> logger,
        IErrorRepository errorRepository,
        IMetrics metrics)
    {
        _logger = logger;
        _errorRepository = errorRepository;
        _metrics = metrics;
    }

    public async Task ReportErrorAsync(
        IError error, 
        IRequestContext context, 
        CancellationToken cancellationToken = default)
    {
        var errorRecord = new ErrorRecord
        {
            Id = Guid.NewGuid().ToString(),
            Code = error.Code ?? "UNKNOWN",
            Message = error.Message,
            Path = error.Path?.ToString(),
            Timestamp = DateTime.UtcNow,
            UserId = GetUserId(context),
            OperationName = context.Request.OperationName,
            Query = context.Request.Query?.ToString(),
            Variables = SerializeVariables(context.Request.VariableValues),
            Extensions = SerializeExtensions(error.Extensions),
            StackTrace = error.Exception?.StackTrace,
            UserAgent = GetUserAgent(context),
            IpAddress = GetIpAddress(context)
        };

        // Store error for analysis
        await _errorRepository.SaveErrorAsync(errorRecord, cancellationToken);

        // Update metrics
        _metrics.Measure.Counter.Increment(
            "graphql.errors.total",
            new MetricTags("code", errorRecord.Code, "operation", errorRecord.OperationName ?? "unknown"));

        // Log structured error
        _logger.LogError("GraphQL error recorded: {@ErrorRecord}", errorRecord);
    }

    public async Task<ErrorReport> GetErrorReportAsync(
        DateTime from, 
        DateTime to, 
        CancellationToken cancellationToken = default)
    {
        var errors = await _errorRepository.GetErrorsAsync(from, to, cancellationToken);
        
        return new ErrorReport
        {
            Period = new DateRange { From = from, To = to },
            TotalErrors = errors.Count,
            ErrorsByCode = errors.GroupBy(e => e.Code)
                .ToDictionary(g => g.Key, g => g.Count()),
            ErrorsByOperation = errors.GroupBy(e => e.OperationName ?? "unknown")
                .ToDictionary(g => g.Key, g => g.Count()),
            TopErrors = errors.GroupBy(e => e.Code)
                .OrderByDescending(g => g.Count())
                .Take(10)
                .Select(g => new ErrorSummary
                {
                    Code = g.Key,
                    Count = g.Count(),
                    LastOccurrence = g.Max(e => e.Timestamp),
                    SampleMessage = g.First().Message
                })
                .ToList(),
            ErrorTrends = CalculateErrorTrends(errors)
        };
    }

    private string? GetUserId(IRequestContext context)
    {
        return context.ContextData.TryGetValue("currentUser", out var user) && user is ClaimsPrincipal principal
            ? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value
            : null;
    }

    private string? GetUserAgent(IRequestContext context)
    {
        return context.ContextData.TryGetValue("httpContext", out var httpContext) && httpContext is HttpContext http
            ? http.Request.Headers.UserAgent.ToString()
            : null;
    }

    private string? GetIpAddress(IRequestContext context)
    {
        return context.ContextData.TryGetValue("httpContext", out var httpContext) && httpContext is HttpContext http
            ? http.Connection.RemoteIpAddress?.ToString()
            : null;
    }

    private string? SerializeVariables(IVariableValueCollection? variables)
    {
        if (variables == null) return null;
        
        try
        {
            return JsonSerializer.Serialize(variables.ToDictionary(v => v.Name, v => v.Value));
        }
        catch
        {
            return null;
        }
    }

    private string? SerializeExtensions(IReadOnlyDictionary<string, object?>? extensions)
    {
        if (extensions == null) return null;
        
        try
        {
            return JsonSerializer.Serialize(extensions);
        }
        catch
        {
            return null;
        }
    }

    private List<ErrorTrend> CalculateErrorTrends(List<ErrorRecord> errors)
    {
        return errors
            .GroupBy(e => new { e.Code, Date = e.Timestamp.Date })
            .Select(g => new ErrorTrend
            {
                Code = g.Key.Code,
                Date = g.Key.Date,
                Count = g.Count()
            })
            .OrderBy(t => t.Date)
            .ToList();
    }
}

// Error monitoring middleware
public class ErrorMonitoringMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IErrorReportingService _errorReporting;
    private readonly ILogger<ErrorMonitoringMiddleware> _logger;

    public ErrorMonitoringMiddleware(
        RequestDelegate next,
        IErrorReportingService errorReporting,
        ILogger<ErrorMonitoringMiddleware> logger)
    {
        _next = next;
        _errorReporting = errorReporting;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in GraphQL pipeline");
            
            // Create error for reporting
            var error = ErrorBuilder.New()
                .SetMessage(ex.Message)
                .SetCode("UNHANDLED_EXCEPTION")
                .SetException(ex)
                .Build();

            // This would need to be adapted to your GraphQL request context
            // await _errorReporting.ReportErrorAsync(error, requestContext);
            
            throw;
        }
    }
}
```

### Error Schema Types

```csharp
// GraphQL schema types for errors
[ObjectType<DocumentError>]
public static partial class DocumentErrorType
{
    public static string GetCode([Parent] DocumentError error) => error.Code;
    public static string GetMessage([Parent] DocumentError error) => error.Message;
    public static IReadOnlyDictionary<string, object?> GetExtensions([Parent] DocumentError error) => error.Extensions;
}

[ObjectType<BatchError>]
public static partial class BatchErrorType
{
    public static string GetId([Parent] BatchError error) => error.Id;
    public static string GetCode([Parent] BatchError error) => error.Code;
    public static string GetMessage([Parent] BatchError error) => error.Message;
    public static IReadOnlyDictionary<string, object?> GetExtensions([Parent] BatchError error) => error.Extensions;
}

[ObjectType<ErrorReport>]
public static partial class ErrorReportType
{
    public static DateRange GetPeriod([Parent] ErrorReport report) => report.Period;
    public static int GetTotalErrors([Parent] ErrorReport report) => report.TotalErrors;
    public static IReadOnlyDictionary<string, int> GetErrorsByCode([Parent] ErrorReport report) => report.ErrorsByCode;
    public static IReadOnlyDictionary<string, int> GetErrorsByOperation([Parent] ErrorReport report) => report.ErrorsByOperation;
    public static IEnumerable<ErrorSummary> GetTopErrors([Parent] ErrorReport report) => report.TopErrors;
    public static IEnumerable<ErrorTrend> GetErrorTrends([Parent] ErrorReport report) => report.ErrorTrends;
}
```

## Usage

### Error Handling Query Examples

```graphql
# Query with union result types
mutation CreateDocument($input: CreateDocumentInput!) {
  createDocument(input: $input) {
    __typename
    ... on DocumentSuccess {
      document {
        id
        title
        createdAt
      }
    }
    ... on DocumentError {
      code
      message
      extensions
    }
  }
}

# Batch operation with error details
mutation CreateDocumentsBatch($inputs: [CreateDocumentInput!]!) {
  createDocumentsBatch(inputs: $inputs) {
    summary {
      total
      successful
      failed
      duration
    }
    successful {
      id
      title
    }
    failed {
      id
      code
      message
      extensions
    }
  }
}

# Error reporting query
query ErrorReport($from: DateTime!, $to: DateTime!) {
  errorReport(from: $from, to: $to) {
    totalErrors
    errorsByCode
    topErrors {
      code
      count
      lastOccurrence
      sampleMessage
    }
    errorTrends {
      code
      date
      count
    }
  }
}
```

## Notes

- **Structured Errors**: Use consistent error structures with codes and extensions
- **Error Classification**: Categorize errors by type (business, validation, authorization, etc.)
- **Error Monitoring**: Implement comprehensive error tracking and reporting
- **User-Friendly Messages**: Provide clear, actionable error messages
- **Security**: Don't expose sensitive information in error messages
- **Debugging**: Include debug information in development environments
- **Resilience**: Design graceful degradation for partial failures
- **Performance**: Consider the performance impact of error handling

## Related Patterns

- [Authorization](authorization.md) - Security-related error handling
- [Performance Optimization](performance-optimization.md) - Error impact on performance
- [Schema Design](schema-design.md) - Error types in schema design

---

**Key Benefits**: Structured error responses, comprehensive monitoring, graceful failure handling, debugging support

**When to Use**: Production applications, complex business logic, user-facing APIs, debugging scenarios

**Performance**: Efficient error handling, structured logging, minimal overhead