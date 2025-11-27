namespace CSharp.ExceptionHandling;

/// <summary>
/// Represents a diagnostic event with timestamp and context.
/// </summary>
public record DiagnosticEvent(
    string EventType,
    string Message,
    Exception? Exception,
    DateTime Timestamp);