# Volatility-Based Decomposition Pattern

## Overview

**Description**: Organize software components based on their rate of change to minimize ripple effects and improve maintainability.

**Language/Technology**: C# 12 / .NET 8.0+

**Category**: Architectural Pattern

**Related Patterns**: Strategy Pattern, Plugin Architecture, Rules Engine, Dependency Inversion

## Intent

Separate components that change for different reasons or at different rates. By grouping code with similar volatility characteristics and isolating components with different volatility rates through stable interfaces, we can:

- Reduce the blast radius of changes
- Enable faster deployment of high-volatility components
- Improve system stability
- Support independent team ownership
- Lower maintenance costs over time

## Volatility Categories

### High Volatility

**Changes frequently** (daily/weekly/monthly)

- Business rules and policies
- UI/UX implementations
- Promotional logic
- A/B testing features
- Pricing algorithms

### Medium Volatility

**Changes periodically** (quarterly/semi-annually)

- Application workflows
- Integration adapters
- Reporting logic
- Configuration management

### Low Volatility

**Rarely changes** (annually or less)

- Infrastructure code
- Framework libraries
- Data access patterns
- Security primitives
- Logging infrastructure

## Implementation

### Example 1: E-Commerce Pricing System

```csharp
// Low volatility - Stable infrastructure
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

// Stable interface - Contract between layers
public interface IPricingStrategy
{
    decimal Calculate(Order order);
}

// High volatility - Business rules change frequently
public class PremiumPricingStrategy : IPricingStrategy
{
    public decimal Calculate(Order order)
    {
        // Premium pricing rules change with market conditions
        return order.BasePrice * 0.85m; // 15% discount
    }
}

public class SeasonalPricingStrategy : IPricingStrategy
{
    public decimal Calculate(Order order)
    {
        // Seasonal rules change throughout the year
        var now = DateTime.Now;
        var isHolidaySeason = now.Month == 11 || now.Month == 12;
        var discount = isHolidaySeason ? 0.20m : 0.05m;
        
        return order.BasePrice * (1 - discount);
    }
}

// Medium volatility - Application service changes periodically
public class OrderService(PricingStrategyRegistry strategyRegistry)
{
    public async Task<decimal> CalculateTotalAsync(Order order)
    {
        var strategy = strategyRegistry.Get(order.CustomerType);
        return strategy.Calculate(order);
    }
}
```

### Example 2: Rules Engine Pattern

```csharp
// Low volatility - Generic rules engine infrastructure
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
            await Task.Yield(); // Simulate async evaluation
            results[name] = rule(context);
        }

        return results;
    }
}

// High volatility - Rule definitions change frequently
var rulesEngine = new RulesEngine<Order, decimal>();

// Business rules that change often
rulesEngine.AddRule("volume-discount", order => 
    order.BasePrice > 500m ? order.BasePrice * 0.10m : 0m);

rulesEngine.AddRule("loyalty-discount", order => 
    order.CustomerType == "premium" ? order.BasePrice * 0.05m : 0m);

rulesEngine.AddRule("first-time-discount", order => 
    order.IsFirstOrder ? 20m : 0m);

// Evaluate all rules
var order = new Order("ORD001", 600m, "premium") { IsFirstOrder = true };
var discounts = await rulesEngine.EvaluateAllAsync(order);
```

### Example 3: Plugin Architecture

