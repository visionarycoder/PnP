# Orleans Grain Fundamentals

**Description**: Essential Orleans grain patterns covering basic grain concepts, lifecycle management, interfaces, and foundational development patterns for virtual actor systems.

**Language/Technology**: C#, Orleans, .NET 9.0

**Code**:

## Table of Contents

1. [Grain Concepts Overview](#grain-concepts-overview)
2. [Grain Interfaces and Implementation](#grain-interfaces-and-implementation)
3. [Grain Lifecycle Management](#grain-lifecycle-management)
4. [Grain Identity and Addressing](#grain-identity-and-addressing)
5. [Communication Patterns](#communication-patterns)
6. [Grain Activation and Deactivation](#grain-activation-and-deactivation)
7. [State Management Basics](#state-management-basics)
8. [Error Handling Fundamentals](#error-handling-fundamentals)

## Grain Concepts Overview

### What are Grains?

Grains are virtual actors that represent individual entities in your application. Each grain has a unique identity and maintains its own state. Orleans automatically manages grain activation, placement, and lifecycle.

```csharp
namespace DocumentProcessor.Orleans.Concepts;

using Orleans;
using Microsoft.Extensions.Logging;

// A grain represents a single document in the system
// Each document has a unique ID and can be processed independently
public interface IDocumentGrain : IGrain
{
    // Grains communicate through async methods
    Task<DocumentStatus> GetStatusAsync();
    Task ProcessAsync(DocumentContent content);
    Task<DocumentMetadata> GetMetadataAsync();
}

public class DocumentGrain : Grain, IDocumentGrain
{
    private readonly ILogger<DocumentGrain> logger;
    private DocumentState state = new();

    // Orleans handles dependency injection automatically
    public DocumentGrain(ILogger<DocumentGrain> logger)
    {
        this.logger = logger;
    }

    public Task<DocumentStatus> GetStatusAsync()
    {
        // Each grain instance is single-threaded
        // No need for locks or synchronization
        logger.LogInformation("Getting status for document {DocumentId}", 
            this.GetPrimaryKeyString());
        
        return Task.FromResult(state.Status);
    }

    public async Task ProcessAsync(DocumentContent content)
    {
        // Grain methods are naturally async
        logger.LogInformation("Processing document {DocumentId}", 
            this.GetPrimaryKeyString());
        
        state.Status = DocumentStatus.Processing;
        state.Content = content;
        state.ProcessedAt = DateTimeOffset.UtcNow;
        
        // Simulate processing work
        await Task.Delay(TimeSpan.FromMilliseconds(100));
        
        state.Status = DocumentStatus.Completed;
    }

    public Task<DocumentMetadata> GetMetadataAsync()
    {
        var metadata = new DocumentMetadata
        {
            DocumentId = this.GetPrimaryKeyString(),
            Status = state.Status,
            ProcessedAt = state.ProcessedAt,
            ContentLength = state.Content?.Data?.Length ?? 0
        };
        
        return Task.FromResult(metadata);
    }
}

// Supporting types for the grain
public record DocumentContent
{
    public string FileName { get; init; } = string.Empty;
    public byte[] Data { get; init; } = Array.Empty<byte>();
    public string ContentType { get; init; } = string.Empty;
}

public record DocumentMetadata
{
    public string DocumentId { get; init; } = string.Empty;
    public DocumentStatus Status { get; init; }
    public DateTimeOffset? ProcessedAt { get; init; }
    public int ContentLength { get; init; }
}

public enum DocumentStatus
{
    Created,
    Processing,
    Completed,
    Failed
}

// Internal grain state (not exposed through interface)
internal class DocumentState
{
    public DocumentStatus Status { get; set; } = DocumentStatus.Created;
    public DocumentContent? Content { get; set; }
    public DateTimeOffset? ProcessedAt { get; set; }
}
```

### Core Grain Characteristics

Orleans grains provide four fundamental characteristics that make distributed applications easier to build:

```csharp
namespace DocumentProcessor.Orleans.Characteristics;

using Orleans;

// 1. IDENTITY-BASED ADDRESSING
// Each grain has a unique identity that never changes
public interface IUserSessionGrain : IGrain
{
    Task<UserSession> GetSessionAsync();
    Task UpdateLastActivityAsync();
}

public class UserSessionGrain : Grain, IUserSessionGrain
{
    private UserSession session = new();

    public Task<UserSession> GetSessionAsync()
    {
        // The grain's identity comes from its key
        // This grain represents user session for a specific user ID
        var userId = this.GetPrimaryKeyString();
        
        session.UserId = userId;
        session.GrainId = this.GetGrainId().ToString();
        
        return Task.FromResult(session);
    }

    public Task UpdateLastActivityAsync()
    {
        session.LastActivity = DateTimeOffset.UtcNow;
        return Task.CompletedTask;
    }
}

// 2. SINGLE-THREADED EXECUTION
// Orleans guarantees that only one thread executes grain code at a time
public interface ICounterGrain : IGrain
{
    Task<int> IncrementAsync();
    Task<int> GetValueAsync();
    Task ResetAsync();
}

public class CounterGrain : Grain, ICounterGrain
{
    private int counter = 0;

    public Task<int> IncrementAsync()
    {
        // No locks needed - Orleans ensures thread safety
        // Multiple concurrent calls will be serialized automatically
        counter++;
        return Task.FromResult(counter);
    }

    public Task<int> GetValueAsync()
    {
        // Safe to read without synchronization
        return Task.FromResult(counter);
    }

    public Task ResetAsync()
    {
        counter = 0;
        return Task.CompletedTask;
    }
}

// 3. PERSISTENT STATE CAPABILITIES
// Grains can maintain state that survives deactivation/reactivation
public interface IShoppingCartGrain : IGrain
{
    Task AddItemAsync(CartItem item);
    Task<List<CartItem>> GetItemsAsync();
    Task<decimal> GetTotalAsync();
    Task ClearAsync();
}

public class ShoppingCartGrain : Grain, IShoppingCartGrain
{
    private readonly List<CartItem> items = new();
    
    public Task AddItemAsync(CartItem item)
    {
        // State is maintained in memory
        // Can be persisted using Orleans state management
        items.Add(item);
        return Task.CompletedTask;
    }

    public Task<List<CartItem>> GetItemsAsync()
    {
        return Task.FromResult(new List<CartItem>(items));
    }

    public Task<decimal> GetTotalAsync()
    {
        var total = items.Sum(item => item.Price * item.Quantity);
        return Task.FromResult(total);
    }

    public Task ClearAsync()
    {
        items.Clear();
        return Task.CompletedTask;
    }
}

// 4. NETWORK TRANSPARENCY
// Clients and other grains interact without knowing physical location
public interface IDocumentProcessorClient
{
    Task ProcessDocumentAsync(string documentId, DocumentContent content);
    Task<DocumentStatus> CheckStatusAsync(string documentId);
}

public class DocumentProcessorClient : IDocumentProcessorClient
{
    private readonly IGrainFactory grainFactory;

    public DocumentProcessorClient(IGrainFactory grainFactory)
    {
        this.grainFactory = grainFactory;
    }

    public async Task ProcessDocumentAsync(string documentId, DocumentContent content)
    {
        // Get a reference to the grain - Orleans handles location
        var documentGrain = grainFactory.GetGrain<IDocumentGrain>(documentId);
        
        // Call appears local but may cross network boundaries
        await documentGrain.ProcessAsync(content);
    }

    public async Task<DocumentStatus> CheckStatusAsync(string documentId)
    {
        // Same grain reference pattern
        var documentGrain = grainFactory.GetGrain<IDocumentGrain>(documentId);
        
        // Orleans routes the call to the correct silo
        return await documentGrain.GetStatusAsync();
    }
}

// Supporting types
public record UserSession
{
    public string UserId { get; set; } = string.Empty;
    public string GrainId { get; set; } = string.Empty;
    public DateTimeOffset LastActivity { get; set; } = DateTimeOffset.UtcNow;
    public bool IsActive => DateTimeOffset.UtcNow - LastActivity < TimeSpan.FromMinutes(30);
}

public record CartItem
{
    public string ProductId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int Quantity { get; init; }
}
```

### Grain Factory and Client Access Patterns

```csharp
namespace DocumentProcessor.Orleans.ClientPatterns;

using Orleans;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Orleans client configuration and usage patterns
public class OrleansClientService : BackgroundService
{
    private readonly IClusterClient orleansClient;
    private readonly ILogger<OrleansClientService> logger;

    public OrleansClientService(
        IClusterClient orleansClient, 
        ILogger<OrleansClientService> logger)
    {
        this.orleansClient = orleansClient;
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Example of using grains from a client application
        await DemonstrateGrainUsageAsync();
    }

    private async Task DemonstrateGrainUsageAsync()
    {
        try
        {
            // 1. Getting grain references by string key
            var documentGrain = orleansClient.GetGrain<IDocumentGrain>("document-123");
            var userGrain = orleansClient.GetGrain<IUserSessionGrain>("user-456");
            
            // 2. Getting grain references by GUID key
            var sessionId = Guid.NewGuid();
            var sessionGrain = orleansClient.GetGrain<IUserSessionGrain>(sessionId);
            
            // 3. Calling grain methods
            var status = await documentGrain.GetStatusAsync();
            logger.LogInformation("Document status: {Status}", status);
            
            // 4. Chaining grain calls
            if (status == DocumentStatus.Created)
            {
                var content = new DocumentContent 
                { 
                    FileName = "example.pdf",
                    Data = new byte[1024],
                    ContentType = "application/pdf"
                };
                
                await documentGrain.ProcessAsync(content);
                
                // Check status after processing
                var newStatus = await documentGrain.GetStatusAsync();
                logger.LogInformation("Updated status: {Status}", newStatus);
            }
            
            // 5. Working with multiple grains
            var tasks = new List<Task>();
            
            for (int i = 0; i < 10; i++)
            {
                var grain = orleansClient.GetGrain<ICounterGrain>($"counter-{i}");
                tasks.Add(grain.IncrementAsync());
            }
            
            await Task.WhenAll(tasks);
            logger.LogInformation("Incremented 10 counters concurrently");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error demonstrating grain usage");
        }
    }
}

// Dependency injection setup for Orleans client
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddOrleansClient(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOrleansClient(builder =>
        {
            builder.UseLocalhostClustering()
                   .ConfigureLogging(logging => logging.AddConsole());
        });
        
        services.AddHostedService<OrleansClientService>();
        
        return services;
    }
}
```

## Grain Interfaces and Implementation

### Defining Grain Interfaces

Orleans grains communicate through interfaces that must inherit from `IGrain`. These interfaces define the contract for grain interactions and support method versioning and evolution.

```csharp
namespace OrderProcessing.Orleans.Interfaces;

using Orleans;

// All grain interfaces must inherit from IGrain
public interface IOrderGrain : IGrain
{
    // All methods must return Task or Task<T> for async operation
    Task<Order> GetOrderAsync();
    Task UpdateStatusAsync(OrderStatus status);
    Task<decimal> CalculateTotalAsync();
    
    // ValueTask is also supported for performance-critical scenarios
    ValueTask<bool> IsValidAsync();
    
    // Methods can accept complex parameters
    Task AddLineItemAsync(OrderLineItem item);
    Task<List<OrderLineItem>> GetLineItemsAsync();
    
    // Support for cancellation tokens
    Task ProcessPaymentAsync(PaymentDetails payment, CancellationToken cancellationToken = default);
}

// Interface versioning pattern for backward compatibility
public interface IOrderGrainV2 : IOrderGrain
{
    // New methods in V2 without breaking existing clients
    Task<OrderSummary> GetOrderSummaryAsync();
    Task ApplyDiscountAsync(DiscountCode discount);
    Task<ShippingInfo> CalculateShippingAsync(Address address);
}

// Specialized interfaces using interface segregation principle
public interface IOrderPaymentGrain : IGrain
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
    Task<PaymentStatus> GetPaymentStatusAsync();
    Task RefundAsync(decimal amount, string reason);
}

public interface IOrderShippingGrain : IGrain
{
    Task<ShippingQuote> GetShippingQuoteAsync(Address destination);
    Task<TrackingInfo> GetTrackingInfoAsync();
    Task UpdateShippingAddressAsync(Address newAddress);
}

// Observer pattern for grain-to-grain notifications
public interface IOrderStatusObserver : IGrainObserver
{
    Task OnOrderStatusChangedAsync(string orderId, OrderStatus oldStatus, OrderStatus newStatus);
    Task OnPaymentProcessedAsync(string orderId, PaymentResult result);
}

// Interface with different key types
public interface ICustomerGrain : IGrain
{
    Task<Customer> GetCustomerDetailsAsync();
    Task UpdateEmailAsync(string email);
    Task<List<string>> GetOrderHistoryAsync();
}

// Supporting types for interfaces
public record Order
{
    public string OrderId { get; init; } = string.Empty;
    public string CustomerId { get; init; } = string.Empty;
    public OrderStatus Status { get; init; }
    public decimal Total { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public List<OrderLineItem> LineItems { get; init; } = new();
}

public record OrderLineItem
{
    public string ProductId { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
    public decimal LineTotal => Quantity * UnitPrice;
}

public record PaymentDetails
{
    public string PaymentMethodId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public string Currency { get; init; } = "USD";
}

public record OrderSummary
{
    public string OrderId { get; init; } = string.Empty;
    public OrderStatus Status { get; init; }
    public decimal Total { get; init; }
    public int ItemCount { get; init; }
    public DateTimeOffset LastUpdated { get; init; }
}

public enum OrderStatus
{
    Created,
    PaymentPending,
    PaymentProcessed,
    Shipped,
    Delivered,
    Cancelled
}
```

### Basic Grain Implementation

Orleans grains inherit from the `Grain` base class and implement their corresponding interfaces. Modern C# patterns with dependency injection make grain implementation clean and testable.

```csharp
namespace OrderProcessing.Orleans.Grains;

using Orleans;
using Microsoft.Extensions.Logging;
using OrderProcessing.Orleans.Interfaces;

// Basic grain implementation with dependency injection
public class OrderGrain : Grain, IOrderGrain, IOrderGrainV2
{
    private readonly ILogger<OrderGrain> logger;
    private readonly IPaymentService paymentService;
    private readonly IInventoryService inventoryService;
    
    // In-memory state (consider persistent state for production)
    private Order? order;
    private readonly List<OrderLineItem> lineItems = new();

    // Constructor with dependency injection - Orleans handles this automatically
    public OrderGrain(
        ILogger<OrderGrain> logger,
        IPaymentService paymentService,
        IInventoryService inventoryService)
    {
        this.logger = logger;
        this.paymentService = paymentService;
        this.inventoryService = inventoryService;
    }

    // Grain activation hook - called when grain becomes active
    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var orderId = this.GetPrimaryKeyString();
        logger.LogInformation("Activating OrderGrain for order {OrderId}", orderId);
        
        // Initialize grain state
        order ??= new Order
        {
            OrderId = orderId,
            Status = OrderStatus.Created,
            CreatedAt = DateTimeOffset.UtcNow,
            LineItems = lineItems
        };
        
        return base.OnActivateAsync(cancellationToken);
    }

    public Task<Order> GetOrderAsync()
    {
        // Null-safety with modern C# patterns
        ArgumentNullException.ThrowIfNull(order);
        
        // Update line items from current state
        var currentOrder = order with { LineItems = new List<OrderLineItem>(lineItems) };
        return Task.FromResult(currentOrder);
    }

    public Task UpdateStatusAsync(OrderStatus status)
    {
        if (order is null)
            throw new InvalidOperationException("Order not initialized");

        var oldStatus = order.Status;
        order = order with { Status = status };
        
        logger.LogInformation(
            "Order {OrderId} status changed from {OldStatus} to {NewStatus}",
            order.OrderId, oldStatus, status);
        
        return Task.CompletedTask;
    }

    public Task<decimal> CalculateTotalAsync()
    {
        var total = lineItems.Sum(item => item.LineTotal);
        
        if (order is not null)
        {
            order = order with { Total = total };
        }
        
        return Task.FromResult(total);
    }

    public ValueTask<bool> IsValidAsync()
    {
        // ValueTask for synchronous operations that might be async
        var isValid = order is not null && 
                     lineItems.Count > 0 && 
                     lineItems.All(item => item.Quantity > 0);
        
        return ValueTask.FromResult(isValid);
    }

    public async Task AddLineItemAsync(OrderLineItem item)
    {
        ArgumentNullException.ThrowIfNull(item);
        
        // Check inventory before adding item
        var available = await inventoryService.CheckAvailabilityAsync(
            item.ProductId, item.Quantity);
        
        if (!available)
        {
            throw new InvalidOperationException(
                $"Insufficient inventory for product {item.ProductId}");
        }
        
        lineItems.Add(item);
        
        logger.LogInformation(
            "Added line item: {ProductName} x{Quantity} to order {OrderId}",
            item.ProductName, item.Quantity, order?.OrderId);
    }

    public Task<List<OrderLineItem>> GetLineItemsAsync()
    {
        return Task.FromResult(new List<OrderLineItem>(lineItems));
    }

    public async Task ProcessPaymentAsync(
        PaymentDetails payment, 
        CancellationToken cancellationToken = default)
    {
        if (order is null)
            throw new InvalidOperationException("Order not initialized");

        try
        {
            await UpdateStatusAsync(OrderStatus.PaymentPending);
            
            // Use injected payment service
            var result = await paymentService.ProcessPaymentAsync(
                payment, cancellationToken);
            
            if (result.IsSuccessful)
            {
                await UpdateStatusAsync(OrderStatus.PaymentProcessed);
                logger.LogInformation("Payment processed for order {OrderId}", order.OrderId);
            }
            else
            {
                logger.LogWarning(
                    "Payment failed for order {OrderId}: {Error}",
                    order.OrderId, result.ErrorMessage);
                throw new PaymentException(result.ErrorMessage);
            }
        }
        catch (Exception ex) when (ex is not PaymentException)
        {
            logger.LogError(ex, "Error processing payment for order {OrderId}", order.OrderId);
            throw;
        }
    }

    // IOrderGrainV2 methods - new functionality
    public async Task<OrderSummary> GetOrderSummaryAsync()
    {
        if (order is null)
            throw new InvalidOperationException("Order not initialized");

        var total = await CalculateTotalAsync();
        
        return new OrderSummary
        {
            OrderId = order.OrderId,
            Status = order.Status,
            Total = total,
            ItemCount = lineItems.Count,
            LastUpdated = DateTimeOffset.UtcNow
        };
    }

    public Task ApplyDiscountAsync(DiscountCode discount)
    {
        // Implementation for discount functionality
        logger.LogInformation(
            "Applying discount {Code} to order {OrderId}",
            discount.Code, order?.OrderId);
        
        return Task.CompletedTask;
    }

    public Task<ShippingInfo> CalculateShippingAsync(Address address)
    {
        // Calculate shipping based on address and order contents
        var shippingInfo = new ShippingInfo
        {
            Address = address,
            EstimatedCost = CalculateShippingCost(address),
            EstimatedDelivery = DateTimeOffset.UtcNow.AddDays(3)
        };
        
        return Task.FromResult(shippingInfo);
    }

    private decimal CalculateShippingCost(Address address)
    {
        // Simple shipping calculation logic
        return address.Country == "US" ? 5.99m : 15.99m;
    }

    // Grain deactivation hook
    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Deactivating OrderGrain {OrderId} due to {Reason}",
            order?.OrderId, reason);
        
        return base.OnDeactivateAsync(reason, cancellationToken);
    }
}

// Supporting service interfaces
public interface IPaymentService
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentDetails payment, CancellationToken cancellationToken);
}

public interface IInventoryService
{
    Task<bool> CheckAvailabilityAsync(string productId, int quantity);
}

// Supporting types
public record PaymentResult
{
    public bool IsSuccessful { get; init; }
    public string TransactionId { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
}

public record DiscountCode
{
    public string Code { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public bool IsPercentage { get; init; }
}

public record Address
{
    public string Street { get; init; } = string.Empty;
    public string City { get; init; } = string.Empty;
    public string State { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public string Country { get; init; } = string.Empty;
}

public record ShippingInfo
{
    public Address Address { get; init; } = new();
    public decimal EstimatedCost { get; init; }
    public DateTimeOffset EstimatedDelivery { get; init; }
}

public record Customer
{
    public string CustomerId { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public Address Address { get; init; } = new();
}

public record PaymentRequest
{
    public string OrderId { get; init; } = string.Empty;
    public decimal Amount { get; init; }
    public PaymentDetails PaymentDetails { get; init; } = new();
}

public record TrackingInfo
{
    public string TrackingNumber { get; init; } = string.Empty;
    public string Carrier { get; init; } = string.Empty;
    public DateTimeOffset LastUpdated { get; init; }
    public string Status { get; init; } = string.Empty;
}

public record ShippingQuote
{
    public decimal Cost { get; init; }
    public TimeSpan EstimatedDelivery { get; init; }
    public string ServiceType { get; init; } = string.Empty;
}

public enum PaymentStatus
{
    Pending,
    Authorized,
    Captured,
    Failed,
    Refunded
}

public class PaymentException : Exception
{
    public PaymentException(string message) : base(message) { }
    public PaymentException(string message, Exception innerException) : base(message, innerException) { }
}
```

### Grain Interface Best Practices

Follow these patterns for designing maintainable and efficient grain interfaces:

```csharp
namespace OrderProcessing.Orleans.BestPractices;

using Orleans;

// ✅ GOOD: Interface segregation - focused responsibilities
public interface IOrderValidationGrain : IGrain
{
    Task<ValidationResult> ValidateOrderAsync(Order order);
    Task<bool> IsCustomerEligibleAsync(string customerId);
}

public interface IOrderPricingGrain : IGrain
{
    Task<PricingResult> CalculatePricingAsync(List<OrderLineItem> items);
    Task<decimal> ApplyDiscountsAsync(decimal basePrice, List<DiscountCode> discounts);
}

// ❌ AVOID: God interface - too many responsibilities
public interface IBadOrderGrain : IGrain
{
    Task<Order> GetOrderAsync();
    Task ValidateOrderAsync();
    Task CalculatePricingAsync();
    Task ProcessPaymentAsync();
    Task HandleShippingAsync();
    Task SendNotificationsAsync();
    Task GenerateReportAsync();
    // ... many more methods
}

// ✅ GOOD: Consistent async patterns
public interface ICustomerPreferencesGrain : IGrain
{
    // All methods return Task or Task<T>
    Task<CustomerPreferences> GetPreferencesAsync();
    Task UpdateEmailPreferenceAsync(bool enabled);
    Task SetLanguageAsync(string languageCode);
    
    // Use CancellationToken for long-running operations
    Task<RecommendationList> GenerateRecommendationsAsync(CancellationToken cancellationToken = default);
}

// ✅ GOOD: Proper method naming - verb-based, descriptive
public interface IInventoryGrain : IGrain
{
    // Clear action verbs
    Task<int> GetAvailableQuantityAsync(string productId);
    Task ReserveInventoryAsync(string productId, int quantity);
    Task ReleaseInventoryAsync(string productId, int quantity);
    Task UpdateStockLevelAsync(string productId, int newLevel);
    
    // Query methods with "Get", "Check", "Is" prefixes
    Task<bool> IsProductAvailableAsync(string productId);
    Task<StockStatus> CheckStockStatusAsync(string productId);
}

// ✅ GOOD: Parameter objects for complex data
public interface IOrderReportingGrain : IGrain
{
    // Use record types for parameter objects
    Task<SalesReport> GenerateSalesReportAsync(ReportCriteria criteria);
    Task<CustomerAnalytics> AnalyzeCustomerBehaviorAsync(AnalyticsCriteria criteria);
}

// ✅ GOOD: Versioned interfaces for evolution
public interface IProductCatalogGrain : IGrain
{
    Task<Product> GetProductAsync(string productId);
    Task<List<Product>> SearchProductsAsync(string query);
}

public interface IProductCatalogGrainV2 : IProductCatalogGrain
{
    // V2 adds new functionality without breaking V1
    Task<ProductDetails> GetProductDetailsAsync(string productId);
    Task<List<Product>> GetRecommendedProductsAsync(string customerId);
    Task<ProductAvailability> CheckAvailabilityAsync(string productId, string locationId);
}

// ✅ GOOD: Observer pattern for notifications
public interface IOrderNotificationGrain : IGrain
{
    Task SubscribeToOrderUpdatesAsync(IOrderStatusObserver observer);
    Task UnsubscribeFromOrderUpdatesAsync(IOrderStatusObserver observer);
    Task NotifyOrderStatusChangeAsync(string orderId, OrderStatus status);
}

// ✅ GOOD: Immutable data transfer objects
public record ValidationResult
{
    public bool IsValid { get; init; }
    public List<ValidationError> Errors { get; init; } = new();
    public DateTimeOffset ValidatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public record ValidationError
{
    public string Field { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
}

public record PricingResult
{
    public decimal BasePrice { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal FinalPrice { get; init; }
    public List<PriceBreakdown> Breakdown { get; init; } = new();
}

public record ReportCriteria
{
    public DateTimeOffset StartDate { get; init; }
    public DateTimeOffset EndDate { get; init; }
    public string? CustomerId { get; init; }
    public List<string> ProductCategories { get; init; } = new();
}

public record CustomerPreferences
{
    public string CustomerId { get; init; } = string.Empty;
    public bool EmailNotificationsEnabled { get; init; } = true;
    public string PreferredLanguage { get; init; } = "en-US";
    public string PreferredCurrency { get; init; } = "USD";
    public List<string> InterestCategories { get; init; } = new();
}

public enum ValidationSeverity
{
    Warning,
    Error,
    Critical
}

// ✅ GOOD: Exception types for domain-specific errors
public class OrderValidationException : Exception
{
    public ValidationResult ValidationResult { get; }
    
    public OrderValidationException(ValidationResult result) 
        : base($"Order validation failed: {result.Errors.Count} errors")
    {
        ValidationResult = result;
    }
}

public class InsufficientInventoryException : Exception
{
    public string ProductId { get; }
    public int RequestedQuantity { get; }
    public int AvailableQuantity { get; }
    
    public InsufficientInventoryException(string productId, int requested, int available)
        : base($"Insufficient inventory for product {productId}: requested {requested}, available {available}")
    {
        ProductId = productId;
        RequestedQuantity = requested;
        AvailableQuantity = available;
    }
}
```

## Grain Lifecycle Management

Orleans automatically manages grain lifecycle, but you can hook into activation and deactivation events to perform initialization, cleanup, and resource management.

### Grain Activation Process

Grain activation occurs when Orleans first routes a call to a grain instance. Use `OnActivateAsync` to initialize state, acquire resources, and set up dependencies.

```csharp
namespace DocumentProcessor.Orleans.Lifecycle;

using Orleans;
using Microsoft.Extensions.Logging;

// Comprehensive activation example with resource management
public class DocumentProcessorGrain : Grain, IDocumentProcessorGrain
{
    private readonly ILogger<DocumentProcessorGrain> logger;
    private readonly IFileStorageService fileStorage;
    private readonly INotificationService notifications;
    private readonly IMetricsCollector metrics;
    
    // Lifecycle tracking
    private DateTimeOffset activationTime;
    private string grainId = string.Empty;
    private CancellationTokenSource? deactivationTokenSource;
    
    // Resource management
    private readonly List<IDisposable> disposableResources = new();
    private Timer? heartbeatTimer;
    
    public DocumentProcessorGrain(
        ILogger<DocumentProcessorGrain> logger,
        IFileStorageService fileStorage,
        INotificationService notifications,
        IMetricsCollector metrics)
    {
        this.logger = logger;
        this.fileStorage = fileStorage;
        this.notifications = notifications;
        this.metrics = metrics;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // 1. Record activation metrics
        activationTime = DateTimeOffset.UtcNow;
        grainId = this.GetPrimaryKeyString();
        
        logger.LogInformation(
            "Activating DocumentProcessorGrain {GrainId} at {ActivationTime}",
            grainId, activationTime);
        
        try
        {
            // 2. Initialize grain-specific state
            await InitializeGrainStateAsync(cancellationToken);
            
            // 3. Set up resource connections
            await SetupResourceConnectionsAsync(cancellationToken);
            
            // 4. Start background processes
            StartBackgroundProcesses();
            
            // 5. Register for notifications
            await RegisterForNotificationsAsync();
            
            // 6. Record successful activation
            metrics.RecordGrainActivation(grainId, activationTime);
            
            logger.LogInformation(
                "Successfully activated DocumentProcessorGrain {GrainId}",
                grainId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, 
                "Failed to activate DocumentProcessorGrain {GrainId}",
                grainId);
            
            // Cleanup on failed activation
            await CleanupResourcesAsync();
            throw;
        }
        
        // Always call base implementation
        await base.OnActivateAsync(cancellationToken);
    }

    private async Task InitializeGrainStateAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Initializing grain state for {GrainId}", grainId);
        
        // Initialize grain-specific state
        deactivationTokenSource = new CancellationTokenSource();
        
        // Load any persisted state if needed
        // await LoadPersistedStateAsync(cancellationToken);
        
        logger.LogDebug("Grain state initialized for {GrainId}", grainId);
    }

    private async Task SetupResourceConnectionsAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Setting up resource connections for {GrainId}", grainId);
        
        try
        {
            // Initialize file storage connection
            await fileStorage.InitializeAsync(grainId, cancellationToken);
            
            // Verify connectivity
            var isConnected = await fileStorage.TestConnectionAsync(cancellationToken);
            if (!isConnected)
            {
                throw new InvalidOperationException("Failed to connect to file storage");
            }
            
            logger.LogDebug("Resource connections established for {GrainId}", grainId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to setup resource connections for {GrainId}", grainId);
            throw;
        }
    }

    private void StartBackgroundProcesses()
    {
        logger.LogDebug("Starting background processes for {GrainId}", grainId);
        
        // Start heartbeat timer for health monitoring
        heartbeatTimer = new Timer(
            SendHeartbeat,
            state: null,
            dueTime: TimeSpan.FromMinutes(1),
            period: TimeSpan.FromMinutes(5));
        
        disposableResources.Add(heartbeatTimer);
        
        logger.LogDebug("Background processes started for {GrainId}", grainId);
    }

    private async Task RegisterForNotificationsAsync()
    {
        logger.LogDebug("Registering for notifications {GrainId}", grainId);
        
        try
        {
            // Register grain for relevant notifications
            await notifications.SubscribeAsync(grainId, NotificationCallback);
            
            logger.LogDebug("Notification registration completed for {GrainId}", grainId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to register for notifications {GrainId}", grainId);
            // Don't fail activation for notification issues
        }
    }

    private void SendHeartbeat(object? state)
    {
        if (deactivationTokenSource?.Token.IsCancellationRequested == true)
            return;
        
        try
        {
            metrics.RecordGrainHeartbeat(grainId, DateTimeOffset.UtcNow);
            logger.LogTrace("Heartbeat sent for {GrainId}", grainId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to send heartbeat for {GrainId}", grainId);
        }
    }

    private Task NotificationCallback(string message)
    {
        logger.LogInformation("Received notification for {GrainId}: {Message}", grainId, message);
        return Task.CompletedTask;
    }

    // Grain method implementations
    public async Task<ProcessingResult> ProcessDocumentAsync(DocumentRequest request)
    {
        logger.LogInformation("Processing document request for {GrainId}", grainId);
        
        try
        {
            // Use initialized resources
            var content = await fileStorage.DownloadAsync(request.FileId);
            
            // Process the document
            var result = new ProcessingResult
            {
                DocumentId = request.FileId,
                ProcessedAt = DateTimeOffset.UtcNow,
                Status = ProcessingStatus.Completed,
                ProcessedBy = grainId
            };
            
            metrics.RecordDocumentProcessed(grainId, request.FileId);
            
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing document for {GrainId}", grainId);
            throw;
        }
    }
}
```

### Grain Deactivation Process

Deactivation occurs when Orleans decides to remove a grain from memory. Use `OnDeactivateAsync` to clean up resources, save state, and ensure graceful shutdown.

```csharp
namespace DocumentProcessor.Orleans.Lifecycle;

using Orleans;
using Microsoft.Extensions.Logging;

public partial class DocumentProcessorGrain : Grain, IDocumentProcessorGrain
{
    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        var deactivationTime = DateTimeOffset.UtcNow;
        var activeDuration = deactivationTime - activationTime;
        
        logger.LogInformation(
            "Deactivating DocumentProcessorGrain {GrainId} due to {Reason} after {Duration}",
            grainId, reason, activeDuration);
        
        try
        {
            // 1. Signal deactivation to background processes
            SignalDeactivation();
            
            // 2. Complete pending operations with timeout
            await CompletePendingOperationsAsync(cancellationToken);
            
            // 3. Save critical state
            await SaveCriticalStateAsync(cancellationToken);
            
            // 4. Unregister from notifications
            await UnregisterFromNotificationsAsync();
            
            // 5. Cleanup resources
            await CleanupResourcesAsync();
            
            // 6. Record deactivation metrics
            metrics.RecordGrainDeactivation(grainId, reason, activeDuration);
            
            logger.LogInformation(
                "Successfully deactivated DocumentProcessorGrain {GrainId}",
                grainId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "Error during deactivation of DocumentProcessorGrain {GrainId}",
                grainId);
            
            // Still try to cleanup what we can
            await ForceCleanupAsync();
        }
        finally
        {
            // Always call base implementation
            await base.OnDeactivateAsync(reason, cancellationToken);
        }
    }

    private void SignalDeactivation()
    {
        logger.LogDebug("Signaling deactivation for {GrainId}", grainId);
        
        // Signal all background processes to stop
        deactivationTokenSource?.Cancel();
        
        logger.LogDebug("Deactivation signal sent for {GrainId}", grainId);
    }

    private async Task CompletePendingOperationsAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Completing pending operations for {GrainId}", grainId);
        
        try
        {
            // Wait for any pending file operations with timeout
            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(30));
            
            // Simulate waiting for operations
            await Task.Delay(100, timeoutCts.Token);
            
            logger.LogDebug("Pending operations completed for {GrainId}", grainId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Pending operations cancelled during deactivation for {GrainId}", grainId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error completing pending operations for {GrainId}", grainId);
        }
    }

    private async Task SaveCriticalStateAsync(CancellationToken cancellationToken)
    {
        logger.LogDebug("Saving critical state for {GrainId}", grainId);
        
        try
        {
            // Save any critical state that must survive deactivation
            var stateSnapshot = new GrainStateSnapshot
            {
                GrainId = grainId,
                LastActivity = DateTimeOffset.UtcNow,
                ProcessingCount = 0, // Track processed items
                SavedAt = DateTimeOffset.UtcNow
            };
            
            // In a real implementation, save to persistent storage
            // await stateManager.SaveStateAsync(stateSnapshot, cancellationToken);
            
            logger.LogDebug("Critical state saved for {GrainId}", grainId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to save critical state for {GrainId}", grainId);
            // Don't throw - log error but continue deactivation
        }
    }

    private async Task UnregisterFromNotificationsAsync()
    {
        logger.LogDebug("Unregistering from notifications for {GrainId}", grainId);
        
        try
        {
            await notifications.UnsubscribeAsync(grainId);
            logger.LogDebug("Unregistered from notifications for {GrainId}", grainId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to unregister from notifications for {GrainId}", grainId);
        }
    }

    private async Task CleanupResourcesAsync()
    {
        logger.LogDebug("Cleaning up resources for {GrainId}", grainId);
        
        try
        {
            // Dispose all tracked resources
            foreach (var resource in disposableResources)
            {
                try
                {
                    resource.Dispose();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Error disposing resource for {GrainId}", grainId);
                }
            }
            disposableResources.Clear();
            
            // Cleanup file storage connections
            await fileStorage.CleanupAsync(grainId);
            
            // Dispose cancellation token source
            deactivationTokenSource?.Dispose();
            
            logger.LogDebug("Resources cleaned up for {GrainId}", grainId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error cleaning up resources for {GrainId}", grainId);
        }
    }

    private async Task ForceCleanupAsync()
    {
        logger.LogWarning("Performing force cleanup for {GrainId}", grainId);
        
        try
        {
            // Best-effort cleanup when normal cleanup fails
            disposableResources.ForEach(r =>
            {
                try { r.Dispose(); } catch { /* Ignore */ }
            });
            
            deactivationTokenSource?.Dispose();
            
            // Force cleanup external resources
            await fileStorage.ForceCleanupAsync(grainId);
        }
        catch
        {
            // Ignore all exceptions during force cleanup
        }
    }
}
```

### Lifecycle Event Handling

Orleans provides hooks for handling various lifecycle events and errors that can occur during grain activation and deactivation.

```csharp
namespace DocumentProcessor.Orleans.LifecycleEvents;

using Orleans;
using Orleans.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

// Advanced lifecycle management with event handling
public class AdvancedLifecycleGrain : Grain, IAdvancedLifecycleGrain
{
    private readonly ILogger<AdvancedLifecycleGrain> logger;
    private readonly IServiceScopeFactory serviceScopeFactory;
    private readonly List<IAsyncDisposable> asyncDisposables = new();
    
    private LifecycleState currentState = LifecycleState.Uninitialized;
    private readonly object stateLock = new object();
    
    public AdvancedLifecycleGrain(
        ILogger<AdvancedLifecycleGrain> logger,
        IServiceScopeFactory serviceScopeFactory)
    {
        this.logger = logger;
        this.serviceScopeFactory = serviceScopeFactory;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var grainId = this.GetPrimaryKeyString();
        
        logger.LogInformation("Starting activation process for grain {GrainId}", grainId);
        
        try
        {
            // Update state safely
            UpdateLifecycleState(LifecycleState.Activating);
            
            // Perform activation steps with error handling
            await ExecuteActivationStepsAsync(grainId, cancellationToken);
            
            // Mark as active
            UpdateLifecycleState(LifecycleState.Active);
            
            logger.LogInformation("Grain {GrainId} activated successfully", grainId);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("Activation cancelled for grain {GrainId}", grainId);
            UpdateLifecycleState(LifecycleState.ActivationFailed);
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Activation failed for grain {GrainId}", grainId);
            UpdateLifecycleState(LifecycleState.ActivationFailed);
            
            // Cleanup on failure
            await PerformFailureCleanupAsync();
            throw;
        }
        
        await base.OnActivateAsync(cancellationToken);
    }

    private async Task ExecuteActivationStepsAsync(string grainId, CancellationToken cancellationToken)
    {
        // Step 1: Initialize core services
        logger.LogDebug("Initializing core services for {GrainId}", grainId);
        await InitializeCoreServicesAsync(cancellationToken);
        
        // Step 2: Load configuration
        logger.LogDebug("Loading configuration for {GrainId}", grainId);
        await LoadConfigurationAsync(cancellationToken);
        
        // Step 3: Establish external connections
        logger.LogDebug("Establishing external connections for {GrainId}", grainId);
        await EstablishConnectionsAsync(cancellationToken);
        
        // Step 4: Start monitoring
        logger.LogDebug("Starting monitoring for {GrainId}", grainId);
        await StartMonitoringAsync(cancellationToken);
    }

    private async Task InitializeCoreServicesAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Create scoped services for this grain instance
            var scope = serviceScopeFactory.CreateScope();
            asyncDisposables.Add(scope);
            
            // Initialize grain-specific services
            await Task.Delay(50, cancellationToken); // Simulate initialization
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize core services");
            throw;
        }
    }

    private async Task LoadConfigurationAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Load grain-specific configuration
            await Task.Delay(25, cancellationToken); // Simulate config loading
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load configuration");
            throw;
        }
    }

    private async Task EstablishConnectionsAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Establish external service connections
            await Task.Delay(100, cancellationToken); // Simulate connection setup
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to establish connections");
            throw;
        }
    }

    private async Task StartMonitoringAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Start health monitoring and metrics collection
            await Task.Delay(25, cancellationToken); // Simulate monitoring setup
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to start monitoring");
            throw;
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        var grainId = this.GetPrimaryKeyString();
        
        logger.LogInformation(
            "Starting deactivation process for grain {GrainId} due to {Reason}",
            grainId, reason);
        
        try
        {
            UpdateLifecycleState(LifecycleState.Deactivating);
            
            // Perform deactivation steps
            await ExecuteDeactivationStepsAsync(grainId, reason, cancellationToken);
            
            UpdateLifecycleState(LifecycleState.Deactivated);
            
            logger.LogInformation("Grain {GrainId} deactivated successfully", grainId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during deactivation of grain {GrainId}", grainId);
            UpdateLifecycleState(LifecycleState.DeactivationFailed);
            
            // Force cleanup on deactivation failure
            await PerformFailureCleanupAsync();
        }
        
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    private async Task ExecuteDeactivationStepsAsync(
        string grainId, 
        DeactivationReason reason, 
        CancellationToken cancellationToken)
    {
        // Deactivation timeout based on reason
        var timeout = reason switch
        {
            DeactivationReason.ApplicationRequested => TimeSpan.FromMinutes(1),
            DeactivationReason.ActivationLimit => TimeSpan.FromSeconds(30),
            DeactivationReason.InternalShutdown => TimeSpan.FromSeconds(10),
            _ => TimeSpan.FromSeconds(15)
        };
        
        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(timeout);
        
        try
        {
            // Step 1: Stop accepting new work
            logger.LogDebug("Stopping new work acceptance for {GrainId}", grainId);
            
            // Step 2: Complete current work
            logger.LogDebug("Completing current work for {GrainId}", grainId);
            await CompleteCurrentWorkAsync(timeoutCts.Token);
            
            // Step 3: Save state
            logger.LogDebug("Saving final state for {GrainId}", grainId);
            await SaveFinalStateAsync(timeoutCts.Token);
            
            // Step 4: Cleanup resources
            logger.LogDebug("Cleaning up resources for {GrainId}", grainId);
            await CleanupAllResourcesAsync();
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
        {
            logger.LogWarning("Deactivation timeout for grain {GrainId}", grainId);
        }
    }

    private async Task CompleteCurrentWorkAsync(CancellationToken cancellationToken)
    {
        // Wait for current operations to complete
        await Task.Delay(100, cancellationToken);
    }

    private async Task SaveFinalStateAsync(CancellationToken cancellationToken)
    {
        // Save any critical state before deactivation
        await Task.Delay(50, cancellationToken);
    }

    private async Task CleanupAllResourcesAsync()
    {
        foreach (var asyncDisposable in asyncDisposables)
        {
            try
            {
                await asyncDisposable.DisposeAsync();
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Error disposing async resource");
            }
        }
        asyncDisposables.Clear();
    }

    private async Task PerformFailureCleanupAsync()
    {
        try
        {
            await CleanupAllResourcesAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during failure cleanup");
        }
    }

    private void UpdateLifecycleState(LifecycleState newState)
    {
        lock (stateLock)
        {
            var oldState = currentState;
            currentState = newState;
            
            logger.LogDebug(
                "Lifecycle state changed from {OldState} to {NewState} for grain {GrainId}",
                oldState, newState, this.GetPrimaryKeyString());
        }
    }

    // Grain method that checks lifecycle state
    public Task<GrainHealthStatus> GetHealthStatusAsync()
    {
        lock (stateLock)
        {
            var status = new GrainHealthStatus
            {
                GrainId = this.GetPrimaryKeyString(),
                State = currentState,
                IsHealthy = currentState == LifecycleState.Active,
                LastChecked = DateTimeOffset.UtcNow
            };
            
            return Task.FromResult(status);
        }
    }
}

// Supporting types for lifecycle management
public enum LifecycleState
{
    Uninitialized,
    Activating,
    Active,
    Deactivating,
    Deactivated,
    ActivationFailed,
    DeactivationFailed
}

public record GrainHealthStatus
{
    public string GrainId { get; init; } = string.Empty;
    public LifecycleState State { get; init; }
    public bool IsHealthy { get; init; }
    public DateTimeOffset LastChecked { get; init; }
}

public record GrainStateSnapshot
{
    public string GrainId { get; init; } = string.Empty;
    public DateTimeOffset LastActivity { get; init; }
    public int ProcessingCount { get; init; }
    public DateTimeOffset SavedAt { get; init; }
}

// Interfaces for lifecycle examples
public interface IAdvancedLifecycleGrain : IGrain
{
    Task<GrainHealthStatus> GetHealthStatusAsync();
}

public interface IDocumentProcessorGrain : IGrain
{
    Task<ProcessingResult> ProcessDocumentAsync(DocumentRequest request);
}

public record DocumentRequest
{
    public string FileId { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public DocumentType Type { get; init; }
}

public record ProcessingResult
{
    public string DocumentId { get; init; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; init; }
    public ProcessingStatus Status { get; init; }
    public string ProcessedBy { get; init; } = string.Empty;
}

public enum DocumentType
{
    Pdf,
    Word,
    Excel,
    PowerPoint,
    Text
}

public enum ProcessingStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
}

// Service interfaces for lifecycle examples
public interface IFileStorageService
{
    Task InitializeAsync(string grainId, CancellationToken cancellationToken);
    Task<bool> TestConnectionAsync(CancellationToken cancellationToken);
    Task<byte[]> DownloadAsync(string fileId);
    Task CleanupAsync(string grainId);
    Task ForceCleanupAsync(string grainId);
}

public interface INotificationService
{
    Task SubscribeAsync(string grainId, Func<string, Task> callback);
    Task UnsubscribeAsync(string grainId);
}

public interface IMetricsCollector
{
    void RecordGrainActivation(string grainId, DateTimeOffset activationTime);
    void RecordGrainDeactivation(string grainId, DeactivationReason reason, TimeSpan activeDuration);
    void RecordGrainHeartbeat(string grainId, DateTimeOffset timestamp);
    void RecordDocumentProcessed(string grainId, string documentId);
}
```

## Grain Identity and Addressing

Orleans grains use unique identities for addressing and routing. Understanding grain key patterns, reference management, and identity design ensures efficient grain communication and proper application architecture.

### Grain Key Patterns

Orleans supports various key types for different use cases. Choose the appropriate key type based on your grain's identity requirements and access patterns.

```csharp
namespace DocumentProcessor.Orleans.Identity;

using Orleans;
using Microsoft.Extensions.Logging;

// STRING KEYS - Most common, human-readable identifiers
public interface IUserProfileGrain : IGrainWithStringKey
{
    Task<UserProfile> GetProfileAsync();
    Task UpdateProfileAsync(UserProfile profile);
    Task<List<string>> GetRecentActivityAsync();
}

public class UserProfileGrain : Grain, IUserProfileGrain
{
    private readonly ILogger<UserProfileGrain> logger;
    private UserProfile profile = new();
    private readonly List<string> recentActivity = new();

    public UserProfileGrain(ILogger<UserProfileGrain> logger)
    {
        this.logger = logger;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // String key accessed via GetPrimaryKeyString()
        var userId = this.GetPrimaryKeyString();
        
        logger.LogInformation("UserProfileGrain activated for user: {UserId}", userId);
        
        // Initialize profile with user ID
        profile = new UserProfile
        {
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            LastAccessed = DateTimeOffset.UtcNow
        };
        
        await base.OnActivateAsync(cancellationToken);
    }

    public async Task<UserProfile> GetProfileAsync()
    {
        var userId = this.GetPrimaryKeyString();
        
        logger.LogDebug("Getting profile for user: {UserId}", userId);
        
        profile.LastAccessed = DateTimeOffset.UtcNow;
        recentActivity.Add($"Profile accessed at {DateTimeOffset.UtcNow}");
        
        // Keep only last 10 activities
        while (recentActivity.Count > 10)
        {
            recentActivity.RemoveAt(0);
        }
        
        return profile;
    }

    public async Task UpdateProfileAsync(UserProfile updatedProfile)
    {
        var userId = this.GetPrimaryKeyString();
        
        logger.LogInformation("Updating profile for user: {UserId}", userId);
        
        // Preserve immutable fields
        updatedProfile.UserId = userId;
        updatedProfile.CreatedAt = profile.CreatedAt;
        updatedProfile.LastModified = DateTimeOffset.UtcNow;
        
        profile = updatedProfile;
        recentActivity.Add($"Profile updated at {DateTimeOffset.UtcNow}");
        
        await Task.CompletedTask;
    }

    public async Task<List<string>> GetRecentActivityAsync()
    {
        return new List<string>(recentActivity);
    }
}

// GUID KEYS - System-generated unique identifiers
public interface IDocumentGrain : IGrainWithGuidKey
{
    Task<DocumentInfo> GetDocumentInfoAsync();
    Task UpdateDocumentAsync(DocumentUpdateRequest request);
    Task<DocumentAccessLog> GetAccessLogAsync();
}

public class DocumentGrain : Grain, IDocumentGrain
{
    private readonly ILogger<DocumentGrain> logger;
    private DocumentInfo documentInfo = new();
    private readonly List<DocumentAccess> accessLog = new();

    public DocumentGrain(ILogger<DocumentGrain> logger)
    {
        this.logger = logger;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // GUID key accessed via GetPrimaryKey()
        var documentId = this.GetPrimaryKey();
        
        logger.LogInformation("DocumentGrain activated for document: {DocumentId}", documentId);
        
        documentInfo = new DocumentInfo
        {
            DocumentId = documentId,
            CreatedAt = DateTimeOffset.UtcNow,
            Status = DocumentStatus.Draft
        };
        
        await base.OnActivateAsync(cancellationToken);
    }

    public async Task<DocumentInfo> GetDocumentInfoAsync()
    {
        var documentId = this.GetPrimaryKey();
        
        logger.LogDebug("Getting document info for: {DocumentId}", documentId);
        
        RecordAccess("GetDocumentInfo");
        
        return documentInfo;
    }

    public async Task UpdateDocumentAsync(DocumentUpdateRequest request)
    {
        var documentId = this.GetPrimaryKey();
        
        logger.LogInformation("Updating document: {DocumentId}", documentId);
        
        documentInfo.Title = request.Title ?? documentInfo.Title;
        documentInfo.Content = request.Content ?? documentInfo.Content;
        documentInfo.LastModified = DateTimeOffset.UtcNow;
        documentInfo.Version++;
        
        RecordAccess("UpdateDocument");
        
        await Task.CompletedTask;
    }

    public async Task<DocumentAccessLog> GetAccessLogAsync()
    {
        return new DocumentAccessLog
        {
            DocumentId = this.GetPrimaryKey(),
            Accesses = new List<DocumentAccess>(accessLog),
            TotalAccesses = accessLog.Count
        };
    }

    private void RecordAccess(string operation)
    {
        accessLog.Add(new DocumentAccess
        {
            Operation = operation,
            AccessedAt = DateTimeOffset.UtcNow,
            AccessedBy = "System" // In real apps, get from context
        });
        
        // Keep only last 50 accesses
        while (accessLog.Count > 50)
        {
            accessLog.RemoveAt(0);
        }
    }
}

// COMPOUND KEYS - Multiple values combined into a single key
public interface IOrderItemGrain : IGrainWithStringKey
{
    Task<OrderItem> GetOrderItemAsync();
    Task UpdateQuantityAsync(int newQuantity);
    Task<decimal> CalculateLineTotalAsync();
}

public class OrderItemGrain : Grain, IOrderItemGrain
{
    private readonly ILogger<OrderItemGrain> logger;
    private OrderItem orderItem = new();

    public OrderItemGrain(ILogger<OrderItemGrain> logger)
    {
        this.logger = logger;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        // Compound key format: "orderId|productId"
        var compositeKey = this.GetPrimaryKeyString();
        var parts = compositeKey.Split('|');
        
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Invalid compound key format: {compositeKey}. Expected: orderId|productId");
        }
        
        var orderId = parts[0];
        var productId = parts[1];
        
        logger.LogInformation("OrderItemGrain activated for Order: {OrderId}, Product: {ProductId}", 
            orderId, productId);
        
        orderItem = new OrderItem
        {
            OrderId = orderId,
            ProductId = productId,
            Quantity = 0,
            UnitPrice = 0,
            CreatedAt = DateTimeOffset.UtcNow
        };
        
        await base.OnActivateAsync(cancellationToken);
    }

    public async Task<OrderItem> GetOrderItemAsync()
    {
        return orderItem;
    }

    public async Task UpdateQuantityAsync(int newQuantity)
    {
        if (newQuantity < 0)
        {
            throw new ArgumentException("Quantity cannot be negative");
        }
        
        var oldQuantity = orderItem.Quantity;
        orderItem.Quantity = newQuantity;
        orderItem.LastModified = DateTimeOffset.UtcNow;
        
        logger.LogInformation("Updated quantity for {OrderId}|{ProductId} from {OldQuantity} to {NewQuantity}",
            orderItem.OrderId, orderItem.ProductId, oldQuantity, newQuantity);
        
        await Task.CompletedTask;
    }

    public async Task<decimal> CalculateLineTotalAsync()
    {
        return orderItem.Quantity * orderItem.UnitPrice;
    }
}

// KEY GENERATION STRATEGIES - Factory patterns for consistent key creation
public static class GrainKeyFactory
{
    // Strategy 1: Simple string keys with prefixes
    public static string CreateUserKey(string username) => $"user:{username.ToLowerInvariant()}";
    
    public static string CreateSessionKey(string userId) => $"session:{userId}:{DateTimeOffset.UtcNow.Ticks}";
    
    // Strategy 2: Hierarchical keys for related entities
    public static string CreateOrderItemKey(string orderId, string productId) => $"{orderId}|{productId}";
    
    public static string CreateAccountKey(string customerId, string accountType) => $"account:{customerId}:{accountType}";
    
    // Strategy 3: Time-based partitioning
    public static string CreateAnalyticsKey(string eventType, DateTimeOffset timestamp)
    {
        var partition = timestamp.ToString("yyyy-MM-dd");
        return $"analytics:{eventType}:{partition}";
    }
    
    // Strategy 4: Hash-based distribution for load balancing
    public static string CreateShardedKey(string baseKey, int shardCount = 100)
    {
        var hash = baseKey.GetHashCode();
        var shard = Math.Abs(hash) % shardCount;
        return $"{baseKey}:shard:{shard:D3}";
    }
    
    // Strategy 5: GUID-based keys with meaningful prefixes
    public static Guid CreateDocumentId() => Guid.NewGuid();
    
    public static Guid CreateProcessingJobId() => Guid.NewGuid();
    
    // Strategy 6: Composite keys for many-to-many relationships
    public static string CreateSubscriptionKey(string userId, string channelId) => $"sub:{userId}:{channelId}";
    
    // Strategy 7: Version-aware keys
    public static string CreateVersionedKey(string baseKey, int version) => $"{baseKey}:v{version}";
}

// Example usage of key generation strategies
public class KeyGenerationDemo
{
    private readonly IGrainFactory grainFactory;
    private readonly ILogger<KeyGenerationDemo> logger;

    public KeyGenerationDemo(IGrainFactory grainFactory, ILogger<KeyGenerationDemo> logger)
    {
        this.grainFactory = grainFactory;
        this.logger = logger;
    }

    public async Task DemonstrateKeyPatternsAsync()
    {
        // String key usage
        var userKey = GrainKeyFactory.CreateUserKey("john.doe");
        var userGrain = grainFactory.GetGrain<IUserProfileGrain>(userKey);
        var userProfile = await userGrain.GetProfileAsync();
        
        logger.LogInformation("Retrieved user profile for key: {UserKey}", userKey);
        
        // GUID key usage
        var documentId = GrainKeyFactory.CreateDocumentId();
        var documentGrain = grainFactory.GetGrain<IDocumentGrain>(documentId);
        var documentInfo = await documentGrain.GetDocumentInfoAsync();
        
        logger.LogInformation("Retrieved document info for ID: {DocumentId}", documentId);
        
        // Compound key usage
        var orderItemKey = GrainKeyFactory.CreateOrderItemKey("order-123", "product-456");
        var orderItemGrain = grainFactory.GetGrain<IOrderItemGrain>(orderItemKey);
        var orderItem = await orderItemGrain.GetOrderItemAsync();
        
        logger.LogInformation("Retrieved order item for key: {OrderItemKey}", orderItemKey);
        
        // Sharded key for load distribution
        var baseKey = "heavy-computation-task";
        var shardedKey = GrainKeyFactory.CreateShardedKey(baseKey);
        
        logger.LogInformation("Generated sharded key: {ShardedKey} from base: {BaseKey}", shardedKey, baseKey);
    }
}
```

### Grain Reference Patterns

Grain references are the primary way to communicate with grains from clients and other grains. Understanding reference patterns ensures efficient and reliable grain communication.

```csharp
namespace DocumentProcessor.Orleans.References;

using Orleans;
using Microsoft.Extensions.Logging;

// CLIENT-SIDE GRAIN ACCESS - Getting references from IGrainFactory
public class DocumentService
{
    private readonly IGrainFactory grainFactory;
    private readonly ILogger<DocumentService> logger;

    public DocumentService(IGrainFactory grainFactory, ILogger<DocumentService> logger)
    {
        this.grainFactory = grainFactory;
        this.logger = logger;
    }

    // PATTERN 1: Direct grain reference by key
    public async Task<DocumentInfo> GetDocumentAsync(string documentId)
    {
        // Get grain reference using string key
        var documentGrain = grainFactory.GetGrain<IDocumentGrain>(Guid.Parse(documentId));
        
        logger.LogDebug("Getting document: {DocumentId}", documentId);
        
        return await documentGrain.GetDocumentInfoAsync();
    }

    // PATTERN 2: Batch operations with multiple grain references
    public async Task<List<DocumentInfo>> GetMultipleDocumentsAsync(List<string> documentIds)
    {
        logger.LogInformation("Getting {Count} documents", documentIds.Count);
        
        // Create tasks for parallel execution
        var tasks = documentIds.Select(async id =>
        {
            try
            {
                var grain = grainFactory.GetGrain<IDocumentGrain>(Guid.Parse(id));
                return await grain.GetDocumentInfoAsync();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to get document: {DocumentId}", id);
                return null; // Handle individual failures
            }
        });
        
        // Wait for all tasks and filter out nulls
        var results = await Task.WhenAll(tasks);
        return results.Where(result => result != null).ToList()!;
    }

    // PATTERN 3: Reference caching for frequently accessed grains
    private readonly Dictionary<string, IUserProfileGrain> userGrainCache = new();
    private readonly object cacheLock = new();

    public IUserProfileGrain GetCachedUserGrain(string userId)
    {
        lock (cacheLock)
        {
            if (!userGrainCache.TryGetValue(userId, out var grain))
            {
                var userKey = GrainKeyFactory.CreateUserKey(userId);
                grain = grainFactory.GetGrain<IUserProfileGrain>(userKey);
                userGrainCache[userId] = grain;
                
                logger.LogDebug("Cached grain reference for user: {UserId}", userId);
            }
            
            return grain;
        }
    }

    // PATTERN 4: Grain reference with retry logic
    public async Task<T> ExecuteWithRetryAsync<T>(Func<Task<T>> grainOperation, int maxRetries = 3)
    {
        var attempts = 0;
        
        while (attempts < maxRetries)
        {
            try
            {
                return await grainOperation();
            }
            catch (Exception ex) when (attempts < maxRetries - 1)
            {
                attempts++;
                var delay = TimeSpan.FromMilliseconds(100 * Math.Pow(2, attempts));
                
                logger.LogWarning(ex, "Grain operation failed, retrying in {Delay}ms (attempt {Attempt}/{MaxRetries})",
                    delay.TotalMilliseconds, attempts, maxRetries);
                
                await Task.Delay(delay);
            }
        }
        
        throw new InvalidOperationException($"Operation failed after {maxRetries} attempts");
    }
}

// GRAIN-TO-GRAIN COMMUNICATION - References from within grains
public class OrderProcessingGrain : Grain, IOrderProcessingGrain
{
    private readonly ILogger<OrderProcessingGrain> logger;

    public OrderProcessingGrain(ILogger<OrderProcessingGrain> logger)
    {
        this.logger = logger;
    }

    // PATTERN 1: Getting references to other grains
    public async Task<OrderProcessingResult> ProcessOrderAsync(OrderRequest request)
    {
        var orderId = this.GetPrimaryKeyString();
        
        logger.LogInformation("Processing order: {OrderId}", orderId);
        
        try
        {
            // Get reference to customer grain
            var customerGrain = GrainFactory.GetGrain<IUserProfileGrain>(
                GrainKeyFactory.CreateUserKey(request.CustomerId));
            
            // Get customer information
            var customer = await customerGrain.GetProfileAsync();
            
            // Process each order item
            var itemResults = new List<OrderItemResult>();
            
            foreach (var item in request.Items)
            {
                // Get reference to order item grain using compound key
                var itemKey = GrainKeyFactory.CreateOrderItemKey(orderId, item.ProductId);
                var itemGrain = GrainFactory.GetGrain<IOrderItemGrain>(itemKey);
                
                // Update item quantity
                await itemGrain.UpdateQuantityAsync(item.Quantity);
                
                // Calculate line total
                var lineTotal = await itemGrain.CalculateLineTotalAsync();
                
                itemResults.Add(new OrderItemResult
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    LineTotal = lineTotal
                });
            }
            
            var totalAmount = itemResults.Sum(r => r.LineTotal);
            
            logger.LogInformation("Order {OrderId} processed successfully. Total: {TotalAmount:C}", 
                orderId, totalAmount);
            
            return new OrderProcessingResult
            {
                OrderId = orderId,
                Success = true,
                TotalAmount = totalAmount,
                Items = itemResults
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process order: {OrderId}", orderId);
            
            return new OrderProcessingResult
            {
                OrderId = orderId,
                Success = false,
                Error = ex.Message
            };
        }
    }

    // PATTERN 2: Observer pattern for grain notifications
    private readonly List<IOrderStatusObserver> observers = new();

    public async Task SubscribeToUpdatesAsync(IOrderStatusObserver observer)
    {
        observers.Add(observer);
        
        logger.LogDebug("Observer subscribed to order updates for: {OrderId}", this.GetPrimaryKeyString());
        
        await Task.CompletedTask;
    }

    public async Task UnsubscribeFromUpdatesAsync(IOrderStatusObserver observer)
    {
        observers.Remove(observer);
        
        logger.LogDebug("Observer unsubscribed from order updates for: {OrderId}", this.GetPrimaryKeyString());
        
        await Task.CompletedTask;
    }

    private async Task NotifyObserversAsync(OrderStatus newStatus)
    {
        var orderId = this.GetPrimaryKeyString();
        
        var notificationTasks = observers.Select(observer => 
            NotifyObserverSafelyAsync(observer, orderId, newStatus));
        
        await Task.WhenAll(notificationTasks);
    }

    private async Task NotifyObserverSafelyAsync(IOrderStatusObserver observer, string orderId, OrderStatus status)
    {
        try
        {
            await observer.OnOrderStatusChangedAsync(orderId, OrderStatus.Processing, status);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to notify observer of status change for order: {OrderId}", orderId);
        }
    }
}

// REFERENCE SERIALIZATION - Passing grain references between systems
public class GrainReferenceSerializer
{
    private readonly IGrainFactory grainFactory;
    private readonly ILogger<GrainReferenceSerializer> logger;

    public GrainReferenceSerializer(IGrainFactory grainFactory, ILogger<GrainReferenceSerializer> logger)
    {
        this.grainFactory = grainFactory;
        this.logger = logger;
    }

    // Serialize grain reference to string for external storage
    public string SerializeGrainReference<T>(T grainReference) where T : IGrain
    {
        if (grainReference is IGrainWithStringKey stringKeyGrain)
        {
            var key = stringKeyGrain.GetPrimaryKeyString();
            var grainType = typeof(T).Name;
            
            return $"{grainType}:string:{key}";
        }
        else if (grainReference is IGrainWithGuidKey guidKeyGrain)
        {
            var key = guidKeyGrain.GetPrimaryKey();
            var grainType = typeof(T).Name;
            
            return $"{grainType}:guid:{key}";
        }
        
        throw new NotSupportedException($"Grain type {typeof(T)} is not supported for serialization");
    }

    // Deserialize string back to grain reference
    public T DeserializeGrainReference<T>(string serializedReference) where T : IGrain
    {
        var parts = serializedReference.Split(':');
        
        if (parts.Length != 3)
        {
            throw new ArgumentException($"Invalid serialized reference format: {serializedReference}");
        }
        
        var grainType = parts[0];
        var keyType = parts[1];
        var key = parts[2];
        
        if (keyType == "string")
        {
            return grainFactory.GetGrain<T>(key);
        }
        else if (keyType == "guid")
        {
            if (Guid.TryParse(key, out var guidKey))
            {
                return grainFactory.GetGrain<T>(guidKey);
            }
            throw new ArgumentException($"Invalid GUID key: {key}");
        }
        
        throw new NotSupportedException($"Key type {keyType} is not supported");
    }

    // Example usage for external system integration
    public async Task ProcessExternalCommandAsync(string serializedGrainRef, string command)
    {
        try
        {
            // Deserialize the grain reference
            var documentGrain = DeserializeGrainReference<IDocumentGrain>(serializedGrainRef);
            
            // Execute command on the grain
            switch (command.ToLowerInvariant())
            {
                case "getinfo":
                    var info = await documentGrain.GetDocumentInfoAsync();
                    logger.LogInformation("Document info retrieved: {DocumentId}", info.DocumentId);
                    break;
                    
                default:
                    logger.LogWarning("Unknown command: {Command}", command);
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process external command: {Command} for grain: {GrainRef}",
                command, serializedGrainRef);
            throw;
        }
    }
}
```

### Identity Management Best Practices

Proper identity design is crucial for Orleans applications. Follow these patterns to ensure efficient grain distribution, avoid conflicts, and maintain scalability.

```csharp
namespace DocumentProcessor.Orleans.IdentityBestPractices;

using Orleans;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

// BEST PRACTICE 1: Consistent naming conventions
public static class GrainIdentityConventions
{
    // Use consistent prefixes for different grain types
    public const string USER_PREFIX = "user";
    public const string DOCUMENT_PREFIX = "doc";
    public const string ORDER_PREFIX = "order";
    public const string SESSION_PREFIX = "session";
    
    // Use separators consistently
    public const char SEPARATOR = ':';
    public const char COMPOUND_SEPARATOR = '|';
    
    // Create well-formed identities
    public static string CreateUserId(string email) => $"{USER_PREFIX}{SEPARATOR}{email.ToLowerInvariant()}";
    
    public static string CreateDocumentId(string category, string name) => 
        $"{DOCUMENT_PREFIX}{SEPARATOR}{category}{COMPOUND_SEPARATOR}{name}";
    
    public static string CreateOrderId(string customerId, DateTimeOffset orderDate) => 
        $"{ORDER_PREFIX}{SEPARATOR}{customerId}{COMPOUND_SEPARATOR}{orderDate:yyyyMMdd}";
    
    // Validate identity format
    public static bool IsValidUserId(string userId) => 
        userId.StartsWith($"{USER_PREFIX}{SEPARATOR}") && userId.Length > USER_PREFIX.Length + 1;
    
    public static bool IsValidDocumentId(string docId) => 
        docId.StartsWith($"{DOCUMENT_PREFIX}{SEPARATOR}") && docId.Contains(COMPOUND_SEPARATOR);
}

// BEST PRACTICE 2: Partitioning strategies for load distribution
public static class GrainPartitioningStrategies
{
    // Strategy 1: Hash-based partitioning
    public static string CreateHashPartitionedKey(string baseKey, int partitionCount = 1000)
    {
        using var sha256 = SHA256.Create();
        var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(baseKey));
        var hashInt = BitConverter.ToInt32(hash, 0);
        var partition = Math.Abs(hashInt) % partitionCount;
        
        return $"{baseKey}:p{partition:D4}";
    }
    
    // Strategy 2: Time-based partitioning for analytics
    public static string CreateTimePartitionedKey(string eventType, DateTimeOffset timestamp, 
        TimePartitionGranularity granularity = TimePartitionGranularity.Daily)
    {
        var partition = granularity switch
        {
            TimePartitionGranularity.Hourly => timestamp.ToString("yyyyMMdd-HH"),
            TimePartitionGranularity.Daily => timestamp.ToString("yyyyMMdd"),
            TimePartitionGranularity.Monthly => timestamp.ToString("yyyyMM"),
            _ => timestamp.ToString("yyyyMMdd")
        };
        
        return $"analytics:{eventType}:{partition}";
    }
    
    // Strategy 3: Geographic partitioning
    public static string CreateGeoPartitionedKey(string baseKey, string regionCode)
    {
        if (string.IsNullOrWhiteSpace(regionCode))
        {
            regionCode = "global";
        }
        
        return $"{baseKey}:region:{regionCode.ToLowerInvariant()}";
    }
    
    // Strategy 4: Customer tier partitioning
    public static string CreateTierPartitionedKey(string customerId, CustomerTier tier)
    {
        var tierCode = tier switch
        {
            CustomerTier.Basic => "basic",
            CustomerTier.Premium => "premium",
            CustomerTier.Enterprise => "enterprise",
            _ => "standard"
        };
        
        return $"customer:{tierCode}:{customerId}";
    }
}

// BEST PRACTICE 3: Identity mapping for complex scenarios
public interface IIdentityMappingService
{
    Task<string> MapExternalIdToGrainKeyAsync(string externalId, string systemName);
    Task<List<string>> GetAlternateIdentitiesAsync(string primaryGrainKey);
    Task RegisterIdentityMappingAsync(string grainKey, string externalId, string systemName);
}

public class IdentityMappingService : IIdentityMappingService
{
    private readonly IGrainFactory grainFactory;
    private readonly ILogger<IdentityMappingService> logger;
    
    // In-memory cache for demonstration (use persistent storage in production)
    private readonly Dictionary<string, string> externalToInternalMap = new();
    private readonly Dictionary<string, List<string>> internalToExternalMap = new();

    public IdentityMappingService(IGrainFactory grainFactory, ILogger<IdentityMappingService> logger)
    {
        this.grainFactory = grainFactory;
        this.logger = logger;
    }

    public async Task<string> MapExternalIdToGrainKeyAsync(string externalId, string systemName)
    {
        var mappingKey = $"{systemName}:{externalId}";
        
        if (externalToInternalMap.TryGetValue(mappingKey, out var grainKey))
        {
            logger.LogDebug("Found existing mapping: {ExternalId} -> {GrainKey}", externalId, grainKey);
            return grainKey;
        }
        
        // Create new internal identity
        grainKey = Guid.NewGuid().ToString();
        
        // Register the mapping
        await RegisterIdentityMappingAsync(grainKey, externalId, systemName);
        
        logger.LogInformation("Created new mapping: {ExternalId} -> {GrainKey}", externalId, grainKey);
        
        return grainKey;
    }

    public async Task<List<string>> GetAlternateIdentitiesAsync(string primaryGrainKey)
    {
        if (internalToExternalMap.TryGetValue(primaryGrainKey, out var alternates))
        {
            return new List<string>(alternates);
        }
        
        return new List<string>();
    }

    public async Task RegisterIdentityMappingAsync(string grainKey, string externalId, string systemName)
    {
        var mappingKey = $"{systemName}:{externalId}";
        
        // Register external -> internal mapping
        externalToInternalMap[mappingKey] = grainKey;
        
        // Register internal -> external mapping
        if (!internalToExternalMap.ContainsKey(grainKey))
        {
            internalToExternalMap[grainKey] = new List<string>();
        }
        
        if (!internalToExternalMap[grainKey].Contains(mappingKey))
        {
            internalToExternalMap[grainKey].Add(mappingKey);
        }
        
        logger.LogDebug("Registered identity mapping: {MappingKey} <-> {GrainKey}", mappingKey, grainKey);
        
        await Task.CompletedTask;
    }
}

// BEST PRACTICE 4: Key collision avoidance patterns
public static class CollisionAvoidancePatterns
{
    // Pattern 1: Namespace isolation
    public static string CreateNamespacedKey(string namespace_, string localKey)
    {
        if (string.IsNullOrWhiteSpace(namespace_))
        {
            throw new ArgumentException("Namespace cannot be null or empty");
        }
        
        if (string.IsNullOrWhiteSpace(localKey))
        {
            throw new ArgumentException("Local key cannot be null or empty");
        }
        
        // Use namespace as prefix to avoid collisions
        return $"ns:{namespace_}:{localKey}";
    }
    
    // Pattern 2: Version-aware keys
    public static string CreateVersionedKey(string baseKey, string version)
    {
        return $"{baseKey}:ver:{version}";
    }
    
    // Pattern 3: Tenant isolation
    public static string CreateTenantIsolatedKey(string tenantId, string resourceKey)
    {
        if (string.IsNullOrWhiteSpace(tenantId))
        {
            throw new ArgumentException("Tenant ID cannot be null or empty");
        }
        
        return $"tenant:{tenantId}:{resourceKey}";
    }
    
    // Pattern 4: Context-aware keys
    public static string CreateContextualKey(string baseKey, Dictionary<string, string> context)
    {
        var contextParts = context
            .Where(kvp => !string.IsNullOrWhiteSpace(kvp.Key) && !string.IsNullOrWhiteSpace(kvp.Value))
            .OrderBy(kvp => kvp.Key) // Ensure consistent ordering
            .Select(kvp => $"{kvp.Key}={kvp.Value}");
        
        var contextString = string.Join(",", contextParts);
        
        return $"{baseKey}:ctx:[{contextString}]";
    }
    
    // Pattern 5: Unique constraint enforcement
    public static async Task<string> CreateUniqueKeyAsync(
        IGrainFactory grainFactory, 
        string baseKey, 
        Func<string, Task<bool>> existsCheck,
        int maxAttempts = 10)
    {
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var candidateKey = attempt == 0 
                ? baseKey 
                : $"{baseKey}-{attempt}";
            
            if (!await existsCheck(candidateKey))
            {
                return candidateKey;
            }
        }
        
        // Fallback to GUID suffix
        return $"{baseKey}-{Guid.NewGuid():N}";
    }
}

// Example usage of identity best practices
public class IdentityBestPracticesDemo
{
    private readonly IGrainFactory grainFactory;
    private readonly IIdentityMappingService identityMapping;
    private readonly ILogger<IdentityBestPracticesDemo> logger;

    public IdentityBestPracticesDemo(
        IGrainFactory grainFactory,
        IIdentityMappingService identityMapping,
        ILogger<IdentityBestPracticesDemo> logger)
    {
        this.grainFactory = grainFactory;
        this.identityMapping = identityMapping;
        this.logger = logger;
    }

    public async Task DemonstrateBestPracticesAsync()
    {
        // Consistent naming conventions
        var userId = GrainIdentityConventions.CreateUserId("user@example.com");
        var userGrain = grainFactory.GetGrain<IUserProfileGrain>(userId);
        
        logger.LogInformation("Created user grain with ID: {UserId}", userId);
        
        // Hash-based partitioning for load distribution
        var partitionedKey = GrainPartitioningStrategies.CreateHashPartitionedKey("analytics-data");
        
        logger.LogInformation("Created partitioned key: {PartitionedKey}", partitionedKey);
        
        // Time-based partitioning for analytics
        var timeKey = GrainPartitioningStrategies.CreateTimePartitionedKey(
            "page-views", 
            DateTimeOffset.UtcNow, 
            TimePartitionGranularity.Hourly);
        
        logger.LogInformation("Created time-partitioned key: {TimeKey}", timeKey);
        
        // External system identity mapping
        var externalId = "ext-system-123";
        var mappedKey = await identityMapping.MapExternalIdToGrainKeyAsync(externalId, "CRM");
        
        logger.LogInformation("Mapped external ID {ExternalId} to grain key: {MappedKey}", externalId, mappedKey);
        
        // Collision avoidance with namespacing
        var namespacedKey = CollisionAvoidancePatterns.CreateNamespacedKey("documents", "contract-123");
        
        logger.LogInformation("Created namespaced key: {NamespacedKey}", namespacedKey);
        
        // Tenant isolation
        var tenantKey = CollisionAvoidancePatterns.CreateTenantIsolatedKey("tenant-abc", "resource-xyz");
        
        logger.LogInformation("Created tenant-isolated key: {TenantKey}", tenantKey);
    }
}

// Supporting types and enums
public record UserProfile
{
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastAccessed { get; set; }
    public DateTimeOffset? LastModified { get; set; }
}

public record DocumentInfo
{
    public Guid DocumentId { get; init; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DocumentStatus Status { get; set; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastModified { get; set; }
    public int Version { get; set; } = 1;
}

public record DocumentUpdateRequest
{
    public string? Title { get; init; }
    public string? Content { get; init; }
}

public record DocumentAccess
{
    public string Operation { get; init; } = string.Empty;
    public DateTimeOffset AccessedAt { get; init; }
    public string AccessedBy { get; init; } = string.Empty;
}

public record DocumentAccessLog
{
    public Guid DocumentId { get; init; }
    public List<DocumentAccess> Accesses { get; init; } = new();
    public int TotalAccesses { get; init; }
}

public record OrderItem
{
    public string OrderId { get; set; } = string.Empty;
    public string ProductId { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? LastModified { get; set; }
}

public record OrderRequest
{
    public string CustomerId { get; init; } = string.Empty;
    public List<OrderRequestItem> Items { get; init; } = new();
}

public record OrderRequestItem
{
    public string ProductId { get; init; } = string.Empty;
    public int Quantity { get; init; }
}

public record OrderItemResult
{
    public string ProductId { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal LineTotal { get; init; }
}

public record OrderProcessingResult
{
    public string OrderId { get; init; } = string.Empty;
    public bool Success { get; init; }
    public string? Error { get; init; }
    public decimal TotalAmount { get; init; }
    public List<OrderItemResult> Items { get; init; } = new();
}

public enum TimePartitionGranularity
{
    Hourly,
    Daily,
    Monthly
}

public enum CustomerTier
{
    Basic,
    Standard,
    Premium,
    Enterprise
}

public interface IOrderProcessingGrain : IGrain
{
    Task<OrderProcessingResult> ProcessOrderAsync(OrderRequest request);
    Task SubscribeToUpdatesAsync(IOrderStatusObserver observer);
    Task UnsubscribeFromUpdatesAsync(IOrderStatusObserver observer);
}

## Communication Patterns

Orleans grains communicate through well-defined patterns that support both synchronous and asynchronous interactions. These patterns enable scalable, distributed communication while maintaining type safety and error handling.

### Synchronous Communication

Synchronous communication uses request-response patterns where the caller waits for the operation to complete and receive a result.

```csharp
namespace ECommerce.Orleans.Communication;

using Orleans;
using Microsoft.Extensions.Logging;

// Request-response patterns with proper error handling
public interface IOrderProcessorGrain : IGrain
{
    Task<OrderValidationResult> ValidateOrderAsync(Order order);
    Task<PricingResult> CalculatePricingAsync(List<OrderItem> items);
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request);
}

public class OrderProcessorGrain : Grain, IOrderProcessorGrain
{
    private readonly ILogger<OrderProcessorGrain> logger;
    private readonly IGrainFactory grainFactory;

    public OrderProcessorGrain(ILogger<OrderProcessorGrain> logger, IGrainFactory grainFactory)
    {
        this.logger = logger;
        this.grainFactory = grainFactory;
    }

    public async Task<OrderValidationResult> ValidateOrderAsync(Order order)
    {
        var orderId = this.GetPrimaryKeyString();
        logger.LogInformation("Validating order {OrderId}", orderId);

        try
        {
            // Synchronous call to customer grain
            var customerGrain = grainFactory.GetGrain<ICustomerGrain>(order.CustomerId);
            var customer = await customerGrain.GetCustomerDetailsAsync();

            // Validate customer eligibility
            if (!customer.IsActive)
            {
                return new OrderValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Customer account is inactive",
                    ValidationCode = ValidationCode.InactiveCustomer
                };
            }

            // Synchronous call to inventory grain for each item
            var validationTasks = order.Items.Select(async item =>
            {
                var inventoryGrain = grainFactory.GetGrain<IInventoryGrain>(item.ProductId);
                var availability = await inventoryGrain.CheckAvailabilityAsync(item.Quantity);
                
                return new ItemValidation
                {
                    ProductId = item.ProductId,
                    IsAvailable = availability.IsAvailable,
                    AvailableQuantity = availability.Quantity,
                    RequestedQuantity = item.Quantity
                };
            });

            var itemValidations = await Task.WhenAll(validationTasks);
            var unavailableItems = itemValidations.Where(v => !v.IsAvailable).ToList();

            if (unavailableItems.Count > 0)
            {
                return new OrderValidationResult
                {
                    IsValid = false,
                    ErrorMessage = "Some items are out of stock",
                    ValidationCode = ValidationCode.InsufficientInventory,
                    UnavailableItems = unavailableItems
                };
            }

            logger.LogInformation("Order {OrderId} validation successful", orderId);
            
            return new OrderValidationResult
            {
                IsValid = true,
                ValidationCode = ValidationCode.Success,
                ValidatedAt = DateTimeOffset.UtcNow
            };
        }
        catch (TimeoutException ex)
        {
            logger.LogError(ex, "Timeout during order validation for {OrderId}", orderId);
            throw new OrderValidationException("Validation timeout", ex);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error validating order {OrderId}", orderId);
            throw new OrderValidationException("Validation failed", ex);
        }
    }

    public async Task<PricingResult> CalculatePricingAsync(List<OrderItem> items)
    {
        logger.LogInformation("Calculating pricing for {ItemCount} items", items.Count);

        try
        {
            // Synchronous calls with timeout management
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));

            var pricingTasks = items.Select(async item =>
            {
                var productGrain = grainFactory.GetGrain<IProductGrain>(item.ProductId);
                var pricing = await productGrain.GetPricingAsync(item.Quantity, cts.Token);
                
                return new ItemPricing
                {
                    ProductId = item.ProductId,
                    UnitPrice = pricing.UnitPrice,
                    Quantity = item.Quantity,
                    LineTotal = pricing.UnitPrice * item.Quantity,
                    AppliedDiscounts = pricing.Discounts
                };
            });

            var itemPricings = await Task.WhenAll(pricingTasks);
            
            var subtotal = itemPricings.Sum(p => p.LineTotal);
            var totalDiscounts = itemPricings.Sum(p => p.AppliedDiscounts);
            
            // Calculate tax synchronously
            var taxGrain = grainFactory.GetGrain<ITaxCalculatorGrain>("default");
            var taxAmount = await taxGrain.CalculateTaxAsync(subtotal - totalDiscounts, cts.Token);

            return new PricingResult
            {
                Subtotal = subtotal,
                TotalDiscounts = totalDiscounts,
                TaxAmount = taxAmount,
                FinalTotal = subtotal - totalDiscounts + taxAmount,
                ItemPricings = itemPricings.ToList(),
                CalculatedAt = DateTimeOffset.UtcNow
            };
        }
        catch (OperationCanceledException)
        {
            logger.LogWarning("Pricing calculation timeout");
            throw new PricingException("Pricing calculation timeout");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calculating pricing");
            throw new PricingException("Pricing calculation failed", ex);
        }
    }

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request)
    {
        logger.LogInformation("Processing payment for order {OrderId}", request.OrderId);

        try
        {
            // Chain of synchronous calls for payment processing
            var paymentGrain = grainFactory.GetGrain<IPaymentGrain>(request.PaymentMethodId);
            
            // 1. Validate payment method
            var validation = await paymentGrain.ValidatePaymentMethodAsync(request.PaymentMethodId);
            if (!validation.IsValid)
            {
                return new PaymentResult
                {
                    IsSuccessful = false,
                    ErrorCode = PaymentErrorCode.InvalidPaymentMethod,
                    ErrorMessage = validation.ErrorMessage
                };
            }

            // 2. Authorize payment
            var authorization = await paymentGrain.AuthorizePaymentAsync(request.Amount, request.Currency);
            if (!authorization.IsAuthorized)
            {
                return new PaymentResult
                {
                    IsSuccessful = false,
                    ErrorCode = PaymentErrorCode.AuthorizationFailed,
                    ErrorMessage = authorization.ErrorMessage,
                    TransactionId = authorization.TransactionId
                };
            }

            // 3. Capture payment
            var capture = await paymentGrain.CapturePaymentAsync(authorization.AuthorizationId);
            if (!capture.IsSuccessful)
            {
                // Void the authorization if capture fails
                await paymentGrain.VoidAuthorizationAsync(authorization.AuthorizationId);
                
                return new PaymentResult
                {
                    IsSuccessful = false,
                    ErrorCode = PaymentErrorCode.CaptureFailed,
                    ErrorMessage = capture.ErrorMessage,
                    TransactionId = authorization.TransactionId
                };
            }

            logger.LogInformation("Payment processed successfully for order {OrderId}", request.OrderId);

            return new PaymentResult
            {
                IsSuccessful = true,
                TransactionId = capture.TransactionId,
                AuthorizationId = authorization.AuthorizationId,
                Amount = request.Amount,
                Currency = request.Currency,
                ProcessedAt = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing payment for order {OrderId}", request.OrderId);
            throw new PaymentException("Payment processing failed", ex);
        }
    }
}
```

### Fire-and-Forget Patterns

Fire-and-forget patterns enable one-way communication where the caller doesn't wait for a response, improving performance for notifications and event broadcasting.

```csharp
namespace ECommerce.Orleans.Notifications;

using Orleans;
using Microsoft.Extensions.Logging;

// Fire-and-forget notification patterns
public interface INotificationGrain : IGrain
{
    Task SendOrderConfirmationAsync(OrderConfirmation confirmation);
    Task BroadcastInventoryUpdateAsync(InventoryUpdate update);
    Task NotifyCustomerServiceAsync(CustomerServiceAlert alert);
    
    // Void methods for true fire-and-forget (no Task return)
    void LogUserActivity(string userId, string activity);
    void RecordMetric(string metricName, double value);
}

public class NotificationGrain : Grain, INotificationGrain
{
    private readonly ILogger<NotificationGrain> logger;
    private readonly IGrainFactory grainFactory;

    public NotificationGrain(ILogger<NotificationGrain> logger, IGrainFactory grainFactory)
    {
        this.logger = logger;
        this.grainFactory = grainFactory;
    }

    public async Task SendOrderConfirmationAsync(OrderConfirmation confirmation)
    {
        var notificationId = this.GetPrimaryKeyString();
        logger.LogInformation("Sending order confirmation for {OrderId}", confirmation.OrderId);

        // Fire-and-forget to multiple notification channels
        var tasks = new List<Task>();

        // Email notification (fire-and-forget)
        var emailGrain = grainFactory.GetGrain<IEmailGrain>("order-confirmations");
        tasks.Add(emailGrain.SendEmailAsync(new EmailMessage
        {
            To = confirmation.CustomerEmail,
            Subject = $"Order Confirmation - {confirmation.OrderId}",
            Body = confirmation.EmailBody,
            Priority = EmailPriority.Normal
        }));

        // SMS notification (fire-and-forget)
        if (!string.IsNullOrEmpty(confirmation.CustomerPhone))
        {
            var smsGrain = grainFactory.GetGrain<ISmsGrain>("order-notifications");
            tasks.Add(smsGrain.SendSmsAsync(new SmsMessage
            {
                To = confirmation.CustomerPhone,
                Message = $"Your order {confirmation.OrderId} has been confirmed!",
                Priority = SmsPriority.Normal
            }));
        }

        // Push notification (fire-and-forget)
        if (!string.IsNullOrEmpty(confirmation.DeviceToken))
        {
            var pushGrain = grainFactory.GetGrain<IPushNotificationGrain>("order-updates");
            tasks.Add(pushGrain.SendPushNotificationAsync(new PushNotification
            {
                DeviceToken = confirmation.DeviceToken,
                Title = "Order Confirmed",
                Body = $"Order {confirmation.OrderId} confirmed",
                Data = new Dictionary<string, string> { ["orderId"] = confirmation.OrderId }
            }));
        }

        // Don't wait for all notifications - true fire-and-forget
        // Just log any failures without blocking
        foreach (var task in tasks)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await task;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Notification delivery failed for order {OrderId}", confirmation.OrderId);
                }
            });
        }

        logger.LogInformation("Order confirmation notifications initiated for {OrderId}", confirmation.OrderId);
    }

    public async Task BroadcastInventoryUpdateAsync(InventoryUpdate update)
    {
        logger.LogInformation("Broadcasting inventory update for product {ProductId}", update.ProductId);

        // Fan-out pattern - notify all interested parties without waiting
        var broadcastTasks = new List<Task>();

        // Notify all active order processors
        for (int i = 0; i < 10; i++) // Assume 10 order processor instances
        {
            var orderProcessor = grainFactory.GetGrain<IOrderProcessorGrain>($"processor-{i}");
            broadcastTasks.Add(orderProcessor.HandleInventoryUpdateAsync(update));
        }

        // Notify recommendation engine
        var recommendationEngine = grainFactory.GetGrain<IRecommendationGrain>("product-recommendations");
        broadcastTasks.Add(recommendationEngine.UpdateProductAvailabilityAsync(update.ProductId, update.NewQuantity));

        // Notify analytics service
        var analyticsGrain = grainFactory.GetGrain<IAnalyticsGrain>("inventory-analytics");
        broadcastTasks.Add(analyticsGrain.RecordInventoryChangeAsync(update));

        // Fire-and-forget - don't wait for completion
        _ = Task.WhenAll(broadcastTasks).ContinueWith(task =>
        {
            if (task.Exception != null)
            {
                logger.LogWarning(task.Exception, "Some inventory update notifications failed");
            }
        }, TaskScheduler.Default);

        logger.LogInformation("Inventory update broadcast initiated for product {ProductId}", update.ProductId);
    }

    public async Task NotifyCustomerServiceAsync(CustomerServiceAlert alert)
    {
        logger.LogInformation("Sending customer service alert: {AlertType}", alert.Type);

        // Priority-based notification routing
        var notificationTasks = alert.Priority switch
        {
            AlertPriority.Critical => await SendCriticalAlertAsync(alert),
            AlertPriority.High => await SendHighPriorityAlertAsync(alert),
            AlertPriority.Normal => await SendNormalAlertAsync(alert),
            _ => new List<Task>()
        };

        // Fire-and-forget notification delivery
        _ = Task.WhenAll(notificationTasks).ContinueWith(task =>
        {
            var successCount = notificationTasks.Count(t => t.Status == TaskStatus.RanToCompletion);
            logger.LogInformation("Customer service alert delivered: {Success}/{Total}", successCount, notificationTasks.Count);
        }, TaskScheduler.Default);
    }

    private async Task<List<Task>> SendCriticalAlertAsync(CustomerServiceAlert alert)
    {
        var tasks = new List<Task>();

        // Critical alerts go to all channels immediately
        var slackGrain = grainFactory.GetGrain<ISlackGrain>("critical-alerts");
        tasks.Add(slackGrain.SendMessageAsync(alert.Message, "#critical-alerts"));

        var emailGrain = grainFactory.GetGrain<IEmailGrain>("critical-notifications");
        tasks.Add(emailGrain.SendEmailAsync(new EmailMessage
        {
            To = "support-team@company.com",
            Subject = $"CRITICAL ALERT: {alert.Type}",
            Body = alert.Message,
            Priority = EmailPriority.High
        }));

        // SMS to on-call team
        var smsGrain = grainFactory.GetGrain<ISmsGrain>("emergency-notifications");
        tasks.Add(smsGrain.SendSmsAsync(new SmsMessage
        {
            To = "+1234567890", // On-call number
            Message = $"CRITICAL: {alert.Type}",
            Priority = SmsPriority.High
        }));

        return tasks;
    }

    private async Task<List<Task>> SendHighPriorityAlertAsync(CustomerServiceAlert alert)
    {
        var tasks = new List<Task>();

        var slackGrain = grainFactory.GetGrain<ISlackGrain>("alerts");
        tasks.Add(slackGrain.SendMessageAsync(alert.Message, "#support"));

        var emailGrain = grainFactory.GetGrain<IEmailGrain>("alert-notifications");
        tasks.Add(emailGrain.SendEmailAsync(new EmailMessage
        {
            To = "support-team@company.com",
            Subject = $"High Priority Alert: {alert.Type}",
            Body = alert.Message,
            Priority = EmailPriority.Normal
        }));

        return tasks;
    }

    private async Task<List<Task>> SendNormalAlertAsync(CustomerServiceAlert alert)
    {
        var tasks = new List<Task>();

        var slackGrain = grainFactory.GetGrain<ISlackGrain>("notifications");
        tasks.Add(slackGrain.SendMessageAsync(alert.Message, "#general"));

        return tasks;
    }

    // True fire-and-forget methods (void return type)
    public void LogUserActivity(string userId, string activity)
    {
        // Completely asynchronous - no Task return, no waiting
        _ = Task.Run(async () =>
        {
            try
            {
                var loggingGrain = grainFactory.GetGrain<IUserActivityGrain>(userId);
                await loggingGrain.RecordActivityAsync(activity, DateTimeOffset.UtcNow);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to log user activity for {UserId}", userId);
            }
        });
    }

    public void RecordMetric(string metricName, double value)
    {
        // Fire-and-forget metric recording
        _ = Task.Run(async () =>
        {
            try
            {
                var metricsGrain = grainFactory.GetGrain<IMetricsGrain>("application-metrics");
                await metricsGrain.RecordMetricAsync(metricName, value, DateTimeOffset.UtcNow);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to record metric {MetricName}", metricName);
            }
        });
    }
}
```

### Grain-to-Grain Communication

Inter-grain communication patterns enable complex distributed workflows while avoiding common pitfalls like circular references and cascading failures.

```csharp
namespace ECommerce.Orleans.GrainCommunication;

