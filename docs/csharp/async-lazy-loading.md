# Async Lazy Loading

**Description**: Asynchronous lazy initialization patterns using AsyncLazy and custom implementations. Essential for expensive async operations that should only execute once and be awaitable by multiple consumers simultaneously.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Net.Http;

// Basic AsyncLazy implementation
public class AsyncLazy<T>
{
    private readonly Lazy<Task<T>> _lazy;

    public AsyncLazy(Func<Task<T>> taskFactory)
    {
        _lazy = new Lazy<Task<T>>(taskFactory);
    }

    public AsyncLazy(Func<T> valueFactory)
    {
        _lazy = new Lazy<Task<T>>(() => Task.FromResult(valueFactory()));
    }

    public Task<T> Value => _lazy.Value;
    
    public bool IsValueCreated => _lazy.IsValueCreated;

    public TaskAwaiter<T> GetAwaiter() => Value.GetAwaiter();
    
    public ConfiguredTaskAwaitable<T> ConfigureAwait(bool continueOnCapturedContext) =>
        Value.ConfigureAwait(continueOnCapturedContext);
}

// Thread-safe AsyncLazy with cancellation support
public class AsyncLazyCancellable<T>
{
    private readonly Func<CancellationToken, Task<T>> _taskFactory;
    private readonly object _lock = new object();
    private Task<T>? _cachedTask;

    public AsyncLazyCancellable(Func<CancellationToken, Task<T>> taskFactory)
    {
        _taskFactory = taskFactory ?? throw new ArgumentNullException(nameof(taskFactory));
    }

    public Task<T> GetValueAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (_cachedTask == null)
            {
                _cachedTask = _taskFactory(cancellationToken);
            }
            else if (_cachedTask.IsCanceled && !cancellationToken.IsCancellationRequested)
            {
                // Previous task was cancelled, but new request isn't - retry
                _cachedTask = _taskFactory(cancellationToken);
            }

            return _cachedTask;
        }
    }

    public bool IsValueCreated
    {
        get
        {
            lock (_lock)
            {
                return _cachedTask?.IsCompletedSuccessfully == true;
            }
        }
    }

    public void Reset()
    {
        lock (_lock)
        {
            _cachedTask = null;
        }
    }
}

// AsyncLazy with expiration
public class AsyncLazyWithExpiration<T>
{
    private readonly Func<Task<T>> _taskFactory;
    private readonly TimeSpan _expiration;
    private readonly object _lock = new object();
    private Task<T>? _cachedTask;
    private DateTime _creationTime;

    public AsyncLazyWithExpiration(Func<Task<T>> taskFactory, TimeSpan expiration)
    {
        _taskFactory = taskFactory ?? throw new ArgumentNullException(nameof(taskFactory));
        _expiration = expiration;
    }

    public Task<T> GetValueAsync()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;

            if (_cachedTask == null || 
                _cachedTask.IsFaulted || 
                now - _creationTime > _expiration)
            {
                _cachedTask = _taskFactory();
                _creationTime = now;
            }

            return _cachedTask;
        }
    }

    public bool IsExpired
    {
        get
        {
            lock (_lock)
            {
                return _cachedTask != null && DateTime.UtcNow - _creationTime > _expiration;
            }
        }
    }
}

// Async memoization utility
public class AsyncMemoizer<TKey, TValue> where TKey : notnull
{
    private readonly Func<TKey, Task<TValue>> _asyncFunc;
    private readonly ConcurrentDictionary<TKey, AsyncLazy<TValue>> _cache;

    public AsyncMemoizer(Func<TKey, Task<TValue>> asyncFunc)
    {
        _asyncFunc = asyncFunc ?? throw new ArgumentNullException(nameof(asyncFunc));
        _cache = new ConcurrentDictionary<TKey, AsyncLazy<TValue>>();
    }

    public Task<TValue> GetAsync(TKey key)
    {
        var lazy = _cache.GetOrAdd(key, k => new AsyncLazy<TValue>(() => _asyncFunc(k)));
        return lazy.Value;
    }

