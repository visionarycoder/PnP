namespace CSharp.ExceptionHandling;

/// <summary>
/// Summary of diagnostic information for an operation.
/// </summary>
public record DiagnosticSummary(
    string CorrelationId,
    DateTime CreatedAt,
    Dictionary<string, object> Data,
    IReadOnlyList<DiagnosticEvent> Events);