```csharp
// Low volatility - Plugin host infrastructure
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

// Stable interface
public interface INotificationPlugin
{
    string Name { get; }
    void Notify(OrderEvent orderEvent);
}

// High volatility - Plugin implementations can be added/changed frequently
public class EmailNotificationPlugin : INotificationPlugin
{
    public string Name => "Email";

    public void Notify(OrderEvent orderEvent)
    {
        // Email template and delivery rules change frequently
        Console.WriteLine($"[Email] Notifying: {orderEvent.Message}");
    }
}

public class SmsNotificationPlugin : INotificationPlugin
{
    public string Name => "SMS";

    public void Notify(OrderEvent orderEvent)
    {
        // SMS provider and formatting change frequently
        Console.WriteLine($"[SMS] Notifying: {orderEvent.Message}");
    }
}

// Register and use plugins
var pluginHost = new PluginHost();
pluginHost.RegisterPlugin(new EmailNotificationPlugin());
pluginHost.RegisterPlugin(new SmsNotificationPlugin());

var orderEvent = new OrderEvent("ORD001", "Order Confirmed");
pluginHost.ExecutePlugins(orderEvent);
```

### Example 4: Configuration-Driven System

```csharp
// Low volatility - Configuration loader infrastructure
public class DiscountConfigurationLoader
{
    public async Task<DiscountConfiguration> LoadAsync(string path)
    {
        var json = await File.ReadAllTextAsync(path);
        return JsonSerializer.Deserialize<DiscountConfiguration>(json)!;
    }
}

// Configuration structure (medium volatility)
public class DiscountConfiguration
{
    public DiscountRule[] Rules { get; init; } = Array.Empty<DiscountRule>();
}

public class DiscountRule
{
    public string CustomerType { get; init; } = string.Empty;
    public decimal Percentage { get; init; }
}

// High volatility - Configuration content (external JSON file)
// discount-config.json:
// {
//   "rules": [
//     { "customerType": "premium", "percentage": 15 },
//     { "customerType": "standard", "percentage": 10 },
//     { "customerType": "basic", "percentage": 5 }
//   ]
// }

// Medium volatility - Application service
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
```

## Usage

### Basic Workflow

```csharp
// 1. Setup low volatility infrastructure
var strategyRegistry = new PricingStrategyRegistry();

// 2. Register high volatility business rules
strategyRegistry.Register("premium", new PremiumPricingStrategy());
strategyRegistry.Register("standard", new StandardPricingStrategy());
strategyRegistry.Register("seasonal", new SeasonalPricingStrategy());

// 3. Create medium volatility application service
var orderService = new OrderService(strategyRegistry);

// 4. Use the service
var order = new Order("ORD001", 100m, "premium");
var total = await orderService.CalculateTotalAsync(order);

Console.WriteLine($"Order total: ${total:F2}");
```

**Expected Output:**

```text
Order total: $85.00
```

### Rules Engine Workflow

```csharp
// Setup rules engine (low volatility)
var rulesEngine = new RulesEngine<Order, decimal>();

// Add business rules (high volatility - can be changed frequently)
rulesEngine.AddRule("volume-discount", order => 
    order.BasePrice > 500m ? order.BasePrice * 0.10m : 0m);

rulesEngine.AddRule("loyalty-discount", order => 
    order.CustomerType == "premium" ? order.BasePrice * 0.05m : 0m);

// Evaluate rules
var order = new Order("ORD001", 600m, "premium");
var discounts = await rulesEngine.EvaluateAllAsync(order);

foreach (var (ruleName, discount) in discounts)
{
    Console.WriteLine($"{ruleName}: ${discount:F2}");
}
```

**Expected Output:**

```text
volume-discount: $60.00
loyalty-discount: $30.00
```

## Key Concepts

### Dependency Direction

Dependencies should flow from high volatility → medium volatility → low volatility:

- High volatility components depend on medium/low volatility abstractions
- Medium volatility components depend on low volatility abstractions
- Low volatility components have minimal dependencies

### Isolation Strategies

1. **Stable Interfaces**: Use interfaces to isolate volatile implementations
2. **Strategy Pattern**: Encapsulate changing algorithms
3. **Plugin Architecture**: Allow dynamic addition/removal of functionality
4. **Configuration-Driven**: Externalize volatile rules
5. **Rules Engine**: Separate rule execution from rule definitions

## When to Use

**Good Fit:**

