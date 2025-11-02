using CSharp.ExceptionHandling;
using System.Diagnostics;

namespace CSharp.ExceptionHandling;

/// <summary>
/// Demonstrates comprehensive exception handling patterns including structured error management,
/// error boundaries, diagnostic collection, and exception transformation.
/// </summary>
class Program
{
    static async Task Main()
    {
        Console.WriteLine("=== Exception Handling Patterns Demonstration ===\n");

        await DemonstrateDomainExceptions();
        await DemonstrateErrorBoundary();
        await DemonstrateDiagnosticCollection();
        await DemonstrateExceptionTransformation();
        await DemonstrateExceptionAggregation();
        DemonstrateStackTracePreservation();
    }

    static async Task DemonstrateDomainExceptions()
    {
        Console.WriteLine("--- Domain Exception Hierarchy ---");

        try
        {
            // Simulate validation error
            var validationErrors = new[]
            {
                new ValidationError("Email", "Email is required", null, "REQUIRED"),
                new ValidationError("Age", "Age must be between 18 and 65", 15, "RANGE_ERROR")
            };

            throw new ValidationException(
                "User validation failed",
                validationErrors)
                .WithData("UserId", 12345)
                .WithData("RequestSource", "Web API");
        }
        catch (ValidationException ex)
        {
            Console.WriteLine($"Caught ValidationException:");
            Console.WriteLine($"  Error Code: {ex.ErrorCode}");
            Console.WriteLine($"  Category: {ex.ErrorCategory}");
            Console.WriteLine($"  Correlation ID: {ex.CorrelationId}");
            Console.WriteLine($"  Timestamp: {ex.Timestamp:HH:mm:ss.fff}");
            Console.WriteLine($"  User ID: {ex.GetData<int>("UserId")}");
            Console.WriteLine($"  Validation Errors: {ex.ValidationErrors.Count}");
            
            foreach (var error in ex.ValidationErrors)
            {
                Console.WriteLine($"    - {error.PropertyName}: {error.ErrorMessage}");
            }
        }

        try
        {
            // Simulate business rule violation
            throw new BusinessRuleException(
                "MaxOrderLimit",
                "Customer has exceeded maximum order limit for the month");
        }
        catch (BusinessRuleException ex)
        {
            Console.WriteLine($"\nCaught BusinessRuleException:");
            Console.WriteLine($"  Rule: {ex.RuleName}");
            Console.WriteLine($"  Message: {ex.Message}");
        }

        Console.WriteLine();
    }

    static async Task DemonstrateErrorBoundary()
    {
        Console.WriteLine("--- Error Boundary with Retry Logic ---");

        var options = new ErrorBoundaryOptions
        {
            RetryOnTransientErrors = true,
            MaxRetryAttempts = 3,
            RetryDelay = TimeSpan.FromMilliseconds(100),
            IsTransientError = ex => ex is TimeoutException || ex.Message.Contains("network")
        };

        var errorBoundary = new ErrorBoundary(options);

        // Simulate transient failure that succeeds on retry
        int attemptCount = 0;
        var result = await errorBoundary.ExecuteAsync(async () =>
        {
            attemptCount++;
            Console.WriteLine($"  Attempt {attemptCount}");
            
            if (attemptCount < 3)
            {
                throw new TimeoutException("Simulated network timeout");
            }
            
            return "Operation succeeded on attempt " + attemptCount;
        });

        result.Match(
            success => Console.WriteLine($"Success: {success}"),
            failure => Console.WriteLine($"Failed: {failure.Message}")
        );

        // Demonstrate non-transient failure
        var failureResult = await errorBoundary.ExecuteAsync<string>(async () =>
        {
            await Task.Delay(10);
            throw new ArgumentException("This is not a transient error");
            return "This won't be reached";
        });

        failureResult.Match(
            success => Console.WriteLine($"Success: {success}"),
            failure => Console.WriteLine($"Non-transient failure: {failure.Message}")
        );

        Console.WriteLine();
    }

    static async Task DemonstrateDiagnosticCollection()
    {
        Console.WriteLine("--- Diagnostic Collection and Context ---");

        try
        {
            await ExceptionHandler.HandleAsync<string>(async () =>
            {
                using var context = new DiagnosticContext("ProcessUserOrder");
                
                context.AddData("UserId", 12345);
                context.AddData("OrderId", 67890);
                context.AddEvent("ValidationStarted", "Starting order validation");
                
                // Simulate some processing
                await Task.Delay(50);
                context.AddEvent("ValidationCompleted", "Order validation completed successfully");
                
                context.AddEvent("PaymentStarted", "Starting payment processing");
                await Task.Delay(30);
                
                // Simulate failure
                throw new ExternalServiceException(
                    "PaymentGateway",
                    "Payment processing failed",
                    503,
                    "Service temporarily unavailable");
                    
                return "Payment processed successfully";
                
            }, "ProcessUserOrder", enrichedInfo =>
            {
                Console.WriteLine("Enriched exception information:");
                Console.WriteLine($"  Operation: {enrichedInfo.OperationName}");
                Console.WriteLine($"  Correlation ID: {enrichedInfo.Diagnostics.CorrelationId}");
                Console.WriteLine($"  Events: {enrichedInfo.Diagnostics.Events.Count}");
                
                foreach (var evt in enrichedInfo.Diagnostics.Events)
                {
                    Console.WriteLine($"    {evt.Timestamp:HH:mm:ss.fff} [{evt.EventType}] {evt.Message}");
                }
                
                Console.WriteLine($"  Environment: Process {enrichedInfo.EnvironmentInfo["ProcessId"]} on {enrichedInfo.EnvironmentInfo["MachineName"]}");
            });
        }
        catch (ExternalServiceException)
        {
            // Exception was handled and logged by the ExceptionHandler
        }

        Console.WriteLine();
    }

