using CSharp.CacheInvalidation;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace CSharp.CacheInvalidation;

internal class Program
{
    private static async Task Main(string[] args)
    {
        Console.WriteLine("Cache Invalidation Strategies Examples");
        Console.WriteLine("====================================");

        // Setup DI container
        var services = new ServiceCollection()
            .AddDistributedMemoryCache()
            .AddMemoryCache()
            .AddLogging(builder => builder.AddConsole())
            .Configure<CacheInvalidationOptions>(opts =>
            {
                opts.CleanupInterval = TimeSpan.FromMinutes(10);
                opts.EventBufferSize = 50;
            })
            .AddSingleton<ICacheDependencyTracker, CacheDependencyTracker>()
            .AddSingleton<ICacheTagManager, CacheTagManager>()
            .AddSingleton<ICacheInvalidationService, CacheInvalidationService>()
            .BuildServiceProvider();

        await RunBasicInvalidationExample(services);
        await RunTagBasedInvalidationExample(services);
        await RunDependencyBasedInvalidationExample(services);
        await RunCustomInvalidationRulesExample(services);
        await RunEventDrivenInvalidationExample(services);
        await RunHierarchicalInvalidationExample(services);
        await RunSmartCacheWarmingExample(services);
        await RunStatisticsExample(services);
    }

    private static async Task RunBasicInvalidationExample(ServiceProvider services)
    {
        Console.WriteLine("\n1. Basic Cache Invalidation Examples");
        Console.WriteLine("------------------------------------");

        var invalidationService = services.GetRequiredService<ICacheInvalidationService>();
        var cache = services.GetRequiredService<IDistributedCache>();

        // Set up some test data
        await cache.SetStringAsync("user:123", JsonSerializer.Serialize(new { Id = 123, Name = "John" }));
        await cache.SetStringAsync("user:124", JsonSerializer.Serialize(new { Id = 124, Name = "Jane" }));
        await cache.SetStringAsync("product:456", JsonSerializer.Serialize(new { Id = 456, Name = "Laptop" }));

        Console.WriteLine("Cached test data");

        // Direct key invalidation
        await invalidationService.InvalidateAsync("user:123");
        Console.WriteLine("✓ Invalidated user:123");

        // Bulk invalidation
        await invalidationService.InvalidateAsync(new[] { "user:124", "product:456" });
        Console.WriteLine("✓ Bulk invalidated multiple keys");

        // Pattern-based invalidation (in a real implementation this would work with Redis KEYS or similar)
        await invalidationService.InvalidateByPatternAsync("user:*");
        Console.WriteLine("✓ Invalidated all user keys by pattern");
    }

    private static async Task RunTagBasedInvalidationExample(ServiceProvider services)
    {
        Console.WriteLine("\n2. Tag-Based Invalidation Examples");
        Console.WriteLine("----------------------------------");

        var invalidationService = services.GetRequiredService<ICacheInvalidationService>();
        var tagManager = services.GetRequiredService<ICacheTagManager>();
        var cache = services.GetRequiredService<IDistributedCache>();

        // Add tags to cache entries
        await tagManager.AddTagsAsync("profile:123", new[] { "user-data", "profile" });
        await tagManager.AddTagsAsync("profile:124", new[] { "user-data", "profile" });
        await tagManager.AddTagsAsync("settings:123", new[] { "user-data", "settings" });

        Console.WriteLine("Added tags to cache entries");

        // Cache some data with tags
        await cache.SetStringAsync("profile:123", JsonSerializer.Serialize(new { UserId = 123, Name = "John" }));
        await cache.SetStringAsync("profile:124", JsonSerializer.Serialize(new { UserId = 124, Name = "Jane" }));
        await cache.SetStringAsync("settings:123", JsonSerializer.Serialize(new { Theme = "Dark" }));

        // Invalidate by tag
        await invalidationService.InvalidateByTagAsync("profile");
        Console.WriteLine("✓ Invalidated all profile entries");

        // Get keys by tag
        var userDataKeys = await tagManager.GetKeysByTagAsync("user-data");
        Console.WriteLine($"✓ Found {userDataKeys.Count()} keys with 'user-data' tag");
    }

