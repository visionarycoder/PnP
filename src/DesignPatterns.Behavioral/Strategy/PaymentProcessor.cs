namespace Snippets.DesignPatterns.Behavioral.Strategy;

/// <summary>
/// Payment processor context that uses different payment strategies
/// </summary>
public class PaymentProcessor
{
    private readonly List<PaymentResult> transactionHistory = [];

    public async Task<PaymentResult> ProcessPaymentAsync(
        IPaymentStrategy strategy,
        decimal amount,
        string merchantId = "MERCHANT_001",
        Dictionary<string, object>? metadata = null)
    {
        if (strategy == null)
            throw new ArgumentNullException(nameof(strategy));

        if (amount <= 0)
            throw new ArgumentException("Amount must be positive", nameof(amount));

        Console.WriteLine($"\n💰 Processing payment of ${amount:F2} using {strategy.Name}");
        Console.WriteLine($"📊 Fee: ${strategy.CalculateFee(amount):F2} | Daily Limit: ${strategy.GetDailyLimit():F2}");
        Console.WriteLine($"🔄 Refunds Supported: {(strategy.SupportsRefunds ? "Yes" : "No")}");

        var result =
            await strategy.ProcessPaymentAsync(amount, merchantId, metadata ?? new Dictionary<string, object>());

        transactionHistory.Add(result);

        Console.WriteLine($"📋 Result: {(result.Success ? "✅ SUCCESS" : "❌ FAILED")}");
        Console.WriteLine($"💬 Message: {result.Message}");
        if (result.Success)
        {
            Console.WriteLine($"🆔 Transaction ID: {result.TransactionId}");
        }

        return result;
    }

    public void CompareStrategies(decimal amount, params IPaymentStrategy[] strategies)
    {
        Console.WriteLine($"\n📊 Payment Method Comparison for ${amount:F2}");
        Console.WriteLine(new string('=', 80));
        Console.WriteLine($"{"Method",-25} {"Fee",-10} {"Total",-12} {"Refunds",-10} {"Daily Limit",-15}");
        Console.WriteLine(new string('-', 80));

        foreach (var strategy in strategies)
        {
            var fee = strategy.CalculateFee(amount);
            var total = amount + fee;
            var refunds = strategy.SupportsRefunds ? "Yes" : "No";
            var limit = strategy.GetDailyLimit();

            Console.WriteLine($"{strategy.Name,-25} ${fee:F2}".PadRight(35) +
                              $"${total:F2}".PadRight(12) +
                              $"{refunds}".PadRight(10) +
                              $"${limit:F0}".PadRight(15));
        }
    }

    public void PrintTransactionHistory()
    {
        if (!transactionHistory.Any())
        {
            Console.WriteLine("No transactions recorded.");
            return;
        }

        Console.WriteLine($"\n📈 Transaction History ({transactionHistory.Count} transactions)");
        Console.WriteLine(new string('=', 90));

        var successful = transactionHistory.Count(t => t.Success);
        var totalProcessed = transactionHistory.Where(t => t.Success).Sum(t => t.ProcessedAmount);
        var totalFees = transactionHistory.Where(t => t.Success).Sum(t => t.Fee);

        Console.WriteLine($"✅ Successful: {successful}/{transactionHistory.Count}");
        Console.WriteLine($"💰 Total Processed: ${totalProcessed:F2}");
        Console.WriteLine($"💸 Total Fees: ${totalFees:F2}");
        Console.WriteLine($"📊 Average Fee Rate: {(totalProcessed > 0 ? (totalFees / totalProcessed * 100) : 0):F2}%");
    }
}