using CSharp.CacheAside;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CSharp.CacheAside;

internal class Program
{
    private static async Task Main(string[] args)
    {
        // Setup DI container
        var services = new ServiceCollection();
        services.AddMemoryCache();
        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = "localhost:6379"; // Configure as needed
        });
        services.AddLogging(builder => builder.AddConsole());
        services.Configure<CacheAsideOptions>(options =>
        {
            options.Expiration = TimeSpan.FromMinutes(10);
            options.UseStaleWhileRevalidate = true;
            options.StaleThreshold = TimeSpan.FromMinutes(2);
            options.EnableStatistics = true;
        });
        services.AddScoped<ICacheAsideService<string, UserData>, MultiLevelCacheAsideService<string, UserData>>();

        var provider = services.BuildServiceProvider();
        
        Console.WriteLine("Cache-Aside Pattern Examples");
        Console.WriteLine("==========================");

        await RunBasicCacheExample(provider);
        await RunBatchOperationsExample(provider);
        await RunStaleWhileRevalidateExample(provider);
        await RunStatisticsExample(provider);
    }

    private static async Task RunBasicCacheExample(ServiceProvider provider)
    {
        Console.WriteLine("\n1. Basic Cache Operations");
        Console.WriteLine("------------------------");

        var cacheService = provider.GetRequiredService<ICacheAsideService<string, UserData>>();

        // Simulate data source
        var userDatabase = new Dictionary<string, UserData>
        {
            { "user1", new UserData("user1", "John Doe", "john@example.com") },
            { "user2", new UserData("user2", "Jane Smith", "jane@example.com") },
            { "user3", new UserData("user3", "Bob Johnson", "bob@example.com") }
        };

        // Value factory function
        Func<string, Task<UserData>> getUserFromDb = async userId =>
        {
            Console.WriteLine($"  Loading user {userId} from database...");
            await Task.Delay(100); // Simulate database latency
            
            if (userDatabase.TryGetValue(userId, out var user))
            {
                return user;
            }
            throw new KeyNotFoundException($"User {userId} not found");
        };

        // First call - cache miss, loads from database
        var user1 = await cacheService.GetAsync("user1", getUserFromDb);
        Console.WriteLine($"  Retrieved: {user1}");

        // Second call - cache hit, loads from cache
        var user1Cached = await cacheService.GetAsync("user1", getUserFromDb);
        Console.WriteLine($"  Retrieved from cache: {user1Cached}");

        // Manual cache operations
        await cacheService.SetAsync("user4", new UserData("user4", "Alice Wilson", "alice@example.com"));
        Console.WriteLine("  Manually cached user4");

        var exists = await cacheService.ExistsAsync("user4");
        Console.WriteLine($"  User4 exists in cache: {exists}");
    }

    private static async Task RunBatchOperationsExample(ServiceProvider provider)
    {
        Console.WriteLine("\n2. Batch Operations");
        Console.WriteLine("------------------");

        var cacheService = provider.GetRequiredService<ICacheAsideService<string, UserData>>();

        // Simulate batch data loading
        Func<IEnumerable<string>, Task<IDictionary<string, UserData>>> getBatchFromDb = async userIds =>
        {
            Console.WriteLine($"  Batch loading users: {string.Join(", ", userIds)}");
            await Task.Delay(200); // Simulate batch database query

            var results = new Dictionary<string, UserData>();
            foreach (var userId in userIds)
            {
                results[userId] = new UserData(userId, $"User {userId}", $"{userId}@example.com");
            }
            return results;
        };

        // Load multiple users
        var userIds = new[] { "batch1", "batch2", "batch3" };
        var users = await cacheService.GetManyAsync(userIds, getBatchFromDb);
        
        Console.WriteLine("  Batch loaded users:");
        foreach (var user in users)
        {
            Console.WriteLine($"    {user}");
        }

        // Second batch call - should hit cache
        var cachedUsers = await cacheService.GetManyAsync(userIds, getBatchFromDb);
        Console.WriteLine($"  Batch retrieved {cachedUsers.Count()} users from cache");
    }

    private static async Task RunStaleWhileRevalidateExample(ServiceProvider provider)
    {
        Console.WriteLine("\n3. Stale-While-Revalidate Pattern");
        Console.WriteLine("---------------------------------");

        var logger = provider.GetRequiredService<ILogger<Program>>();
        var memoryCache = provider.GetRequiredService<IMemoryCache>();
        var distributedCache = provider.GetRequiredService<IDistributedCache>();

        // Create cache service with short expiration
        var options = new CacheAsideOptions
        {
            Expiration = TimeSpan.FromSeconds(2),
            UseStaleWhileRevalidate = true,
            StaleThreshold = TimeSpan.FromSeconds(1),
            EnableStatistics = true
        };

        var cacheService = new MultiLevelCacheAsideService<string, UserData>(
            memoryCache, distributedCache, Microsoft.Extensions.Options.Options.Create(options), 
            logger.CreateLogger<MultiLevelCacheAsideService<string, UserData>>());

        var loadCount = 0;
        Func<string, Task<UserData>> slowValueFactory = async userId =>
        {
            loadCount++;
            Console.WriteLine($"  Loading user {userId} (call #{loadCount}) - slow operation...");
            await Task.Delay(1000); // Simulate slow data source
            return new UserData(userId, $"User {userId} - Updated", $"{userId}@updated.com");
        };

        // Initial load
        var user = await cacheService.GetAsync("stale-user", slowValueFactory);
        Console.WriteLine($"  Initial load: {user}");

        // Wait for entry to become stale but not expired
        await Task.Delay(1500);

        // This should return stale data immediately and trigger background refresh
        var staleUser = await cacheService.GetAsync("stale-user", slowValueFactory);
        Console.WriteLine($"  Stale data returned: {staleUser}");

        // Wait for background refresh to complete
        await Task.Delay(1500);

        // This should return fresh data from cache
        var freshUser = await cacheService.GetAsync("stale-user", slowValueFactory);
        Console.WriteLine($"  Fresh data: {freshUser}");
    }

    private static async Task RunStatisticsExample(ServiceProvider provider)
    {
        Console.WriteLine("\n4. Cache Statistics");
        Console.WriteLine("------------------");

        var cacheService = provider.GetRequiredService<ICacheAsideService<string, UserData>>();

        // Generate some cache activity
        Func<string, Task<UserData>> valueFactory = async userId =>
        {
            await Task.Delay(50);
            return new UserData(userId, $"Stats User {userId}", $"{userId}@stats.com");
        };

        // Mix of hits and misses
        for (int i = 0; i < 10; i++)
        {
            var userId = $"stats-user-{i % 3}"; // This will create cache hits
            await cacheService.GetAsync(userId, valueFactory);
        }

        // Get statistics
        var stats = await cacheService.GetStatisticsAsync();
        Console.WriteLine($"  Memory Hits: {stats.MemoryHits}");
        Console.WriteLine($"  Distributed Hits: {stats.DistributedHits}");
        Console.WriteLine($"  Total Hits: {stats.TotalHits}");
        Console.WriteLine($"  Misses: {stats.Misses}");
        Console.WriteLine($"  Hit Ratio: {stats.HitRatio:P2}");
        Console.WriteLine($"  Average Operation Time: {stats.AverageOperationTime.TotalMilliseconds:F2}ms");
    }
}