namespace Snippets.DesignPatterns.Behavioral.Strategy;

/// <summary>
/// Bank transfer payment strategy with processing delays
/// </summary>
public class BankTransferPaymentStrategy(string accountNumber, string routingNumber, string bankName)
    : IPaymentStrategy
{
    private readonly string accountNumber = accountNumber ?? throw new ArgumentNullException(nameof(accountNumber));
    private readonly string routingNumber = routingNumber ?? throw new ArgumentNullException(nameof(routingNumber));
    private readonly string bankName = bankName ?? throw new ArgumentNullException(nameof(bankName));

    public string Name => "Bank Transfer (ACH)";
    public bool SupportsRefunds => true;

    public decimal CalculateFee(decimal amount)
    {
        // Flat fee structure: free over $1000, $1.50 under $1000
        return amount >= 1000m ? 0m : 1.50m;
    }

    public decimal GetDailyLimit() => 25000m;

    public async Task<PaymentResult> ProcessPaymentAsync(decimal amount, string merchantId,
        Dictionary<string, object> metadata)
    {
        Console.WriteLine($"🏛️ Processing Bank Transfer: ${amount:F2}");
        Console.WriteLine($"   Bank: {bankName} | Account: ****{accountNumber[^4..]}");

        await Task.Delay(1000);

        var fee = CalculateFee(amount);
        var transactionId = $"ACH_{DateTime.UtcNow:yyyyMMdd}_{Guid.NewGuid().ToString("N")[..10]}";

        // Bank transfers have very high success rate (99.5%)
        var success = new Random().NextDouble() > 0.005;
        var processingDays = new Random().Next(1, 4); // 1-3 business days

        return new PaymentResult(
            success,
            success ? transactionId : string.Empty,
            success
                ? $"ACH transfer initiated (Processing: {processingDays} business days)"
                : "Bank declined transaction",
            success ? amount : 0,
            success ? fee : 0,
            DateTime.UtcNow,
            new Dictionary<string, object>
            {
                ["bankName"] = bankName,
                ["processingDays"] = processingDays,
                ["achType"] = amount > 25000 ? "Wire" : "Standard ACH"
            });
    }
}