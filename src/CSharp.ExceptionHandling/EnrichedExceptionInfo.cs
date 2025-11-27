using System.Diagnostics;

namespace CSharp.ExceptionHandling;

/// <summary>
/// Rich exception information with diagnostic context.
/// </summary>
public record EnrichedExceptionInfo(
    Exception Exception,
    string OperationName,
    DiagnosticSummary Diagnostics,
    StackTrace StackTrace,
    Dictionary<string, object> EnvironmentInfo,
    DateTime CapturedAt);