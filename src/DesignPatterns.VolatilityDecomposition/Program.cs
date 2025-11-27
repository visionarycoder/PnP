namespace DesignPatterns.VolatilityDecomposition;

/// <summary>
/// Demonstrates volatility-based decomposition pattern where components are organized
/// by their rate of change rather than purely by functional boundaries.
/// </summary>
public class Program
{
    public static async Task Main()
    {
        Console.WriteLine("=== Volatility-Based Decomposition Pattern ===\n");

        // Example 1: E-Commerce Pricing System
        await DemonstratePricingSystemAsync();

        // Example 2: Rules Engine Pattern
        await DemonstrateRulesEngineAsync();

        // Example 3: Configuration-Driven System
        await DemonstrateConfigurationDrivenAsync();

        // Example 4: Plugin Architecture
        DemonstratePluginArchitecture();
    }

    private static async Task DemonstratePricingSystemAsync()
    {
        Console.WriteLine("Example 1: E-Commerce Pricing System");
        Console.WriteLine("=====================================\n");

        // Low volatility - Infrastructure setup (rarely changes)
        var strategyRegistry = new PricingStrategyRegistry();
        
        // High volatility - Business rules (change frequently)
        strategyRegistry.Register("premium", new PremiumPricingStrategy());
        strategyRegistry.Register("standard", new StandardPricingStrategy());
        strategyRegistry.Register("seasonal", new SeasonalPricingStrategy());
        
        // Medium volatility - Application service (changes periodically)
        var orderService = new OrderService(strategyRegistry);

        var orders = new[]
        {
            new Order("ORD001", 100m, "premium"),
            new Order("ORD002", 150m, "standard"),
            new Order("ORD003", 200m, "seasonal")
        };

        foreach (var order in orders)
        {
            var total = await orderService.CalculateTotalAsync(order);
            Console.WriteLine($"Order {order.Id} ({order.CustomerType}): Base=${order.BasePrice:F2}, Final=${total:F2}");
        }

        Console.WriteLine();
    }

    private static async Task DemonstrateRulesEngineAsync()
    {
        Console.WriteLine("Example 2: Rules Engine Pattern");
        Console.WriteLine("================================\n");

        // Low volatility - Rules engine infrastructure
        var rulesEngine = new RulesEngine<Order, decimal>();

        // High volatility - Rule definitions (added/modified frequently)
        rulesEngine.AddRule("volume-discount", order => 
            order.BasePrice > 500m ? order.BasePrice * 0.10m : 0m);
        
        rulesEngine.AddRule("loyalty-discount", order => 
            order.CustomerType == "premium" ? order.BasePrice * 0.05m : 0m);
        
        rulesEngine.AddRule("first-time-discount", order => 
            order.IsFirstOrder ? 20m : 0m);

        var order = new Order("ORD004", 600m, "premium") { IsFirstOrder = true };
        
        var discounts = await rulesEngine.EvaluateAllAsync(order);
        var totalDiscount = discounts.Values.Sum();

        Console.WriteLine($"Order: {order.Id}");
        Console.WriteLine($"Base Price: ${order.BasePrice:F2}");
        foreach (var (ruleName, discount) in discounts)
        {
            Console.WriteLine($"  {ruleName}: -${discount:F2}");
        }
        Console.WriteLine($"Total Discount: -${totalDiscount:F2}");
        Console.WriteLine($"Final Price: ${order.BasePrice - totalDiscount:F2}\n");
    }

    private static async Task DemonstrateConfigurationDrivenAsync()
    {
        Console.WriteLine("Example 3: Configuration-Driven System");
        Console.WriteLine("=======================================\n");

        // Low volatility - Configuration loader (stable infrastructure)
        var configLoader = new DiscountConfigurationLoader();

        // High volatility - Configuration content (changes frequently via external file)
        var config = await configLoader.LoadFromMemoryAsync(); // Simulates loading from file/database

        // Medium volatility - Application logic using configuration
        var discountService = new DiscountService(config);

        var customers = new[] { "premium", "standard", "basic" };
        foreach (var customerType in customers)
        {
            var discount = discountService.GetDiscount(customerType, 100m);
            Console.WriteLine($"{customerType} customer: {discount.Percentage}% discount = ${discount.Amount:F2}");
        }

        Console.WriteLine();
    }

    private static void DemonstratePluginArchitecture()
    {
        Console.WriteLine("Example 4: Plugin Architecture");
        Console.WriteLine("===============================\n");

        // Low volatility - Plugin host infrastructure
        var pluginHost = new PluginHost();

        // High volatility - Plugin implementations (can be added/removed dynamically)
        pluginHost.RegisterPlugin(new EmailNotificationPlugin());
        pluginHost.RegisterPlugin(new SmsNotificationPlugin());
        pluginHost.RegisterPlugin(new PushNotificationPlugin());

        // Medium volatility - Application using plugins
        var orderEvent = new OrderEvent("ORD005", "Order Confirmed");
        
        Console.WriteLine($"Processing event: {orderEvent.OrderId} - {orderEvent.Message}");
        pluginHost.ExecutePlugins(orderEvent);

        Console.WriteLine();
    }
}

// ============================================
// Low Volatility Components (Infrastructure)
// ============================================

/// <summary>
/// Low volatility: Registry infrastructure that rarely changes
/// </summary>
public class PricingStrategyRegistry
{
    private readonly Dictionary<string, IPricingStrategy> strategies = new();

    public void Register(string key, IPricingStrategy strategy) =>
        strategies[key] = strategy;

    public IPricingStrategy Get(string key) =>
        strategies.TryGetValue(key, out var strategy)
            ? strategy
            : throw new KeyNotFoundException($"Pricing strategy '{key}' not found");
}

