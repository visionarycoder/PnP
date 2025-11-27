using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Diagnostics;

namespace CSharp.DistributedCache;

/// <summary>
/// Demonstrates comprehensive distributed caching patterns and strategies
/// </summary>
public class DistributedCacheDemo
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("=== Distributed Cache Patterns Demo ===\n");

        // For demo purposes, we'll simulate Redis with in-memory cache
        await DemoInMemoryDistributedCache();
        Console.WriteLine();

        await DemoCacheAsidePattern();
        Console.WriteLine();

        await DemoWriteThroughPattern();
        Console.WriteLine();

        await DemoPerformanceComparison();
        Console.WriteLine();

        await DemoCacheWarmup();
        Console.WriteLine();

        Console.WriteLine("Demo completed. In a real application, you would:");
        Console.WriteLine("- Use actual Redis connection for production");
        Console.WriteLine("- Configure proper connection pooling");
        Console.WriteLine("- Set up Redis cluster for high availability");
        Console.WriteLine("- Implement monitoring and alerting");
        Console.WriteLine("- Use distributed locking for cache invalidation");
    }

    private static async Task DemoInMemoryDistributedCache()
    {
        Console.WriteLine("--- In-Memory Distributed Cache Demo ---");

        // Create a simulated advanced distributed cache
        var cache = new SimulatedDistributedCache();

        // Basic operations
        await cache.SetAsync("user:123", new User { Id = 123, Name = "John Doe", Email = "john@example.com" });
        var user = await cache.GetAsync<User>("user:123");
        
        Console.WriteLine($"Retrieved user: {user?.Name} ({user?.Email})");

        // Batch operations
        var users = new Dictionary<string, User>
        {
            ["user:124"] = new() { Id = 124, Name = "Jane Smith", Email = "jane@example.com" },
            ["user:125"] = new() { Id = 125, Name = "Bob Johnson", Email = "bob@example.com" }
        };

        await cache.SetManyAsync(users);
        var retrievedUsers = await cache.GetManyAsync<User>(users.Keys);
        
        Console.WriteLine($"Batch retrieved {retrievedUsers.Count} users");

        // Atomic operations
        await cache.SetAsync("counter", 0);
        var counter1 = await cache.IncrementAsync("counter", 5);
        var counter2 = await cache.IncrementAsync("counter", 3);
        
        Console.WriteLine($"Counter after increments: {counter2}");

        // Statistics
        var stats = await cache.GetStatisticsAsync();
        Console.WriteLine($"Cache statistics - Hits: {stats.HitCount}, Misses: {stats.MissCount}, Hit Rate: {stats.HitRate:P}");
    }

    private static async Task DemoCacheAsidePattern()
    {
        Console.WriteLine("--- Cache-Aside Pattern Demo ---");

        var cache = new SimulatedDistributedCache();
        var userService = new UserService(); // Simulated data source
        var cacheAsideService = new CacheAsideService<int, User>(
            cache, 
            new UserKeyGenerator(),
            logger: null);

        Console.WriteLine("First access (cache miss, will fetch from data source):");
        var sw = Stopwatch.StartNew();
        var user1 = await cacheAsideService.GetAsync(123, userService.GetUserAsync);
        sw.Stop();
        
        Console.WriteLine($"User: {user1.Name}, Fetch time: {sw.ElapsedMilliseconds}ms");

        Console.WriteLine("\nSecond access (cache hit, faster):");
        sw.Restart();
        var user2 = await cacheAsideService.GetAsync(123, userService.GetUserAsync);
        sw.Stop();
        
        Console.WriteLine($"User: {user2.Name}, Fetch time: {sw.ElapsedMilliseconds}ms");

        // Explicit cache update
        user1.Name = "John Updated";
        await cacheAsideService.SetAsync(123, user1);
        
        var updatedUser = await cacheAsideService.GetAsync(123, userService.GetUserAsync);
        Console.WriteLine($"Updated user: {updatedUser.Name}");
    }

    private static async Task DemoWriteThroughPattern()
    {
        Console.WriteLine("--- Write-Through Pattern Demo ---");

        var cache = new SimulatedDistributedCache();
        var dataStore = new SimulatedDataStore<int, User>();
        
        var writeThroughService = new WriteThroughCacheService<int, User>(
            cache, 
            dataStore,
            new UserKeyGenerator());

        // Set data (writes to both cache and data store)
        var user = new User { Id = 456, Name = "Alice Wilson", Email = "alice@example.com" };
        await writeThroughService.SetAsync(456, user);
        Console.WriteLine($"Stored user via write-through: {user.Name}");

        // Get data (from cache if available, otherwise from data store and cache)
        var retrievedUser = await writeThroughService.GetAsync(456, _ => Task.FromResult(user));
        Console.WriteLine($"Retrieved user: {retrievedUser.Name}");

        // Verify data is in both cache and data store
        var cachedUser = await cache.GetAsync<User>("user:456");
        var storedUser = await dataStore.GetAsync(456);
        
        Console.WriteLine($"In cache: {cachedUser?.Name}");
        Console.WriteLine($"In data store: {storedUser?.Name}");
    }

    private static async Task DemoPerformanceComparison()
    {
        Console.WriteLine("--- Performance Comparison ---");

        var cache = new SimulatedDistributedCache();
        var userService = new UserService();
        var cacheAsideService = new CacheAsideService<int, User>(cache, new UserKeyGenerator());

        const int iterations = 1000;
        
        // Warm up cache
        await cacheAsideService.GetAsync(1, userService.GetUserAsync);

        // Test cache hits
        var sw = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            await cacheAsideService.GetAsync(1, userService.GetUserAsync);
        }
        sw.Stop();
        var cacheTime = sw.ElapsedMilliseconds;

        // Test direct data source access
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            await userService.GetUserAsync(1);
        }
        sw.Stop();
        var directTime = sw.ElapsedMilliseconds;

        Console.WriteLine($"Cache access ({iterations} ops): {cacheTime}ms");
        Console.WriteLine($"Direct access ({iterations} ops): {directTime}ms");
        Console.WriteLine($"Cache speedup: {(double)directTime / cacheTime:F1}x faster");
    }

    private static async Task DemoCacheWarmup()
    {
        Console.WriteLine("--- Cache Warmup Demo ---");

        var cache = new SimulatedDistributedCache();
        var userService = new UserService();
        var cacheAsideService = new CacheAsideService<int, User>(cache, new UserKeyGenerator());

        // Warm up cache with multiple users
        var userIds = Enumerable.Range(1, 10);
        
        Console.WriteLine("Warming up cache...");
        var sw = Stopwatch.StartNew();
        
        await cacheAsideService.WarmupAsync(userIds, userService.GetUserAsync);
        
        sw.Stop();
        Console.WriteLine($"Cache warmup completed in {sw.ElapsedMilliseconds}ms for 10 users");

        // Verify cache is warmed up
        Console.WriteLine("\nTesting warmed up cache:");
        for (int i = 1; i <= 5; i++)
        {
            sw.Restart();
            var user = await cacheAsideService.GetAsync(i, userService.GetUserAsync);
            sw.Stop();
            Console.WriteLine($"User {i}: {user.Name}, Access time: {sw.ElapsedMilliseconds}ms");
        }
    }
}

// Supporting classes for demo

// Simulated advanced distributed cache for demo purposes