    private static async Task RunDependencyBasedInvalidationExample(ServiceProvider services)
    {
        Console.WriteLine("\n3. Dependency-Based Invalidation Examples");
        Console.WriteLine("-----------------------------------------");

        var invalidationService = services.GetRequiredService<ICacheInvalidationService>();
        var dependencyTracker = services.GetRequiredService<ICacheDependencyTracker>();
        var cache = services.GetRequiredService<IDistributedCache>();

        // Set up dependencies
        await dependencyTracker.AddDependencyAsync("user:123:profile", "user:123");
        await dependencyTracker.AddDependencyAsync("user:123:orders", "user:123");
        await dependencyTracker.AddDependencyAsync("user:123:preferences", "user:123");

        Console.WriteLine("Set up dependency relationships");

        // Cache dependent data
        await cache.SetStringAsync("user:123", JsonSerializer.Serialize(new { Id = 123, Name = "John" }));
        await cache.SetStringAsync("user:123:profile", JsonSerializer.Serialize(new { Bio = "Developer" }));
        await cache.SetStringAsync("user:123:orders", JsonSerializer.Serialize(new[] { 1, 2, 3 }));

        // Invalidate dependencies
        await invalidationService.InvalidateDependenciesAsync("user:123");
        Console.WriteLine("✓ Invalidated all dependencies of user:123");

        // Verify dependencies were tracked correctly
        var dependentKeys = await dependencyTracker.GetDependentKeysAsync("user:123");
        Console.WriteLine($"✓ Found {dependentKeys.Count()} dependent keys");
    }

    private static async Task RunCustomInvalidationRulesExample(ServiceProvider services)
    {
        Console.WriteLine("\n4. Custom Invalidation Rules Examples");
        Console.WriteLine("-------------------------------------");

        var invalidationService = services.GetRequiredService<ICacheInvalidationService>();

        // Time-based invalidation rule
        var timeBasedRule = new TimeBasedInvalidationRule(
            TimeSpan.FromMinutes(30),
            async context =>
            {
                // In a real scenario, you'd check the last modified time from database
                return DateTime.UtcNow.AddMinutes(-45); // Simulate old data
            });

        invalidationService.RegisterInvalidationRule(timeBasedRule);
        Console.WriteLine("✓ Registered time-based invalidation rule");

        // Conditional invalidation rule
        var conditionalRule = new ConditionalInvalidationRule(
            "UserProfileInvalidation",
            context => context.TriggerKey.StartsWith("user:") && context.TriggerType == "direct",
            async context =>
            {
                var userId = context.TriggerKey.Split(':')[1];
                return new[]
                {
                    $"user:{userId}:profile",
                    $"user:{userId}:avatar",
                    $"user:{userId}:permissions"
                };
            });

        invalidationService.RegisterInvalidationRule(conditionalRule);
        Console.WriteLine("✓ Registered conditional invalidation rule");

        // Trigger rule-based invalidation
        await invalidationService.InvalidateAsync("user:999");
        Console.WriteLine("✓ Triggered rule-based invalidation for user:999");
    }

    private static async Task RunEventDrivenInvalidationExample(ServiceProvider services)
    {
        Console.WriteLine("\n5. Event-Driven Invalidation Examples");
        Console.WriteLine("-------------------------------------");

        var invalidationService = services.GetRequiredService<ICacheInvalidationService>();

        // Mock event subscriber for demonstration
        var eventSubscriber = new MockEventSubscriber();
        var eventInvalidationService = new EventDrivenInvalidationService(
            invalidationService, 
            eventSubscriber);

        await eventInvalidationService.StartAsync();
        Console.WriteLine("✓ Started event-driven invalidation service");

        // Simulate events
        await eventSubscriber.PublishEventAsync(new EventMessage
        {
            EventType = "user.updated",
            Data = new UserUpdatedEvent 
            { 
                UserId = 123, 
                ChangedFields = new[] { "Name", "Email" },
                UpdatedAt = DateTime.UtcNow
            }
        });

        Console.WriteLine("✓ Published user.updated event");

        await eventSubscriber.PublishEventAsync(new EventMessage
        {
            EventType = "product.updated",
            Data = new ProductUpdatedEvent 
            { 
                ProductId = 456, 
                CategoryId = 10,
                ChangedFields = new[] { "Price" },
                UpdatedAt = DateTime.UtcNow
            }
        });

        Console.WriteLine("✓ Published product.updated event");

        await eventInvalidationService.StopAsync();
        Console.WriteLine("✓ Stopped event-driven invalidation service");
    }

