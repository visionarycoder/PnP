namespace CSharp.ExceptionHandling;

/// <summary>
/// Represents a single validation error.
/// </summary>
public record ValidationError(
    string PropertyName,
    string ErrorMessage,
    object? AttemptedValue = null,
    string? ErrorCode = null);