/// <summary>
/// Low volatility: Generic rules engine infrastructure
/// </summary>
public class RulesEngine<TContext, TResult>
{
    private readonly Dictionary<string, Func<TContext, TResult>> rules = new();

    public void AddRule(string name, Func<TContext, TResult> rule) =>
        rules[name] = rule;

    public async Task<Dictionary<string, TResult>> EvaluateAllAsync(TContext context)
    {
        var results = new Dictionary<string, TResult>();
        
        foreach (var (name, rule) in rules)
        {
            // Simulate async evaluation
            await Task.Yield();
            results[name] = rule(context);
        }

        return results;
    }
}

/// <summary>
/// Low volatility: Configuration loading infrastructure
/// </summary>
public class DiscountConfigurationLoader
{
    public async Task<DiscountConfiguration> LoadFromMemoryAsync()
    {
        // Simulates loading from external source (file, database, API)
        await Task.Yield();
        
        return new DiscountConfiguration
        {
            Rules = new[]
            {
                new DiscountRule { CustomerType = "premium", Percentage = 15m },
                new DiscountRule { CustomerType = "standard", Percentage = 10m },
                new DiscountRule { CustomerType = "basic", Percentage = 5m }
            }
        };
    }
}

/// <summary>
/// Low volatility: Plugin host infrastructure
/// </summary>
public class PluginHost
{
    private readonly List<INotificationPlugin> plugins = new();

    public void RegisterPlugin(INotificationPlugin plugin) =>
        plugins.Add(plugin);

    public void ExecutePlugins(OrderEvent orderEvent)
    {
        foreach (var plugin in plugins)
        {
            plugin.Notify(orderEvent);
        }
    }
}

// =====================================================
// Stable Interfaces (Contract between volatility layers)
// =====================================================

public interface IPricingStrategy
{
    decimal Calculate(Order order);
}

public interface INotificationPlugin
{
    string Name { get; }
    void Notify(OrderEvent orderEvent);
}

// ============================================
// Medium Volatility Components (Application Services)
// ============================================

/// <summary>
/// Medium volatility: Application service that changes periodically
/// </summary>
public class OrderService(PricingStrategyRegistry strategyRegistry)
{
    public async Task<decimal> CalculateTotalAsync(Order order)
    {
        await Task.Yield(); // Simulate async work
        
        var strategy = strategyRegistry.Get(order.CustomerType);
        return strategy.Calculate(order);
    }
}

/// <summary>
/// Medium volatility: Service using configuration
/// </summary>
public class DiscountService(DiscountConfiguration configuration)
{
    public (decimal Percentage, decimal Amount) GetDiscount(string customerType, decimal amount)
    {
        var rule = configuration.Rules.FirstOrDefault(r => r.CustomerType == customerType);
        if (rule is null)
            return (0m, 0m);

        var discountAmount = amount * (rule.Percentage / 100m);
        return (rule.Percentage, discountAmount);
    }
}

// ============================================
// High Volatility Components (Business Rules)
// ============================================

/// <summary>
/// High volatility: Premium pricing changes frequently with promotions
/// </summary>
public class PremiumPricingStrategy : IPricingStrategy
{
    public decimal Calculate(Order order)
    {
        // Premium customers get 15% off (subject to change frequently)
        return order.BasePrice * 0.85m;
    }
}

/// <summary>
/// High volatility: Standard pricing adjusted for market conditions
/// </summary>
public class StandardPricingStrategy : IPricingStrategy
{
    public decimal Calculate(Order order)
    {
        // Standard customers get 10% off (changes with market conditions)
        return order.BasePrice * 0.90m;
    }
}

/// <summary>
/// High volatility: Seasonal pricing changes with calendar events
/// </summary>
public class SeasonalPricingStrategy : IPricingStrategy
{
    public decimal Calculate(Order order)
    {
        // Seasonal discount logic changes frequently
        var now = DateTime.Now;
        var isHolidaySeason = now.Month == 11 || now.Month == 12;
        var discount = isHolidaySeason ? 0.20m : 0.05m;
        
        return order.BasePrice * (1 - discount);
    }
}

/// <summary>
/// High volatility: Email templates and rules change frequently
/// </summary>
public class EmailNotificationPlugin : INotificationPlugin
{
    public string Name => "Email";

    public void Notify(OrderEvent orderEvent)
    {
        // Email template and rules change frequently
        Console.WriteLine($"  [{Name}] Sending email for {orderEvent.OrderId}: {orderEvent.Message}");
    }
}

/// <summary>
/// High volatility: SMS formatting and providers change
/// </summary>
public class SmsNotificationPlugin : INotificationPlugin
{
    public string Name => "SMS";

    public void Notify(OrderEvent orderEvent)
    {
        // SMS provider and format change frequently
        Console.WriteLine($"  [{Name}] Sending SMS for {orderEvent.OrderId}: {orderEvent.Message}");
    }
}

/// <summary>
/// High volatility: Push notification platforms evolve rapidly
/// </summary>
public class PushNotificationPlugin : INotificationPlugin
{
    public string Name => "Push";

    public void Notify(OrderEvent orderEvent)
    {
        // Push notification logic changes with platform updates
        Console.WriteLine($"  [{Name}] Sending push for {orderEvent.OrderId}: {orderEvent.Message}");
    }
}

// ============================================
// Domain Models (Shared across volatility layers)
// ============================================

public record Order(string Id, decimal BasePrice, string CustomerType)
{
    public bool IsFirstOrder { get; init; }
}

public record OrderEvent(string OrderId, string Message);

public class DiscountConfiguration
{
    public DiscountRule[] Rules { get; init; } = Array.Empty<DiscountRule>();
}

public class DiscountRule
{
    public string CustomerType { get; init; } = string.Empty;
    public decimal Percentage { get; init; }
}
