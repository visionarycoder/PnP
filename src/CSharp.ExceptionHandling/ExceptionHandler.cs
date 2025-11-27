using System.Diagnostics;

namespace CSharp.ExceptionHandling;

/// <summary>
/// Exception handler that captures rich diagnostic information.
/// </summary>
public static class ExceptionHandler
{
    /// <summary>
    /// Handles an exception with rich diagnostic information collection.
    /// </summary>
    public static EnrichedExceptionInfo HandleException(Exception exception, string? operationName = null)
    {
        var diagnostics = DiagnosticContext.Current?.GetSummary() ?? CreateEmptyDiagnostics();
        var stackTrace = new StackTrace(exception, true);
        var environmentInfo = CollectEnvironmentInfo();

        return new EnrichedExceptionInfo(
            exception,
            operationName ?? "Unknown Operation",
            diagnostics,
            stackTrace,
            environmentInfo,
            DateTime.UtcNow);
    }

    /// <summary>
    /// Executes an operation with automatic exception handling and diagnostics.
    /// </summary>
    public static async Task<T> HandleAsync<T>(
        Func<Task<T>> operation, 
        string operationName,
        Action<EnrichedExceptionInfo>? onError = null)
    {
        using var context = new DiagnosticContext(operationName);
        
        try
        {
            context.AddEvent("OperationStarted", $"Starting operation: {operationName}");
            var result = await operation();
            context.AddEvent("OperationCompleted", $"Operation completed successfully: {operationName}");
            return result;
        }
        catch (Exception ex)
        {
            context.AddEvent("OperationFailed", $"Operation failed: {operationName}", ex);
            var enrichedInfo = HandleException(ex, operationName);
            onError?.Invoke(enrichedInfo);
            throw;
        }
    }

    private static DiagnosticSummary CreateEmptyDiagnostics()
    {
        return new DiagnosticSummary(
            Guid.NewGuid().ToString(),
            DateTime.UtcNow,
            new Dictionary<string, object>(),
            new List<DiagnosticEvent>());
    }

    private static Dictionary<string, object> CollectEnvironmentInfo()
    {
        return new Dictionary<string, object>
        {
            ["MachineName"] = Environment.MachineName,
            ["ProcessId"] = Environment.ProcessId,
            ["ThreadId"] = Environment.CurrentManagedThreadId,
            ["WorkingSet"] = Environment.WorkingSet,
            ["TickCount"] = Environment.TickCount64,
            ["OSVersion"] = Environment.OSVersion.ToString(),
            ["CLRVersion"] = Environment.Version.ToString()
        };
    }
}