    public void Invalidate(TKey key)
    {
        _cache.TryRemove(key, out _);
    }

    public void Clear()
    {
        _cache.Clear();
    }

    public int CacheSize => _cache.Count;
}

// Async lazy factory with dependency injection support
public class AsyncLazyFactory<T>
{
    private readonly Func<IServiceProvider, Task<T>> _factory;
    private readonly AsyncLazy<T> _lazy;

    public AsyncLazyFactory(Func<IServiceProvider, Task<T>> factory, IServiceProvider serviceProvider)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _lazy = new AsyncLazy<T>(() => _factory(serviceProvider));
    }

    public Task<T> GetValueAsync() => _lazy.Value;
    
    public bool IsValueCreated => _lazy.IsValueCreated;
}

// Async lazy collection for batch operations
public class AsyncLazyCollection<T>
{
    private readonly Func<Task<T[]>> _batchLoader;
    private readonly AsyncLazy<T[]> _lazy;
    private readonly ConcurrentDictionary<int, AsyncLazy<T>> _itemCache;

    public AsyncLazyCollection(Func<Task<T[]>> batchLoader)
    {
        _batchLoader = batchLoader ?? throw new ArgumentNullException(nameof(batchLoader));
        _lazy = new AsyncLazy<T[]>(batchLoader);
        _itemCache = new ConcurrentDictionary<int, AsyncLazy<T>>();
    }

    public async Task<T[]> GetAllAsync()
    {
        return await _lazy.Value;
    }

    public Task<T> GetItemAsync(int index)
    {
        return _itemCache.GetOrAdd(index, i => new AsyncLazy<T>(async () =>
        {
            var items = await _lazy.Value;
            if (i < 0 || i >= items.Length)
                throw new ArgumentOutOfRangeException(nameof(index));
            return items[i];
        })).Value;
    }

    public async Task<int> GetCountAsync()
    {
        var items = await _lazy.Value;
        return items.Length;
    }
}

// Real-world examples
public class ConfigurationService
{
    private readonly AsyncLazyWithExpiration<AppConfig> _configLazy;
    private readonly string _configSource;

    public ConfigurationService(string configSource)
    {
        _configSource = configSource;
        _configLazy = new AsyncLazyWithExpiration<AppConfig>(
            LoadConfigurationAsync,
            TimeSpan.FromMinutes(5)); // Refresh config every 5 minutes
    }

    public Task<AppConfig> GetConfigurationAsync() => _configLazy.GetValueAsync();

    private async Task<AppConfig> LoadConfigurationAsync()
    {
        Console.WriteLine($"Loading configuration from {_configSource}...");
        
        // Simulate expensive config loading
        await Task.Delay(2000);
        
        return new AppConfig
        {
            DatabaseConnectionString = "Server=localhost;Database=MyApp",
            ApiKey = "secret-api-key",
            MaxConcurrentUsers = 1000,
            EnableFeatureX = true
        };
    }
}

public class DatabaseConnectionService
{
    private readonly AsyncLazyCancellable<IDbConnection> _connectionLazy;

    public DatabaseConnectionService(string connectionString)
    {
        _connectionLazy = new AsyncLazyCancellable<IDbConnection>(async cancellationToken =>
        {
            Console.WriteLine("Establishing database connection...");
            
            // Simulate connection establishment
            await Task.Delay(1000, cancellationToken);
            
            if (cancellationToken.IsCancellationRequested)
                throw new OperationCanceledException();
            
            return new DatabaseConnection(connectionString);
        });
    }

    public Task<IDbConnection> GetConnectionAsync(CancellationToken cancellationToken = default) =>
        _connectionLazy.GetValueAsync(cancellationToken);

    public void ResetConnection() => _connectionLazy.Reset();
}

public class CacheService<TKey, TValue> where TKey : notnull
{
    private readonly AsyncMemoizer<TKey, TValue> _memoizer;

    public CacheService(Func<TKey, Task<TValue>> valueFactory)
    {
        _memoizer = new AsyncMemoizer<TKey, TValue>(valueFactory);
    }