using Orleans;
using Microsoft.Extensions.Logging;

// Complex grain-to-grain communication patterns
public interface IOrderWorkflowGrain : IGrain
{
    Task<WorkflowResult> ProcessOrderAsync(Order order);
    Task<WorkflowStatus> GetWorkflowStatusAsync();
}

public class OrderWorkflowGrain : Grain, IOrderWorkflowGrain
{
    private readonly ILogger<OrderWorkflowGrain> logger;
    private readonly IGrainFactory grainFactory;
    private WorkflowState workflowState = new();

    public OrderWorkflowGrain(ILogger<OrderWorkflowGrain> logger, IGrainFactory grainFactory)
    {
        this.logger = logger;
        this.grainFactory = grainFactory;
    }

    public async Task<WorkflowResult> ProcessOrderAsync(Order order)
    {
        var workflowId = this.GetPrimaryKeyString();
        logger.LogInformation("Starting order workflow {WorkflowId} for order {OrderId}", workflowId, order.OrderId);

        workflowState.Status = WorkflowStatus.InProgress;
        workflowState.StartedAt = DateTimeOffset.UtcNow;

        try
        {
            // Step 1: Validate order (direct grain call)
            var validationResult = await ValidateOrderAsync(order);
            if (!validationResult.IsValid)
            {
                return await CompleteWorkflowAsync(WorkflowStatus.Failed, validationResult.ErrorMessage);
            }

            // Step 2: Reserve inventory (chain of responsibility pattern)
            var reservationResult = await ReserveInventoryAsync(order.Items);
            if (!reservationResult.IsSuccessful)
            {
                return await CompleteWorkflowAsync(WorkflowStatus.Failed, "Inventory reservation failed");
            }

            // Step 3: Calculate pricing (parallel grain calls)
            var pricingResult = await CalculateFinalPricingAsync(order);
            
            // Step 4: Process payment (conditional chain)
            var paymentResult = await ProcessPaymentAsync(order, pricingResult.FinalTotal);
            if (!paymentResult.IsSuccessful)
            {
                // Rollback inventory reservation
                await RollbackInventoryReservationAsync(reservationResult.ReservationId);
                return await CompleteWorkflowAsync(WorkflowStatus.Failed, "Payment processing failed");
            }

            // Step 5: Create fulfillment order (fan-out pattern)
            await CreateFulfillmentOrderAsync(order, paymentResult.TransactionId);

            // Step 6: Send notifications (fire-and-forget)
            _ = SendNotificationsAsync(order, paymentResult);

            return await CompleteWorkflowAsync(WorkflowStatus.Completed, "Order processed successfully");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Workflow failed for order {OrderId}", order.OrderId);
            return await CompleteWorkflowAsync(WorkflowStatus.Failed, ex.Message);
        }
    }

