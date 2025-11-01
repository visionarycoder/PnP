namespace Snippets.DesignPatterns.Behavioral.Observer;

public class StockTrader(string name, decimal buyThreshold, decimal sellThreshold) : IObserver<StockPrice>
{
    public string Name { get; } = name;

    public void Update(StockPrice stock)
    {
        Console.WriteLine(
            $"[{Name}] Stock {stock.Symbol} updated: ${stock.Price:F2} (Change: {stock.PriceChange:+0.00;-0.00;0})");

        if (stock.Price <= buyThreshold)
        {
            Console.WriteLine($"[{Name}] 📈 BUY signal for {stock.Symbol} at ${stock.Price:F2}");
        }
        else if (stock.Price >= sellThreshold)
        {
            Console.WriteLine($"[{Name}] 📉 SELL signal for {stock.Symbol} at ${stock.Price:F2}");
        }
    }
}