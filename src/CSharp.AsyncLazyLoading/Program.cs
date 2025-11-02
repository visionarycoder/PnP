using CSharp.AsyncLazyLoading;
using System.Collections.Concurrent;

namespace CSharp.AsyncLazyLoading;

class Program
{
    static async Task Main()
    {
        Console.WriteLine("=== Async Lazy Loading Demo ===\n");

        await DemoBasicAsyncLazy();
        await DemoAsyncLazyCancellable();
        await DemoAsyncLazyWithExpiration();
        await DemoAsyncMemoizer();
        await DemoRealWorldExamples();
    }

    static async Task DemoBasicAsyncLazy()
    {
        Console.WriteLine("--- Basic AsyncLazy Demo ---");
        
        var expensiveOperation = new AsyncLazy<string>(async () =>
        {
            Console.WriteLine("Executing expensive operation...");
            await Task.Delay(2000); // Simulate expensive work
            return "Expensive result";
        });

        Console.WriteLine($"IsValueCreated: {expensiveOperation.IsValueCreated}");

        // Multiple simultaneous calls - only one execution
        var task1 = expensiveOperation.Value;
        var task2 = expensiveOperation.Value;
        var task3 = expensiveOperation.Value;

        var results = await Task.WhenAll(task1, task2, task3);
        Console.WriteLine($"Result 1: {results[0]}");
        Console.WriteLine($"Result 2: {results[1]}");
        Console.WriteLine($"Result 3: {results[2]}");
        Console.WriteLine($"IsValueCreated: {expensiveOperation.IsValueCreated}\n");
    }

    static async Task DemoAsyncLazyCancellable()
    {
        Console.WriteLine("--- AsyncLazy with Cancellation Demo ---");

        var cancellableLazy = new AsyncLazyCancellable<string>(async token =>
        {
            Console.WriteLine("Starting cancellable operation...");
            
            for (int i = 0; i < 10; i++)
            {
                token.ThrowIfCancellationRequested();
                await Task.Delay(200, token);
                Console.WriteLine($"Progress: {(i + 1) * 10}%");
            }
            
            return "Completed successfully";
        });

        using var cts = new CancellationTokenSource();
        
        // Start the operation
        var task = cancellableLazy.GetValueAsync(cts.Token);
        
        // Cancel after 1 second
        await Task.Delay(1000);
        cts.Cancel();
        
        try
        {
            var result = await task;
            Console.WriteLine($"Result: {result}");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("Operation was cancelled");
        }

        // Reset and try again
        cancellableLazy.Reset();
        try
        {
            var result = await cancellableLazy.GetValueAsync();
            Console.WriteLine($"Second attempt result: {result}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Second attempt failed: {ex.Message}");
        }
        Console.WriteLine();
    }

    static async Task DemoAsyncLazyWithExpiration()
    {
        Console.WriteLine("--- AsyncLazy with Expiration Demo ---");

        var expiringLazy = new AsyncLazyWithExpiration<DateTime>(
            async () =>
            {
                Console.WriteLine("Loading timestamp...");
                await Task.Delay(500);
                return DateTime.Now;
            },
            TimeSpan.FromSeconds(2)); // Expires after 2 seconds

        // First call
        var timestamp1 = await expiringLazy.GetValueAsync();
        Console.WriteLine($"First timestamp: {timestamp1:HH:mm:ss.fff}");
        Console.WriteLine($"IsValueCreated: {expiringLazy.IsValueCreated}");

        // Second call (should use cached value)
        var timestamp2 = await expiringLazy.GetValueAsync();
        Console.WriteLine($"Second timestamp: {timestamp2:HH:mm:ss.fff}");

        // Wait for expiration
        Console.WriteLine("Waiting for expiration...");
        await Task.Delay(3000);
        Console.WriteLine($"IsExpired: {expiringLazy.IsExpired}");

        // Third call (should reload)
        var timestamp3 = await expiringLazy.GetValueAsync();
        Console.WriteLine($"Third timestamp: {timestamp3:HH:mm:ss.fff}");
        Console.WriteLine();
    }

