# Observer Pattern

**Description**: Defines a one-to-many dependency between objects so that when one object changes state, all its dependents are notified automatically. Also known as the Publish-Subscribe pattern.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Linq;

// Observer interface
public interface IObserver<T>
{
    void Update(T data);
    string Name { get; }
}

// Subject interface
public interface ISubject<T>
{
    void Subscribe(IObserver<T> observer);
    void Unsubscribe(IObserver<T> observer);
    void NotifyObservers();
}

// Concrete Subject - Stock Price Monitor
public class StockPrice : ISubject<StockPrice>
{
    private readonly List<IObserver<StockPrice>> _observers = new();
    private string _symbol;
    private decimal _price;
    private DateTime _lastUpdated;
    
    public string Symbol 
    { 
        get => _symbol; 
        set 
        { 
            _symbol = value; 
            NotifyObservers(); 
        } 
    }
    
    public decimal Price 
    { 
        get => _price; 
        set 
        { 
            var previousPrice = _price;
            _price = value; 
            _lastUpdated = DateTime.Now;
            PriceChange = _price - previousPrice;
            NotifyObservers(); 
        } 
    }
    
    public decimal PriceChange { get; private set; }
    public DateTime LastUpdated => _lastUpdated;
    
    public StockPrice(string symbol, decimal initialPrice)
    {
        _symbol = symbol;
        _price = initialPrice;
        _lastUpdated = DateTime.Now;
    }
    
    public void Subscribe(IObserver<StockPrice> observer)
    {
        if (!_observers.Contains(observer))
        {
            _observers.Add(observer);
            Console.WriteLine($"{observer.Name} subscribed to {Symbol} stock updates");
        }
    }
    
    public void Unsubscribe(IObserver<StockPrice> observer)
    {
        if (_observers.Remove(observer))
        {
            Console.WriteLine($"{observer.Name} unsubscribed from {Symbol} stock updates");
        }
    }
    
    public void NotifyObservers()
    {
        Console.WriteLine($"Notifying {_observers.Count} observers about {Symbol} update");
        foreach (var observer in _observers.ToList()) // ToList to avoid modification during iteration
        {
            observer.Update(this);
        }
    }
}

// Concrete Observers
public class StockTrader : IObserver<StockPrice>
{
    public string Name { get; }
    private readonly decimal _buyThreshold;
    private readonly decimal _sellThreshold;
    
    public StockTrader(string name, decimal buyThreshold, decimal sellThreshold)
    {
        Name = name;
        _buyThreshold = buyThreshold;
        _sellThreshold = sellThreshold;
    }
    
    public void Update(StockPrice stock)
    {
        Console.WriteLine($"[{Name}] Stock {stock.Symbol} updated: ${stock.Price:F2} (Change: {stock.PriceChange:+0.00;-0.00;0})");
        
        if (stock.Price <= _buyThreshold)
        {
            Console.WriteLine($"[{Name}] 📈 BUY signal for {stock.Symbol} at ${stock.Price:F2}");
        }
        else if (stock.Price >= _sellThreshold)
        {
            Console.WriteLine($"[{Name}] 📉 SELL signal for {stock.Symbol} at ${stock.Price:F2}");
        }
    }
}

public class StockAnalyst : IObserver<StockPrice>
{
    public string Name { get; }
    private readonly List<decimal> _priceHistory = new();
    
    public StockAnalyst(string name)
    {
        Name = name;
    }
    
    public void Update(StockPrice stock)
    {
        _priceHistory.Add(stock.Price);
        
        Console.WriteLine($"[{Name}] Analyzing {stock.Symbol}:");
        Console.WriteLine($"  Current Price: ${stock.Price:F2}");
        Console.WriteLine($"  Price Change: {stock.PriceChange:+0.00;-0.00;0}");
        
        if (_priceHistory.Count >= 5)
        {
            var recent5 = _priceHistory.TakeLast(5);
            var average = recent5.Average();
            var trend = stock.Price > average ? "📈 Upward" : "📉 Downward";
            Console.WriteLine($"  5-period average: ${average:F2}, Trend: {trend}");
        }
    }
}

