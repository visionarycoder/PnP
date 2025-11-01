namespace Snippets.DesignPatterns.Behavioral.Strategy;

/// <summary>
/// Cryptocurrency payment strategy with network fee calculation
/// </summary>
public class CryptocurrencyPaymentStrategy(
    string walletAddress,
    CryptocurrencyPaymentStrategy.CryptoNetwork network = CryptocurrencyPaymentStrategy.CryptoNetwork.Bitcoin)
    : IPaymentStrategy
{
    private readonly string walletAddress = walletAddress ?? throw new ArgumentNullException(nameof(walletAddress));

    public string Name => $"Cryptocurrency ({network})";
    public bool SupportsRefunds => false; // Crypto transactions are irreversible

    public enum CryptoNetwork
    {
        Bitcoin,
        Ethereum,
        Litecoin,
        BitcoinCash
    }

    public decimal CalculateFee(decimal amount)
    {
        return network switch
        {
            CryptoNetwork.Bitcoin => Math.Max(amount * 0.001m, 5.0m), // Min $5 network fee
            CryptoNetwork.Ethereum => Math.Max(amount * 0.002m, 3.0m), // Higher gas fees
            CryptoNetwork.Litecoin => Math.Max(amount * 0.0005m, 1.0m), // Lower fees
            CryptoNetwork.BitcoinCash => Math.Max(amount * 0.0008m, 0.50m), // Lowest fees
            _ => amount * 0.001m
        };
    }

    public decimal GetDailyLimit() => 100000m; // High limit for crypto

    public async Task<PaymentResult> ProcessPaymentAsync(decimal amount, string merchantId,
        Dictionary<string, object> metadata)
    {
        Console.WriteLine($"₿ Processing {network} payment: ${amount:F2}");
        Console.WriteLine($"   Wallet: {walletAddress[..6]}...{walletAddress[^4..]}");

        // Simulate blockchain confirmation time
        await Task.Delay(2000);

        var fee = CalculateFee(amount);
        var transactionId = GenerateTransactionHash();

        // Simulate network confirmation (90% success rate)
        var success = new Random().NextDouble() > 0.10;
        var confirmations = success ? new Random().Next(1, 7) : 0;

        return new PaymentResult(
            success,
            success ? transactionId : string.Empty,
            success ? $"Transaction broadcast to {network} network" : "Transaction failed to broadcast",
            success ? amount : 0,
            success ? fee : 0,
            DateTime.UtcNow,
            new Dictionary<string, object>
            {
                ["network"] = network.ToString(),
                ["confirmations"] = confirmations,
                ["blockHeight"] = new Random().Next(700000, 800000),
                ["networkFee"] = fee
            });
    }

    private string GenerateTransactionHash()
    {
        var bytes = new byte[32];
        new Random().NextBytes(bytes);
        return Convert.ToHexString(bytes).ToLower();
    }
}