namespace CSharp.ExceptionHandling;

/// <summary>
/// Context manager for maintaining diagnostic information across operation chains.
/// </summary>
public class DiagnosticContext : IDisposable
{
    private static readonly AsyncLocal<DiagnosticContext?> current = new();
    private readonly DiagnosticCollector collector;
    private readonly DiagnosticContext? parent;

    public static DiagnosticContext? Current => current.Value;

    public DiagnosticContext(string? operationName = null)
    {
        collector = new DiagnosticCollector();
        parent = current.Value;
        current.Value = this;

        if (!string.IsNullOrEmpty(operationName))
        {
            collector.AddData("OperationName", operationName);
        }

        collector.AddEvent("ContextCreated", $"Diagnostic context created for operation: {operationName}");
    }

    public void AddData(string key, object value) => collector.AddData(key, value);
    public void AddEvent(string eventType, string message, Exception? exception = null) => collector.AddEvent(eventType, message, exception);
    public DiagnosticSummary GetSummary() => collector.CreateSummary();

    public void Dispose()
    {
        collector.AddEvent("ContextDisposed", "Diagnostic context disposed");
        current.Value = parent;
    }
}