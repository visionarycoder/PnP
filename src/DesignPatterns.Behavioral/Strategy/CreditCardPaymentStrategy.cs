namespace Snippets.DesignPatterns.Behavioral.Strategy;

/// <summary>
/// Credit card payment strategy with fraud detection
/// </summary>
public class CreditCardPaymentStrategy(string cardNumber, string cardholderName, string expiryDate, string cvv)
    : IPaymentStrategy
{
    private readonly string cardNumber = cardNumber ?? throw new ArgumentNullException(nameof(cardNumber));
    private readonly string cardholderName = cardholderName ?? throw new ArgumentNullException(nameof(cardholderName));
    private readonly string expiryDate = expiryDate ?? throw new ArgumentNullException(nameof(expiryDate));
    private readonly string cvv = cvv ?? throw new ArgumentNullException(nameof(cvv));

    public string Name => "Credit Card";
    public bool SupportsRefunds => true;

    public decimal CalculateFee(decimal amount)
    {
        // 2.9% + $0.30 per transaction (typical credit card fee)
        return Math.Round(amount * 0.029m + 0.30m, 2);
    }

    public decimal GetDailyLimit() => 10000m;

    public async Task<PaymentResult> ProcessPaymentAsync(decimal amount, string merchantId,
        Dictionary<string, object> metadata)
    {
        Console.WriteLine($"💳 Processing Credit Card payment: ${amount:F2}");
        Console.WriteLine($"   Card: ****{cardNumber[^4..]} | Cardholder: {cardholderName}");

        // Simulate processing delay
        await Task.Delay(500);

        // Fraud detection simulation
        var fraudScore = CalculateFraudScore(amount, metadata);
        if (fraudScore > 75)
        {
            return new PaymentResult(
                false,
                string.Empty,
                "Transaction declined due to potential fraud",
                0,
                0,
                DateTime.UtcNow,
                new Dictionary<string, object> { ["fraudScore"] = fraudScore });
        }

        var fee = CalculateFee(amount);
        var transactionId = $"CC_{DateTime.UtcNow:yyyyMMdd}_{Guid.NewGuid().ToString("N")[..8]}";

        // Simulate success/failure (95% success rate)
        var success = new Random().NextDouble() > 0.05;

        return new PaymentResult(
            success,
            success ? transactionId : string.Empty,
            success ? "Payment processed successfully" : "Card declined by issuer",
            success ? amount : 0,
            success ? fee : 0,
            DateTime.UtcNow,
            new Dictionary<string, object>
            {
                ["cardType"] = GetCardType(cardNumber),
                ["fraudScore"] = fraudScore,
                ["processorResponse"] = success ? "APPROVED" : "DECLINED"
            });
    }

    private int CalculateFraudScore(decimal amount, Dictionary<string, object> metadata)
    {
        var score = 0;

        // High amount increases fraud score
        if (amount > 5000) score += 30;
        else if (amount > 1000) score += 15;

        // Multiple transactions in short time
        if (metadata?.ContainsKey("recentTransactionCount") == true &&
            (int)metadata["recentTransactionCount"] > 3)
        {
            score += 25;
        }

        // International transaction
        if (metadata?.ContainsKey("isInternational") == true &&
            (bool)metadata["isInternational"])
        {
            score += 20;
        }

        return Math.Min(score, 100);
    }

    private string GetCardType(string cardNumber)
    {
        return cardNumber[0] switch
        {
            '4' => "Visa",
            '5' => "Mastercard",
            '3' => "American Express",
            _ => "Unknown"
        };
    }
}