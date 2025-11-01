namespace Snippets.DesignPatterns.Behavioral.Observer;

public class StockAnalyst(string name) : IObserver<StockPrice>
{
    public string Name { get; } = name;
    private readonly List<decimal> priceHistory = [];

    public void Update(StockPrice stock)
    {
        priceHistory.Add(stock.Price);

        Console.WriteLine($"[{Name}] Analyzing {stock.Symbol}:");
        Console.WriteLine($"  Current Price: ${stock.Price:F2}");
        Console.WriteLine($"  Price Change: {stock.PriceChange:+0.00;-0.00;0}");

        if (priceHistory.Count >= 5)
        {
            var recent5 = priceHistory.TakeLast(5);
            var average = recent5.Average();
            var trend = stock.Price > average ? "📈 Upward" : "📉 Downward";
            Console.WriteLine($"  5-period average: ${average:F2}, Trend: {trend}");
        }
    }
}