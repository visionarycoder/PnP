namespace Snippets.DesignPatterns.Behavioral.Strategy;

/// <summary>
/// Payment strategy interface defining common payment operations
/// </summary>
public interface IPaymentStrategy
{
    string Name { get; }
    decimal CalculateFee(decimal amount);
    Task<PaymentResult> ProcessPaymentAsync(decimal amount, string merchantId, Dictionary<string, object> metadata);
    bool SupportsRefunds { get; }
    decimal GetDailyLimit();
}