    public Task<TValue> GetAsync(TKey key) => _memoizer.GetAsync(key);
    
    public void Invalidate(TKey key) => _memoizer.Invalidate(key);
    
    public void Clear() => _memoizer.Clear();
    
    public int CacheSize => _memoizer.CacheSize;
}

public class ApiClientService
{
    private readonly AsyncMemoizer<string, string> _apiMemoizer;
    private readonly HttpClient _httpClient;

    public ApiClientService(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _apiMemoizer = new AsyncMemoizer<string, string>(FetchFromApiAsync);
    }

    public Task<string> GetDataAsync(string endpoint) => _apiMemoizer.GetAsync(endpoint);

    private async Task<string> FetchFromApiAsync(string endpoint)
    {
        Console.WriteLine($"Fetching data from API: {endpoint}");
        
        // Simulate API call
        await Task.Delay(500);
        var response = await _httpClient.GetStringAsync(endpoint);
        
        return response;
    }

    public void InvalidateCache(string endpoint) => _apiMemoizer.Invalidate(endpoint);
}

public class ResourceManagerService
{
    private readonly AsyncLazyCollection<ResourceItem> _resourcesLazy;

    public ResourceManagerService(Func<Task<ResourceItem[]>> resourceLoader)
    {
        _resourcesLazy = new AsyncLazyCollection<ResourceItem>(resourceLoader);
    }

    public Task<ResourceItem[]> GetAllResourcesAsync() => _resourcesLazy.GetAllAsync();
    
    public Task<ResourceItem> GetResourceAsync(int index) => _resourcesLazy.GetItemAsync(index);
    
    public Task<int> GetResourceCountAsync() => _resourcesLazy.GetCountAsync();
}

// Advanced pattern: Async lazy with refresh trigger
public class RefreshableAsyncLazy<T>
{
    private readonly Func<Task<T>> _factory;
    private readonly object _lock = new object();
    private AsyncLazy<T>? _currentLazy;
    private int _version;

    public RefreshableAsyncLazy(Func<Task<T>> factory)
    {
        _factory = factory ?? throw new ArgumentNullException(nameof(factory));
        _currentLazy = new AsyncLazy<T>(factory);
    }

    public Task<T> GetValueAsync()
    {
        lock (_lock)
        {
            return _currentLazy!.Value;
        }
    }

    public void Refresh()
    {
        lock (_lock)
        {
            _currentLazy = new AsyncLazy<T>(_factory);
            _version++;
        }
    }

    public int Version
    {
        get
        {
            lock (_lock)
            {
                return _version;
            }
        }
    }

    public bool IsValueCreated
    {
        get
        {
            lock (_lock)
            {
                return _currentLazy?.IsValueCreated == true;
            }
        }
    }
}

// Supporting data models and interfaces
public class AppConfig
{
    public string DatabaseConnectionString { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public int MaxConcurrentUsers { get; set; }
    public bool EnableFeatureX { get; set; }
}

public interface IDbConnection : IDisposable
{
    bool IsOpen { get; }
    Task OpenAsync();
    Task CloseAsync();
}

public class DatabaseConnection : IDbConnection
{
    private readonly string _connectionString;
    
    public DatabaseConnection(string connectionString)
    {
        _connectionString = connectionString;
        IsOpen = true; // Simulate open connection
    }
    
    public bool IsOpen { get; private set; }
    
    public Task OpenAsync()
    {
        IsOpen = true;
        return Task.CompletedTask;
    }
    
    public Task CloseAsync()
    {
        IsOpen = false;
        return Task.CompletedTask;
    }
    
    public void Dispose()
    {
        IsOpen = false;
    }
}

public class ResourceItem
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public long Size { get; set; }
}

// Extension methods for common scenarios
public static class AsyncLazyExtensions
{
    public static AsyncLazy<T> ToAsyncLazy<T>(this Task<T> task)
    {
        return new AsyncLazy<T>(() => task);
    }

    public static AsyncLazy<T> ToAsyncLazy<T>(this Func<Task<T>> taskFactory)
    {
        return new AsyncLazy<T>(taskFactory);
    }

