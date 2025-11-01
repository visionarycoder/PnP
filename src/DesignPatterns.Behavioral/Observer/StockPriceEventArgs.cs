namespace Snippets.DesignPatterns.Behavioral.Observer;

public class StockPriceEventArgs : EventArgs
{
    public string Symbol { get; init; } = "";
    public decimal NewPrice { get; init; }
    public decimal PreviousPrice { get; init; }
    public decimal Change { get; init; }
    public decimal ChangePercent { get; init; }
    public DateTime Timestamp { get; init; }
}