using Microsoft.Extensions.Caching.Distributed;
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

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
}

public class UserKeyGenerator : IKeyGenerator<int>
{
    public string GenerateKey(int key) => $"user:{key}";
}

public class UserService
{
    public async Task<User> GetUserAsync(int id)
    {
        // Simulate database call delay
        await Task.Delay(10);
        
        return new User
        {
            Id = id,
            Name = $"User {id}",
            Email = $"user{id}@example.com"
        };
    }
}

public class SimulatedDataStore<TKey, TValue> : IDataStore<TKey, TValue>
{
    private readonly Dictionary<TKey, TValue> store = new();

    public Task<TValue> GetAsync(TKey key, CancellationToken token = default)
    {
        store.TryGetValue(key, out var value);
        return Task.FromResult(value!);
    }

    public Task SetAsync(TKey key, TValue value, CancellationToken token = default)
    {
        store[key] = value;
        return Task.CompletedTask;
    }

    public Task RemoveAsync(TKey key, CancellationToken token = default)
    {
        store.Remove(key);
        return Task.CompletedTask;
    }
}

// Simulated advanced distributed cache for demo purposes
public class SimulatedDistributedCache : IAdvancedDistributedCache
{
    private readonly Dictionary<string, object> cache = new();
    private readonly Dictionary<string, DateTime> expiration = new();
    private long hitCount = 0;
    private long missCount = 0;

    public Task<T> GetAsync<T>(string key, CancellationToken token = default)
    {
        if (IsExpired(key))
        {
            cache.Remove(key);
            expiration.Remove(key);
        }

        if (cache.TryGetValue(key, out var value))
        {
            Interlocked.Increment(ref hitCount);
            if (value is CachedItem<T> cachedItem)
            {
                return Task.FromResult(cachedItem.Value);
            }
            if (value is T directValue)
            {
                return Task.FromResult(directValue);
            }
        }

        Interlocked.Increment(ref missCount);
        return Task.FromResult(default(T)!);
    }

    public Task SetAsync<T>(string key, T value, DistributedCacheEntryOptions? options = null,
        CancellationToken token = default)
    {
        cache[key] = value!;
        
        if (options?.AbsoluteExpirationRelativeToNow.HasValue == true)
        {
            expiration[key] = DateTime.UtcNow.Add(options.AbsoluteExpirationRelativeToNow.Value);
        }
        
        return Task.CompletedTask;
    }

    public async Task<(bool found, T value)> TryGetAsync<T>(string key, CancellationToken token = default)
    {
        var value = await GetAsync<T>(key, token);
        return (!EqualityComparer<T>.Default.Equals(value, default(T)), value);
    }

    public async Task<IDictionary<string, T>> GetManyAsync<T>(IEnumerable<string> keys,
        CancellationToken token = default)
    {
        var result = new Dictionary<string, T>();
        
        foreach (var key in keys)
        {
            var value = await GetAsync<T>(key, token);
            if (!EqualityComparer<T>.Default.Equals(value, default(T)))
            {
                result[key] = value;
            }
        }
        
        return result;
    }

    public async Task SetManyAsync<T>(IDictionary<string, T> items,
        DistributedCacheEntryOptions? options = null, CancellationToken token = default)
    {
        foreach (var item in items)
        {
            await SetAsync(item.Key, item.Value, options, token);
        }
    }

    public Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken token = default)
    {
        foreach (var key in keys)
        {
            cache.Remove(key);
            expiration.Remove(key);
        }
        return Task.CompletedTask;
    }

    public Task RemoveByPatternAsync(string pattern, CancellationToken token = default)
    {
        var keysToRemove = cache.Keys.Where(k => k.Contains(pattern.Replace("*", ""))).ToList();
        foreach (var key in keysToRemove)
        {
            cache.Remove(key);
            expiration.Remove(key);
        }
        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken token = default)
    {
        return Task.FromResult(cache.ContainsKey(key) && !IsExpired(key));
    }

    public Task<TimeSpan?> GetTtlAsync(string key, CancellationToken token = default)
    {
        if (expiration.TryGetValue(key, out var exp))
        {
            var ttl = exp - DateTime.UtcNow;
            return Task.FromResult<TimeSpan?>(ttl.TotalSeconds > 0 ? ttl : null);
        }
        return Task.FromResult<TimeSpan?>(null);
    }

    public Task<long> IncrementAsync(string key, long value = 1, CancellationToken token = default)
    {
        if (cache.TryGetValue(key, out var existing) && existing is long currentValue)
        {
            var newValue = currentValue + value;
            cache[key] = newValue;
            return Task.FromResult(newValue);
        }
        
        cache[key] = value;
        return Task.FromResult(value);
    }

    public Task<double> IncrementAsync(string key, double value, CancellationToken token = default)
    {
        if (cache.TryGetValue(key, out var existing) && existing is double currentValue)
        {
            var newValue = currentValue + value;
            cache[key] = newValue;
            return Task.FromResult(newValue);
        }
        
        cache[key] = value;
        return Task.FromResult(value);
    }

    public Task<ICacheStatistics> GetStatisticsAsync(CancellationToken token = default)
    {
        var total = hitCount + missCount;
        var hitRate = total > 0 ? (double)hitCount / total : 0;
        
        return Task.FromResult<ICacheStatistics>(new CacheStatistics
        {
            HitCount = hitCount,
            MissCount = missCount,
            HitRate = hitRate,
            KeyCount = cache.Count,
            UsedMemory = cache.Count * 100, // Simulated
            MaxMemory = 1000000 // Simulated
        });
    }

    public Task InvalidateTagAsync(string tag, CancellationToken token = default)
    {
        // Simulated tag-based invalidation
        return Task.CompletedTask;
    }

    // IDistributedCache implementation
    public byte[]? Get(string key) => GetAsync(key).GetAwaiter().GetResult();
    public Task<byte[]?> GetAsync(string key, CancellationToken token = default) => GetAsync<byte[]?>(key, token)!;
    public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => SetAsync(key, value, options).GetAwaiter().GetResult();
    public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => SetAsync<byte[]>(key, value, options, token);
    public void Refresh(string key) { }
    public Task RefreshAsync(string key, CancellationToken token = default) => Task.CompletedTask;
    public void Remove(string key) => RemoveAsync(key).GetAwaiter().GetResult();
    public Task RemoveAsync(string key, CancellationToken token = default) { cache.Remove(key); expiration.Remove(key); return Task.CompletedTask; }

    private bool IsExpired(string key)
    {
        return expiration.TryGetValue(key, out var exp) && DateTime.UtcNow > exp;
    }
}