    public static async Task<TResult> SelectAsync<T, TResult>(
        this AsyncLazy<T> asyncLazy,
        Func<T, TResult> selector)
    {
        var value = await asyncLazy.Value;
        return selector(value);
    }

    public static async Task<TResult> SelectAsync<T, TResult>(
        this AsyncLazy<T> asyncLazy,
        Func<T, Task<TResult>> selector)
    {
        var value = await asyncLazy.Value;
        return await selector(value);
    }
}
```

**Usage**:

```csharp
// Example 1: Basic AsyncLazy usage
var expensiveOperation = new AsyncLazy<string>(async () =>
{
    Console.WriteLine("Starting expensive operation...");
    await Task.Delay(2000); // Simulate expensive work
    return "Expensive result";
});

// Multiple calls will only execute the operation once
var result1 = await expensiveOperation;
var result2 = await expensiveOperation; // Uses cached result
Console.WriteLine($"Results: {result1}, {result2}");

// Example 2: Configuration service with expiration
var configService = new ConfigurationService("config.json");

// First call loads the configuration
var config1 = await configService.GetConfigurationAsync();
Console.WriteLine($"Config loaded: {config1.DatabaseConnectionString}");

// Subsequent calls within 5 minutes use cached config
var config2 = await configService.GetConfigurationAsync();
Console.WriteLine($"Config from cache: {config2.DatabaseConnectionString}");

// Example 3: Database connection with cancellation
var dbService = new DatabaseConnectionService("Server=localhost;Database=Test");

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
try
{
    var connection = await dbService.GetConnectionAsync(cts.Token);
    Console.WriteLine($"Connection established: {connection.IsOpen}");
}
catch (OperationCanceledException)
{
    Console.WriteLine("Connection establishment was cancelled");
}

// Example 4: API caching with memoization
using var httpClient = new HttpClient();
var apiService = new ApiClientService(httpClient);

// First call hits the API
var data1 = await apiService.GetDataAsync("https://api.example.com/users");
Console.WriteLine($"API data: {data1.Substring(0, Math.Min(50, data1.Length))}...");

// Second call uses cached result
var data2 = await apiService.GetDataAsync("https://api.example.com/users");
Console.WriteLine("Second call used cache");

// Invalidate cache and call again
apiService.InvalidateCache("https://api.example.com/users");
var data3 = await apiService.GetDataAsync("https://api.example.com/users");
Console.WriteLine("Third call hit API again after cache invalidation");

// Example 5: Resource collection lazy loading
var resourceLoader = async () =>
{
    Console.WriteLine("Loading resources...");
    await Task.Delay(1000);
    return new[]
    {
        new ResourceItem { Id = 1, Name = "Resource 1", Type = "Image", Size = 1024 },
        new ResourceItem { Id = 2, Name = "Resource 2", Type = "Document", Size = 2048 },
        new ResourceItem { Id = 3, Name = "Resource 3", Type = "Video", Size = 10240 }
    };
};

var resourceManager = new ResourceManagerService(resourceLoader);

// Get specific resource (loads all resources on first access)
var resource1 = await resourceManager.GetResourceAsync(0);
Console.WriteLine($"First resource: {resource1.Name} ({resource1.Size} bytes)");

// Get count (uses already loaded data)
var count = await resourceManager.GetResourceCountAsync();
Console.WriteLine($"Total resources: {count}");

// Get all resources (uses already loaded data)
var allResources = await resourceManager.GetAllResourcesAsync();
Console.WriteLine($"All resources loaded: {allResources.Length} items");

// Example 6: Refreshable lazy loading
var refreshableLazy = new RefreshableAsyncLazy<DateTime>(async () =>
{
    await Task.Delay(100);
    return DateTime.Now;
});

var time1 = await refreshableLazy.GetValueAsync();
Console.WriteLine($"First time: {time1}");

await Task.Delay(1000);

// Same cached value
var time2 = await refreshableLazy.GetValueAsync();
Console.WriteLine($"Cached time: {time2}");

