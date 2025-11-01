namespace Snippets.DesignPatterns.Behavioral.Observer;

// Observer interface

// Subject interface

// Concrete Subject - Stock Price Monitor
public class StockPrice(string symbol, decimal initialPrice) : ISubject<StockPrice>
{
    private readonly List<IObserver<StockPrice>> observers = [];
    private string symbol = symbol;
    private decimal price = initialPrice;

    public string Symbol
    {
        get => symbol;
        set
        {
            symbol = value;
            NotifyObservers();
        }
    }

    public decimal Price
    {
        get => price;
        set
        {
            var previousPrice = price;
            price = value;
            LastUpdated = DateTime.Now;
            PriceChange = price - previousPrice;
            NotifyObservers();
        }
    }

    public decimal PriceChange { get; private set; }
    public DateTime LastUpdated { get; private set; } = DateTime.Now;

    public void Subscribe(IObserver<StockPrice> observer)
    {
        if (!observers.Contains(observer))
        {
            observers.Add(observer);
            Console.WriteLine($"{observer.Name} subscribed to {Symbol} stock updates");
        }
    }

    public void Unsubscribe(IObserver<StockPrice> observer)
    {
        if (observers.Remove(observer))
        {
            Console.WriteLine($"{observer.Name} unsubscribed from {Symbol} stock updates");
        }
    }

    public void NotifyObservers()
    {
        Console.WriteLine($"Notifying {observers.Count} observers about {Symbol} update");
        foreach (var observer in observers.ToList()) // ToList to avoid modification during iteration
        {
            observer.Update(this);
        }
    }
}

// Concrete Observers

// Event-based Observer Pattern (C# Events)

// Event subscribers

// Advanced Observer with Weak References to prevent memory leaks