public class StockPortfolio : IObserver<StockPrice>
{
    public string Name { get; }
    private readonly Dictionary<string, int> _holdings = new();
    private decimal _totalValue;
    
    public StockPortfolio(string name)
    {
        Name = name;
    }
    
    public void AddHolding(string symbol, int shares)
    {
        _holdings[symbol] = _holdings.GetValueOrDefault(symbol) + shares;
    }
    
    public void Update(StockPrice stock)
    {
        if (_holdings.TryGetValue(stock.Symbol, out var shares))
        {
            var holdingValue = shares * stock.Price;
            var previousValue = shares * (stock.Price - stock.PriceChange);
            var changeInValue = holdingValue - previousValue;
            
            Console.WriteLine($"[{Name}] Portfolio update for {stock.Symbol}:");
            Console.WriteLine($"  Shares: {shares}, Current Value: ${holdingValue:F2}");
            Console.WriteLine($"  Change in Value: {changeInValue:+$0.00;-$0.00;$0}");
        }
    }
}

// Event-based Observer Pattern using C# events
public class NewsService
{
    public event Action<string, string> NewsPublished;
    
    public string ServiceName { get; }
    
    public NewsService(string serviceName)
    {
        ServiceName = serviceName;
    }
    
    public void PublishNews(string headline, string content)
    {
        Console.WriteLine($"\n📰 {ServiceName} publishing: {headline}");
        NewsPublished?.Invoke(headline, content);
    }
}

public class NewsSubscriber
{
    public string Name { get; }
    private readonly List<string> _keywords;
    
    public NewsSubscriber(string name, params string[] keywords)
    {
        Name = name;
        _keywords = keywords.ToList();
    }
    
    public void OnNewsPublished(string headline, string content)
    {
        // Check if news contains any of our keywords
        var isRelevant = _keywords.Any(keyword => 
            headline.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
            content.Contains(keyword, StringComparison.OrdinalIgnoreCase));
            
        if (isRelevant)
        {
            Console.WriteLine($"[{Name}] 🔔 Relevant news: {headline}");
        }
        else
        {
            Console.WriteLine($"[{Name}] 📄 News received: {headline}");
        }
    }
}

// Generic Observable class for any type
public class Observable<T> : ISubject<T>
{
    private readonly List<IObserver<T>> _observers = new();
    private T _value;
    
    public T Value
    {
        get => _value;
        set
        {
            _value = value;
            NotifyObservers();
        }
    }
    
    public void Subscribe(IObserver<T> observer)
    {
        _observers.Add(observer);
    }
    
    public void Unsubscribe(IObserver<T> observer)
    {
        _observers.Remove(observer);
    }
    
    public void NotifyObservers()
    {
        foreach (var observer in _observers.ToList())
        {
            observer.Update(_value);
        }
    }
}

// Simple observer for the generic observable
public class GenericObserver<T> : IObserver<T>
{
    public string Name { get; }
    private readonly Action<T> _onUpdate;
    
    public GenericObserver(string name, Action<T> onUpdate)
    {
        Name = name;
        _onUpdate = onUpdate;
    }
    
    public void Update(T data)
    {
        _onUpdate(data);
    }
}
```

**Usage**:

```csharp
using System;
using System.Threading;