    private static async Task RunHierarchicalInvalidationExample(ServiceProvider services)
    {
        Console.WriteLine("\n6. Hierarchical Invalidation Examples");
        Console.WriteLine("-------------------------------------");

        var invalidationService = services.GetRequiredService<ICacheInvalidationService>();
        var cache = services.GetRequiredService<IDistributedCache>();

        // Set up hierarchical cache data
        await cache.SetStringAsync("app:config", "Global config");
        await cache.SetStringAsync("app:config:database", "DB config");
        await cache.SetStringAsync("app:config:database:connection", "Connection string");
        await cache.SetStringAsync("app:config:logging", "Logging config");
        await cache.SetStringAsync("app:config:logging:level", "Debug");

        Console.WriteLine("✓ Set up hierarchical cache data");

        // Invalidate entire hierarchy
        await invalidationService.InvalidateHierarchyAsync("app:config");
        Console.WriteLine("✓ Invalidated entire app:config hierarchy");
    }

    private static async Task RunSmartCacheWarmingExample(ServiceProvider services)
    {
        Console.WriteLine("\n7. Smart Cache Warming Examples");
        Console.WriteLine("-------------------------------");

        var cache = services.GetRequiredService<IDistributedCache>();

        // Mock implementations for demonstration
        var accessTracker = new MockCacheAccessTracker();
        var warmingStrategy = new MockCacheWarmingStrategy(cache);

        var warmingService = new SmartCacheWarmingService(
            cache,
            accessTracker,
            warmingStrategy,
            Options.Create(new CacheWarmingOptions
            {
                WarmingInterval = TimeSpan.FromMinutes(5),
                TopKeysCount = 10
            }));

        // Simulate access patterns
        await accessTracker.RecordAccessAsync("popular:item1");
        await accessTracker.RecordAccessAsync("popular:item1");
        await accessTracker.RecordAccessAsync("popular:item2");
        await accessTracker.RecordAccessAsync("popular:item1"); // Most popular

        Console.WriteLine("✓ Simulated access patterns");

        // Get most frequent keys
        var frequentKeys = await accessTracker.GetMostFrequentKeysAsync(
            TimeSpan.FromHours(1), 5, CancellationToken.None);
        
        Console.WriteLine($"✓ Top frequent keys: {string.Join(", ", frequentKeys)}");

        // Simulate cache warming
        await warmingStrategy.WarmCacheAsync(frequentKeys, CancellationToken.None);
        Console.WriteLine("✓ Warmed cache with frequent keys");
    }

    private static async Task RunStatisticsExample(ServiceProvider services)
    {
        Console.WriteLine("\n8. Cache Invalidation Statistics");
        Console.WriteLine("--------------------------------");

        var invalidationService = services.GetRequiredService<ICacheInvalidationService>();

        // Generate some invalidation activity
        await invalidationService.InvalidateAsync("test:key1");
        await invalidationService.InvalidateAsync(new[] { "test:key2", "test:key3" });
        await invalidationService.InvalidateByPatternAsync("test:*");
        await invalidationService.InvalidateByTagAsync("test-tag");

        // Get statistics
        var statistics = await invalidationService.GetStatisticsAsync();
        Console.WriteLine($"✓ Total Invalidations: {statistics.TotalInvalidations}");
        Console.WriteLine($"✓ Pattern Invalidations: {statistics.PatternInvalidations}");
        Console.WriteLine($"✓ Tag Invalidations: {statistics.TagInvalidations}");
        Console.WriteLine($"✓ Dependency Invalidations: {statistics.DependencyInvalidations}");
        Console.WriteLine($"✓ Active Rules: {statistics.ActiveRules}");
        Console.WriteLine($"✓ Last Updated: {statistics.LastUpdated}");

        Console.WriteLine("\n✅ Cache invalidation strategies examples completed!");
    }
}