namespace CSharp.CacheInvalidation;

// Conditional invalidation rules
public class ConditionalInvalidationRule : ICacheInvalidationRule
{
    public string Name { get; }
    private readonly Func<CacheInvalidationContext, bool> condition;
    private readonly Func<CacheInvalidationContext, Task<IEnumerable<string>>> keySelector;

    public ConditionalInvalidationRule(
        string name,
        Func<CacheInvalidationContext, bool> condition,
        Func<CacheInvalidationContext, Task<IEnumerable<string>>> keySelector)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        this.condition = condition ?? throw new ArgumentNullException(nameof(condition));
        this.keySelector = keySelector ?? throw new ArgumentNullException(nameof(keySelector));
    }

    public bool ShouldInvalidate(CacheInvalidationContext context)
    {
        return condition(context);
    }

    public Task<IEnumerable<string>> GetKeysToInvalidateAsync(CacheInvalidationContext context, 
        CancellationToken token = default)
    {
        return keySelector(context);
    }
}

public class TimeBasedInvalidationRule : ICacheInvalidationRule
{
    public string Name => "TimeBasedInvalidation";
    private readonly TimeSpan maxAge;
    private readonly Func<CacheInvalidationContext, Task<DateTime?>> lastModifiedSelector;

    public TimeBasedInvalidationRule(
        TimeSpan maxAge,
        Func<CacheInvalidationContext, Task<DateTime?>> lastModifiedSelector)
    {
        this.maxAge = maxAge;
        this.lastModifiedSelector = lastModifiedSelector ?? throw new ArgumentNullException(nameof(lastModifiedSelector));
    }

    public bool ShouldInvalidate(CacheInvalidationContext context)
    {
        var lastModified = lastModifiedSelector(context).GetAwaiter().GetResult();
        
        if (!lastModified.HasValue)
            return false;
            
        return DateTime.UtcNow - lastModified.Value > maxAge;
    }

    public async Task<IEnumerable<string>> GetKeysToInvalidateAsync(CacheInvalidationContext context, 
        CancellationToken token = default)
    {
        if (ShouldInvalidate(context))
        {
            // Return the context trigger key itself
            return new[] { context.TriggerKey };
        }
        
        return Enumerable.Empty<string>();
    }
}

// Event system interfaces
public interface IEventSubscriber : IDisposable
{
    Task SubscribeAsync(Func<EventMessage, Task> handler, CancellationToken token = default);
    Task UnsubscribeAsync(CancellationToken token = default);
}

public class EventMessage
{
    public string EventType { get; set; } = string.Empty;
    public object Data { get; set; } = new();
    public DateTime Timestamp { get; set; }
    public string CorrelationId { get; set; } = string.Empty;
}

// Event data classes
public class UserUpdatedEvent
{
    public int UserId { get; set; }
    public string[] ChangedFields { get; set; } = Array.Empty<string>();
    public DateTime UpdatedAt { get; set; }
}

public class ProductUpdatedEvent
{
    public int ProductId { get; set; }
    public int CategoryId { get; set; }
    public string[] ChangedFields { get; set; } = Array.Empty<string>();
    public DateTime UpdatedAt { get; set; }
}

public class OrderCreatedEvent
{
    public int OrderId { get; set; }
    public int UserId { get; set; }
    public int[] ProductIds { get; set; } = Array.Empty<int>();
    public DateTime CreatedAt { get; set; }
}

public class ConfigChangedEvent
{
    public string ConfigKey { get; set; } = string.Empty;
    public object? OldValue { get; set; }
    public object? NewValue { get; set; }
    public DateTime ChangedAt { get; set; }
}

// Cache invalidation events and statistics
public class CacheInvalidationEvent
{
    public string[] Keys { get; set; } = Array.Empty<string>();
    public CacheInvalidationType InvalidationType { get; set; }
    public DateTime Timestamp { get; set; }
    public CacheInvalidationContext? Context { get; set; }
    public string? Pattern { get; set; }
    public string[]? Tags { get; set; }
    public string? DependencyKey { get; set; }
    public string? HierarchyKey { get; set; }
}

