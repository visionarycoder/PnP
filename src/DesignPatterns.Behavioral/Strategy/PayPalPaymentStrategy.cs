namespace Snippets.DesignPatterns.Behavioral.Strategy;

/// <summary>
/// PayPal payment strategy with account verification
/// </summary>
public class PayPalPaymentStrategy(string email, bool isVerified = true) : IPaymentStrategy
{
    private readonly string email = email ?? throw new ArgumentNullException(nameof(email));

    public string Name => "PayPal";
    public bool SupportsRefunds => true;

    public decimal CalculateFee(decimal amount)
    {
        // 3.49% + $0.49 for unverified, 2.9% + $0.30 for verified accounts
        return isVerified
            ? Math.Round(amount * 0.029m + 0.30m, 2)
            : Math.Round(amount * 0.0349m + 0.49m, 2);
    }

    public decimal GetDailyLimit() => isVerified ? 60000m : 2000m;

    public async Task<PaymentResult> ProcessPaymentAsync(decimal amount, string merchantId,
        Dictionary<string, object> metadata)
    {
        Console.WriteLine($"🏦 Processing PayPal payment: ${amount:F2}");
        Console.WriteLine($"   Account: {email} | Verified: {(isVerified ? "✅" : "❌")}");

        await Task.Delay(300);

        var fee = CalculateFee(amount);
        var transactionId = $"PP_{DateTime.UtcNow:yyyyMMdd}_{Guid.NewGuid().ToString("N")[..8]}";

        // Check account limits
        if (amount > GetDailyLimit())
        {
            return new PaymentResult(
                false,
                string.Empty,
                $"Amount exceeds daily limit of ${GetDailyLimit():F2}",
                0,
                0,
                DateTime.UtcNow);
        }

        // 97% success rate for verified accounts, 92% for unverified
        var successRate = isVerified ? 0.97 : 0.92;
        var success = new Random().NextDouble() < successRate;

        return new PaymentResult(
            success,
            success ? transactionId : string.Empty,
            success ? "PayPal payment completed" : "PayPal payment failed",
            success ? amount : 0,
            success ? fee : 0,
            DateTime.UtcNow,
            new Dictionary<string, object>
            {
                ["accountVerified"] = isVerified,
                ["paymentMethod"] = "PayPal Balance"
            });
    }
}