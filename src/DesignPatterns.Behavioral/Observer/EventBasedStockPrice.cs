namespace Snippets.DesignPatterns.Behavioral.Observer;

public class EventBasedStockPrice
{
    private string symbol = "";
    private decimal price;

    // C# Events for type-safe observer notifications
    public event EventHandler<StockPriceEventArgs>? PriceChanged;
    public event EventHandler<StockPriceEventArgs>? SignificantPriceChange;
    public event EventHandler<string>? StockAlert;

    public string Symbol
    {
        get => symbol;
        set
        {
            symbol = value;
            OnStockAlert($"Stock symbol changed to {value}");
        }
    }

    public decimal Price
    {
        get => price;
        set
        {
            var previousPrice = price;
            price = value;
            var change = price - previousPrice;
            var changePercent = previousPrice != 0 ? (change / previousPrice) * 100 : 0;

            var eventArgs = new StockPriceEventArgs
            {
                Symbol = symbol,
                NewPrice = price,
                PreviousPrice = previousPrice,
                Change = change,
                ChangePercent = changePercent,
                Timestamp = DateTime.Now
            };

            OnPriceChanged(eventArgs);

            // Trigger significant change event if change is > 5%
            if (Math.Abs(changePercent) > 5)
            {
                OnSignificantPriceChange(eventArgs);
            }
        }
    }

    public EventBasedStockPrice(string symbol, decimal initialPrice)
    {
        this.symbol = symbol;
        price = initialPrice;
    }

    protected virtual void OnPriceChanged(StockPriceEventArgs e)
    {
        PriceChanged?.Invoke(this, e);
    }

    protected virtual void OnSignificantPriceChange(StockPriceEventArgs e)
    {
        SignificantPriceChange?.Invoke(this, e);
    }

    protected virtual void OnStockAlert(string message)
    {
        StockAlert?.Invoke(this, message);
    }
}