class Program
{
    static void Main()
    {
        Console.WriteLine("=== Stock Market Observer Pattern ===");
        
        // Create stock
        var appleStock = new StockPrice("AAPL", 150.00m);
        
        // Create observers
        var dayTrader = new StockTrader("Day Trader", 145.00m, 160.00m);
        var swingTrader = new StockTrader("Swing Trader", 140.00m, 165.00m);
        var analyst = new StockAnalyst("Market Analyst");
        var portfolio = new StockPortfolio("Retirement Portfolio");
        
        // Add holdings to portfolio
        portfolio.AddHolding("AAPL", 100);
        
        // Subscribe observers
        appleStock.Subscribe(dayTrader);
        appleStock.Subscribe(swingTrader);
        appleStock.Subscribe(analyst);
        appleStock.Subscribe(portfolio);
        
        // Simulate price changes
        Console.WriteLine("\n--- Price Updates ---");
        appleStock.Price = 152.50m; // Small increase
        Thread.Sleep(1000);
        
        appleStock.Price = 148.75m; // Decrease
        Thread.Sleep(1000);
        
        appleStock.Price = 144.00m; // Hit buy threshold
        Thread.Sleep(1000);
        
        appleStock.Price = 161.25m; // Hit sell threshold
        Thread.Sleep(1000);
        
        // Unsubscribe an observer
        Console.WriteLine("\n--- Unsubscribing Day Trader ---");
        appleStock.Unsubscribe(dayTrader);
        
        appleStock.Price = 155.00m; // Day trader won't be notified
        
        Console.WriteLine("\n=== Event-Based Observer Pattern ===");
        
        // Create news service and subscribers
        var newsService = new NewsService("Financial Times");
        var stockSubscriber = new NewsSubscriber("Stock Investor", "stock", "market", "trading");
        var techSubscriber = new NewsSubscriber("Tech Enthusiast", "technology", "AI", "software");
        var generalSubscriber = new NewsSubscriber("General Reader");
        
        // Subscribe to news events
        newsService.NewsPublished += stockSubscriber.OnNewsPublished;
        newsService.NewsPublished += techSubscriber.OnNewsPublished;
        newsService.NewsPublished += generalSubscriber.OnNewsPublished;
        
        // Publish news
        newsService.PublishNews("Stock Market Reaches All-Time High", "The stock market hit record levels today...");
        newsService.PublishNews("New AI Technology Breakthrough", "Scientists develop advanced AI system...");
        newsService.PublishNews("Weather Update for Tomorrow", "Sunny skies expected tomorrow...");
        
        Console.WriteLine("\n=== Generic Observable Pattern ===");
        
        // Generic observable example
        var temperatureSensor = new Observable<double>();
        
        var thermostat = new GenericObserver<double>("Smart Thermostat", 
            temp => Console.WriteLine($"[Thermostat] Temperature: {temp}°C - {(temp > 25 ? "Cooling ON" : "Cooling OFF")}"));
        
        var weatherStation = new GenericObserver<double>("Weather Station",
            temp => Console.WriteLine($"[Weather Station] Recording temperature: {temp}°C"));
        
        temperatureSensor.Subscribe(thermostat);
        temperatureSensor.Subscribe(weatherStation);
        
        // Simulate temperature changes
        temperatureSensor.Value = 22.5;
        temperatureSensor.Value = 26.8;
        temperatureSensor.Value = 24.1;
        
        Console.WriteLine("\n=== Pattern Benefits Demonstration ===");
        
        // Show how easy it is to add new observers
        var newTrader = new StockTrader("Algorithmic Trader", 149.00m, 158.00m);
        appleStock.Subscribe(newTrader);
        
        Console.WriteLine("Added new algorithmic trader:");
        appleStock.Price = 151.50m; // All observers (including new one) get notified
    }
}
```

**Notes**:

- Implements loose coupling between subject and observers
- Supports broadcast communication (one-to-many relationship)
- Dynamic subscription/unsubscription of observers
- C# events provide built-in observer pattern support
- Generic Observable&lt;T&gt; class can be used for any data type
- Thread-safe considerations needed for multi-threaded environments
- Can lead to memory leaks if observers aren't properly unsubscribed
- Performance impact with many observers (consider async notifications for heavy processing)
- Related patterns: [Mediator](mediator.md), [Command](command.md), [Chain of Responsibility](chain-of-responsibility.md)

**Prerequisites**:

- .NET Framework 2.0+ or .NET Core
- Understanding of events and delegates in C#
- Knowledge of Action and Func delegates

**References**:

- Gang of Four Design Patterns book
- [Microsoft Docs: Events](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/events/)

---

*Created: 2025-10-31*  
*Last Updated: 2025-10-31*  
*Tags: #observer #behavioral #events #publish-subscribe*