// Refresh and get new value
refreshableLazy.Refresh();
var time3 = await refreshableLazy.GetValueAsync();
Console.WriteLine($"Refreshed time: {time3}");

// Example 7: Async lazy with LINQ-style operations
var numberLazy = new AsyncLazy<int>(async () =>
{
    await Task.Delay(500);
    return 42;
});

var doubledResult = await numberLazy.SelectAsync(x => x * 2);
Console.WriteLine($"Doubled result: {doubledResult}");

var stringResult = await numberLazy.SelectAsync(async x =>
{
    await Task.Delay(100);
    return $"The answer is {x}";
});
Console.WriteLine($"String result: {stringResult}");

// Example 8: Multiple concurrent access to same AsyncLazy
var sharedLazy = new AsyncLazy<string>(async () =>
{
    Console.WriteLine("Executing shared operation...");
    await Task.Delay(1000);
    return $"Shared result computed at {DateTime.Now}";
});

// Start multiple concurrent operations
var tasks = Enumerable.Range(1, 5)
    .Select(async i =>
    {
        Console.WriteLine($"Task {i} starting...");
        var result = await sharedLazy;
        Console.WriteLine($"Task {i} got: {result}");
        return result;
    })
    .ToArray();

await Task.WhenAll(tasks);
Console.WriteLine("All tasks completed - only one execution occurred");

// Example 9: Error handling and retry
var flakyLazy = new AsyncLazy<string>(async () =>
{
    var random = new Random();
    if (random.NextDouble() < 0.5) // 50% chance of failure
    {
        throw new Exception("Simulated failure");
    }
    
    await Task.Delay(500);
    return "Success!";
});

for (int attempt = 1; attempt <= 3; attempt++)
{
    try
    {
        var result = await flakyLazy;
        Console.WriteLine($"Attempt {attempt} succeeded: {result}");
        break;
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Attempt {attempt} failed: {ex.Message}");
        if (attempt < 3)
        {
            // Create new AsyncLazy for retry
            flakyLazy = new AsyncLazy<string>(async () =>
            {
                var random = new Random();
                if (random.NextDouble() < 0.3) // Reduce failure rate
                {
                    throw new Exception("Simulated failure");
                }
                
                await Task.Delay(500);
                return "Success!";
            });
        }
    }
}

// Example 10: Integration with dependency injection
IServiceProvider serviceProvider = null; // Would be injected

var serviceLazy = new AsyncLazyFactory<IMyService>(async provider =>
{
    // Simulate expensive service initialization
    await Task.Delay(1000);
    return new MyService();
}, serviceProvider);

if (serviceLazy.IsValueCreated)
{
    Console.WriteLine("Service was already created");
}
else
{
    var service = await serviceLazy.GetValueAsync();
    Console.WriteLine($"Service created: {service?.GetType().Name}");
}

// Supporting service interface and implementation
public interface IMyService
{
    Task<string> GetDataAsync();
}

public class MyService : IMyService
{
    public Task<string> GetDataAsync()
    {
        return Task.FromResult("Service data");
    }
}
```

**Notes**:

- AsyncLazy ensures expensive async operations execute only once, even with concurrent access
- Use cancellation-aware versions for operations that might need to be cancelled
- Expiration-based lazy loading is useful for configuration and cached data
- Memoization with AsyncMemoizer provides per-key caching for function results
- Always consider error handling - failed AsyncLazy instances cache the failure
- Thread safety is built into these implementations using appropriate synchronization
- Memory usage grows with cache size in memoization scenarios
- Consider implementing cache size limits and eviction policies for long-running applications

**Prerequisites**:

- .NET Framework 4.5+ or .NET Core for Task-based async programming
- Understanding of lazy initialization patterns and thread safety
- Knowledge of async/await and Task coordination
- Familiarity with concurrent collections and synchronization primitives

**Related Snippets**:

- [Task Combinators](task-combinators.md) - Advanced task coordination patterns
- [Async Enumerable](async-enumerable.md) - Streaming async operations
- [Memory Cache](memory-cache.md) - Caching strategies and patterns
