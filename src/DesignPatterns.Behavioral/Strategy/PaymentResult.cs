namespace Snippets.DesignPatterns.Behavioral.Strategy;

/// <summary>
/// Strategy Pattern Implementation
/// Defines a family of algorithms, encapsulates each one, and makes them interchangeable.
/// The algorithm can vary independently from clients that use it.
/// </summary>

#region Payment Processing Strategy

/// <summary>
/// Payment result with detailed information
/// </summary>
public record PaymentResult(
    bool Success,
    string TransactionId,
    string Message,
    decimal ProcessedAmount,
    decimal Fee,
    DateTime ProcessedAt,
    Dictionary<string, object>? Metadata = null);

#endregion

#region Sorting Strategy

#endregion

#region Compression Strategy

#endregion

#region Data Validation Strategy

#endregion