- Systems with frequent business rule changes
- Regulatory environments requiring rapid updates
- A/B testing and experimentation-heavy applications
- Multi-tenant systems with custom business logic
- Long-lived systems (5+ years expected lifespan)

**Poor Fit:**

- Small applications with uniform change rates
- Prototypes and proof-of-concepts
- Systems with purely technical volatility
- Applications with < 2 years expected lifespan

## Benefits

1. **Faster Time-to-Market**: High volatility features can be changed independently
2. **Reduced Risk**: Changes isolated to specific layers
3. **Better Team Autonomy**: Clear ownership boundaries
4. **Improved Stability**: Infrastructure remains stable while business rules evolve
5. **Lower Maintenance Costs**: Less coupling reduces technical debt

## Common Pitfalls

### Anti-Pattern 1: Mixed Volatility

```csharp
// ❌ BAD: High and low volatility mixed
public class OrderProcessor
{
    private readonly IDbConnection connection; // Low volatility
    
    private decimal CalculateDiscount(Order order) // High volatility
    {
        return order.Season == Season.Holiday ? 0.20m : 0.0m;
    }
    
    public async Task ProcessAsync(Order order) { } // Medium volatility
}
```

### Anti-Pattern 2: Volatility Inversion

```csharp
// ❌ BAD: Low volatility depending on high volatility
namespace Core.Infrastructure;

public class DatabaseContext
{
    // Infrastructure should not depend on volatile business rules
    public void ApplyDiscount(Order order, PremiumDiscountStrategy strategy) { }
}

// ✅ GOOD: Low volatility depends on stable abstraction
namespace Core.Infrastructure;

public class DatabaseContext
{
    public void ApplyDiscount(Order order, IDiscountStrategy strategy) { }
}
```

### Anti-Pattern 3: Premature Abstraction

Creating complex layers before understanding actual volatility patterns.

## Testing Strategies

### High Volatility Components

- Extensive unit tests (frequent changes require quick feedback)
- Property-based testing for business rules
- A/B testing in production

### Medium Volatility Components

- Integration tests (verify component interactions)
- Contract tests (ensure interface stability)

### Low Volatility Components

- Minimal unit tests (focus on edge cases)
- Performance tests
- Security tests

## Related Patterns

- **Strategy Pattern**: Encapsulates volatile algorithms
- **Dependency Inversion**: Stable abstractions between layers
- **Plugin Architecture**: Dynamic component loading
- **Rules Engine**: Separates rule execution from definitions
- **Template Method**: Provides stable structure with volatile steps

## Notes

- Measure actual change frequency before decomposing
- Use stable interfaces to abstract volatile implementations
- High volatility components need independent versioning
- Match testing strategy to volatility level
- Document expected change rates explicitly
- Review volatility patterns regularly
- Align team structure with volatility levels
- Automate deployment for high volatility components

## Performance Considerations

- **Plugin Architecture**: Small overhead from indirection
- **Rules Engine**: Evaluation cost scales with rule count
- **Configuration-Driven**: I/O cost for loading configuration
- **Strategy Pattern**: Negligible overhead from interface dispatch

## Thread Safety

- Ensure immutability in high volatility components
- Use thread-safe collections for registries
- Consider concurrent rule evaluation
- Document thread safety guarantees in interfaces

## References

- **Component Design Principles** (Robert C. Martin)
- **Domain-Driven Design** (Eric Evans)
- **Building Evolutionary Architectures** (Ford, Parsons, Kua)
- **Software Architecture: The Hard Parts** (Ford, Richards, Sadalage, Dehghani)
- **Accelerate** (Forsgren, Humble, Kim)

## Related Snippets

- [Strategy Pattern](strategy-pattern.md)
- [Dependency Inversion](dependency-inversion.md)
- [Plugin Architecture](plugin-architecture.md)
- [Rules Engine](rules-engine.md)
- [Configuration Management](configuration-management.md)