public enum CacheInvalidationType
{
    Direct,
    Bulk,
    Pattern,
    Tag,
    Dependency,
    Hierarchy,
    TimeBased,
    EventDriven
}

public interface ICacheInvalidationStatistics
{
    long TotalInvalidations { get; }
    long PatternInvalidations { get; }
    long TagInvalidations { get; }
    long DependencyInvalidations { get; }
    int ActiveRules { get; }
    DateTime LastUpdated { get; }
}

public class CacheInvalidationStatistics : ICacheInvalidationStatistics
{
    public long TotalInvalidations { get; set; }
    public long PatternInvalidations { get; set; }
    public long TagInvalidations { get; set; }
    public long DependencyInvalidations { get; set; }
    public int ActiveRules { get; set; }
    public DateTime LastUpdated { get; set; }
}

// Configuration classes
public class CacheInvalidationOptions
{
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(30);
    public int EventBufferSize { get; set; } = 100;
    public int EventBufferSeconds { get; set; } = 10;
}

public class CacheWarmingOptions
{
    public TimeSpan WarmingInterval { get; set; } = TimeSpan.FromMinutes(15);
    public TimeSpan AnalysisPeriod { get; set; } = TimeSpan.FromHours(1);
    public int TopKeysCount { get; set; } = 100;
}

// Cache access tracking and warming strategy interfaces
public interface ICacheAccessTracker
{
    Task RecordAccessAsync(string key, CancellationToken token = default);
    Task<IEnumerable<string>> GetMostFrequentKeysAsync(TimeSpan period, int count, 
        CancellationToken token = default);
}

public interface ICacheWarmingStrategy
{
    Task WarmCacheAsync(IEnumerable<string> keys, CancellationToken token = default);
}

public interface ICacheExpirationTracker
{
    Task<IEnumerable<string>> GetExpiredKeysAsync(CancellationToken token = default);
}

// Mock implementations for examples
public class MockEventSubscriber : IEventSubscriber
{
    private Func<EventMessage, Task>? handler;

    public Task SubscribeAsync(Func<EventMessage, Task> handler, CancellationToken token = default)
    {
        this.handler = handler;
        return Task.CompletedTask;
    }

    public async Task PublishEventAsync(EventMessage eventMessage)
    {
        if (handler != null)
        {
            await handler(eventMessage);
        }
    }

    public Task UnsubscribeAsync(CancellationToken token = default)
    {
        handler = null;
        return Task.CompletedTask;
    }

    public void Dispose() { }
}

public class MockCacheAccessTracker : ICacheAccessTracker
{
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, int> accessCounts = new();

    public Task RecordAccessAsync(string key, CancellationToken token = default)
    {
        accessCounts.AddOrUpdate(key, 1, (k, count) => count + 1);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetMostFrequentKeysAsync(TimeSpan period, int count, 
        CancellationToken token = default)
    {
        var topKeys = accessCounts
            .OrderByDescending(kvp => kvp.Value)
            .Take(count)
            .Select(kvp => kvp.Key);
            
        return Task.FromResult(topKeys);
    }
}

public class MockCacheWarmingStrategy : ICacheWarmingStrategy
{
    private readonly Microsoft.Extensions.Caching.Distributed.IDistributedCache cache;

    public MockCacheWarmingStrategy(Microsoft.Extensions.Caching.Distributed.IDistributedCache cache)
    {
        this.cache = cache;
    }

    public async Task WarmCacheAsync(IEnumerable<string> keys, CancellationToken token = default)
    {
        foreach (var key in keys)
        {
            // Simulate loading data and caching it
            var data = $"Warmed data for {key}";
            await cache.SetStringAsync(key, data, token);
        }
    }
}

public class MockCacheExpirationTracker : ICacheExpirationTracker
{
    public Task<IEnumerable<string>> GetExpiredKeysAsync(CancellationToken token = default)
    {
        // Simulate finding expired keys
        var expiredKeys = new[] { "expired:key1", "expired:key2" };
        return Task.FromResult<IEnumerable<string>>(expiredKeys);
    }
}