    private async Task<OrderValidationResult> ValidateOrderAsync(Order order)
    {
        logger.LogDebug("Validating order {OrderId}", order.OrderId);
        
        // Direct grain-to-grain call
        var validationGrain = grainFactory.GetGrain<IOrderValidatorGrain>("order-validator");
        return await validationGrain.ValidateAsync(order);
    }

    private async Task<InventoryReservationResult> ReserveInventoryAsync(List<OrderItem> items)
    {
        logger.LogDebug("Reserving inventory for {ItemCount} items", items.Count);

        // Chain of responsibility pattern - each item handled by its inventory grain
        var reservationTasks = items.Select(async item =>
        {
            var inventoryGrain = grainFactory.GetGrain<IInventoryGrain>(item.ProductId);
            return await inventoryGrain.ReserveQuantityAsync(item.Quantity, TimeSpan.FromMinutes(15));
        });

        var reservations = await Task.WhenAll(reservationTasks);
        
        // Check if all reservations succeeded
        var failedReservations = reservations.Where(r => !r.IsSuccessful).ToList();
        if (failedReservations.Count > 0)
        {
            // Rollback successful reservations
            var rollbackTasks = reservations
                .Where(r => r.IsSuccessful)
                .Select(r => RollbackSingleReservationAsync(r.ReservationId));
            
            await Task.WhenAll(rollbackTasks);
            
            return new InventoryReservationResult
            {
                IsSuccessful = false,
                ErrorMessage = $"Failed to reserve {failedReservations.Count} items"
            };
        }

        return new InventoryReservationResult
        {
            IsSuccessful = true,
            ReservationId = Guid.NewGuid().ToString(),
            ReservedItems = reservations.ToList()
        };
    }