    static async Task DemonstrateExceptionTransformation()
    {
        Console.WriteLine("--- Exception Transformation ---");

        var exceptions = new Exception[]
        {
            new ArgumentException("Invalid email format", "email"),
            new InvalidOperationException("Cannot process order in current state"),
            new TimeoutException("Request timed out after 30 seconds"),
            new HttpRequestException("HTTP 404: Not Found"),
            new DivideByZeroException("Attempted to divide by zero")
        };

        foreach (var exception in exceptions)
        {
            var domainException = ExceptionTransformer.ToDomainException(exception, "UserService");
            Console.WriteLine($"  {exception.GetType().Name} -> {domainException.GetType().Name}");
            Console.WriteLine($"    Code: {domainException.ErrorCode}, Category: {domainException.ErrorCategory}");
        }

        // Demonstrate exception flattening
        var aggregateException = new AggregateException(
            new InvalidOperationException("First error"),
            new AggregateException(
                new ArgumentException("Nested error 1"),
                new TimeoutException("Nested error 2")
            ),
            new ExternalServiceException("TestService", "Third error")
        );

        Console.WriteLine("\nFlattened exception hierarchy:");
        var flattenedExceptions = ExceptionTransformer.FlattenExceptions(aggregateException);
        foreach (var ex in flattenedExceptions)
        {
            Console.WriteLine($"  - {ex.GetType().Name}: {ex.Message}");
        }

        Console.WriteLine();
    }

    static async Task DemonstrateExceptionAggregation()
    {
        Console.WriteLine("--- Exception Aggregation for Batch Operations ---");

        var aggregator = new ExceptionAggregator();
        var tasks = new List<Task>();

        // Simulate batch operations with some failures
        for (int i = 1; i <= 5; i++)
        {
            int index = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(50);
                    
                    if (index % 2 == 0) // Simulate failures on even numbers
                    {
                        throw new InvalidOperationException($"Batch item {index} failed");
                    }
                    
                    Console.WriteLine($"  Batch item {index} completed successfully");
                }
                catch (Exception ex)
                {
                    aggregator.Add(ex);
                    Console.WriteLine($"  Batch item {index} failed: {ex.Message}");
                }
            }));
        }

        await Task.WhenAll(tasks);

        Console.WriteLine($"\nBatch operation completed:");
        Console.WriteLine($"  Total exceptions: {aggregator.ExceptionCount}");
        
        if (aggregator.HasExceptions)
        {
            Console.WriteLine("  Failed items:");
            foreach (var ex in aggregator.Exceptions)
            {
                Console.WriteLine($"    - {ex.Message}");
            }
            
            // Could throw aggregated exceptions if needed
            // aggregator.ThrowIfAny();
        }

        Console.WriteLine();
    }

    static void DemonstrateStackTracePreservation()
    {
        Console.WriteLine("--- Stack Trace Preservation ---");

        try
        {
            ThrowNestedExceptions();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Original exception caught:");
            Console.WriteLine($"  Type: {ex.GetType().Name}");
            Console.WriteLine($"  Message: {ex.Message}");
            
            // Transform while preserving stack trace
            var transformed = ExceptionDispatchHelper.TransformException(
                ex, 
                originalEx => new BusinessRuleException(
                    "TransformedError",
                    $"Transformed: {originalEx.Message}",
                    originalEx));

            Console.WriteLine("\nTransformed exception:");
            Console.WriteLine($"  Type: {transformed.GetType().Name}");
            Console.WriteLine($"  Message: {transformed.Message}");
            Console.WriteLine($"  Has Inner Exception: {transformed.InnerException != null}");
            Console.WriteLine($"  Inner Exception Type: {transformed.InnerException?.GetType().Name}");
        }

        Console.WriteLine();
    }

    static void ThrowNestedExceptions()
    {
        try
        {
            DeepMethod();
        }
        catch (Exception ex)
        {
            // Re-throw with preserved stack trace
            ExceptionDispatchHelper.RethrowWithStackTrace(ex);
        }
    }

    static void DeepMethod()
    {
        throw new InvalidOperationException("This exception originated deep in the call stack");
    }
}