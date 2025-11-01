namespace Snippets.DesignPatterns.Behavioral.Observer;

public class EventBasedTrader(string name)
{
    public string Name { get; } = name;

    public void SubscribeToStock(EventBasedStockPrice stock)
    {
        stock.PriceChanged += OnPriceChanged;
        stock.SignificantPriceChange += OnSignificantPriceChange;
        stock.StockAlert += OnStockAlert;
    }

    public void UnsubscribeFromStock(EventBasedStockPrice stock)
    {
        stock.PriceChanged -= OnPriceChanged;
        stock.SignificantPriceChange -= OnSignificantPriceChange;
        stock.StockAlert -= OnStockAlert;
    }

    private void OnPriceChanged(object? sender, StockPriceEventArgs e)
    {
        Console.WriteLine($"[{Name}] {e.Symbol} price changed to ${e.NewPrice:F2} ({e.ChangePercent:+0.00;-0.00;0}%)");
    }

    private void OnSignificantPriceChange(object? sender, StockPriceEventArgs e)
    {
        Console.WriteLine($"[{Name}] 🚨 SIGNIFICANT CHANGE in {e.Symbol}: {e.ChangePercent:+0.00;-0.00;0}% change!");
    }

    private void OnStockAlert(object? sender, string message)
    {
        Console.WriteLine($"[{Name}] 📢 ALERT: {message}");
    }
}