    private async Task<PricingResult> CalculateFinalPricingAsync(Order order)
    {
        logger.LogDebug("Calculating final pricing for order {OrderId}", order.OrderId);

        // Parallel grain calls for different pricing components
        var pricingTasks = new List<Task<decimal>>();

        // Base pricing
        var productPricingGrain = grainFactory.GetGrain<IProductPricingGrain>("base-pricing");
        pricingTasks.Add(productPricingGrain.CalculateBasePriceAsync(order.Items));

        // Discounts
        var discountGrain = grainFactory.GetGrain<IDiscountGrain>(order.CustomerId);
        pricingTasks.Add(discountGrain.CalculateDiscountsAsync(order.Items));

        // Tax calculation
        var taxGrain = grainFactory.GetGrain<ITaxGrain>(order.ShippingAddress.Region);
        pricingTasks.Add(taxGrain.CalculateTaxAsync(order.Items, order.ShippingAddress));

        // Shipping cost
        var shippingGrain = grainFactory.GetGrain<IShippingGrain>("standard-shipping");
        pricingTasks.Add(shippingGrain.CalculateShippingCostAsync(order.Items, order.ShippingAddress));

        var results = await Task.WhenAll(pricingTasks);
        
        return new PricingResult
        {
            BasePrice = results[0],
            Discounts = results[1],
            Tax = results[2],
            Shipping = results[3],
            FinalTotal = results[0] - results[1] + results[2] + results[3]
        };
    }

    private async Task<PaymentResult> ProcessPaymentAsync(Order order, decimal amount)
    {
        logger.LogDebug("Processing payment for order {OrderId}, amount {Amount}", order.OrderId, amount);

        // Conditional chain based on payment method
        var paymentGrain = order.PaymentMethod.Type switch
        {
            PaymentType.CreditCard => grainFactory.GetGrain<ICreditCardGrain>(order.PaymentMethod.Id),
            PaymentType.PayPal => grainFactory.GetGrain<IPayPalGrain>(order.PaymentMethod.Id),
            PaymentType.BankTransfer => grainFactory.GetGrain<IBankTransferGrain>(order.PaymentMethod.Id),
            _ => throw new NotSupportedException($"Payment type {order.PaymentMethod.Type} not supported")
        };

        return await paymentGrain.ProcessPaymentAsync(new PaymentRequest
        {
            OrderId = order.OrderId,
            Amount = amount,
            Currency = order.Currency,
            PaymentMethodId = order.PaymentMethod.Id
        });
    }

    private async Task CreateFulfillmentOrderAsync(Order order, string transactionId)
    {
        logger.LogDebug("Creating fulfillment order for {OrderId}", order.OrderId);

        // Fan-out pattern - create fulfillment requests for different warehouses
        var warehouseGroups = order.Items
            .GroupBy(item => GetWarehouseForProduct(item.ProductId))
            .ToList();

        var fulfillmentTasks = warehouseGroups.Select(async group =>
        {
            var warehouseGrain = grainFactory.GetGrain<IWarehouseGrain>(group.Key);
            return await warehouseGrain.CreateFulfillmentOrderAsync(new FulfillmentRequest
            {
                OrderId = order.OrderId,
                TransactionId = transactionId,
                Items = group.ToList(),
                ShippingAddress = order.ShippingAddress,
                Priority = order.Priority
            });
        });

        var fulfillmentResults = await Task.WhenAll(fulfillmentTasks);
        
        // Log fulfillment creation results
        foreach (var result in fulfillmentResults)
        {
            if (result.IsSuccessful)
            {
                logger.LogInformation("Fulfillment order created: {FulfillmentId}", result.FulfillmentId);
            }
            else
            {
                logger.LogWarning("Fulfillment order failed: {Error}", result.ErrorMessage);
            }
        }
    }

    private async Task SendNotificationsAsync(Order order, PaymentResult paymentResult)
    {
        // Fire-and-forget notification pattern
        var notificationGrain = grainFactory.GetGrain<INotificationGrain>("order-notifications");
        
        await notificationGrain.SendOrderConfirmationAsync(new OrderConfirmation
        {
            OrderId = order.OrderId,
            CustomerId = order.CustomerId,
            CustomerEmail = order.CustomerEmail,
            CustomerPhone = order.CustomerPhone,
            TransactionId = paymentResult.TransactionId,
            EmailBody = GenerateConfirmationEmail(order, paymentResult)
        });
    }

    private async Task<WorkflowResult> CompleteWorkflowAsync(WorkflowStatus status, string message)
    {
        workflowState.Status = status;
        workflowState.CompletedAt = DateTimeOffset.UtcNow;
        workflowState.Duration = workflowState.CompletedAt.Value - workflowState.StartedAt;
        workflowState.Message = message;

        logger.LogInformation(
            "Workflow completed with status {Status} in {Duration}ms",
            status, workflowState.Duration?.TotalMilliseconds);

        return new WorkflowResult
        {
            Status = status,
            Message = message,
            Duration = workflowState.Duration ?? TimeSpan.Zero,
            CompletedAt = workflowState.CompletedAt ?? DateTimeOffset.UtcNow
        };
    }