    static async Task DemoAsyncMemoizer()
    {
        Console.WriteLine("--- AsyncMemoizer Demo ---");

        var memoizer = new AsyncMemoizer<int, string>(async key =>
        {
            Console.WriteLine($"Computing result for key: {key}");
            await Task.Delay(1000); // Simulate expensive computation
            return $"Result for {key}";
        });

        // Multiple calls with same key - only computed once
        var tasks = new[]
        {
            memoizer.GetAsync(1),
            memoizer.GetAsync(2),
            memoizer.GetAsync(1), // Cached
            memoizer.GetAsync(3),
            memoizer.GetAsync(2)  // Cached
        };

        var results = await Task.WhenAll(tasks);
        foreach (var result in results)
        {
            Console.WriteLine(result);
        }

        Console.WriteLine($"Cache size: {memoizer.CacheSize}");

        // Invalidate and retry
        memoizer.Invalidate(1);
        var newResult = await memoizer.GetAsync(1);
        Console.WriteLine($"After invalidation: {newResult}");
        Console.WriteLine();
    }

    static async Task DemoRealWorldExamples()
    {
        Console.WriteLine("--- Real-World Examples ---");

        // Configuration service
        var configService = new ConfigurationService("app.config");
        var config = await configService.GetConfigurationAsync();
        Console.WriteLine($"Database: {config.DatabaseConnectionString}");
        Console.WriteLine($"Max users: {config.MaxConcurrentUsers}");

        // Cache service
        var cacheService = new CacheService<string, UserData>(async userId =>
        {
            Console.WriteLine($"Loading user data for: {userId}");
            await Task.Delay(800);
            return new UserData(userId, $"User {userId}", $"{userId}@example.com");
        });

        var user1 = await cacheService.GetAsync("user123");
        var user2 = await cacheService.GetAsync("user456");
        var user1Again = await cacheService.GetAsync("user123"); // Cached

        Console.WriteLine($"User 1: {user1.Name} ({user1.Email})");
        Console.WriteLine($"User 2: {user2.Name} ({user2.Email})");
        Console.WriteLine($"Cache size: {cacheService.CacheSize}");
    }
}

// Supporting classes and interfaces
public class AppConfig
{
    public string DatabaseConnectionString { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public int MaxConcurrentUsers { get; set; }
    public bool EnableFeatureX { get; set; }
}

public interface IDbConnection
{
    string ConnectionString { get; }
    Task<bool> TestConnectionAsync();
}

public class DatabaseConnection(string connectionString) : IDbConnection
{
    public string ConnectionString { get; } = connectionString;

    public async Task<bool> TestConnectionAsync()
    {
        await Task.Delay(100);
        return true;
    }
}

public record UserData(string Id, string Name, string Email);

public record ResourceItem(string Id, string Name, string Type, long Size);

public class ConfigurationService(string configSource)
{
    private readonly string configSource = configSource;
    private readonly AsyncLazyWithExpiration<AppConfig> configLazy = new(
        () => LoadConfigurationAsync(configSource),
        TimeSpan.FromMinutes(5)); // Refresh config every 5 minutes

    public Task<AppConfig> GetConfigurationAsync() => configLazy.GetValueAsync();

    private static async Task<AppConfig> LoadConfigurationAsync(string source)
    {
        Console.WriteLine($"Loading configuration from {source}...");
        
        // Simulate expensive config loading
        await Task.Delay(500);
        
        return new AppConfig
        {
            DatabaseConnectionString = "Server=localhost;Database=MyApp",
            ApiKey = "secret-api-key",
            MaxConcurrentUsers = 1000,
            EnableFeatureX = true
        };
    }
}

public class AsyncMemoizer<TKey, TValue>(Func<TKey, Task<TValue>> asyncFunc) where TKey : notnull
{
    private readonly Func<TKey, Task<TValue>> asyncFunc = asyncFunc ?? throw new ArgumentNullException(nameof(asyncFunc));
    private readonly ConcurrentDictionary<TKey, AsyncLazy<TValue>> cache = new();

    public Task<TValue> GetAsync(TKey key)
    {
        var lazy = cache.GetOrAdd(key, k => new AsyncLazy<TValue>(() => asyncFunc(k)));
        return lazy.Value;
    }

    public void Invalidate(TKey key)
    {
        cache.TryRemove(key, out _);
    }

    public void Clear()
    {
        cache.Clear();
    }

    public int CacheSize => cache.Count;
}

public class CacheService<TKey, TValue> where TKey : notnull
{
    private readonly AsyncMemoizer<TKey, TValue> memoizer;

    public CacheService(Func<TKey, Task<TValue>> valueFactory)
    {
        memoizer = new AsyncMemoizer<TKey, TValue>(valueFactory);
    }

    public Task<TValue> GetAsync(TKey key) => memoizer.GetAsync(key);
    
    public void Invalidate(TKey key) => memoizer.Invalidate(key);
    
    public void Clear() => memoizer.Clear();
    
    public int CacheSize => memoizer.CacheSize;
}