    private async Task RollbackInventoryReservationAsync(string reservationId)
    {
        // Rollback pattern - best effort cleanup
        try
        {
            var inventoryManagerGrain = grainFactory.GetGrain<IInventoryManagerGrain>("inventory-manager");
            await inventoryManagerGrain.RollbackReservationAsync(reservationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to rollback inventory reservation {ReservationId}", reservationId);
        }
    }

    private async Task RollbackSingleReservationAsync(string reservationId)
    {
        try
        {
            // Call specific inventory grain to release the reservation
            var inventoryGrain = GrainFactory.GetGrain<IInventoryGrain>(reservationId.ProductId);
            await inventoryGrain.ReleaseReservationAsync(reservationId.ReservationId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to rollback single reservation {ReservationId}", reservationId);
        }
    }

    private string GetWarehouseForProduct(string productId)
    {
        // Simple warehouse assignment logic
        return productId.GetHashCode() % 3 switch
        {
            0 => "warehouse-east",
            1 => "warehouse-west",
            _ => "warehouse-central"
        };
    }

    private string GenerateConfirmationEmail(Order order, PaymentResult paymentResult)
    {
        return $"Your order {order.OrderId} has been confirmed. Transaction ID: {paymentResult.TransactionId}";
    }

    public Task<WorkflowStatus> GetWorkflowStatusAsync()
    {
        return Task.FromResult(workflowState.Status);
    }
}

// Supporting types and interfaces for communication patterns
public record WorkflowState
{
    public WorkflowStatus Status { get; set; } = WorkflowStatus.NotStarted;
    public DateTimeOffset StartedAt { get; set; }
    public DateTimeOffset? CompletedAt { get; set; }
    public TimeSpan? Duration { get; set; }
    public string Message { get; set; } = string.Empty;
}

public record WorkflowResult
{
    public WorkflowStatus Status { get; init; }
    public string Message { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; }
    public DateTimeOffset CompletedAt { get; init; }
}

public enum WorkflowStatus
{
    NotStarted,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

public record OrderConfirmation
{
    public string OrderId { get; init; } = string.Empty;
    public string CustomerId { get; init; } = string.Empty;
    public string CustomerEmail { get; init; } = string.Empty;
    public string CustomerPhone { get; init; } = string.Empty;
    public string DeviceToken { get; init; } = string.Empty;
    public string TransactionId { get; init; } = string.Empty;
    public string EmailBody { get; init; } = string.Empty;
}

public record InventoryUpdate
{
    public string ProductId { get; init; } = string.Empty;
    public int OldQuantity { get; init; }
    public int NewQuantity { get; init; }
    public DateTimeOffset UpdatedAt { get; init; } = DateTimeOffset.UtcNow;
}

public record CustomerServiceAlert
{
    public string Type { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public AlertPriority Priority { get; init; } = AlertPriority.Normal;
    public Dictionary<string, string> Metadata { get; init; } = new();
}

public enum AlertPriority
{
    Normal,
    High,
    Critical
}

public record InventoryReservationResult
{
    public bool IsSuccessful { get; init; }
    public string ReservationId { get; init; } = string.Empty;
    public string ErrorMessage { get; init; } = string.Empty;
    public List<ReservationItem> ReservedItems { get; init; } = new();
}

public record ReservationItem
{
    public string ProductId { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public string ReservationId { get; init; } = string.Empty;
    public bool IsSuccessful { get; init; }
}

public record FulfillmentRequest
{
    public string OrderId { get; init; } = string.Empty;
    public string TransactionId { get; init; } = string.Empty;
    public List<OrderItem> Items { get; init; } = new();
    public Address ShippingAddress { get; init; } = new();
    public OrderPriority Priority { get; init; } = OrderPriority.Standard;
}

public enum OrderPriority
{
    Standard,
    Express,
    Overnight
}

// Exception types for communication patterns
public class OrderValidationException : Exception
{
    public OrderValidationException(string message) : base(message) { }
    public OrderValidationException(string message, Exception innerException) : base(message, innerException) { }
}

public class PricingException : Exception
{
    public PricingException(string message) : base(message) { }
    public PricingException(string message, Exception innerException) : base(message, innerException) { }
}
```

## Grain Activation and Deactivation

Orleans automatically manages grain lifecycle, but understanding activation triggers and deactivation conditions helps you build efficient, scalable applications. This section covers what causes grains to activate, when they deactivate, and how to control this behavior.

### Activation Triggers

Grain activation occurs when Orleans routes the first call to a grain instance. Understanding these triggers helps optimize application performance and resource usage.

```csharp
namespace DocumentProcessor.Orleans.Activation;

using Orleans;
using Microsoft.Extensions.Logging;

// Demonstrates various activation triggers
public interface IDocumentAnalyzerGrain : IGrain
{
    Task<AnalysisResult> AnalyzeDocumentAsync(string documentId);
    Task<ProcessingStatus> GetStatusAsync();
    Task ScheduleAnalysisAsync(TimeSpan delay);
}

public class DocumentAnalyzerGrain : Grain, IDocumentAnalyzerGrain
{
    private readonly ILogger<DocumentAnalyzerGrain> logger;
    private readonly List<string> activationTriggers = new();
    private DateTimeOffset activationTime;

    public DocumentAnalyzerGrain(ILogger<DocumentAnalyzerGrain> logger)
    {
        this.logger = logger;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var grainId = this.GetPrimaryKeyString();
        activationTime = DateTimeOffset.UtcNow;
        
        logger.LogInformation("DocumentAnalyzerGrain {GrainId} activated at {Time}", 
            grainId, activationTime);
        
        return base.OnActivateAsync(cancellationToken);
    }

    // TRIGGER 1: Direct method call - most common activation trigger
    public async Task<AnalysisResult> AnalyzeDocumentAsync(string documentId)
    {
        var grainId = this.GetPrimaryKeyString();
        activationTriggers.Add($"Method call: AnalyzeDocumentAsync at {DateTimeOffset.UtcNow}");
        
        logger.LogInformation("Analyzing document {DocumentId} for grain {GrainId}", 
            documentId, grainId);

        try
        {
            // Simulate document analysis
            await Task.Delay(1000);
            
            var result = new AnalysisResult
            {
                DocumentId = documentId,
                GrainId = grainId,
                AnalyzedAt = DateTimeOffset.UtcNow,
                Status = AnalysisStatus.Completed,
                ActivationTriggers = new List<string>(activationTriggers),
                ProcessingDuration = DateTimeOffset.UtcNow - activationTime
            };
            
            logger.LogInformation("Analysis completed for document {DocumentId}", documentId);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Analysis failed for document {DocumentId}", documentId);
            throw;
        }
    }

    // TRIGGER 2: Status queries also cause activation if grain isn't active
    public Task<ProcessingStatus> GetStatusAsync()
    {
        activationTriggers.Add($"Status query at {DateTimeOffset.UtcNow}");
        
        var status = new ProcessingStatus
        {
            GrainId = this.GetPrimaryKeyString(),
            IsActive = true,
            ActivatedAt = activationTime,
            TriggerHistory = new List<string>(activationTriggers)
        };
        
        return Task.FromResult(status);
    }

    // TRIGGER 3: Timer-based activation (scheduled work)
    public async Task ScheduleAnalysisAsync(TimeSpan delay)
    {
        var grainId = this.GetPrimaryKeyString();
        activationTriggers.Add($"Scheduled analysis at {DateTimeOffset.UtcNow}");
        
        logger.LogInformation("Scheduling analysis for grain {GrainId} with delay {Delay}", 
            grainId, delay);
        
        // Register a timer that will keep the grain active and trigger work
        this.RegisterTimer(
            callback: PerformScheduledAnalysis,
            state: grainId,
            dueTime: delay,
            period: TimeSpan.FromMinutes(5) // Repeat every 5 minutes
        );
        
        logger.LogInformation("Scheduled analysis timer registered for grain {GrainId}", grainId);
    }

    private async Task PerformScheduledAnalysis(object state)
    {
        var grainId = (string)state;
        activationTriggers.Add($"Timer callback at {DateTimeOffset.UtcNow}");
        
        logger.LogInformation("Performing scheduled analysis for grain {GrainId}", grainId);
        
        try
        {
            // Simulate scheduled work
            await Task.Delay(500);
            
            logger.LogInformation("Scheduled analysis completed for grain {GrainId}", grainId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Scheduled analysis failed for grain {GrainId}", grainId);
        }
    }
}

// TRIGGER 4: Stream events cause activation when grain subscribes to streams
public interface IStreamProcessorGrain : IGrain
{
    Task SubscribeToDocumentEventsAsync();
    Task<List<string>> GetProcessedEventsAsync();
}

public class StreamProcessorGrain : Grain, IStreamProcessorGrain
{
    private readonly ILogger<StreamProcessorGrain> logger;
    private readonly List<string> processedEvents = new();
    private StreamSubscriptionHandle<DocumentEvent>? streamSubscription;

    public StreamProcessorGrain(ILogger<StreamProcessorGrain> logger)
    {
        this.logger = logger;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var grainId = this.GetPrimaryKeyString();
        logger.LogInformation("StreamProcessorGrain {GrainId} activated by stream event", grainId);
        
        return base.OnActivateAsync(cancellationToken);
    }

    public async Task SubscribeToDocumentEventsAsync()
    {
        var grainId = this.GetPrimaryKeyString();
        logger.LogInformation("Subscribing to document events for grain {GrainId}", grainId);
        
        // Get stream provider and subscribe to events
        var streamProvider = this.GetStreamProvider("DocumentEvents");
        var stream = streamProvider.GetStream<DocumentEvent>("documents", grainId);
        
        // Stream events will cause grain activation when they arrive
        streamSubscription = await stream.SubscribeAsync(OnDocumentEvent);
        
        logger.LogInformation("Stream subscription created for grain {GrainId}", grainId);
    }

    private async Task OnDocumentEvent(DocumentEvent documentEvent, StreamSequenceToken token)
    {
        var grainId = this.GetPrimaryKeyString();
        var eventInfo = $"Event {documentEvent.EventId} at {DateTimeOffset.UtcNow}";
        
        processedEvents.Add(eventInfo);
        
        logger.LogInformation("Processing document event {EventId} for grain {GrainId}", 
            documentEvent.EventId, grainId);
        
        try
        {
            // Process the stream event
            await Task.Delay(100);
            
            logger.LogDebug("Processed event {EventId} for grain {GrainId}", 
                documentEvent.EventId, grainId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process event {EventId} for grain {GrainId}", 
                documentEvent.EventId, grainId);
        }
    }

    public Task<List<string>> GetProcessedEventsAsync()
    {
        return Task.FromResult(new List<string>(processedEvents));
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        // Cleanup stream subscription on deactivation
        if (streamSubscription != null)
        {
            await streamSubscription.UnsubscribeAsync();
        }
        
        await base.OnDeactivateAsync(reason, cancellationToken);
    }
}

// TRIGGER 5: Grain-to-grain calls cause activation of target grains
public interface IGrainActivationDemoGrain : IGrain
{
    Task DemonstrateActivationCascadeAsync();
}

public class GrainActivationDemoGrain : Grain, IGrainActivationDemoGrain
{
    private readonly ILogger<GrainActivationDemoGrain> logger;
    private readonly IGrainFactory grainFactory;

    public GrainActivationDemoGrain(ILogger<GrainActivationDemoGrain> logger, IGrainFactory grainFactory)
    {
        this.logger = logger;
        this.grainFactory = grainFactory;
    }

    public async Task DemonstrateActivationCascadeAsync()
    {
        var grainId = this.GetPrimaryKeyString();
        logger.LogInformation("Demonstrating activation cascade from grain {GrainId}", grainId);

        // These calls will trigger activation of target grains if they're not already active
        var tasks = new List<Task>();

        // Call to DocumentAnalyzerGrain - will activate if not active
        var analyzerGrain = grainFactory.GetGrain<IDocumentAnalyzerGrain>("analyzer-1");
        tasks.Add(analyzerGrain.GetStatusAsync());

        // Call to StreamProcessorGrain - will activate if not active
        var processorGrain = grainFactory.GetGrain<IStreamProcessorGrain>("processor-1");
        tasks.Add(processorGrain.GetProcessedEventsAsync());

        // Multiple calls to different grain instances
        for (int i = 0; i < 5; i++)
        {
            var grain = grainFactory.GetGrain<IDocumentAnalyzerGrain>($"analyzer-{i}");
            tasks.Add(grain.GetStatusAsync());
        }

        await Task.WhenAll(tasks);
        
        logger.LogInformation("Activation cascade completed from grain {GrainId}", grainId);
    }
}
```

### Deactivation Conditions

Orleans automatically deactivates grains based on various conditions. Understanding these helps optimize resource usage and application performance.

```csharp
namespace DocumentProcessor.Orleans.Deactivation;

using Orleans;
using Orleans.Configuration;
using Microsoft.Extensions.Logging;

// Demonstrates various deactivation scenarios and how to handle them
public interface IResourceMonitorGrain : IGrain
{
    Task<ResourceStatus> GetResourceStatusAsync();
    Task SimulateHighMemoryUsageAsync();
    Task ForceDeactivationAsync();
    Task ConfigureIdleTimeoutAsync(TimeSpan timeout);
}

public class ResourceMonitorGrain : Grain, IResourceMonitorGrain
{
    private readonly ILogger<ResourceMonitorGrain> logger;
    private DateTimeOffset lastActivity = DateTimeOffset.UtcNow;
    private readonly List<byte[]> memoryBlocks = new(); // Simulate memory usage
    private bool isDeactivationRequested = false;

    public ResourceMonitorGrain(ILogger<ResourceMonitorGrain> logger)
    {
        this.logger = logger;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var grainId = this.GetPrimaryKeyString();
        lastActivity = DateTimeOffset.UtcNow;
        
        logger.LogInformation("ResourceMonitorGrain {GrainId} activated", grainId);
        
        // Start monitoring for idle timeout
        StartIdleMonitoring();
        
        return base.OnActivateAsync(cancellationToken);
    }

    // DEACTIVATION CONDITION 1: Idle timeout (most common)
    private void StartIdleMonitoring()
    {
        // Register a timer to check for idle timeout
        // Orleans will deactivate grains that exceed idle timeout
        this.RegisterTimer(
            callback: CheckIdleTimeout,
            state: null,
            dueTime: TimeSpan.FromMinutes(1),
            period: TimeSpan.FromMinutes(1)
        );
    }

    private async Task CheckIdleTimeout(object state)
    {
        var grainId = this.GetPrimaryKeyString();
        var idleDuration = DateTimeOffset.UtcNow - lastActivity;
        
        logger.LogDebug("Idle check for grain {GrainId}: {IdleDuration}", grainId, idleDuration);
        
        // Orleans default idle timeout is typically 2 hours
        // This can be configured per grain type or globally
        if (idleDuration > TimeSpan.FromHours(1))
        {
            logger.LogInformation("Grain {GrainId} approaching idle timeout threshold", grainId);
        }
        
        // Note: Orleans handles actual deactivation automatically
        // This is just for monitoring and logging purposes
    }

    public Task<ResourceStatus> GetResourceStatusAsync()
    {
        lastActivity = DateTimeOffset.UtcNow; // Update activity timestamp
        var grainId = this.GetPrimaryKeyString();
        
        var status = new ResourceStatus
        {
            GrainId = grainId,
            LastActivity = lastActivity,
            MemoryUsage = memoryBlocks.Sum(b => b.Length),
            IsDeactivationRequested = isDeactivationRequested,
            UpTime = DateTimeOffset.UtcNow - lastActivity
        };
        
        logger.LogDebug("Resource status requested for grain {GrainId}", grainId);
        return Task.FromResult(status);
    }

    // DEACTIVATION CONDITION 2: Memory pressure
    public async Task SimulateHighMemoryUsageAsync()
    {
        lastActivity = DateTimeOffset.UtcNow;
        var grainId = this.GetPrimaryKeyString();
        
        logger.LogInformation("Simulating high memory usage for grain {GrainId}", grainId);
        
        try
        {
            // Allocate large memory blocks to simulate memory pressure
            for (int i = 0; i < 10; i++)
            {
                var block = new byte[10 * 1024 * 1024]; // 10 MB blocks
                memoryBlocks.Add(block);
                
                logger.LogDebug("Allocated memory block {BlockNumber} for grain {GrainId}", i + 1, grainId);
                await Task.Delay(100);
            }
            
            logger.LogWarning("High memory usage simulation complete for grain {GrainId}. " +
                             "Orleans may deactivate this grain due to memory pressure.", grainId);
            
            // Orleans runtime monitors memory usage and may deactivate grains
            // with high memory consumption to free up resources
        }
        catch (OutOfMemoryException ex)
        {
            logger.LogError(ex, "Out of memory during simulation for grain {GrainId}", grainId);
            
            // Clean up allocated memory
            memoryBlocks.Clear();
            GC.Collect();
            
            throw;
        }
    }

    // DEACTIVATION CONDITION 3: Explicit deactivation request
    public async Task ForceDeactivationAsync()
    {
        lastActivity = DateTimeOffset.UtcNow;
        var grainId = this.GetPrimaryKeyString();
        
        logger.LogInformation("Forcing deactivation for grain {GrainId}", grainId);
        
        isDeactivationRequested = true;
        
        try
        {
            // Perform cleanup before deactivation
            await CleanupResourcesAsync();
            
            // Request deactivation from Orleans runtime
            this.DeactivateOnIdle();
            
            logger.LogInformation("Deactivation requested for grain {GrainId}", grainId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during forced deactivation for grain {GrainId}", grainId);
            throw;
        }
    }

    // DEACTIVATION CONDITION 4: Configuration-based timeout
    public async Task ConfigureIdleTimeoutAsync(TimeSpan timeout)
    {
        lastActivity = DateTimeOffset.UtcNow;
        var grainId = this.GetPrimaryKeyString();
        
        logger.LogInformation("Configuring idle timeout to {Timeout} for grain {GrainId}", 
            timeout, grainId);
        
        // Note: In real applications, idle timeout is typically configured
        // at the silo level or per grain type, not per grain instance
        // This is for demonstration purposes
        
        // Simulate configuration change
        await Task.Delay(100);
        
        logger.LogInformation("Idle timeout configured for grain {GrainId}", grainId);
    }

    private async Task CleanupResourcesAsync()
    {
        var grainId = this.GetPrimaryKeyString();
        logger.LogDebug("Cleaning up resources for grain {GrainId}", grainId);
        
        try
        {
            // Clean up memory blocks
            memoryBlocks.Clear();
            
            // Perform other cleanup operations
            await Task.Delay(50);
            
            // Force garbage collection
            GC.Collect();
            
            logger.LogDebug("Resource cleanup completed for grain {GrainId}", grainId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error during resource cleanup for grain {GrainId}", grainId);
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        var grainId = this.GetPrimaryKeyString();
        var upTime = DateTimeOffset.UtcNow - lastActivity;
        
        logger.LogInformation(
            "ResourceMonitorGrain {GrainId} deactivating due to {Reason} after {UpTime}",
            grainId, reason, upTime);
        
        // Log deactivation reason for monitoring
        switch (reason)
        {
            case DeactivationReason.ActivationLimit:
                logger.LogInformation("Deactivation due to activation limit reached");
                break;
            case DeactivationReason.InternalShutdown:
                logger.LogInformation("Deactivation due to internal silo shutdown");
                break;
            case DeactivationReason.ApplicationRequested:
                logger.LogInformation("Deactivation explicitly requested by application");
                break;
            default:
                logger.LogInformation("Deactivation due to other reason: {Reason}", reason);
                break;
        }
        
        // Perform final cleanup
        await CleanupResourcesAsync();
        
        await base.OnDeactivateAsync(reason, cancellationToken);
    }
}
```

### Controlling Activation Behavior

Orleans provides configuration options and patterns to control how grains activate and manage their lifecycle.

```csharp
namespace DocumentProcessor.Orleans.Configuration;

using Orleans;
using Orleans.Configuration;
using Orleans.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Demonstrates advanced activation control and configuration
public static class GrainActivationConfiguration
{
    public static ISiloHostBuilder ConfigureGrainActivation(this ISiloHostBuilder builder)
    {
        return builder
            // Configure global grain collection settings
            .Configure<GrainCollectionOptions>(options =>
            {
                // Default idle timeout for all grains
                options.CollectionAge = TimeSpan.FromMinutes(30);
                
                // How often to check for idle grains
                options.CollectionQuantum = TimeSpan.FromMinutes(1);
                
                // Activation limit per grain type
                options.ActivationLimit = 1000;
            })
            
            // Configure per-grain-type settings
            .Configure<GrainTypeOptions<DocumentAnalyzerGrain>>(options =>
            {
                // Specific idle timeout for DocumentAnalyzerGrain
                options.IdleTimeout = TimeSpan.FromHours(2);
            })
            
            .Configure<GrainTypeOptions<ResourceMonitorGrain>>(options =>
            {
                // Shorter timeout for resource monitors
                options.IdleTimeout = TimeSpan.FromMinutes(15);
            })
            
            // Configure placement strategies
            .ConfigureServices(services =>
            {
                services.AddSingleton<IPlacementDirector, CustomPlacementDirector>();
            });
    }
}

// Custom placement director for controlling grain placement
public class CustomPlacementDirector : IPlacementDirector
{
    private readonly ILogger<CustomPlacementDirector> logger;

    public CustomPlacementDirector(ILogger<CustomPlacementDirector> logger)
    {
        this.logger = logger;
    }

    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        logger.LogDebug("Placing grain {GrainType} with strategy {Strategy}", 
            target.GrainIdentity.Type, strategy.GetType().Name);
        
        // Custom placement logic based on grain type
        var availableSilos = context.GetCompatibleSilos(target).ToList();
        
        if (target.GrainIdentity.Type == typeof(ResourceMonitorGrain))
        {
            // Place resource monitors on silos with lowest memory usage
            var selectedSilo = SelectSiloByMemoryUsage(availableSilos, context);
            logger.LogDebug("Selected silo {Silo} for ResourceMonitorGrain", selectedSilo);
            return Task.FromResult(selectedSilo);
        }
        
        if (target.GrainIdentity.Type == typeof(DocumentAnalyzerGrain))
        {
            // Place document analyzers on silos with highest CPU availability
            var selectedSilo = SelectSiloByCpuAvailability(availableSilos, context);
            logger.LogDebug("Selected silo {Silo} for DocumentAnalyzerGrain", selectedSilo);
            return Task.FromResult(selectedSilo);
        }
        
        // Default placement - random selection
        var randomSilo = availableSilos[Random.Shared.Next(availableSilos.Count)];
        return Task.FromResult(randomSilo);
    }

    private SiloAddress SelectSiloByMemoryUsage(List<SiloAddress> silos, IPlacementContext context)
    {
        // In a real implementation, this would query actual memory usage
        // For demo purposes, we'll use a simple selection
        return silos.OrderBy(s => s.GetHashCode()).First();
    }

    private SiloAddress SelectSiloByCpuAvailability(List<SiloAddress> silos, IPlacementContext context)
    {
        // In a real implementation, this would query actual CPU metrics
        // For demo purposes, we'll use a simple selection
        return silos.OrderByDescending(s => s.GetHashCode()).First();
    }
}

// Advanced grain with custom activation behavior
[PreferLocalPlacement]
public class OptimizedDocumentGrain : Grain, IOptimizedDocumentGrain
{
    private readonly ILogger<OptimizedDocumentGrain> logger;
    private readonly IHostApplicationLifetime applicationLifetime;
    private ActivationTracker activationTracker = new();

    public OptimizedDocumentGrain(
        ILogger<OptimizedDocumentGrain> logger,
        IHostApplicationLifetime applicationLifetime)
    {
        this.logger = logger;
        this.applicationLifetime = applicationLifetime;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var grainId = this.GetPrimaryKeyString();
        activationTracker.RecordActivation(DateTimeOffset.UtcNow);
        
        logger.LogInformation("OptimizedDocumentGrain {GrainId} activated (attempt #{AttemptNumber})",
            grainId, activationTracker.ActivationCount);
        
        try
        {
            // Optimized initialization based on previous activations
            await OptimizedInitializationAsync(cancellationToken);
            
            // Register for application shutdown to handle graceful deactivation
            applicationLifetime.ApplicationStopping.Register(() =>
            {
                logger.LogInformation("Application stopping - preparing grain {GrainId} for shutdown", grainId);
            });
            
            logger.LogInformation("OptimizedDocumentGrain {GrainId} initialization completed", grainId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to initialize OptimizedDocumentGrain {GrainId}", grainId);
            activationTracker.RecordFailure();
            throw;
        }
        
        await base.OnActivateAsync(cancellationToken);
    }

    private async Task OptimizedInitializationAsync(CancellationToken cancellationToken)
    {
        // Adaptive initialization based on activation history
        var initializationDelay = activationTracker.CalculateOptimalInitializationDelay();
        
        logger.LogDebug("Using optimized initialization delay: {Delay}ms", 
            initializationDelay.TotalMilliseconds);
        
        await Task.Delay(initializationDelay, cancellationToken);
    }

    public Task<DocumentProcessingResult> ProcessDocumentAsync(DocumentProcessingRequest request)
    {
        var grainId = this.GetPrimaryKeyString();
        activationTracker.RecordActivity();
        
        logger.LogInformation("Processing document {DocumentId} in optimized grain {GrainId}",
            request.DocumentId, grainId);
        
        var result = new DocumentProcessingResult
        {
            DocumentId = request.DocumentId,
            ProcessedBy = grainId,
            ProcessedAt = DateTimeOffset.UtcNow,
            ActivationStats = activationTracker.GetStats()
        };
        
        return Task.FromResult(result);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        var grainId = this.GetPrimaryKeyString();
        var stats = activationTracker.GetStats();
        
        logger.LogInformation(
            "OptimizedDocumentGrain {GrainId} deactivating. " +
            "Activations: {ActivationCount}, Activities: {ActivityCount}, Uptime: {Uptime}",
            grainId, stats.ActivationCount, stats.ActivityCount, stats.TotalUptime);
        
        // Save optimization data for next activation
        await SaveOptimizationDataAsync(stats, cancellationToken);
        
        await base.OnDeactivateAsync(reason, cancellationToken);
    }

    private async Task SaveOptimizationDataAsync(ActivationStats stats, CancellationToken cancellationToken)
    {
        try
        {
            // In a real implementation, save stats to persistent storage
            await Task.Delay(10, cancellationToken);
            
            logger.LogDebug("Optimization data saved for grain {GrainId}", this.GetPrimaryKeyString());
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to save optimization data for grain {GrainId}", 
                this.GetPrimaryKeyString());
        }
    }
}

// Supporting classes for activation tracking and optimization
public class ActivationTracker
{
    private readonly List<DateTimeOffset> activationTimes = new();
    private readonly List<DateTimeOffset> activityTimes = new();
    private int failureCount = 0;

    public int ActivationCount => activationTimes.Count;
    public int ActivityCount => activityTimes.Count;

    public void RecordActivation(DateTimeOffset time)
    {
        activationTimes.Add(time);
    }

    public void RecordActivity()
    {
        activityTimes.Add(DateTimeOffset.UtcNow);
    }

    public void RecordFailure()
    {
        failureCount++;
    }

    public TimeSpan CalculateOptimalInitializationDelay()
    {
        // Adaptive delay based on activation history
        if (failureCount > 0)
        {
            // Increase delay if previous activations failed
            return TimeSpan.FromMilliseconds(500 * failureCount);
        }
        
        if (activationTimes.Count > 1)
        {
            // Reduce delay for frequently activated grains
            var averageActivationInterval = CalculateAverageActivationInterval();
            return TimeSpan.FromMilliseconds(Math.Max(50, averageActivationInterval.TotalMilliseconds / 10));
        }
        
        return TimeSpan.FromMilliseconds(100); // Default delay
    }

    private TimeSpan CalculateAverageActivationInterval()
    {
        if (activationTimes.Count < 2) return TimeSpan.FromMinutes(1);
        
        var intervals = new List<TimeSpan>();
        for (int i = 1; i < activationTimes.Count; i++)
        {
            intervals.Add(activationTimes[i] - activationTimes[i - 1]);
        }
        
        return TimeSpan.FromMilliseconds(intervals.Average(i => i.TotalMilliseconds));
    }

    public ActivationStats GetStats()
    {
        var totalUptime = activationTimes.Count > 0 
            ? DateTimeOffset.UtcNow - activationTimes.First()
            : TimeSpan.Zero;
            
        return new ActivationStats
        {
            ActivationCount = ActivationCount,
            ActivityCount = ActivityCount,
            FailureCount = failureCount,
            TotalUptime = totalUptime,
            AverageActivationInterval = CalculateAverageActivationInterval()
        };
    }
}

// Supporting types for activation examples
public interface IOptimizedDocumentGrain : IGrain
{
    Task<DocumentProcessingResult> ProcessDocumentAsync(DocumentProcessingRequest request);
}

public record AnalysisResult
{
    public string DocumentId { get; init; } = string.Empty;
    public string GrainId { get; init; } = string.Empty;
    public DateTimeOffset AnalyzedAt { get; init; }
    public AnalysisStatus Status { get; init; }
    public List<string> ActivationTriggers { get; init; } = new();
    public TimeSpan ProcessingDuration { get; init; }
}

public record ProcessingStatus
{
    public string GrainId { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTimeOffset ActivatedAt { get; init; }
    public List<string> TriggerHistory { get; init; } = new();
}

public record ResourceStatus
{
    public string GrainId { get; init; } = string.Empty;
    public DateTimeOffset LastActivity { get; init; }
    public long MemoryUsage { get; init; }
    public bool IsDeactivationRequested { get; init; }
    public TimeSpan UpTime { get; init; }
}

public record DocumentEvent
{
    public string EventId { get; init; } = string.Empty;
    public string DocumentId { get; init; } = string.Empty;
    public DocumentEventType Type { get; init; }
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}

public record DocumentProcessingRequest
{
    public string DocumentId { get; init; } = string.Empty;
    public string DocumentType { get; init; } = string.Empty;
    public Dictionary<string, object> Parameters { get; init; } = new();
}

public record DocumentProcessingResult
{
    public string DocumentId { get; init; } = string.Empty;
    public string ProcessedBy { get; init; } = string.Empty;
    public DateTimeOffset ProcessedAt { get; init; }
    public ActivationStats ActivationStats { get; init; } = new();
}

public record ActivationStats
{
    public int ActivationCount { get; init; }
    public int ActivityCount { get; init; }
    public int FailureCount { get; init; }
    public TimeSpan TotalUptime { get; init; }
    public TimeSpan AverageActivationInterval { get; init; }
}

public enum AnalysisStatus
{
    Pending,
    InProgress,
    Completed,
    Failed
}

public enum DocumentEventType
{
    Created,
    Updated,
    Deleted,
    Processed
}

// Placement attributes for controlling grain placement
[AttributeUsage(AttributeTargets.Class)]
public class PreferLocalPlacementAttribute : Attribute, IPlacementDirectorAttribute
{
    public IPlacementDirector CreatePlacementDirector() => new LocalPlacementDirector();
}

public class LocalPlacementDirector : IPlacementDirector
{
    public Task<SiloAddress> OnAddActivation(PlacementStrategy strategy, PlacementTarget target, IPlacementContext context)
    {
        // Prefer local silo placement when possible
        var localSilo = context.LocalSilo;
        var compatibleSilos = context.GetCompatibleSilos(target);
        
        if (compatibleSilos.Contains(localSilo))
        {
            return Task.FromResult(localSilo);
        }
        
        return Task.FromResult(compatibleSilos.First());
    }
}

## State Management Basics

Orleans provides flexible state management options for grains, from stateless computations to persistent state storage. Understanding these patterns helps you choose the right approach for your application needs and performance requirements.

### Stateless Grains

Stateless grains perform pure computations without maintaining internal state. They're highly scalable and efficient for processing operations that don't require persistence.

```csharp
namespace DocumentProcessor.Orleans.Stateless;

using Orleans;
using Microsoft.Extensions.Logging;
using System.Text.Json;

// Pure computation grain - no state, high performance
public interface IDocumentValidatorGrain : IGrain
{
    Task<ValidationResult> ValidateDocumentAsync(DocumentValidationRequest request);
    Task<List<ValidationRule>> GetValidationRulesAsync();
    Task<bool> IsValidDocumentTypeAsync(string documentType);
}

public class DocumentValidatorGrain : Grain, IDocumentValidatorGrain
{
    private readonly ILogger<DocumentValidatorGrain> logger;
    
    // Stateless grains can still have dependencies injected
    private static readonly Dictionary<string, List<ValidationRule>> ValidationRules = new()
    {
        ["invoice"] = new List<ValidationRule>
        {
            new() { Field = "amount", Type = ValidationType.Required },
            new() { Field = "amount", Type = ValidationType.Numeric },
            new() { Field = "date", Type = ValidationType.Date },
            new() { Field = "vendor", Type = ValidationType.Required }
        },
        ["receipt"] = new List<ValidationRule>
        {
            new() { Field = "total", Type = ValidationType.Required },
            new() { Field = "total", Type = ValidationType.Numeric },
            new() { Field = "items", Type = ValidationType.Array }
        },
        ["contract"] = new List<ValidationRule>
        {
            new() { Field = "parties", Type = ValidationType.Required },
            new() { Field = "terms", Type = ValidationType.Required },
            new() { Field = "signature", Type = ValidationType.Required }
        }
    };

    public DocumentValidatorGrain(ILogger<DocumentValidatorGrain> logger)
    {
        this.logger = logger;
    }

    public async Task<ValidationResult> ValidateDocumentAsync(DocumentValidationRequest request)
    {
        var grainId = this.GetPrimaryKeyString();
        logger.LogInformation("Validating document {DocumentId} of type {DocumentType} in grain {GrainId}",
            request.DocumentId, request.DocumentType, grainId);

        try
        {
            // Pure computation - no state modification
            var result = await PerformValidationAsync(request);
            
            logger.LogInformation("Validation completed for document {DocumentId}: {IsValid}",
                request.DocumentId, result.IsValid);
            
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Validation failed for document {DocumentId}", request.DocumentId);
            
            return new ValidationResult
            {
                DocumentId = request.DocumentId,
                IsValid = false,
                Errors = new List<string> { $"Validation error: {ex.Message}" }
            };
        }
    }

    private async Task<ValidationResult> PerformValidationAsync(DocumentValidationRequest request)
    {
        // Simulate async validation processing
        await Task.Delay(50);
        
        var errors = new List<string>();
        
        // Get validation rules for document type
        if (!ValidationRules.TryGetValue(request.DocumentType.ToLowerInvariant(), out var rules))
        {
            return new ValidationResult
            {
                DocumentId = request.DocumentId,
                IsValid = false,
                Errors = new List<string> { $"Unknown document type: {request.DocumentType}" }
            };
        }

        // Apply validation rules
        foreach (var rule in rules)
        {
            var fieldValue = GetFieldValue(request.DocumentData, rule.Field);
            
            switch (rule.Type)
            {
                case ValidationType.Required:
                    if (string.IsNullOrWhiteSpace(fieldValue))
                    {
                        errors.Add($"Field '{rule.Field}' is required");
                    }
                    break;
                    
                case ValidationType.Numeric:
                    if (!string.IsNullOrWhiteSpace(fieldValue) && !decimal.TryParse(fieldValue, out _))
                    {
                        errors.Add($"Field '{rule.Field}' must be numeric");
                    }
                    break;
                    
                case ValidationType.Date:
                    if (!string.IsNullOrWhiteSpace(fieldValue) && !DateTime.TryParse(fieldValue, out _))
                    {
                        errors.Add($"Field '{rule.Field}' must be a valid date");
                    }
                    break;
                    
                case ValidationType.Array:
                    if (!IsValidArray(fieldValue))
                    {
                        errors.Add($"Field '{rule.Field}' must be a valid array");
                    }
                    break;
            }
        }

        return new ValidationResult
        {
            DocumentId = request.DocumentId,
            IsValid = errors.Count == 0,
            Errors = errors,
            ValidatedAt = DateTimeOffset.UtcNow,
            DocumentType = request.DocumentType
        };
    }

    private static string GetFieldValue(Dictionary<string, object> data, string fieldName)
    {
        return data.TryGetValue(fieldName, out var value) ? value?.ToString() ?? string.Empty : string.Empty;
    }

    private static bool IsValidArray(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return false;
        
        try
        {
            JsonSerializer.Deserialize<object[]>(value);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Task<List<ValidationRule>> GetValidationRulesAsync()
    {
        var grainId = this.GetPrimaryKeyString();
        logger.LogDebug("Retrieving validation rules in grain {GrainId}", grainId);
        
        // Return all available validation rules
        var allRules = ValidationRules.Values.SelectMany(rules => rules).Distinct().ToList();
        return Task.FromResult(allRules);
    }

    public Task<bool> IsValidDocumentTypeAsync(string documentType)
    {
        var grainId = this.GetPrimaryKeyString();
        logger.LogDebug("Checking document type {DocumentType} in grain {GrainId}", documentType, grainId);
        
        var isValid = ValidationRules.ContainsKey(documentType.ToLowerInvariant());
        return Task.FromResult(isValid);
    }
}

// Stateless service grain for mathematical operations
public interface ICalculationServiceGrain : IGrain
{
    Task<decimal> CalculateDocumentTotalAsync(DocumentCalculationRequest request);
    Task<TaxCalculationResult> CalculateTaxAsync(TaxCalculationRequest request);
    Task<CurrencyConversionResult> ConvertCurrencyAsync(CurrencyConversionRequest request);
}

public class CalculationServiceGrain : Grain, ICalculationServiceGrain
{
    private readonly ILogger<CalculationServiceGrain> logger;
    
    // Static configuration - no mutable state
    private static readonly Dictionary<string, decimal> TaxRates = new()
    {
        ["US"] = 0.08m,    // 8% sales tax
        ["CA"] = 0.12m,    // 12% HST
        ["UK"] = 0.20m,    // 20% VAT
        ["DE"] = 0.19m,    // 19% VAT
        ["FR"] = 0.20m     // 20% VAT
    };
    
    private static readonly Dictionary<string, decimal> ExchangeRates = new()
    {
        ["USD_EUR"] = 0.85m,
        ["USD_GBP"] = 0.73m,
        ["USD_CAD"] = 1.25m,
        ["EUR_USD"] = 1.18m,
        ["GBP_USD"] = 1.37m,
        ["CAD_USD"] = 0.80m
    };

    public CalculationServiceGrain(ILogger<CalculationServiceGrain> logger)
    {
        this.logger = logger;
    }

    public async Task<decimal> CalculateDocumentTotalAsync(DocumentCalculationRequest request)
    {
        var grainId = this.GetPrimaryKeyString();
        logger.LogInformation("Calculating total for document {DocumentId} in grain {GrainId}",
            request.DocumentId, grainId);

        try
        {
            // Pure calculation - no state changes
            await Task.Delay(10); // Simulate processing time
            
            var subtotal = request.LineItems.Sum(item => item.Quantity * item.UnitPrice);
            var discount = subtotal * (request.DiscountPercent / 100m);
            var total = subtotal - discount;
            
            logger.LogDebug("Calculated total {Total} for document {DocumentId} (subtotal: {Subtotal}, discount: {Discount})",
                total, request.DocumentId, subtotal, discount);
            
            return total;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to calculate total for document {DocumentId}", request.DocumentId);
            throw;
        }
    }

    public async Task<TaxCalculationResult> CalculateTaxAsync(TaxCalculationRequest request)
    {
        var grainId = this.GetPrimaryKeyString();
        logger.LogInformation("Calculating tax for amount {Amount} in country {Country} in grain {GrainId}",
            request.Amount, request.CountryCode, grainId);

        try
        {
            await Task.Delay(5); // Simulate processing time
            
            if (!TaxRates.TryGetValue(request.CountryCode.ToUpperInvariant(), out var taxRate))
            {
                throw new ArgumentException($"Tax rate not available for country: {request.CountryCode}");
            }
            
            var taxAmount = request.Amount * taxRate;
            var totalWithTax = request.Amount + taxAmount;
            
            var result = new TaxCalculationResult
            {
                OriginalAmount = request.Amount,
                TaxRate = taxRate,
                TaxAmount = taxAmount,
                TotalWithTax = totalWithTax,
                CountryCode = request.CountryCode.ToUpperInvariant(),
                CalculatedAt = DateTimeOffset.UtcNow
            };
            
            logger.LogDebug("Tax calculation completed: {TaxAmount} tax on {OriginalAmount}",
                result.TaxAmount, result.OriginalAmount);
            
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Tax calculation failed for amount {Amount} in country {Country}",
                request.Amount, request.CountryCode);
            throw;
        }
    }

    public async Task<CurrencyConversionResult> ConvertCurrencyAsync(CurrencyConversionRequest request)
    {
        var grainId = this.GetPrimaryKeyString();
        logger.LogInformation("Converting {Amount} from {FromCurrency} to {ToCurrency} in grain {GrainId}",
            request.Amount, request.FromCurrency, request.ToCurrency, grainId);

        try
        {
            await Task.Delay(15); // Simulate API call time
            
            var conversionKey = $"{request.FromCurrency}_{request.ToCurrency}";
            
            if (!ExchangeRates.TryGetValue(conversionKey, out var exchangeRate))
            {
                throw new ArgumentException($"Exchange rate not available for {conversionKey}");
            }
            
            var convertedAmount = request.Amount * exchangeRate;
            
            var result = new CurrencyConversionResult
            {
                OriginalAmount = request.Amount,
                ConvertedAmount = convertedAmount,
                FromCurrency = request.FromCurrency,
                ToCurrency = request.ToCurrency,
                ExchangeRate = exchangeRate,
                ConvertedAt = DateTimeOffset.UtcNow
            };
            
            logger.LogDebug("Currency conversion completed: {OriginalAmount} {FromCurrency} = {ConvertedAmount} {ToCurrency}",
                result.OriginalAmount, result.FromCurrency, result.ConvertedAmount, result.ToCurrency);
            
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Currency conversion failed for {Amount} {FromCurrency} to {ToCurrency}",
                request.Amount, request.FromCurrency, request.ToCurrency);
            throw;
        }
    }
}
```

### Basic State Patterns

Grains can maintain in-memory state that persists for the lifetime of the grain activation. This provides fast access to data while the grain is active.

```csharp
namespace DocumentProcessor.Orleans.State;

using Orleans;
using Microsoft.Extensions.Logging;

// In-memory state management with validation and caching
public interface IDocumentCacheGrain : IGrain
{
    Task<bool> StoreDocumentAsync(string documentId, DocumentMetadata metadata);
    Task<DocumentMetadata?> GetDocumentAsync(string documentId);
    Task<List<DocumentMetadata>> GetAllDocumentsAsync();
    Task<bool> RemoveDocumentAsync(string documentId);
    Task ClearCacheAsync();
    Task<CacheStatistics> GetStatisticsAsync();
}

public class DocumentCacheGrain : Grain, IDocumentCacheGrain
{
    private readonly ILogger<DocumentCacheGrain> logger;
    
    // In-memory state - persists during grain lifetime
    private readonly Dictionary<string, DocumentMetadata> documentCache = new();
    private readonly Dictionary<string, DateTimeOffset> accessTimes = new();
    private readonly Dictionary<string, int> accessCounts = new();
    private DateTimeOffset cacheCreated;
    private int totalOperations = 0;

    public DocumentCacheGrain(ILogger<DocumentCacheGrain> logger)
    {
        this.logger = logger;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var grainId = this.GetPrimaryKeyString();
        cacheCreated = DateTimeOffset.UtcNow;
        
        logger.LogInformation("DocumentCacheGrain {GrainId} activated - initializing cache", grainId);
        
        return base.OnActivateAsync(cancellationToken);
    }

    public Task<bool> StoreDocumentAsync(string documentId, DocumentMetadata metadata)
    {
        var grainId = this.GetPrimaryKeyString();
        totalOperations++;
        
        logger.LogInformation("Storing document {DocumentId} in cache {GrainId}", documentId, grainId);
        
        try
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(documentId))
            {
                throw new ArgumentException("Document ID cannot be null or empty", nameof(documentId));
            }
            
            if (metadata == null)
            {
                throw new ArgumentException("Document metadata cannot be null", nameof(metadata));
            }
            
            // Store in cache with timestamp tracking
            var now = DateTimeOffset.UtcNow;
            documentCache[documentId] = metadata with { LastModified = now };
            accessTimes[documentId] = now;
            accessCounts[documentId] = accessCounts.GetValueOrDefault(documentId, 0) + 1;
            
            logger.LogDebug("Document {DocumentId} stored in cache {GrainId}. Cache size: {CacheSize}",
                documentId, grainId, documentCache.Count);
            
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to store document {DocumentId} in cache {GrainId}", documentId, grainId);
            return Task.FromResult(false);
        }
    }

    public Task<DocumentMetadata?> GetDocumentAsync(string documentId)
    {
        var grainId = this.GetPrimaryKeyString();
        totalOperations++;
        
        logger.LogDebug("Retrieving document {DocumentId} from cache {GrainId}", documentId, grainId);
        
        try
        {
            if (string.IsNullOrWhiteSpace(documentId))
            {
                logger.LogWarning("Attempted to retrieve document with null/empty ID from cache {GrainId}", grainId);
                return Task.FromResult<DocumentMetadata?>(null);
            }
            
            if (documentCache.TryGetValue(documentId, out var metadata))
            {
                // Update access tracking
                accessTimes[documentId] = DateTimeOffset.UtcNow;
                accessCounts[documentId] = accessCounts.GetValueOrDefault(documentId, 0) + 1;
                
                logger.LogDebug("Document {DocumentId} found in cache {GrainId}. Access count: {AccessCount}",
                    documentId, grainId, accessCounts[documentId]);
                
                return Task.FromResult<DocumentMetadata?>(metadata);
            }
            
            logger.LogDebug("Document {DocumentId} not found in cache {GrainId}", documentId, grainId);
            return Task.FromResult<DocumentMetadata?>(null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving document {DocumentId} from cache {GrainId}", documentId, grainId);
            return Task.FromResult<DocumentMetadata?>(null);
        }
    }

    public Task<List<DocumentMetadata>> GetAllDocumentsAsync()
    {
        var grainId = this.GetPrimaryKeyString();
        totalOperations++;
        
        logger.LogDebug("Retrieving all documents from cache {GrainId}. Count: {Count}",
            grainId, documentCache.Count);
        
        try
        {
            var documents = documentCache.Values.ToList();
            
            // Update access time for cache-level operation
            var now = DateTimeOffset.UtcNow;
            foreach (var documentId in documentCache.Keys)
            {
                accessTimes[documentId] = now;
            }
            
            return Task.FromResult(documents);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving all documents from cache {GrainId}", grainId);
            return Task.FromResult(new List<DocumentMetadata>());
        }
    }

    public Task<bool> RemoveDocumentAsync(string documentId)
    {
        var grainId = this.GetPrimaryKeyString();
        totalOperations++;
        
        logger.LogInformation("Removing document {DocumentId} from cache {GrainId}", documentId, grainId);
        
        try
        {
            if (string.IsNullOrWhiteSpace(documentId))
            {
                logger.LogWarning("Attempted to remove document with null/empty ID from cache {GrainId}", grainId);
                return Task.FromResult(false);
            }
            
            var removed = documentCache.Remove(documentId);
            if (removed)
            {
                accessTimes.Remove(documentId);
                accessCounts.Remove(documentId);
                
                logger.LogInformation("Document {DocumentId} removed from cache {GrainId}. Remaining: {Count}",
                    documentId, grainId, documentCache.Count);
            }
            else
            {
                logger.LogDebug("Document {DocumentId} was not in cache {GrainId}", documentId, grainId);
            }
            
            return Task.FromResult(removed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error removing document {DocumentId} from cache {GrainId}", documentId, grainId);
            return Task.FromResult(false);
        }
    }

    public Task ClearCacheAsync()
    {
        var grainId = this.GetPrimaryKeyString();
        var previousCount = documentCache.Count;
        totalOperations++;
        
        logger.LogInformation("Clearing cache {GrainId} with {Count} documents", grainId, previousCount);
        
        try
        {
            documentCache.Clear();
            accessTimes.Clear();
            accessCounts.Clear();
            
            logger.LogInformation("Cache {GrainId} cleared. {Count} documents removed", grainId, previousCount);
            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error clearing cache {GrainId}", grainId);
            throw;
        }
    }

    public Task<CacheStatistics> GetStatisticsAsync()
    {
        var grainId = this.GetPrimaryKeyString();
        totalOperations++;
        
        logger.LogDebug("Generating statistics for cache {GrainId}", grainId);
        
        try
        {
            var now = DateTimeOffset.UtcNow;
            var uptime = now - cacheCreated;
            
            var stats = new CacheStatistics
            {
                GrainId = grainId,
                DocumentCount = documentCache.Count,
                TotalOperations = totalOperations,
                CacheUptime = uptime,
                AverageAccessCount = accessCounts.Count > 0 ? (double)accessCounts.Values.Sum() / accessCounts.Count : 0,
                MostAccessedDocument = GetMostAccessedDocument(),
                OldestDocument = GetOldestDocument(),
                NewestDocument = GetNewestDocument(),
                GeneratedAt = now
            };
            
            logger.LogDebug("Statistics generated for cache {GrainId}: {DocumentCount} documents, {TotalOperations} operations",
                grainId, stats.DocumentCount, stats.TotalOperations);
            
            return Task.FromResult(stats);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating statistics for cache {GrainId}", grainId);
            throw;
        }
    }

    private string? GetMostAccessedDocument()
    {
        return accessCounts.Count > 0
            ? accessCounts.OrderByDescending(kvp => kvp.Value).First().Key
            : null;
    }

    private DocumentMetadata? GetOldestDocument()
    {
        if (documentCache.Count == 0) return null;
        
        var oldestId = documentCache.OrderBy(kvp => kvp.Value.CreatedAt).First().Key;
        return documentCache[oldestId];
    }

    private DocumentMetadata? GetNewestDocument()
    {
        if (documentCache.Count == 0) return null;
        
        var newestId = documentCache.OrderByDescending(kvp => kvp.Value.CreatedAt).First().Key;
        return documentCache[newestId];
    }

    public override Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        var grainId = this.GetPrimaryKeyString();
        var uptime = DateTimeOffset.UtcNow - cacheCreated;
        
        logger.LogInformation(
            "DocumentCacheGrain {GrainId} deactivating. " +
            "Final stats: {DocumentCount} documents, {TotalOperations} operations, {Uptime} uptime",
            grainId, documentCache.Count, totalOperations, uptime);
        
        return base.OnDeactivateAsync(reason, cancellationToken);
    }
}

// Advanced in-memory state with expiration and cleanup
public interface ISessionManagerGrain : IGrain
{
    Task<string> CreateSessionAsync(SessionRequest request);
    Task<SessionInfo?> GetSessionAsync(string sessionId);
    Task<bool> UpdateSessionAsync(string sessionId, SessionUpdate update);
    Task<bool> EndSessionAsync(string sessionId);
    Task<List<SessionInfo>> GetActiveSessionsAsync();
    Task<SessionManagerStats> GetManagerStatsAsync();
}

public class SessionManagerGrain : Grain, ISessionManagerGrain
{
    private readonly ILogger<SessionManagerGrain> logger;
    
    // Complex in-memory state with automatic cleanup
    private readonly Dictionary<string, SessionInfo> activeSessions = new();
    private readonly Dictionary<string, Timer> sessionTimers = new();
    private DateTimeOffset managerStarted;
    private int totalSessionsCreated = 0;
    private int totalSessionsExpired = 0;

    public SessionManagerGrain(ILogger<SessionManagerGrain> logger)
    {
        this.logger = logger;
    }

    public override Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var grainId = this.GetPrimaryKeyString();
        managerStarted = DateTimeOffset.UtcNow;
        
        logger.LogInformation("SessionManagerGrain {GrainId} activated", grainId);
        
        // Start periodic cleanup of expired sessions
        this.RegisterTimer(CleanupExpiredSessions, null, 
            TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(5));
        
        return base.OnActivateAsync(cancellationToken);
    }

    public Task<string> CreateSessionAsync(SessionRequest request)
    {
        var grainId = this.GetPrimaryKeyString();
        var sessionId = Guid.NewGuid().ToString();
        totalSessionsCreated++;
        
        logger.LogInformation("Creating session {SessionId} for user {UserId} in grain {GrainId}",
            sessionId, request.UserId, grainId);
        
        try
        {
            var now = DateTimeOffset.UtcNow;
            var expiresAt = now.Add(request.Duration);
            
            var sessionInfo = new SessionInfo
            {
                SessionId = sessionId,
                UserId = request.UserId,
                CreatedAt = now,
                LastActivity = now,
                ExpiresAt = expiresAt,
                UserAgent = request.UserAgent,
                IpAddress = request.IpAddress,
                SessionData = new Dictionary<string, object>(request.InitialData ?? new Dictionary<string, object>())
            };
            
            activeSessions[sessionId] = sessionInfo;
            
            // Set up automatic expiration
            var timer = new Timer(async _ => await ExpireSessionAsync(sessionId), 
                null, request.Duration, Timeout.InfiniteTimeSpan);
            sessionTimers[sessionId] = timer;
            
            logger.LogInformation("Session {SessionId} created for user {UserId}, expires at {ExpiresAt}",
                sessionId, request.UserId, expiresAt);
            
            return Task.FromResult(sessionId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create session for user {UserId} in grain {GrainId}",
                request.UserId, grainId);
            throw;
        }
    }

    public Task<SessionInfo?> GetSessionAsync(string sessionId)
    {
        var grainId = this.GetPrimaryKeyString();
        
        logger.LogDebug("Retrieving session {SessionId} from grain {GrainId}", sessionId, grainId);
        
        try
        {
            if (activeSessions.TryGetValue(sessionId, out var session))
            {
                // Check if session has expired
                if (DateTimeOffset.UtcNow > session.ExpiresAt)
                {
                    logger.LogDebug("Session {SessionId} has expired, removing from grain {GrainId}", 
                        sessionId, grainId);
                    
                    _ = Task.Run(() => ExpireSessionAsync(sessionId));
                    return Task.FromResult<SessionInfo?>(null);
                }
                
                // Update last activity
                var updatedSession = session with { LastActivity = DateTimeOffset.UtcNow };
                activeSessions[sessionId] = updatedSession;
                
                logger.LogDebug("Session {SessionId} found and updated in grain {GrainId}", sessionId, grainId);
                return Task.FromResult<SessionInfo?>(updatedSession);
            }
            
            logger.LogDebug("Session {SessionId} not found in grain {GrainId}", sessionId, grainId);
            return Task.FromResult<SessionInfo?>(null);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving session {SessionId} from grain {GrainId}", 
                sessionId, grainId);
            return Task.FromResult<SessionInfo?>(null);
        }
    }

    public Task<bool> UpdateSessionAsync(string sessionId, SessionUpdate update)
    {
        var grainId = this.GetPrimaryKeyString();
        
        logger.LogDebug("Updating session {SessionId} in grain {GrainId}", sessionId, grainId);
        
        try
        {
            if (!activeSessions.TryGetValue(sessionId, out var session))
            {
                logger.LogWarning("Attempted to update non-existent session {SessionId} in grain {GrainId}",
                    sessionId, grainId);
                return Task.FromResult(false);
            }
            
            // Check if session has expired
            if (DateTimeOffset.UtcNow > session.ExpiresAt)
            {
                logger.LogDebug("Cannot update expired session {SessionId} in grain {GrainId}", 
                    sessionId, grainId);
                
                _ = Task.Run(() => ExpireSessionAsync(sessionId));
                return Task.FromResult(false);
            }
            
            // Apply updates
            var updatedData = new Dictionary<string, object>(session.SessionData);
            foreach (var kvp in update.DataUpdates ?? new Dictionary<string, object>())
            {
                updatedData[kvp.Key] = kvp.Value;
            }
            
            var newExpiresAt = update.ExtendDuration.HasValue 
                ? session.ExpiresAt.Add(update.ExtendDuration.Value)
                : session.ExpiresAt;
            
            var updatedSession = session with
            {
                LastActivity = DateTimeOffset.UtcNow,
                ExpiresAt = newExpiresAt,
                SessionData = updatedData
            };
            
            activeSessions[sessionId] = updatedSession;
            
            // Update timer if duration was extended
            if (update.ExtendDuration.HasValue)
            {
                if (sessionTimers.TryGetValue(sessionId, out var existingTimer))
                {
                    existingTimer.Dispose();
                }
                
                var newTimeout = newExpiresAt - DateTimeOffset.UtcNow;
                if (newTimeout > TimeSpan.Zero)
                {
                    var timer = new Timer(async _ => await ExpireSessionAsync(sessionId),
                        null, newTimeout, Timeout.InfiniteTimeSpan);
                    sessionTimers[sessionId] = timer;
                }
            }
            
            logger.LogDebug("Session {SessionId} updated in grain {GrainId}", sessionId, grainId);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating session {SessionId} in grain {GrainId}", 
                sessionId, grainId);
            return Task.FromResult(false);
        }
    }

    public Task<bool> EndSessionAsync(string sessionId)
    {
        var grainId = this.GetPrimaryKeyString();
        
        logger.LogInformation("Ending session {SessionId} in grain {GrainId}", sessionId, grainId);
        
        return ExpireSessionAsync(sessionId);
    }

    public Task<List<SessionInfo>> GetActiveSessionsAsync()
    {
        var grainId = this.GetPrimaryKeyString();
        
        logger.LogDebug("Retrieving active sessions from grain {GrainId}. Count: {Count}",
            grainId, activeSessions.Count);
        
        try
        {
            var now = DateTimeOffset.UtcNow;
            var activeSessions = this.activeSessions.Values
                .Where(session => session.ExpiresAt > now)
                .ToList();
            
            return Task.FromResult(activeSessions);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error retrieving active sessions from grain {GrainId}", grainId);
            return Task.FromResult(new List<SessionInfo>());
        }
    }

    public Task<SessionManagerStats> GetManagerStatsAsync()
    {
        var grainId = this.GetPrimaryKeyString();
        
        logger.LogDebug("Generating manager statistics for grain {GrainId}", grainId);
        
        try
        {
            var now = DateTimeOffset.UtcNow;
            var uptime = now - managerStarted;
            var activeCount = activeSessions.Values.Count(s => s.ExpiresAt > now);
            
            var stats = new SessionManagerStats
            {
                GrainId = grainId,
                ActiveSessionCount = activeCount,
                TotalSessionsCreated = totalSessionsCreated,
                TotalSessionsExpired = totalSessionsExpired,
                ManagerUptime = uptime,
                AverageSessionDuration = CalculateAverageSessionDuration(),
                GeneratedAt = now
            };
            
            return Task.FromResult(stats);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error generating manager statistics for grain {GrainId}", this.GetPrimaryKeyString());
            throw;
        }
    }

    private async Task ExpireSessionAsync(string sessionId)
    {
        var grainId = this.GetPrimaryKeyString();
        
        try
        {
            if (activeSessions.Remove(sessionId))
            {
                totalSessionsExpired++;
                
                if (sessionTimers.TryGetValue(sessionId, out var timer))
                {
                    timer.Dispose();
                    sessionTimers.Remove(sessionId);
                }
                
                logger.LogInformation("Session {SessionId} expired and removed from grain {GrainId}",
                    sessionId, grainId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error expiring session {SessionId} in grain {GrainId}", 
                sessionId, grainId);
        }
    }

    private async Task CleanupExpiredSessions(object state)
    {
        var grainId = this.GetPrimaryKeyString();
        var now = DateTimeOffset.UtcNow;
        
        try
        {
            var expiredSessions = activeSessions
                .Where(kvp => kvp.Value.ExpiresAt <= now)
                .Select(kvp => kvp.Key)
                .ToList();
            
            if (expiredSessions.Count > 0)
            {
                logger.LogInformation("Cleaning up {Count} expired sessions in grain {GrainId}",
                    expiredSessions.Count, grainId);
                
                foreach (var sessionId in expiredSessions)
                {
                    await ExpireSessionAsync(sessionId);
                }
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during expired session cleanup in grain {GrainId}", grainId);
        }
    }

    private TimeSpan CalculateAverageSessionDuration()
    {
        if (totalSessionsExpired == 0) return TimeSpan.Zero;
        
        // Calculate average session duration based on actual metrics
        // In production, this would use historical session data
        var avgDuration = activeSessions.Any() 
            ? TimeSpan.FromTicks((long)activeSessions.Average(s => (DateTimeOffset.UtcNow - s.StartTime).Ticks))
            : TimeSpan.FromMinutes(30);
        
        return avgDuration;
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        var grainId = this.GetPrimaryKeyString();
        
        logger.LogInformation("SessionManagerGrain {GrainId} deactivating. Active sessions: {ActiveCount}",
            grainId, activeSessions.Count);
        
        // Clean up all timers
        foreach (var timer in sessionTimers.Values)
        {
            timer.Dispose();
        }
        sessionTimers.Clear();
        
        await base.OnDeactivateAsync(reason, cancellationToken);
    }
}
```

### State Persistence Fundamentals

Orleans provides built-in state persistence through storage providers. This enables durable state that survives grain deactivation and reactivation.

```csharp
namespace DocumentProcessor.Orleans.Persistence;

using Orleans;
using Orleans.Runtime;
using Microsoft.Extensions.Logging;

// Persistent state using Orleans state management
public interface IPersistentDocumentGrain : IGrainWithStringKey
{
    Task<bool> CreateDocumentAsync(DocumentCreationRequest request);
    Task<DocumentState?> GetDocumentAsync();
    Task<bool> UpdateDocumentAsync(DocumentUpdateRequest request);
    Task<bool> DeleteDocumentAsync();
    Task<DocumentHistory> GetDocumentHistoryAsync();
    Task<bool> SetDocumentStatusAsync(DocumentStatus status);
}

public class PersistentDocumentGrain : Grain, IPersistentDocumentGrain
{
    private readonly ILogger<PersistentDocumentGrain> logger;
    
    // Orleans-managed persistent state
    private readonly IPersistentState<DocumentState> documentState;
    private readonly IPersistentState<DocumentHistory> historyState;

    public PersistentDocumentGrain(
        ILogger<PersistentDocumentGrain> logger,
        [PersistentState("documentState", "documentStore")] IPersistentState<DocumentState> documentState,
        [PersistentState("historyState", "documentStore")] IPersistentState<DocumentHistory> historyState)
    {
        this.logger = logger;
        this.documentState = documentState;
        this.historyState = historyState;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var documentId = this.GetPrimaryKeyString();
        
        logger.LogInformation("PersistentDocumentGrain {DocumentId} activated", documentId);
        
        // Initialize history state if it doesn't exist
        if (historyState.State.DocumentId == null)
        {
            historyState.State.DocumentId = documentId;
            historyState.State.Events = new List<DocumentEvent>();
            historyState.State.CreatedAt = DateTimeOffset.UtcNow;
        }
        
        await base.OnActivateAsync(cancellationToken);
    }

    public async Task<bool> CreateDocumentAsync(DocumentCreationRequest request)
    {
        var documentId = this.GetPrimaryKeyString();
        
        logger.LogInformation("Creating document {DocumentId} with type {DocumentType}",
            documentId, request.DocumentType);
        
        try
        {
            // Check if document already exists
            if (documentState.State.DocumentId != null)
            {
                logger.LogWarning("Attempted to create existing document {DocumentId}", documentId);
                return false;
            }
            
            // Create new document state
            var now = DateTimeOffset.UtcNow;
            documentState.State = new DocumentState
            {
                DocumentId = documentId,
                DocumentType = request.DocumentType,
                Title = request.Title,
                Content = request.Content,
                Metadata = new Dictionary<string, object>(request.Metadata ?? new Dictionary<string, object>()),
                Status = DocumentStatus.Draft,
                CreatedAt = now,
                LastModified = now,
                Version = 1,
                CreatedBy = request.CreatedBy,
                LastModifiedBy = request.CreatedBy
            };
            
            // Save document state
            await documentState.WriteStateAsync();
            
            // Record creation event in history
            await RecordHistoryEventAsync(new DocumentEvent
            {
                EventType = DocumentEventType.Created,
                Timestamp = now,
                UserId = request.CreatedBy,
                Description = $"Document created with type {request.DocumentType}"
            });
            
            logger.LogInformation("Document {DocumentId} created successfully", documentId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create document {DocumentId}", documentId);
            
            // Record failure event
            await RecordHistoryEventAsync(new DocumentEvent
            {
                EventType = DocumentEventType.Error,
                Timestamp = DateTimeOffset.UtcNow,
                UserId = request.CreatedBy,
                Description = $"Document creation failed: {ex.Message}"
            });
            
            throw;
        }
    }

    public async Task<DocumentState?> GetDocumentAsync()
    {
        var documentId = this.GetPrimaryKeyString();
        
        logger.LogDebug("Retrieving document {DocumentId}", documentId);
        
        try
        {
            // Orleans automatically loads state on first access
            if (documentState.State.DocumentId == null)
            {
                logger.LogDebug("Document {DocumentId} not found", documentId);
                return null;
            }
            
            // Record access event
            await RecordHistoryEventAsync(new DocumentEvent
            {
                EventType = DocumentEventType.Accessed,
                Timestamp = DateTimeOffset.UtcNow,
                Description = "Document accessed"
            });
            
            logger.LogDebug("Document {DocumentId} retrieved successfully", documentId);
            return documentState.State;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve document {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<bool> UpdateDocumentAsync(DocumentUpdateRequest request)
    {
        var documentId = this.GetPrimaryKeyString();
        
        logger.LogInformation("Updating document {DocumentId}", documentId);
        
        try
        {
            // Check if document exists
            if (documentState.State.DocumentId == null)
            {
                logger.LogWarning("Attempted to update non-existent document {DocumentId}", documentId);
                return false;
            }
            
            // Store previous state for history
            var previousVersion = documentState.State.Version;
            var now = DateTimeOffset.UtcNow;
            
            // Apply updates
            if (!string.IsNullOrWhiteSpace(request.Title))
            {
                documentState.State.Title = request.Title;
            }
            
            if (!string.IsNullOrWhiteSpace(request.Content))
            {
                documentState.State.Content = request.Content;
            }
            
            if (request.MetadataUpdates != null)
            {
                foreach (var kvp in request.MetadataUpdates)
                {
                    documentState.State.Metadata[kvp.Key] = kvp.Value;
                }
            }
            
            // Update tracking fields
            documentState.State.LastModified = now;
            documentState.State.Version++;
            documentState.State.LastModifiedBy = request.ModifiedBy;
            
            // Save updated state
            await documentState.WriteStateAsync();
            
            // Record update event
            await RecordHistoryEventAsync(new DocumentEvent
            {
                EventType = DocumentEventType.Updated,
                Timestamp = now,
                UserId = request.ModifiedBy,
                Description = $"Document updated from version {previousVersion} to {documentState.State.Version}"
            });
            
            logger.LogInformation("Document {DocumentId} updated to version {Version}",
                documentId, documentState.State.Version);
            
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update document {DocumentId}", documentId);
            
            // Record failure event
            await RecordHistoryEventAsync(new DocumentEvent
            {
                EventType = DocumentEventType.Error,
                Timestamp = DateTimeOffset.UtcNow,
                UserId = request.ModifiedBy,
                Description = $"Document update failed: {ex.Message}"
            });
            
            throw;
        }
    }

    public async Task<bool> DeleteDocumentAsync()
    {
        var documentId = this.GetPrimaryKeyString();
        
        logger.LogInformation("Deleting document {DocumentId}", documentId);
        
        try
        {
            // Check if document exists
            if (documentState.State.DocumentId == null)
            {
                logger.LogWarning("Attempted to delete non-existent document {DocumentId}", documentId);
                return false;
            }
            
            // Record deletion event before clearing state
            await RecordHistoryEventAsync(new DocumentEvent
            {
                EventType = DocumentEventType.Deleted,
                Timestamp = DateTimeOffset.UtcNow,
                Description = $"Document deleted (was version {documentState.State.Version})"
            });
            
            // Clear the document state
            await documentState.ClearStateAsync();
            
            logger.LogInformation("Document {DocumentId} deleted successfully", documentId);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete document {DocumentId}", documentId);
            
            // Record failure event
            await RecordHistoryEventAsync(new DocumentEvent
            {
                EventType = DocumentEventType.Error,
                Timestamp = DateTimeOffset.UtcNow,
                Description = $"Document deletion failed: {ex.Message}"
            });
            
            throw;
        }
    }

    public async Task<DocumentHistory> GetDocumentHistoryAsync()
    {
        var documentId = this.GetPrimaryKeyString();
        
        logger.LogDebug("Retrieving history for document {DocumentId}", documentId);
        
        try
        {
            // Orleans automatically loads history state
            return historyState.State;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to retrieve history for document {DocumentId}", documentId);
            throw;
        }
    }

    public async Task<bool> SetDocumentStatusAsync(DocumentStatus status)
    {
        var documentId = this.GetPrimaryKeyString();
        
        logger.LogInformation("Setting status of document {DocumentId} to {Status}", documentId, status);
        
        try
        {
            // Check if document exists
            if (documentState.State.DocumentId == null)
            {
                logger.LogWarning("Attempted to set status of non-existent document {DocumentId}", documentId);
                return false;
            }
            
            var previousStatus = documentState.State.Status;
            documentState.State.Status = status;
            documentState.State.LastModified = DateTimeOffset.UtcNow;
            
            // Save state
            await documentState.WriteStateAsync();
            
            // Record status change event
            await RecordHistoryEventAsync(new DocumentEvent
            {
                EventType = DocumentEventType.StatusChanged,
                Timestamp = DateTimeOffset.UtcNow,
                Description = $"Status changed from {previousStatus} to {status}"
            });
            
            logger.LogInformation("Document {DocumentId} status changed to {Status}", documentId, status);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to set status of document {DocumentId}", documentId);
            throw;
        }
    }

    private async Task RecordHistoryEventAsync(DocumentEvent eventItem)
    {
        try
        {
            historyState.State.Events.Add(eventItem);
            historyState.State.LastUpdated = DateTimeOffset.UtcNow;
            
            await historyState.WriteStateAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to record history event for document {DocumentId}", 
                this.GetPrimaryKeyString());
            // Don't throw - history recording failures shouldn't break main operations
        }
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken cancellationToken)
    {
        var documentId = this.GetPrimaryKeyString();
        
        logger.LogInformation("PersistentDocumentGrain {DocumentId} deactivating due to {Reason}", 
            documentId, reason);
        
        // Orleans automatically saves any pending state changes during deactivation
        // No explicit state saving needed here unless you have custom cleanup
        
        await base.OnDeactivateAsync(reason, cancellationToken);
    }
}

// Supporting types for state management examples

// Stateless grain types
public record DocumentValidationRequest
{
    public string DocumentId { get; init; } = string.Empty;
    public string DocumentType { get; init; } = string.Empty;
    public Dictionary<string, object> DocumentData { get; init; } = new();
}

public record ValidationResult
{
    public string DocumentId { get; init; } = string.Empty;
    public bool IsValid { get; init; }
    public List<string> Errors { get; init; } = new();
    public DateTimeOffset ValidatedAt { get; init; }
    public string DocumentType { get; init; } = string.Empty;
}

public record ValidationRule
{
    public string Field { get; init; } = string.Empty;
    public ValidationType Type { get; init; }
}

public record DocumentCalculationRequest
{
    public string DocumentId { get; init; } = string.Empty;
    public List<LineItem> LineItems { get; init; } = new();
    public decimal DiscountPercent { get; init; }
}

public record LineItem
{
    public string Description { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}

public record TaxCalculationRequest
{
    public decimal Amount { get; init; }
    public string CountryCode { get; init; } = string.Empty;
}

public record TaxCalculationResult
{
    public decimal OriginalAmount { get; init; }
    public decimal TaxRate { get; init; }
    public decimal TaxAmount { get; init; }
    public decimal TotalWithTax { get; init; }
    public string CountryCode { get; init; } = string.Empty;
    public DateTimeOffset CalculatedAt { get; init; }
}

public record CurrencyConversionRequest
{
    public decimal Amount { get; init; }
    public string FromCurrency { get; init; } = string.Empty;
    public string ToCurrency { get; init; } = string.Empty;
}

public record CurrencyConversionResult
{
    public decimal OriginalAmount { get; init; }
    public decimal ConvertedAmount { get; init; }
    public string FromCurrency { get; init; } = string.Empty;
    public string ToCurrency { get; init; } = string.Empty;
    public decimal ExchangeRate { get; init; }
    public DateTimeOffset ConvertedAt { get; init; }
}

// In-memory state types
public record DocumentMetadata
{
    public string DocumentId { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string DocumentType { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset LastModified { get; init; }
    public Dictionary<string, object> Properties { get; init; } = new();
}

public record CacheStatistics
{
    public string GrainId { get; init; } = string.Empty;
    public int DocumentCount { get; init; }
    public int TotalOperations { get; init; }
    public TimeSpan CacheUptime { get; init; }
    public double AverageAccessCount { get; init; }
    public string? MostAccessedDocument { get; init; }
    public DocumentMetadata? OldestDocument { get; init; }
    public DocumentMetadata? NewestDocument { get; init; }
    public DateTimeOffset GeneratedAt { get; init; }
}

public record SessionRequest
{
    public string UserId { get; init; } = string.Empty;
    public TimeSpan Duration { get; init; } = TimeSpan.FromHours(1);
    public string UserAgent { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
    public Dictionary<string, object>? InitialData { get; init; }
}

public record SessionInfo
{
    public string SessionId { get; init; } = string.Empty;
    public string UserId { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset LastActivity { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public string UserAgent { get; init; } = string.Empty;
    public string IpAddress { get; init; } = string.Empty;
    public Dictionary<string, object> SessionData { get; init; } = new();
}

public record SessionUpdate
{
    public Dictionary<string, object>? DataUpdates { get; init; }
    public TimeSpan? ExtendDuration { get; init; }
}

public record SessionManagerStats
{
    public string GrainId { get; init; } = string.Empty;
    public int ActiveSessionCount { get; init; }
    public int TotalSessionsCreated { get; init; }
    public int TotalSessionsExpired { get; init; }
    public TimeSpan ManagerUptime { get; init; }
    public TimeSpan AverageSessionDuration { get; init; }
    public DateTimeOffset GeneratedAt { get; init; }
}

// Persistent state types
[GenerateSerializer]
public class DocumentState
{
    [Id(0)] public string? DocumentId { get; set; }
    [Id(1)] public string DocumentType { get; set; } = string.Empty;
    [Id(2)] public string Title { get; set; } = string.Empty;
    [Id(3)] public string Content { get; set; } = string.Empty;
    [Id(4)] public Dictionary<string, object> Metadata { get; set; } = new();
    [Id(5)] public DocumentStatus Status { get; set; }
    [Id(6)] public DateTimeOffset CreatedAt { get; set; }
    [Id(7)] public DateTimeOffset LastModified { get; set; }
    [Id(8)] public int Version { get; set; }
    [Id(9)] public string CreatedBy { get; set; } = string.Empty;
    [Id(10)] public string LastModifiedBy { get; set; } = string.Empty;
}

[GenerateSerializer]
public class DocumentHistory
{
    [Id(0)] public string? DocumentId { get; set; }
    [Id(1)] public List<DocumentEvent> Events { get; set; } = new();
    [Id(2)] public DateTimeOffset CreatedAt { get; set; }
    [Id(3)] public DateTimeOffset LastUpdated { get; set; }
}

[GenerateSerializer]
public class DocumentEvent
{
    [Id(0)] public DocumentEventType EventType { get; set; }
    [Id(1)] public DateTimeOffset Timestamp { get; set; }
    [Id(2)] public string UserId { get; set; } = string.Empty;
    [Id(3)] public string Description { get; set; } = string.Empty;
}

public record DocumentCreationRequest
{
    public string DocumentType { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
}

public record DocumentUpdateRequest
{
    public string? Title { get; init; }
    public string? Content { get; init; }
    public Dictionary<string, object>? MetadataUpdates { get; init; }
    public string ModifiedBy { get; init; } = string.Empty;
}

// Enumerations
public enum ValidationType
{
    Required,
    Numeric,
    Date,
    Array,
    Email,
    Url
}

public enum DocumentStatus
{
    Draft,
    InReview,
    Approved,
    Published,
    Archived,
    Deleted
}

public enum DocumentEventType
{
    Created,
    Updated,
    Deleted,
    Accessed,
    StatusChanged,
    Error
}
```

## Error Handling Fundamentals

Orleans provides robust error handling capabilities that help build resilient distributed applications. Understanding exception propagation, retry patterns, and fault tolerance is essential for production deployments.

### Exception Propagation

Orleans propagates exceptions from grains back to callers, maintaining the distributed call stack. Understanding how different exception types behave helps you implement effective error handling strategies.

```csharp
namespace DocumentProcessor.Orleans.ErrorHandling;

using Orleans;
using Microsoft.Extensions.Logging;

// Demonstrates various exception scenarios and handling patterns
public interface IDocumentProcessorGrain : IGrain
{
    Task<ProcessingResult> ProcessDocumentAsync(DocumentProcessingRequest request);
    Task<ValidationResult> ValidateDocumentAsync(string documentId);
    Task<bool> DeleteDocumentAsync(string documentId, bool forceDelete = false);
    Task<HealthCheckResult> HealthCheckAsync();
}

public class DocumentProcessorGrain : Grain, IDocumentProcessorGrain
{
    private readonly ILogger<DocumentProcessorGrain> logger;
    private readonly IDocumentRepository documentRepository;
    private readonly IExternalService externalService;
    private int consecutiveFailures = 0;
    private DateTimeOffset lastFailure = DateTimeOffset.MinValue;

    public DocumentProcessorGrain(
        ILogger<DocumentProcessorGrain> logger,
        IDocumentRepository documentRepository,
        IExternalService externalService)
    {
        this.logger = logger;
        this.documentRepository = documentRepository;
        this.externalService = externalService;
    }

    // EXCEPTION TYPE 1: Business logic exceptions (should propagate to caller)
    public async Task<ProcessingResult> ProcessDocumentAsync(DocumentProcessingRequest request)
    {
        var grainId = this.GetPrimaryKeyString();
        
        logger.LogInformation("Processing document {DocumentId} in grain {GrainId}",
            request.DocumentId, grainId);

        try
        {
            // Validate input - business logic exceptions
            if (string.IsNullOrWhiteSpace(request.DocumentId))
            {
                throw new ArgumentException("Document ID cannot be null or empty", nameof(request.DocumentId));
            }

            if (request.Content?.Length > 10_000_000) // 10MB limit
            {
                throw new DocumentTooLargeException(
                    $"Document {request.DocumentId} exceeds size limit of 10MB", 
                    request.DocumentId, 
                    request.Content.Length);
            }

            // Check if document already exists
            var existingDocument = await documentRepository.GetDocumentAsync(request.DocumentId);
            if (existingDocument != null && !request.OverwriteExisting)
            {
                throw new DocumentAlreadyExistsException(
                    $"Document {request.DocumentId} already exists", 
                    request.DocumentId);
            }

            // Process document with external service
            ExternalProcessingResult externalResult;
            try
            {
                externalResult = await externalService.ProcessAsync(request.DocumentId, request.Content);
            }
            catch (ExternalServiceException ex)
            {
                consecutiveFailures++;
                lastFailure = DateTimeOffset.UtcNow;
                
                logger.LogError(ex, "External service failed for document {DocumentId}. " +
                               "Consecutive failures: {ConsecutiveFailures}",
                    request.DocumentId, consecutiveFailures);

                // Wrap external exceptions with domain-specific context
                throw new DocumentProcessingException(
                    $"Failed to process document {request.DocumentId} due to external service error",
                    request.DocumentId,
                    ex);
            }
            catch (TimeoutException ex)
            {
                logger.LogWarning(ex, "External service timeout for document {DocumentId}", request.DocumentId);
                
                throw new DocumentProcessingException(
                    $"Processing timeout for document {request.DocumentId}",
                    request.DocumentId,
                    ex);
            }

            // Save processed document
            try
            {
                await documentRepository.SaveDocumentAsync(new ProcessedDocument
                {
                    DocumentId = request.DocumentId,
                    OriginalContent = request.Content,
                    ProcessedContent = externalResult.ProcessedContent,
                    ProcessedAt = DateTimeOffset.UtcNow,
                    ProcessingMetadata = externalResult.Metadata
                });
            }
            catch (RepositoryException ex)
            {
                logger.LogError(ex, "Failed to save processed document {DocumentId}", request.DocumentId);
                
                throw new DocumentPersistenceException(
                    $"Failed to save processed document {request.DocumentId}",
                    request.DocumentId,
                    ex);
            }

            // Reset failure counter on success
            consecutiveFailures = 0;
            
            logger.LogInformation("Document {DocumentId} processed successfully", request.DocumentId);

            return new ProcessingResult
            {
                DocumentId = request.DocumentId,
                Success = true,
                ProcessedAt = DateTimeOffset.UtcNow,
                ProcessingDuration = externalResult.ProcessingDuration,
                ResultSize = externalResult.ProcessedContent?.Length ?? 0
            };
        }
        catch (DocumentProcessingException)
        {
            // Domain exceptions - re-throw as-is
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error processing document {DocumentId} in grain {GrainId}",
                request.DocumentId, grainId);
            
            // Wrap unexpected exceptions
            throw new GrainOperationException(
                $"Unexpected error in grain {grainId} while processing document {request.DocumentId}",
                grainId,
                ex);
        }
    }

    // EXCEPTION TYPE 2: Validation exceptions with detailed error information
    public async Task<ValidationResult> ValidateDocumentAsync(string documentId)
    {
        var grainId = this.GetPrimaryKeyString();
        
        logger.LogDebug("Validating document {DocumentId} in grain {GrainId}", documentId, grainId);

        try
        {
            if (string.IsNullOrWhiteSpace(documentId))
            {
                throw new ValidationException("Document ID is required for validation");
            }

            var document = await documentRepository.GetDocumentAsync(documentId);
            if (document == null)
            {
                throw new DocumentNotFoundException($"Document {documentId} not found", documentId);
            }

            var validationErrors = new List<ValidationError>();

            // Perform various validations
            if (string.IsNullOrWhiteSpace(document.Content))
            {
                validationErrors.Add(new ValidationError
                {
                    Field = "Content",
                    Code = "CONTENT_EMPTY",
                    Message = "Document content cannot be empty"
                });
            }

            if (document.Content?.Length < 10)
            {
                validationErrors.Add(new ValidationError
                {
                    Field = "Content",
                    Code = "CONTENT_TOO_SHORT",
                    Message = "Document content must be at least 10 characters"
                });
            }

            // External validation
            try
            {
                var externalValidation = await externalService.ValidateAsync(documentId);
                if (!externalValidation.IsValid)
                {
                    validationErrors.AddRange(externalValidation.Errors.Select(e => new ValidationError
                    {
                        Field = e.Field,
                        Code = e.Code,
                        Message = e.Message,
                        Source = "ExternalService"
                    }));
                }
            }
            catch (ExternalServiceException ex)
            {
                logger.LogWarning(ex, "External validation service unavailable for document {DocumentId}. " +
                                     "Proceeding with internal validation only.", documentId);
                
                // Don't fail validation if external service is unavailable
                // Add a warning instead
                validationErrors.Add(new ValidationError
                {
                    Field = "External",
                    Code = "EXTERNAL_SERVICE_UNAVAILABLE",
                    Message = "External validation service temporarily unavailable",
                    Severity = ValidationSeverity.Warning
                });
            }

            return new ValidationResult
            {
                DocumentId = documentId,
                IsValid = validationErrors.All(e => e.Severity != ValidationSeverity.Error),
                Errors = validationErrors,
                ValidatedAt = DateTimeOffset.UtcNow
            };
        }
        catch (DocumentNotFoundException)
        {
            // Let document not found exceptions propagate
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Validation failed for document {DocumentId} in grain {GrainId}",
                documentId, grainId);
            
            throw new ValidationException(
                $"Validation failed for document {documentId}: {ex.Message}",
                ex);
        }
    }

    // EXCEPTION TYPE 3: Authorization and security exceptions
    public async Task<bool> DeleteDocumentAsync(string documentId, bool forceDelete = false)
    {
        var grainId = this.GetPrimaryKeyString();
        
        logger.LogInformation("Deleting document {DocumentId} in grain {GrainId} (force: {ForceDelete})",
            documentId, grainId, forceDelete);

        try
        {
            if (string.IsNullOrWhiteSpace(documentId))
            {
                throw new ArgumentException("Document ID cannot be null or empty", nameof(documentId));
            }

            var document = await documentRepository.GetDocumentAsync(documentId);
            if (document == null)
            {
                logger.LogWarning("Attempted to delete non-existent document {DocumentId}", documentId);
                return false; // Not an error - idempotent operation
            }

            // Check deletion permissions
            if (document.Status == DocumentStatus.Published && !forceDelete)
            {
                throw new InvalidOperationException(
                    $"Cannot delete published document {documentId} without force flag");
            }

            if (document.IsLocked && !forceDelete)
            {
                throw new DocumentLockedException(
                    $"Document {documentId} is locked and cannot be deleted",
                    documentId,
                    document.LockedBy);
            }

            // Perform cascading deletion checks
            var dependencies = await documentRepository.GetDocumentDependenciesAsync(documentId);
            if (dependencies.Count > 0 && !forceDelete)
            {
                throw new DocumentHasDependenciesException(
                    $"Document {documentId} has {dependencies.Count} dependencies",
                    documentId,
                    dependencies);
            }

            // Delete document
            await documentRepository.DeleteDocumentAsync(documentId);
            
            logger.LogInformation("Document {DocumentId} deleted successfully", documentId);
            return true;
        }
        catch (DocumentLockedException)
        {
            throw; // Business rule violations should propagate
        }
        catch (DocumentHasDependenciesException)
        {
            throw; // Business rule violations should propagate
        }
        catch (InvalidOperationException)
        {
            throw; // Business rule violations should propagate
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete document {DocumentId} in grain {GrainId}",
                documentId, grainId);
            
            throw new GrainOperationException(
                $"Failed to delete document {documentId} in grain {grainId}",
                grainId,
                ex);
        }
    }

    // EXCEPTION TYPE 4: Health checks and system status exceptions
    public async Task<HealthCheckResult> HealthCheckAsync()
    {
        var grainId = this.GetPrimaryKeyString();
        var healthChecks = new List<ComponentHealth>();

        try
        {
            // Check repository health
            try
            {
                await documentRepository.HealthCheckAsync();
                healthChecks.Add(new ComponentHealth
                {
                    Component = "DocumentRepository",
                    Status = HealthStatus.Healthy,
                    ResponseTime = TimeSpan.FromMilliseconds(50)
                });
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Repository health check failed in grain {GrainId}", grainId);
                
                healthChecks.Add(new ComponentHealth
                {
                    Component = "DocumentRepository",
                    Status = HealthStatus.Unhealthy,
                    Error = ex.Message,
                    ResponseTime = TimeSpan.FromMilliseconds(5000)
                });
            }

            // Check external service health
            try
            {
                await externalService.HealthCheckAsync();
                healthChecks.Add(new ComponentHealth
                {
                    Component = "ExternalService",
                    Status = HealthStatus.Healthy,
                    ResponseTime = TimeSpan.FromMilliseconds(100)
                });
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "External service health check failed in grain {GrainId}", grainId);
                
                healthChecks.Add(new ComponentHealth
                {
                    Component = "ExternalService",
                    Status = HealthStatus.Degraded,
                    Error = ex.Message,
                    ResponseTime = TimeSpan.FromMilliseconds(10000)
                });
            }

            // Check grain-specific health
            var grainStatus = consecutiveFailures > 5 ? HealthStatus.Degraded : HealthStatus.Healthy;
            healthChecks.Add(new ComponentHealth
            {
                Component = "GrainProcessor",
                Status = grainStatus,
                Metadata = new Dictionary<string, object>
                {
                    ["ConsecutiveFailures"] = consecutiveFailures,
                    ["LastFailure"] = lastFailure,
                    ["GrainId"] = grainId
                }
            });

            var overallStatus = healthChecks.Any(h => h.Status == HealthStatus.Unhealthy)
                ? HealthStatus.Unhealthy
                : healthChecks.Any(h => h.Status == HealthStatus.Degraded)
                    ? HealthStatus.Degraded
                    : HealthStatus.Healthy;

            return new HealthCheckResult
            {
                Status = overallStatus,
                Components = healthChecks,
                CheckedAt = DateTimeOffset.UtcNow,
                GrainId = grainId
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Health check failed in grain {GrainId}", grainId);
            
            return new HealthCheckResult
            {
                Status = HealthStatus.Unhealthy,
                Components = healthChecks,
                Error = ex.Message,
                CheckedAt = DateTimeOffset.UtcNow,
                GrainId = grainId
            };
        }
    }
}

// Client-side exception handling patterns
public class DocumentProcessorClient
{
    private readonly IGrainFactory grainFactory;
    private readonly ILogger<DocumentProcessorClient> logger;

    public DocumentProcessorClient(IGrainFactory grainFactory, ILogger<DocumentProcessorClient> logger)
    {
        this.grainFactory = grainFactory;
        this.logger = logger;
    }

    public async Task<ProcessingResult> ProcessDocumentWithRetryAsync(DocumentProcessingRequest request)
    {
        var maxRetries = 3;
        var baseDelay = TimeSpan.FromSeconds(1);
        
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var grain = grainFactory.GetGrain<IDocumentProcessorGrain>(request.ProcessorId);
                var result = await grain.ProcessDocumentAsync(request);
                
                logger.LogInformation("Document {DocumentId} processed successfully on attempt {Attempt}",
                    request.DocumentId, attempt);
                
                return result;
            }
            catch (DocumentProcessingException ex) when (IsRetriableError(ex))
            {
                if (attempt == maxRetries)
                {
                    logger.LogError(ex, "Document processing failed after {MaxRetries} attempts for {DocumentId}",
                        maxRetries, request.DocumentId);
                    throw;
                }
                
                var delay = TimeSpan.FromMilliseconds(baseDelay.TotalMilliseconds * Math.Pow(2, attempt - 1));
                
                logger.LogWarning(ex, "Document processing attempt {Attempt} failed for {DocumentId}. " +
                                     "Retrying in {Delay}ms",
                    attempt, request.DocumentId, delay.TotalMilliseconds);
                
                await Task.Delay(delay);
            }
            catch (Exception ex) when (!IsRetriableError(ex))
            {
                logger.LogError(ex, "Non-retriable error processing document {DocumentId} on attempt {Attempt}",
                    request.DocumentId, attempt);
                throw;
            }
        }
        
        throw new InvalidOperationException("This should never be reached");
    }

    private static bool IsRetriableError(Exception ex)
    {
        return ex switch
        {
            TimeoutException => true,
            ExternalServiceException externalEx when externalEx.IsTransient => true,
            DocumentProcessingException docEx when docEx.InnerException is TimeoutException => true,
            DocumentProcessingException docEx when docEx.InnerException is ExternalServiceException => true,
            GrainOperationException => true,
            _ => false
        };
    }
}
```

### Retry Patterns

Orleans applications benefit from implementing retry patterns to handle transient failures gracefully. This section demonstrates various retry strategies and their appropriate use cases.

```csharp
namespace DocumentProcessor.Orleans.RetryPatterns;

using Orleans;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Extensions.Http;

// Advanced retry patterns with Polly integration
public interface IResilientDocumentGrain : IGrain
{
    Task<ProcessingResult> ProcessWithExponentialBackoffAsync(DocumentProcessingRequest request);
    Task<ProcessingResult> ProcessWithCircuitBreakerAsync(DocumentProcessingRequest request);
    Task<ProcessingResult> ProcessWithBulkheadAsync(DocumentProcessingRequest request);
    Task<List<ProcessingResult>> ProcessBatchWithTimeoutAsync(List<DocumentProcessingRequest> requests);
}

public class ResilientDocumentGrain : Grain, IResilientDocumentGrain
{
    private readonly ILogger<ResilientDocumentGrain> logger;
    private readonly IExternalService externalService;
    private readonly IAsyncPolicy exponentialBackoffPolicy;
    private readonly IAsyncPolicy circuitBreakerPolicy;
    private readonly IAsyncPolicy bulkheadPolicy;
    private readonly IAsyncPolicy timeoutPolicy;

    public ResilientDocumentGrain(
        ILogger<ResilientDocumentGrain> logger,
        IExternalService externalService)
    {
        this.logger = logger;
        this.externalService = externalService;
        
        // Initialize retry policies
        this.exponentialBackoffPolicy = CreateExponentialBackoffPolicy();
        this.circuitBreakerPolicy = CreateCircuitBreakerPolicy();
        this.bulkheadPolicy = CreateBulkheadPolicy();
        this.timeoutPolicy = Policy.TimeoutAsync(TimeSpan.FromSeconds(30));
    }

    // PATTERN 1: Exponential backoff with jitter
    public async Task<ProcessingResult> ProcessWithExponentialBackoffAsync(DocumentProcessingRequest request)
    {
        var grainId = this.GetPrimaryKeyString();
        
        logger.LogInformation("Processing document {DocumentId} with exponential backoff in grain {GrainId}",
            request.DocumentId, grainId);

        try
        {
            var result = await exponentialBackoffPolicy.ExecuteAsync(async () =>
            {
                logger.LogDebug("Attempting to process document {DocumentId}", request.DocumentId);
                
                // Simulate external service call that may fail transiently
                var externalResult = await externalService.ProcessAsync(request.DocumentId, request.Content);
                
                return new ProcessingResult
                {
                    DocumentId = request.DocumentId,
                    Success = true,
                    ProcessedAt = DateTimeOffset.UtcNow,
                    ProcessingDuration = externalResult.ProcessingDuration,
                    ResultSize = externalResult.ProcessedContent?.Length ?? 0
                };
            });
            
            logger.LogInformation("Document {DocumentId} processed successfully with retry policy", request.DocumentId);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process document {DocumentId} after all retry attempts", request.DocumentId);
            
            return new ProcessingResult
            {
                DocumentId = request.DocumentId,
                Success = false,
                Error = ex.Message,
                ProcessedAt = DateTimeOffset.UtcNow
            };
        }
    }

    // PATTERN 2: Circuit breaker pattern
    public async Task<ProcessingResult> ProcessWithCircuitBreakerAsync(DocumentProcessingRequest request)
    {
        var grainId = this.GetPrimaryKeyString();
        
        logger.LogInformation("Processing document {DocumentId} with circuit breaker in grain {GrainId}",
            request.DocumentId, grainId);

        try
        {
            var result = await circuitBreakerPolicy.ExecuteAsync(async () =>
            {
                logger.LogDebug("Circuit breaker: Attempting to process document {DocumentId}", request.DocumentId);
                
                var externalResult = await externalService.ProcessAsync(request.DocumentId, request.Content);
                
                return new ProcessingResult
                {
                    DocumentId = request.DocumentId,
                    Success = true,
                    ProcessedAt = DateTimeOffset.UtcNow,
                    ProcessingDuration = externalResult.ProcessingDuration,
                    ResultSize = externalResult.ProcessedContent?.Length ?? 0
                };
            });
            
            logger.LogInformation("Document {DocumentId} processed successfully via circuit breaker", request.DocumentId);
            return result;
        }
        catch (BrokenCircuitException ex)
        {
            logger.LogWarning("Circuit breaker open - failing fast for document {DocumentId}: {Error}",
                request.DocumentId, ex.Message);
            
            return new ProcessingResult
            {
                DocumentId = request.DocumentId,
                Success = false,
                Error = "Service temporarily unavailable - circuit breaker open",
                ProcessedAt = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process document {DocumentId} via circuit breaker", request.DocumentId);
            
            return new ProcessingResult
            {
                DocumentId = request.DocumentId,
                Success = false,
                Error = ex.Message,
                ProcessedAt = DateTimeOffset.UtcNow
            };
        }
    }

    // PATTERN 3: Bulkhead isolation pattern
    public async Task<ProcessingResult> ProcessWithBulkheadAsync(DocumentProcessingRequest request)
    {
        var grainId = this.GetPrimaryKeyString();
        
        logger.LogInformation("Processing document {DocumentId} with bulkhead isolation in grain {GrainId}",
            request.DocumentId, grainId);

        try
        {
            var result = await bulkheadPolicy.ExecuteAsync(async () =>
            {
                logger.LogDebug("Bulkhead: Processing document {DocumentId}", request.DocumentId);
                
                var externalResult = await externalService.ProcessAsync(request.DocumentId, request.Content);
                
                return new ProcessingResult
                {
                    DocumentId = request.DocumentId,
                    Success = true,
                    ProcessedAt = DateTimeOffset.UtcNow,
                    ProcessingDuration = externalResult.ProcessingDuration,
                    ResultSize = externalResult.ProcessedContent?.Length ?? 0
                };
            });
            
            logger.LogInformation("Document {DocumentId} processed successfully via bulkhead", request.DocumentId);
            return result;
        }
        catch (BulkheadRejectedException ex)
        {
            logger.LogWarning("Bulkhead capacity exceeded for document {DocumentId}: {Error}",
                request.DocumentId, ex.Message);
            
            return new ProcessingResult
            {
                DocumentId = request.DocumentId,
                Success = false,
                Error = "Service capacity exceeded - please retry later",
                ProcessedAt = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process document {DocumentId} via bulkhead", request.DocumentId);
            
            return new ProcessingResult
            {
                DocumentId = request.DocumentId,
                Success = false,
                Error = ex.Message,
                ProcessedAt = DateTimeOffset.UtcNow
            };
        }
    }

    // PATTERN 4: Batch processing with timeout and partial success handling
    public async Task<List<ProcessingResult>> ProcessBatchWithTimeoutAsync(List<DocumentProcessingRequest> requests)
    {
        var grainId = this.GetPrimaryKeyString();
        
        logger.LogInformation("Processing batch of {Count} documents with timeout in grain {GrainId}",
            requests.Count, grainId);

        var results = new List<ProcessingResult>();
        var semaphore = new SemaphoreSlim(5); // Limit concurrent processing

        try
        {
            var tasks = requests.Select(async request =>
            {
                await semaphore.WaitAsync();
                try
                {
                    return await ProcessSingleDocumentWithTimeoutAsync(request);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            // Wait for all tasks with overall timeout
            var batchTimeout = TimeSpan.FromMinutes(5);
            using var cts = new CancellationTokenSource(batchTimeout);
            
            try
            {
                results.AddRange(await Task.WhenAll(tasks));
            }
            catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
            {
                logger.LogWarning("Batch processing timed out after {Timeout} in grain {GrainId}. " +
                                 "Returning partial results.", batchTimeout, grainId);
                
                // Collect completed results
                foreach (var task in tasks)
                {
                    if (task.IsCompletedSuccessfully)
                    {
                        results.Add(task.Result);
                    }
                    else if (task.IsFaulted)
                    {
                        var failedRequest = requests[Array.IndexOf(tasks.ToArray(), task)];
                        results.Add(new ProcessingResult
                        {
                            DocumentId = failedRequest.DocumentId,
                            Success = false,
                            Error = task.Exception?.GetBaseException().Message ?? "Unknown error",
                            ProcessedAt = DateTimeOffset.UtcNow
                        });
                    }
                }
            }

            var successCount = results.Count(r => r.Success);
            logger.LogInformation("Batch processing completed in grain {GrainId}. " +
                                 "Successful: {SuccessCount}, Failed: {FailedCount}",
                grainId, successCount, results.Count - successCount);

            return results;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Batch processing failed in grain {GrainId}", grainId);
            
            // Return failure results for any unprocessed requests
            foreach (var request in requests.Where(r => !results.Any(res => res.DocumentId == r.DocumentId)))
            {
                results.Add(new ProcessingResult
                {
                    DocumentId = request.DocumentId,
                    Success = false,
                    Error = $"Batch processing error: {ex.Message}",
                    ProcessedAt = DateTimeOffset.UtcNow
                });
            }
            
            return results;
        }
        finally
        {
            semaphore.Dispose();
        }
    }

    private async Task<ProcessingResult> ProcessSingleDocumentWithTimeoutAsync(DocumentProcessingRequest request)
    {
        try
        {
            return await timeoutPolicy.ExecuteAsync(async () =>
            {
                logger.LogDebug("Processing single document {DocumentId} with timeout", request.DocumentId);
                
                var externalResult = await externalService.ProcessAsync(request.DocumentId, request.Content);
                
                return new ProcessingResult
                {
                    DocumentId = request.DocumentId,
                    Success = true,
                    ProcessedAt = DateTimeOffset.UtcNow,
                    ProcessingDuration = externalResult.ProcessingDuration,
                    ResultSize = externalResult.ProcessedContent?.Length ?? 0
                };
            });
        }
        catch (TimeoutRejectedException ex)
        {
            logger.LogWarning("Document {DocumentId} processing timed out: {Error}",
                request.DocumentId, ex.Message);
            
            return new ProcessingResult
            {
                DocumentId = request.DocumentId,
                Success = false,
                Error = "Processing timeout exceeded",
                ProcessedAt = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to process document {DocumentId}", request.DocumentId);
            
            return new ProcessingResult
            {
                DocumentId = request.DocumentId,
                Success = false,
                Error = ex.Message,
                ProcessedAt = DateTimeOffset.UtcNow
            };
        }
    }

    // Policy creation methods
    private IAsyncPolicy CreateExponentialBackoffPolicy()
    {
        return Policy
            .Handle<ExternalServiceException>(ex => ex.IsTransient)
            .Or<TimeoutException>()
            .Or<HttpRequestException>()
            .WaitAndRetryAsync(
                retryCount: 5,
                sleepDurationProvider: retryAttempt =>
                {
                    // Exponential backoff with jitter
                    var delay = TimeSpan.FromSeconds(Math.Pow(2, retryAttempt));
                    var jitter = TimeSpan.FromMilliseconds(Random.Shared.Next(0, 1000));
                    return delay.Add(jitter);
                },
                onRetry: (outcome, timespan, retryCount, context) =>
                {
                    logger.LogWarning("Retry attempt {RetryCount} in {Delay}ms due to: {Exception}",
                        retryCount, timespan.TotalMilliseconds, outcome.Exception?.Message);
                });
    }

    private IAsyncPolicy CreateCircuitBreakerPolicy()
    {
        return Policy
            .Handle<ExternalServiceException>()
            .Or<TimeoutException>()
            .CircuitBreakerAsync(
                handledEventsAllowedBeforeBreaking: 3,
                durationOfBreak: TimeSpan.FromSeconds(30),
                onBreak: (exception, timespan) =>
                {
                    logger.LogWarning("Circuit breaker opened for {Duration} due to: {Exception}",
                        timespan, exception.Message);
                },
                onReset: () =>
                {
                    logger.LogInformation("Circuit breaker reset - service calls resumed");
                },
                onHalfOpen: () =>
                {
                    logger.LogInformation("Circuit breaker half-open - testing service availability");
                });
    }

    private IAsyncPolicy CreateBulkheadPolicy()
    {
        return Policy.BulkheadAsync(
            maxParallelization: 10,
            maxQueuingActions: 20,
            onBulkheadRejected: context =>
            {
                logger.LogWarning("Bulkhead rejected execution - capacity exceeded");
                return Task.CompletedTask;
            });
    }
}
```

### Fault Tolerance Basics

Building fault-tolerant Orleans applications requires implementing patterns for grain recovery, state corruption handling, and graceful degradation under various failure scenarios.

```csharp
namespace DocumentProcessor.Orleans.FaultTolerance;

using Orleans;
using Orleans.Runtime;
using Microsoft.Extensions.Logging;

// Fault-tolerant grain with comprehensive error recovery
public interface IFaultTolerantDocumentGrain : IGrainWithStringKey
{
    Task<DocumentOperationResult> CreateDocumentAsync(CreateDocumentRequest request);
    Task<DocumentOperationResult> UpdateDocumentAsync(UpdateDocumentRequest request);
    Task<DocumentRecoveryResult> RecoverFromCorruptionAsync();
    Task<GrainHealthStatus> GetHealthStatusAsync();
    Task<bool> ValidateStateIntegrityAsync();
}

public class FaultTolerantDocumentGrain : Grain, IFaultTolerantDocumentGrain
{
    private readonly ILogger<FaultTolerantDocumentGrain> logger;
    private readonly IPersistentState<DocumentState> documentState;
    private readonly IPersistentState<DocumentBackup> backupState;
    private readonly IPersistentState<OperationLog> operationLog;
    
    private int corruptionDetectionCount = 0;
    private DateTimeOffset lastStateValidation = DateTimeOffset.MinValue;
    private readonly Dictionary<string, DateTimeOffset> recentErrors = new();

    public FaultTolerantDocumentGrain(
        ILogger<FaultTolerantDocumentGrain> logger,
        [PersistentState("documentState", "primaryStore")] IPersistentState<DocumentState> documentState,
        [PersistentState("backupState", "backupStore")] IPersistentState<DocumentBackup> backupState,
        [PersistentState("operationLog", "auditStore")] IPersistentState<OperationLog> operationLog)
    {
        this.logger = logger;
        this.documentState = documentState;
        this.backupState = backupState;
        this.operationLog = operationLog;
    }

    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        var documentId = this.GetPrimaryKeyString();
        
        logger.LogInformation("FaultTolerantDocumentGrain {DocumentId} activating", documentId);
        
        try
        {
            // Validate state integrity on activation
            var isStateValid = await ValidateStateIntegrityInternalAsync();
            if (!isStateValid)
            {
                logger.LogWarning("State corruption detected on activation for document {DocumentId}. " +
                                 "Attempting automatic recovery.", documentId);
                
                await RecoverFromCorruptionInternalAsync();
            }
            
            // Initialize operation log if needed
            if (operationLog.State.DocumentId == null)
            {
                operationLog.State.DocumentId = documentId;
                operationLog.State.Operations = new List<DocumentOperation>();
                operationLog.State.CreatedAt = DateTimeOffset.UtcNow;
                await operationLog.WriteStateAsync();
            }
            
            lastStateValidation = DateTimeOffset.UtcNow;
            
            logger.LogInformation("FaultTolerantDocumentGrain {DocumentId} activated successfully", documentId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to activate FaultTolerantDocumentGrain {DocumentId}", documentId);
            throw;
        }
        
        await base.OnActivateAsync(cancellationToken);
    }

    // FAULT TOLERANCE 1: Transactional operations with rollback
    public async Task<DocumentOperationResult> CreateDocumentAsync(CreateDocumentRequest request)
    {
        var documentId = this.GetPrimaryKeyString();
        var operationId = Guid.NewGuid().ToString();
        
        logger.LogInformation("Creating document {DocumentId} with operation {OperationId}",
            documentId, operationId);

        try
        {
            // Log operation start
            await LogOperationAsync(new DocumentOperation
            {
                OperationId = operationId,
                Type = OperationType.Create,
                StartTime = DateTimeOffset.UtcNow,
                Request = request,
                Status = OperationStatus.Started
            });

            // Check if document already exists
            if (documentState.State.DocumentId != null)
            {
                return new DocumentOperationResult
                {
                    OperationId = operationId,
                    Success = false,
                    Error = $"Document {documentId} already exists",
                    CompletedAt = DateTimeOffset.UtcNow
                };
            }

            // Create backup of current state (empty in this case)
            await CreateBackupAsync();

            // Create document state
            var newState = new DocumentState
            {
                DocumentId = documentId,
                Title = request.Title,
                Content = request.Content,
                Status = DocumentStatus.Draft,
                CreatedAt = DateTimeOffset.UtcNow,
                LastModified = DateTimeOffset.UtcNow,
                Version = 1,
                CreatedBy = request.CreatedBy,
                Metadata = new Dictionary<string, object>(request.Metadata ?? new Dictionary<string, object>())
            };

            try
            {
                documentState.State = newState;
                await documentState.WriteStateAsync();
                
                // Log successful operation
                await LogOperationAsync(new DocumentOperation
                {
                    OperationId = operationId,
                    Type = OperationType.Create,
                    StartTime = DateTimeOffset.UtcNow,
                    EndTime = DateTimeOffset.UtcNow,
                    Request = request,
                    Status = OperationStatus.Completed
                });

                logger.LogInformation("Document {DocumentId} created successfully with operation {OperationId}",
                    documentId, operationId);

                return new DocumentOperationResult
                {
                    OperationId = operationId,
                    Success = true,
                    DocumentState = newState,
                    CompletedAt = DateTimeOffset.UtcNow
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to persist document {DocumentId}. Attempting rollback.", documentId);
                
                // Rollback - restore previous state
                await RestoreFromBackupAsync();
                
                // Log failed operation
                await LogOperationAsync(new DocumentOperation
                {
                    OperationId = operationId,
                    Type = OperationType.Create,
                    StartTime = DateTimeOffset.UtcNow,
                    EndTime = DateTimeOffset.UtcNow,
                    Request = request,
                    Status = OperationStatus.Failed,
                    Error = ex.Message
                });

                return new DocumentOperationResult
                {
                    OperationId = operationId,
                    Success = false,
                    Error = $"Document creation failed and was rolled back: {ex.Message}",
                    CompletedAt = DateTimeOffset.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical error during document {DocumentId} creation", documentId);
            
            RecordError("CreateDocument", ex);
            
            return new DocumentOperationResult
            {
                OperationId = operationId,
                Success = false,
                Error = $"Critical error during document creation: {ex.Message}",
                CompletedAt = DateTimeOffset.UtcNow
            };
        }
    }

    // FAULT TOLERANCE 2: State validation and corruption detection
    public async Task<DocumentOperationResult> UpdateDocumentAsync(UpdateDocumentRequest request)
    {
        var documentId = this.GetPrimaryKeyString();
        var operationId = Guid.NewGuid().ToString();
        
        logger.LogInformation("Updating document {DocumentId} with operation {OperationId}",
            documentId, operationId);

        try
        {
            // Validate state before operation
            var isStateValid = await ValidateStateIntegrityInternalAsync();
            if (!isStateValid)
            {
                logger.LogWarning("State corruption detected before update for document {DocumentId}. " +
                                 "Attempting recovery.", documentId);
                
                var recoveryResult = await RecoverFromCorruptionInternalAsync();
                if (!recoveryResult.Success)
                {
                    return new DocumentOperationResult
                    {
                        OperationId = operationId,
                        Success = false,
                        Error = "Cannot update document due to unrecoverable state corruption",
                        CompletedAt = DateTimeOffset.UtcNow
                    };
                }
            }

            // Check if document exists
            if (documentState.State.DocumentId == null)
            {
                return new DocumentOperationResult
                {
                    OperationId = operationId,
                    Success = false,
                    Error = $"Document {documentId} not found",
                    CompletedAt = DateTimeOffset.UtcNow
                };
            }

            // Create backup before modification
            await CreateBackupAsync();
            
            // Log operation start
            await LogOperationAsync(new DocumentOperation
            {
                OperationId = operationId,
                Type = OperationType.Update,
                StartTime = DateTimeOffset.UtcNow,
                Request = request,
                Status = OperationStatus.Started,
                PreviousVersion = documentState.State.Version
            });

            var previousState = documentState.State;
            
            try
            {
                // Apply updates
                if (!string.IsNullOrWhiteSpace(request.Title))
                {
                    documentState.State.Title = request.Title;
                }
                
                if (!string.IsNullOrWhiteSpace(request.Content))
                {
                    documentState.State.Content = request.Content;
                }
                
                if (request.MetadataUpdates != null)
                {
                    foreach (var kvp in request.MetadataUpdates)
                    {
                        documentState.State.Metadata[kvp.Key] = kvp.Value;
                    }
                }
                
                documentState.State.LastModified = DateTimeOffset.UtcNow;
                documentState.State.Version++;
                documentState.State.LastModifiedBy = request.ModifiedBy;

                // Persist changes
                await documentState.WriteStateAsync();

                // Validate state after update
                var postUpdateValidation = await ValidateStateIntegrityInternalAsync();
                if (!postUpdateValidation)
                {
                    logger.LogError("State validation failed after update for document {DocumentId}. " +
                                   "Rolling back to previous state.", documentId);
                    
                    // Rollback to previous state
                    documentState.State = previousState;
                    await documentState.WriteStateAsync();
                    
                    return new DocumentOperationResult
                    {
                        OperationId = operationId,
                        Success = false,
                        Error = "Update rolled back due to state validation failure",
                        CompletedAt = DateTimeOffset.UtcNow
                    };
                }

                // Log successful operation
                await LogOperationAsync(new DocumentOperation
                {
                    OperationId = operationId,
                    Type = OperationType.Update,
                    StartTime = DateTimeOffset.UtcNow,
                    EndTime = DateTimeOffset.UtcNow,
                    Request = request,
                    Status = OperationStatus.Completed,
                    PreviousVersion = previousState.Version,
                    NewVersion = documentState.State.Version
                });

                logger.LogInformation("Document {DocumentId} updated successfully to version {Version}",
                    documentId, documentState.State.Version);

                return new DocumentOperationResult
                {
                    OperationId = operationId,
                    Success = true,
                    DocumentState = documentState.State,
                    CompletedAt = DateTimeOffset.UtcNow
                };
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to update document {DocumentId}. Rolling back changes.", documentId);
                
                // Rollback to backup
                await RestoreFromBackupAsync();
                
                // Log failed operation
                await LogOperationAsync(new DocumentOperation
                {
                    OperationId = operationId,
                    Type = OperationType.Update,
                    StartTime = DateTimeOffset.UtcNow,
                    EndTime = DateTimeOffset.UtcNow,
                    Request = request,
                    Status = OperationStatus.Failed,
                    Error = ex.Message
                });

                return new DocumentOperationResult
                {
                    OperationId = operationId,
                    Success = false,
                    Error = $"Update failed and was rolled back: {ex.Message}",
                    CompletedAt = DateTimeOffset.UtcNow
                };
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Critical error during document {DocumentId} update", documentId);
            
            RecordError("UpdateDocument", ex);
            
            return new DocumentOperationResult
            {
                OperationId = operationId,
                Success = false,
                Error = $"Critical error during document update: {ex.Message}",
                CompletedAt = DateTimeOffset.UtcNow
            };
        }
    }

    // FAULT TOLERANCE 3: Corruption recovery mechanisms
    public async Task<DocumentRecoveryResult> RecoverFromCorruptionAsync()
    {
        var documentId = this.GetPrimaryKeyString();
        
        logger.LogWarning("Manual recovery initiated for document {DocumentId}", documentId);
        
        return await RecoverFromCorruptionInternalAsync();
    }

    private async Task<DocumentRecoveryResult> RecoverFromCorruptionInternalAsync()
    {
        var documentId = this.GetPrimaryKeyString();
        var recoveryId = Guid.NewGuid().ToString();
        
        logger.LogInformation("Starting corruption recovery {RecoveryId} for document {DocumentId}",
            recoveryId, documentId);

        try
        {
            var recoverySteps = new List<string>();
            
            // Step 1: Attempt to restore from backup
            if (backupState.State.DocumentId != null)
            {
                logger.LogInformation("Attempting backup restoration for document {DocumentId}", documentId);
                
                try
                {
                    documentState.State = backupState.State.DocumentState ?? new DocumentState();
                    await documentState.WriteStateAsync();
                    
                    recoverySteps.Add("Restored from backup state");
                    
                    // Validate restored state
                    var isValid = await ValidateStateIntegrityInternalAsync();
                    if (isValid)
                    {
                        logger.LogInformation("Successfully recovered document {DocumentId} from backup", documentId);
                        
                        return new DocumentRecoveryResult
                        {
                            RecoveryId = recoveryId,
                            Success = true,
                            RecoveryMethod = "BackupRestore",
                            RecoverySteps = recoverySteps,
                            RecoveredAt = DateTimeOffset.UtcNow
                        };
                    }
                    else
                    {
                        recoverySteps.Add("Backup restoration failed validation");
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Backup restoration failed for document {DocumentId}", documentId);
                    recoverySteps.Add($"Backup restoration failed: {ex.Message}");
                }
            }

            // Step 2: Attempt to reconstruct from operation log
            if (operationLog.State.Operations.Count > 0)
            {
                logger.LogInformation("Attempting state reconstruction from operation log for document {DocumentId}",
                    documentId);
                
                try
                {
                    var reconstructedState = await ReconstructStateFromLogAsync();
                    if (reconstructedState != null)
                    {
                        documentState.State = reconstructedState;
                        await documentState.WriteStateAsync();
                        
                        recoverySteps.Add("Reconstructed from operation log");
                        
                        var isValid = await ValidateStateIntegrityInternalAsync();
                        if (isValid)
                        {
                            logger.LogInformation("Successfully recovered document {DocumentId} from operation log",
                                documentId);
                            
                            return new DocumentRecoveryResult
                            {
                                RecoveryId = recoveryId,
                                Success = true,
                                RecoveryMethod = "LogReconstruction",
                                RecoverySteps = recoverySteps,
                                RecoveredAt = DateTimeOffset.UtcNow
                            };
                        }
                        else
                        {
                            recoverySteps.Add("Log reconstruction failed validation");
                        }
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Log reconstruction failed for document {DocumentId}", documentId);
                    recoverySteps.Add($"Log reconstruction failed: {ex.Message}");
                }
            }

            // Step 3: Initialize with empty state as last resort
            logger.LogWarning("Creating empty state as last resort for document {DocumentId}", documentId);
            
            documentState.State = new DocumentState
            {
                DocumentId = documentId,
                Title = "Recovered Document",
                Content = "This document was recovered from corruption",
                Status = DocumentStatus.Draft,
                CreatedAt = DateTimeOffset.UtcNow,
                LastModified = DateTimeOffset.UtcNow,
                Version = 1,
                CreatedBy = "System Recovery"
            };
            
            await documentState.WriteStateAsync();
            recoverySteps.Add("Created empty recovery state");
            
            corruptionDetectionCount++;
            
            return new DocumentRecoveryResult
            {
                RecoveryId = recoveryId,
                Success = true,
                RecoveryMethod = "EmptyStateInitialization",
                RecoverySteps = recoverySteps,
                RecoveredAt = DateTimeOffset.UtcNow,
                DataLoss = true
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "All recovery attempts failed for document {DocumentId}", documentId);
            
            return new DocumentRecoveryResult
            {
                RecoveryId = recoveryId,
                Success = false,
                Error = ex.Message,
                RecoverySteps = new List<string> { "All recovery methods failed" },
                RecoveredAt = DateTimeOffset.UtcNow
            };
        }
    }

    // FAULT TOLERANCE 4: Health monitoring and diagnostics
    public async Task<GrainHealthStatus> GetHealthStatusAsync()
    {
        var documentId = this.GetPrimaryKeyString();
        
        try
        {
            var healthChecks = new List<HealthCheck>();
            
            // Check state integrity
            var stateIntegrity = await ValidateStateIntegrityInternalAsync();
            healthChecks.Add(new HealthCheck
            {
                Name = "StateIntegrity",
                Status = stateIntegrity ? HealthStatus.Healthy : HealthStatus.Unhealthy,
                Message = stateIntegrity ? "State is valid" : "State corruption detected"
            });
            
            // Check recent error rate
            var recentErrorCount = recentErrors.Count(kvp => 
                DateTimeOffset.UtcNow - kvp.Value < TimeSpan.FromMinutes(5));
            
            var errorStatus = recentErrorCount switch
            {
                0 => HealthStatus.Healthy,
                <= 3 => HealthStatus.Degraded,
                _ => HealthStatus.Unhealthy
            };
            
            healthChecks.Add(new HealthCheck
            {
                Name = "ErrorRate",
                Status = errorStatus,
                Message = $"{recentErrorCount} errors in last 5 minutes"
            });
            
            // Check backup availability
            var hasBackup = backupState.State.DocumentId != null;
            healthChecks.Add(new HealthCheck
            {
                Name = "BackupAvailability",
                Status = hasBackup ? HealthStatus.Healthy : HealthStatus.Degraded,
                Message = hasBackup ? "Backup available" : "No backup available"
            });
            
            // Check operation log integrity
            var logIntegrity = operationLog.State.Operations?.Count >= 0;
            healthChecks.Add(new HealthCheck
            {
                Name = "OperationLog",
                Status = logIntegrity ? HealthStatus.Healthy : HealthStatus.Unhealthy,
                Message = $"Operation log has {operationLog.State.Operations?.Count ?? 0} entries"
            });
            
            var overallStatus = healthChecks.Any(h => h.Status == HealthStatus.Unhealthy)
                ? HealthStatus.Unhealthy
                : healthChecks.Any(h => h.Status == HealthStatus.Degraded)
                    ? HealthStatus.Degraded
                    : HealthStatus.Healthy;
            
            return new GrainHealthStatus
            {
                DocumentId = documentId,
                OverallStatus = overallStatus,
                HealthChecks = healthChecks,
                CorruptionDetectionCount = corruptionDetectionCount,
                LastStateValidation = lastStateValidation,
                CheckedAt = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Health check failed for document {DocumentId}", documentId);
            
            return new GrainHealthStatus
            {
                DocumentId = documentId,
                OverallStatus = HealthStatus.Unhealthy,
                Error = ex.Message,
                CheckedAt = DateTimeOffset.UtcNow
            };
        }
    }

    public async Task<bool> ValidateStateIntegrityAsync()
    {
        return await ValidateStateIntegrityInternalAsync();
    }

    private async Task<bool> ValidateStateIntegrityInternalAsync()
    {
        try
        {
            lastStateValidation = DateTimeOffset.UtcNow;
            
            // Basic state validation
            if (documentState.State.DocumentId != this.GetPrimaryKeyString())
            {
                logger.LogWarning("Document ID mismatch in state for grain {GrainId}", this.GetPrimaryKeyString());
                return false;
            }
            
            if (documentState.State.Version < 1)
            {
                logger.LogWarning("Invalid version number in state for document {DocumentId}", 
                    documentState.State.DocumentId);
                return false;
            }
            
            if (documentState.State.CreatedAt > documentState.State.LastModified)
            {
                logger.LogWarning("Invalid timestamp relationship in state for document {DocumentId}",
                    documentState.State.DocumentId);
                return false;
            }
            
            // Additional validation can be added here
            
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "State validation failed for document {DocumentId}", this.GetPrimaryKeyString());
            return false;
        }
    }

    // Helper methods for fault tolerance
    private async Task CreateBackupAsync()
    {
        try
        {
            backupState.State = new DocumentBackup
            {
                DocumentId = documentState.State.DocumentId,
                DocumentState = documentState.State,
                BackupCreatedAt = DateTimeOffset.UtcNow
            };
            
            await backupState.WriteStateAsync();
            
            logger.LogDebug("Backup created for document {DocumentId}", documentState.State.DocumentId);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to create backup for document {DocumentId}", 
                documentState.State.DocumentId);
        }
    }

    private async Task RestoreFromBackupAsync()
    {
        try
        {
            if (backupState.State.DocumentState != null)
            {
                documentState.State = backupState.State.DocumentState;
                await documentState.WriteStateAsync();
                
                logger.LogInformation("Restored document {DocumentId} from backup", 
                    documentState.State.DocumentId);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to restore from backup for document {DocumentId}",
                this.GetPrimaryKeyString());
            throw;
        }
    }

    private async Task<DocumentState?> ReconstructStateFromLogAsync()
    {
        try
        {
            var createOperation = operationLog.State.Operations
                .FirstOrDefault(op => op.Type == OperationType.Create && 
                                     op.Status == OperationStatus.Completed);
            
            if (createOperation?.Request is CreateDocumentRequest createRequest)
            {
                var reconstructedState = new DocumentState
                {
                    DocumentId = this.GetPrimaryKeyString(),
                    Title = createRequest.Title,
                    Content = createRequest.Content,
                    Status = DocumentStatus.Draft,
                    CreatedAt = createOperation.StartTime,
                    LastModified = createOperation.EndTime ?? createOperation.StartTime,
                    Version = 1,
                    CreatedBy = createRequest.CreatedBy,
                    Metadata = new Dictionary<string, object>(createRequest.Metadata ?? new Dictionary<string, object>())
                };
                
                // Apply subsequent update operations
                var updateOperations = operationLog.State.Operations
                    .Where(op => op.Type == OperationType.Update && 
                                op.Status == OperationStatus.Completed &&
                                op.StartTime > createOperation.StartTime)
                    .OrderBy(op => op.StartTime);
                
                foreach (var updateOp in updateOperations)
                {
                    if (updateOp.Request is UpdateDocumentRequest updateRequest)
                    {
                        if (!string.IsNullOrWhiteSpace(updateRequest.Title))
                        {
                            reconstructedState.Title = updateRequest.Title;
                        }
                        
                        if (!string.IsNullOrWhiteSpace(updateRequest.Content))
                        {
                            reconstructedState.Content = updateRequest.Content;
                        }
                        
                        if (updateRequest.MetadataUpdates != null)
                        {
                            foreach (var kvp in updateRequest.MetadataUpdates)
                            {
                                reconstructedState.Metadata[kvp.Key] = kvp.Value;
                            }
                        }
                        
                        reconstructedState.LastModified = updateOp.EndTime ?? updateOp.StartTime;
                        reconstructedState.Version++;
                        reconstructedState.LastModifiedBy = updateRequest.ModifiedBy;
                    }
                }
                
                logger.LogInformation("Reconstructed state from {OperationCount} operations for document {DocumentId}",
                    1 + updateOperations.Count(), this.GetPrimaryKeyString());
                
                return reconstructedState;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reconstruct state from log for document {DocumentId}",
                this.GetPrimaryKeyString());
            return null;
        }
    }

    private async Task LogOperationAsync(DocumentOperation operation)
    {
        try
        {
            operationLog.State.Operations.Add(operation);
            operationLog.State.LastUpdated = DateTimeOffset.UtcNow;
            
            await operationLog.WriteStateAsync();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to log operation for document {DocumentId}", 
                this.GetPrimaryKeyString());
        }
    }

    private void RecordError(string operation, Exception ex)
    {
        var errorKey = $"{operation}:{ex.GetType().Name}";
        recentErrors[errorKey] = DateTimeOffset.UtcNow;
        
        // Clean up old errors (older than 1 hour)
        var cutoff = DateTimeOffset.UtcNow.AddHours(-1);
        var oldErrors = recentErrors.Where(kvp => kvp.Value < cutoff).Select(kvp => kvp.Key).ToList();
        
        foreach (var oldError in oldErrors)
        {
            recentErrors.Remove(oldError);
        }
    }
}

// Supporting types for error handling and fault tolerance
namespace DocumentProcessor.Orleans.ErrorHandling.Types;

// Custom exception types for domain-specific errors
[GenerateSerializer]
public class DocumentProcessingException : Exception
{
    [Id(0)] public string DocumentId { get; init; } = string.Empty;
    
    public DocumentProcessingException() { }
    public DocumentProcessingException(string message) : base(message) { }
    public DocumentProcessingException(string message, Exception innerException) : base(message, innerException) { }
    public DocumentProcessingException(string message, string documentId) : base(message)
    {
        DocumentId = documentId;
    }
    public DocumentProcessingException(string message, string documentId, Exception innerException) : base(message, innerException)
    {
        DocumentId = documentId;
    }
}

[GenerateSerializer]
public class DocumentTooLargeException : DocumentProcessingException
{
    [Id(0)] public long ActualSize { get; init; }
    
    public DocumentTooLargeException() { }
    public DocumentTooLargeException(string message) : base(message) { }
    public DocumentTooLargeException(string message, string documentId, long actualSize) : base(message, documentId)
    {
        ActualSize = actualSize;
    }
}

[GenerateSerializer]
public class DocumentAlreadyExistsException : DocumentProcessingException
{
    public DocumentAlreadyExistsException() { }
    public DocumentAlreadyExistsException(string message) : base(message) { }
    public DocumentAlreadyExistsException(string message, string documentId) : base(message, documentId) { }
}

[GenerateSerializer]
public class DocumentNotFoundException : DocumentProcessingException
{
    public DocumentNotFoundException() { }
    public DocumentNotFoundException(string message) : base(message) { }
    public DocumentNotFoundException(string message, string documentId) : base(message, documentId) { }
}

[GenerateSerializer]
public class DocumentLockedException : DocumentProcessingException
{
    [Id(0)] public string LockedBy { get; init; } = string.Empty;
    
    public DocumentLockedException() { }
    public DocumentLockedException(string message) : base(message) { }
    public DocumentLockedException(string message, string documentId, string lockedBy) : base(message, documentId)
    {
        LockedBy = lockedBy;
    }
}

[GenerateSerializer]
public class DocumentHasDependenciesException : DocumentProcessingException
{
    [Id(0)] public List<string> Dependencies { get; init; } = new();
    
    public DocumentHasDependenciesException() { }
    public DocumentHasDependenciesException(string message) : base(message) { }
    public DocumentHasDependenciesException(string message, string documentId, List<string> dependencies) : base(message, documentId)
    {
        Dependencies = dependencies;
    }
}

[GenerateSerializer]
public class DocumentPersistenceException : DocumentProcessingException
{
    public DocumentPersistenceException() { }
    public DocumentPersistenceException(string message) : base(message) { }
    public DocumentPersistenceException(string message, string documentId, Exception innerException) : base(message, documentId, innerException) { }
}

[GenerateSerializer]
public class ValidationException : Exception
{
    public ValidationException() { }
    public ValidationException(string message) : base(message) { }
    public ValidationException(string message, Exception innerException) : base(message, innerException) { }
}

[GenerateSerializer]
public class GrainOperationException : Exception
{
    [Id(0)] public string GrainId { get; init; } = string.Empty;
    
    public GrainOperationException() { }
    public GrainOperationException(string message) : base(message) { }
    public GrainOperationException(string message, string grainId, Exception innerException) : base(message, innerException)
    {
        GrainId = grainId;
    }
}

[GenerateSerializer]
public class ExternalServiceException : Exception
{
    [Id(0)] public bool IsTransient { get; init; }
    [Id(1)] public string ServiceName { get; init; } = string.Empty;
    
    public ExternalServiceException() { }
    public ExternalServiceException(string message) : base(message) { }
    public ExternalServiceException(string message, bool isTransient) : base(message)
    {
        IsTransient = isTransient;
    }
    public ExternalServiceException(string message, string serviceName, bool isTransient) : base(message)
    {
        ServiceName = serviceName;
        IsTransient = isTransient;
    }
}

// Processing and validation result types
[GenerateSerializer]
public record ProcessingResult
{
    [Id(0)] public string DocumentId { get; init; } = string.Empty;
    [Id(1)] public bool Success { get; init; }
    [Id(2)] public string? Error { get; init; }
    [Id(3)] public DateTimeOffset ProcessedAt { get; init; }
    [Id(4)] public TimeSpan ProcessingDuration { get; init; }
    [Id(5)] public long ResultSize { get; init; }
}

[GenerateSerializer]
public record ValidationResult
{
    [Id(0)] public string DocumentId { get; init; } = string.Empty;
    [Id(1)] public bool IsValid { get; init; }
    [Id(2)] public List<ValidationError> Errors { get; init; } = new();
    [Id(3)] public DateTimeOffset ValidatedAt { get; init; }
}

[GenerateSerializer]
public record ValidationError
{
    [Id(0)] public string Field { get; init; } = string.Empty;
    [Id(1)] public string Code { get; init; } = string.Empty;
    [Id(2)] public string Message { get; init; } = string.Empty;
    [Id(3)] public ValidationSeverity Severity { get; init; } = ValidationSeverity.Error;
    [Id(4)] public string? Source { get; init; }
}

[GenerateSerializer]
public enum ValidationSeverity
{
    [Id(0)] Error,
    [Id(1)] Warning,
    [Id(2)] Information
}

// Health check types
[GenerateSerializer]
public record HealthCheckResult
{
    [Id(0)] public HealthStatus Status { get; init; }
    [Id(1)] public List<ComponentHealth> Components { get; init; } = new();
    [Id(2)] public string? Error { get; init; }
    [Id(3)] public DateTimeOffset CheckedAt { get; init; }
    [Id(4)] public string GrainId { get; init; } = string.Empty;
}

[GenerateSerializer]
public record ComponentHealth
{
    [Id(0)] public string Component { get; init; } = string.Empty;
    [Id(1)] public HealthStatus Status { get; init; }
    [Id(2)] public string? Error { get; init; }
    [Id(3)] public TimeSpan ResponseTime { get; init; }
    [Id(4)] public Dictionary<string, object> Metadata { get; init; } = new();
}

[GenerateSerializer]
public enum HealthStatus
{
    [Id(0)] Healthy,
    [Id(1)] Degraded,
    [Id(2)] Unhealthy
}

// External service interfaces and types
public interface IExternalService
{
    Task<ExternalProcessingResult> ProcessAsync(string documentId, string content);
    Task<ExternalValidationResult> ValidateAsync(string documentId);
    Task HealthCheckAsync();
}

[GenerateSerializer]
public record ExternalProcessingResult
{
    [Id(0)] public string ProcessedContent { get; init; } = string.Empty;
    [Id(1)] public TimeSpan ProcessingDuration { get; init; }
    [Id(2)] public Dictionary<string, object> Metadata { get; init; } = new();
}

[GenerateSerializer]
public record ExternalValidationResult
{
    [Id(0)] public bool IsValid { get; init; }
    [Id(1)] public List<ExternalValidationError> Errors { get; init; } = new();
}

[GenerateSerializer]
public record ExternalValidationError
{
    [Id(0)] public string Field { get; init; } = string.Empty;
    [Id(1)] public string Code { get; init; } = string.Empty;
    [Id(2)] public string Message { get; init; } = string.Empty;
}

// Repository interface for demonstration
public interface IDocumentRepository
{
    Task<ProcessedDocument?> GetDocumentAsync(string documentId);
    Task SaveDocumentAsync(ProcessedDocument document);
    Task DeleteDocumentAsync(string documentId);
    Task<List<string>> GetDocumentDependenciesAsync(string documentId);
    Task HealthCheckAsync();
}

[GenerateSerializer]
public record ProcessedDocument
{
    [Id(0)] public string DocumentId { get; init; } = string.Empty;
    [Id(1)] public string OriginalContent { get; init; } = string.Empty;
    [Id(2)] public string ProcessedContent { get; init; } = string.Empty;
    [Id(3)] public DateTimeOffset ProcessedAt { get; init; }
    [Id(4)] public Dictionary<string, object> ProcessingMetadata { get; init; } = new();
    [Id(5)] public DocumentStatus Status { get; init; }
    [Id(6)] public bool IsLocked { get; init; }
    [Id(7)] public string? LockedBy { get; init; }
}

// Fault tolerance operation types
[GenerateSerializer]
public record DocumentOperationResult
{
    [Id(0)] public string OperationId { get; init; } = string.Empty;
    [Id(1)] public bool Success { get; init; }
    [Id(2)] public string? Error { get; init; }
    [Id(3)] public DocumentState? DocumentState { get; init; }
    [Id(4)] public DateTimeOffset CompletedAt { get; init; }
}

[GenerateSerializer]
public record DocumentRecoveryResult
{
    [Id(0)] public string RecoveryId { get; init; } = string.Empty;
    [Id(1)] public bool Success { get; init; }
    [Id(2)] public string? Error { get; init; }
    [Id(3)] public string RecoveryMethod { get; init; } = string.Empty;
    [Id(4)] public List<string> RecoverySteps { get; init; } = new();
    [Id(5)] public bool DataLoss { get; init; }
    [Id(6)] public DateTimeOffset RecoveredAt { get; init; }
}

[GenerateSerializer]
public record GrainHealthStatus
{
    [Id(0)] public string DocumentId { get; init; } = string.Empty;
    [Id(1)] public HealthStatus OverallStatus { get; init; }
    [Id(2)] public List<HealthCheck> HealthChecks { get; init; } = new();
    [Id(3)] public int CorruptionDetectionCount { get; init; }
    [Id(4)] public DateTimeOffset LastStateValidation { get; init; }
    [Id(5)] public string? Error { get; init; }
    [Id(6)] public DateTimeOffset CheckedAt { get; init; }
}

[GenerateSerializer]
public record HealthCheck
{
    [Id(0)] public string Name { get; init; } = string.Empty;
    [Id(1)] public HealthStatus Status { get; init; }
    [Id(2)] public string Message { get; init; } = string.Empty;
}

// Operation logging types
[GenerateSerializer]
public record DocumentOperation
{
    [Id(0)] public string OperationId { get; init; } = string.Empty;
    [Id(1)] public OperationType Type { get; init; }
    [Id(2)] public DateTimeOffset StartTime { get; init; }
    [Id(3)] public DateTimeOffset? EndTime { get; init; }
    [Id(4)] public object? Request { get; init; }
    [Id(5)] public OperationStatus Status { get; init; }
    [Id(6)] public string? Error { get; init; }
    [Id(7)] public int? PreviousVersion { get; init; }
    [Id(8)] public int? NewVersion { get; init; }
}

[GenerateSerializer]
public enum OperationType
{
    [Id(0)] Create,
    [Id(1)] Update,
    [Id(2)] Delete,
    [Id(3)] Validate
}

[GenerateSerializer]
public enum OperationStatus
{
    [Id(0)] Started,
    [Id(1)] Completed,
    [Id(2)] Failed,
    [Id(3)] Cancelled
}

// State types for fault tolerance
[GenerateSerializer]
public class OperationLog
{
    [Id(0)] public string DocumentId { get; set; } = string.Empty;
    [Id(1)] public List<DocumentOperation> Operations { get; set; } = new();
    [Id(2)] public DateTimeOffset CreatedAt { get; set; }
    [Id(3)] public DateTimeOffset LastUpdated { get; set; }
}

[GenerateSerializer]
public class DocumentBackup
{
    [Id(0)] public string DocumentId { get; set; } = string.Empty;
    [Id(1)] public DocumentState? DocumentState { get; set; }
    [Id(2)] public DateTimeOffset BackupCreatedAt { get; set; }
}

// Request types
[GenerateSerializer]
public record CreateDocumentRequest
{
    [Id(0)] public string Title { get; init; } = string.Empty;
    [Id(1)] public string Content { get; init; } = string.Empty;
    [Id(2)] public string CreatedBy { get; init; } = string.Empty;
    [Id(3)] public Dictionary<string, object>? Metadata { get; init; }
}

[GenerateSerializer]
public record UpdateDocumentRequest
{
    [Id(0)] public string? Title { get; init; }
    [Id(1)] public string? Content { get; init; }
    [Id(2)] public string ModifiedBy { get; init; } = string.Empty;
    [Id(3)] public Dictionary<string, object>? MetadataUpdates { get; init; }
}

**Usage**:

1. **Grain Definition**: Start with simple grain interfaces and implementations
2. **Lifecycle Management**: Understand activation and deactivation patterns
3. **Identity Design**: Choose appropriate grain key strategies
4. **Communication**: Implement basic grain-to-grain communication
5. **State Handling**: Begin with stateless grains before adding persistence
6. **Error Management**: Implement basic error handling and retry patterns
7. **Best Practices**: Follow Orleans conventions and patterns

**Notes**:

- **Single-Threading**: Orleans guarantees single-threaded execution per grain instance
- **Location Transparency**: Clients don't need to know where grains are located
- **Automatic Scaling**: Orleans handles grain distribution and load balancing
- **Fault Tolerance**: Built-in failure detection and recovery mechanisms
- **State Consistency**: Orleans provides strong consistency within grain boundaries
- **Performance**: Consider grain granularity and communication patterns
- **Testing**: Use Orleans test cluster for unit and integration testing

**Related Snippets**:

- [Document Processing Grains](document-processing-grains.md) - Specialized grain implementations
- [State Management](state-management.md) - Advanced persistence patterns
- [Streaming Patterns](streaming-patterns.md) - Event-driven communication
- [Testing Strategies](testing-strategies.md) - Testing grain implementations
