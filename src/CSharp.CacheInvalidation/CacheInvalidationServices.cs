using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using System.Collections.Concurrent;

namespace CSharp.CacheInvalidation;

// Time-based cache invalidation
public class TimeBasedInvalidationService : BackgroundService
{
    private readonly ICacheInvalidationService invalidationService;
    private readonly ICacheExpirationTracker expirationTracker;
    private readonly ILogger? logger;
    private readonly TimeSpan checkInterval;

    public TimeBasedInvalidationService(
        ICacheInvalidationService invalidationService,
        ICacheExpirationTracker expirationTracker,
        ILogger<TimeBasedInvalidationService>? logger = null,
        TimeSpan? checkInterval = null)
    {
        this.invalidationService = invalidationService ?? throw new ArgumentNullException(nameof(invalidationService));
        this.expirationTracker = expirationTracker ?? throw new ArgumentNullException(nameof(expirationTracker));
        this.logger = logger;
        this.checkInterval = checkInterval ?? TimeSpan.FromMinutes(1);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var expiredKeys = await expirationTracker.GetExpiredKeysAsync(stoppingToken)
                    .ConfigureAwait(false);
                
                if (expiredKeys.Any())
                {
                    await invalidationService.InvalidateAsync(expiredKeys, stoppingToken)
                        .ConfigureAwait(false);
                    
                    logger?.LogTrace("Time-based invalidation processed {Count} expired keys", 
                        expiredKeys.Count());
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error during time-based cache invalidation");
            }

            await Task.Delay(checkInterval, stoppingToken).ConfigureAwait(false);
        }
    }
}

// Event-driven cache invalidation
public class EventDrivenInvalidationService : IDisposable
{
    private readonly ICacheInvalidationService invalidationService;
    private readonly IEventSubscriber eventSubscriber;
    private readonly Dictionary<string, Func<object, Task>> eventHandlers;
    private readonly ILogger? logger;
    private bool disposed = false;

    public EventDrivenInvalidationService(
        ICacheInvalidationService invalidationService,
        IEventSubscriber eventSubscriber,
        ILogger<EventDrivenInvalidationService>? logger = null)
    {
        this.invalidationService = invalidationService ?? throw new ArgumentNullException(nameof(invalidationService));
        this.eventSubscriber = eventSubscriber ?? throw new ArgumentNullException(nameof(eventSubscriber));
        this.logger = logger;
        
        eventHandlers = new Dictionary<string, Func<object, Task>>();
        SetupEventHandlers();
    }

    public void RegisterEventHandler(string eventType, Func<object, Task> handler)
    {
        eventHandlers[eventType] = handler;
        logger?.LogInformation("Registered event handler for {EventType}", eventType);
    }

    private void SetupEventHandlers()
    {
        // User-related events
        RegisterEventHandler("user.updated", async eventData =>
        {
            if (eventData is UserUpdatedEvent userEvent)
            {
                await invalidationService.InvalidateByPatternAsync($"user:{userEvent.UserId}*")
                    .ConfigureAwait(false);
                await invalidationService.InvalidateByTagAsync("user-profiles")
                    .ConfigureAwait(false);
            }
        });

        // Product-related events  
        RegisterEventHandler("product.updated", async eventData =>
        {
            if (eventData is ProductUpdatedEvent productEvent)
            {
                await invalidationService.InvalidateAsync($"product:{productEvent.ProductId}")
                    .ConfigureAwait(false);
                await invalidationService.InvalidateByTagAsync($"category:{productEvent.CategoryId}")
                    .ConfigureAwait(false);
            }
        });

        // Order-related events
        RegisterEventHandler("order.created", async eventData =>
        {
            if (eventData is OrderCreatedEvent orderEvent)
            {
                await invalidationService.InvalidateByPatternAsync($"inventory:*")
                    .ConfigureAwait(false);
                await invalidationService.InvalidateAsync($"user:{orderEvent.UserId}:orders")
                    .ConfigureAwait(false);
            }
        });

        // Configuration changes
        RegisterEventHandler("config.changed", async eventData =>
        {
            if (eventData is ConfigChangedEvent configEvent)
            {
                await invalidationService.InvalidateByTagAsync("configuration")
                    .ConfigureAwait(false);
                await invalidationService.InvalidateByPatternAsync($"config:*")
                    .ConfigureAwait(false);
            }
        });
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await eventSubscriber.SubscribeAsync(HandleEventAsync, cancellationToken)
            .ConfigureAwait(false);
        
        logger?.LogInformation("Event-driven cache invalidation service started");
    }

    private async Task HandleEventAsync(EventMessage eventMessage)
    {
        try
        {
            if (eventHandlers.TryGetValue(eventMessage.EventType, out var handler))
            {
                await handler(eventMessage.Data).ConfigureAwait(false);
                logger?.LogTrace("Processed cache invalidation for event {EventType}", 
                    eventMessage.EventType);
            }
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Error handling cache invalidation event {EventType}", 
                eventMessage.EventType);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await eventSubscriber.UnsubscribeAsync(cancellationToken).ConfigureAwait(false);
        logger?.LogInformation("Event-driven cache invalidation service stopped");
    }

    public void Dispose()
    {
        if (!disposed)
        {
            eventSubscriber?.Dispose();
            disposed = true;
        }
    }
}

// Smart cache warming service
public class SmartCacheWarmingService : BackgroundService
{
    private readonly IDistributedCache cache;
    private readonly ICacheAccessTracker accessTracker;
    private readonly ICacheWarmingStrategy warmingStrategy;
    private readonly ILogger? logger;
    private readonly CacheWarmingOptions options;

    public SmartCacheWarmingService(
        IDistributedCache cache,
        ICacheAccessTracker accessTracker,
        ICacheWarmingStrategy warmingStrategy,
        Microsoft.Extensions.Options.IOptions<CacheWarmingOptions>? options = null,
        ILogger<SmartCacheWarmingService>? logger = null)
    {
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.accessTracker = accessTracker ?? throw new ArgumentNullException(nameof(accessTracker));
        this.warmingStrategy = warmingStrategy ?? throw new ArgumentNullException(nameof(warmingStrategy));
        this.options = options?.Value ?? new CacheWarmingOptions();
        this.logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Get frequently accessed but expired/missing keys
                var keysToWarm = await GetKeysRequiringWarmup(stoppingToken).ConfigureAwait(false);
                
                if (keysToWarm.Any())
                {
                    await warmingStrategy.WarmCacheAsync(keysToWarm, stoppingToken)
                        .ConfigureAwait(false);
                    
                    logger?.LogInformation("Cache warming completed for {Count} keys", 
                        keysToWarm.Count());
                }
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error during cache warming");
            }

            await Task.Delay(options.WarmingInterval, stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task<IEnumerable<string>> GetKeysRequiringWarmup(CancellationToken token)
    {
        // Get most frequently accessed keys in the last period
        var frequentKeys = await accessTracker.GetMostFrequentKeysAsync(
            options.AnalysisPeriod, 
            options.TopKeysCount, 
            token).ConfigureAwait(false);

        var keysToWarm = new List<string>();
        
        foreach (var key in frequentKeys)
        {
            // Check if key exists in cache
            var cachedValue = await cache.GetAsync(key, token).ConfigureAwait(false);
            
            if (cachedValue == null)
            {
                keysToWarm.Add(key);
            }
        }

        return keysToWarm;
    }
}

// Dependency tracker implementation
public interface ICacheDependencyTracker
{
    Task AddDependencyAsync(string key, string dependsOn, CancellationToken token = default);
    Task<IEnumerable<string>> GetDependentKeysAsync(string dependencyKey, CancellationToken token = default);
    Task RemoveDependencyAsync(string dependencyKey, CancellationToken token = default);
    Task RemoveDependenciesAsync(string key, CancellationToken token = default);
    Task CleanupExpiredDependenciesAsync(CancellationToken token = default);
}

public class CacheDependencyTracker : ICacheDependencyTracker
{
    private readonly ConcurrentDictionary<string, HashSet<string>> dependencies = new();
    private readonly ConcurrentDictionary<string, DateTime> dependencyTimestamps = new();

    public Task AddDependencyAsync(string key, string dependsOn, CancellationToken token = default)
    {
        dependencies.AddOrUpdate(dependsOn,
            new HashSet<string> { key },
            (k, existing) =>
            {
                existing.Add(key);
                return existing;
            });
        
        dependencyTimestamps[dependsOn] = DateTime.UtcNow;
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetDependentKeysAsync(string dependencyKey, CancellationToken token = default)
    {
        dependencies.TryGetValue(dependencyKey, out var dependentKeys);
        return Task.FromResult(dependentKeys?.AsEnumerable() ?? Enumerable.Empty<string>());
    }

    public Task RemoveDependencyAsync(string dependencyKey, CancellationToken token = default)
    {
        dependencies.TryRemove(dependencyKey, out _);
        dependencyTimestamps.TryRemove(dependencyKey, out _);
        return Task.CompletedTask;
    }

    public Task RemoveDependenciesAsync(string key, CancellationToken token = default)
    {
        var keysToRemove = new List<string>();
        
        foreach (var kvp in dependencies)
        {
            if (kvp.Value.Contains(key))
            {
                kvp.Value.Remove(key);
                if (kvp.Value.Count == 0)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }
        }
        
        foreach (var keyToRemove in keysToRemove)
        {
            dependencies.TryRemove(keyToRemove, out _);
            dependencyTimestamps.TryRemove(keyToRemove, out _);
        }
        
        return Task.CompletedTask;
    }

    public Task CleanupExpiredDependenciesAsync(CancellationToken token = default)
    {
        var cutoffTime = DateTime.UtcNow.AddHours(-24); // Remove dependencies older than 24 hours
        var expiredKeys = dependencyTimestamps
            .Where(kvp => kvp.Value < cutoffTime)
            .Select(kvp => kvp.Key)
            .ToList();

        foreach (var key in expiredKeys)
        {
            dependencies.TryRemove(key, out _);
            dependencyTimestamps.TryRemove(key, out _);
        }

        return Task.CompletedTask;
    }
}

// Tag manager implementation
public interface ICacheTagManager
{
    Task AddTagAsync(string key, string tag, CancellationToken token = default);
    Task AddTagsAsync(string key, IEnumerable<string> tags, CancellationToken token = default);
    Task<IEnumerable<string>> GetKeysByTagAsync(string tag, CancellationToken token = default);
    Task<IEnumerable<string>> GetTagsByKeyAsync(string key, CancellationToken token = default);
    Task RemoveTagAsync(string tag, CancellationToken token = default);
    Task RemoveTagsAsync(IEnumerable<string> tags, CancellationToken token = default);
    Task CleanupExpiredTagsAsync(CancellationToken token = default);
}

public class CacheTagManager : ICacheTagManager
{
    private readonly IDistributedCache cache;
    private readonly ILogger? logger;
    private readonly string tagPrefix = "tag:";
    private readonly string keyTagsPrefix = "key-tags:";

    public CacheTagManager(IDistributedCache cache, ILogger? logger = null)
    {
        this.cache = cache ?? throw new ArgumentNullException(nameof(cache));
        this.logger = logger;
    }

    public async Task AddTagAsync(string key, string tag, CancellationToken token = default)
    {
        await AddTagsAsync(key, new[] { tag }, token).ConfigureAwait(false);
    }

    public async Task AddTagsAsync(string key, IEnumerable<string> tags, CancellationToken token = default)
    {
        var tagList = tags.ToList();
        
        foreach (var tag in tagList)
        {
            // Add key to tag's key list
            var tagKey = $"{tagPrefix}{tag}";
            var existingKeys = await GetKeysFromTagStorage(tagKey, token).ConfigureAwait(false);
            existingKeys.Add(key);
            
            var serializedKeys = JsonSerializer.Serialize(existingKeys);
            await cache.SetStringAsync(tagKey, serializedKeys, token).ConfigureAwait(false);
        }
        
        // Add tags to key's tag list
        var keyTagsKey = $"{keyTagsPrefix}{key}";
        var existingTags = await GetTagsFromKeyStorage(keyTagsKey, token).ConfigureAwait(false);
        existingTags.UnionWith(tagList);
        
        var serializedTags = JsonSerializer.Serialize(existingTags);
        await cache.SetStringAsync(keyTagsKey, serializedTags, token).ConfigureAwait(false);
    }

    public async Task<IEnumerable<string>> GetKeysByTagAsync(string tag, CancellationToken token = default)
    {
        var tagKey = $"{tagPrefix}{tag}";
        return await GetKeysFromTagStorage(tagKey, token).ConfigureAwait(false);
    }

    public async Task<IEnumerable<string>> GetTagsByKeyAsync(string key, CancellationToken token = default)
    {
        var keyTagsKey = $"{keyTagsPrefix}{key}";
        return await GetTagsFromKeyStorage(keyTagsKey, token).ConfigureAwait(false);
    }

    public async Task RemoveTagAsync(string tag, CancellationToken token = default)
    {
        var tagKey = $"{tagPrefix}{tag}";
        await cache.RemoveAsync(tagKey, token).ConfigureAwait(false);
    }

    public async Task RemoveTagsAsync(IEnumerable<string> tags, CancellationToken token = default)
    {
        var tasks = tags.Select(tag => RemoveTagAsync(tag, token));
        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async Task CleanupExpiredTagsAsync(CancellationToken token = default)
    {
        // In a real implementation, you would need to:
        // 1. Get all tag keys
        // 2. Check if the keys they reference still exist
        // 3. Remove tags that reference non-existent keys
        await Task.CompletedTask.ConfigureAwait(false);
    }

    private async Task<HashSet<string>> GetKeysFromTagStorage(string tagKey, CancellationToken token)
    {
        var serializedKeys = await cache.GetStringAsync(tagKey, token).ConfigureAwait(false);
        
        if (string.IsNullOrEmpty(serializedKeys))
            return new HashSet<string>();
            
        try
        {
            var keys = JsonSerializer.Deserialize<string[]>(serializedKeys);
            return new HashSet<string>(keys ?? Array.Empty<string>());
        }
        catch
        {
            return new HashSet<string>();
        }
    }

    private async Task<HashSet<string>> GetTagsFromKeyStorage(string keyTagsKey, CancellationToken token)
    {
        var serializedTags = await cache.GetStringAsync(keyTagsKey, token).ConfigureAwait(false);
        
        if (string.IsNullOrEmpty(serializedTags))
            return new HashSet<string>();
            
        try
        {
            var tags = JsonSerializer.Deserialize<string[]>(serializedTags);
            return new HashSet<string>(tags ?? Array.Empty<string>());
        }
        catch
        {
            return new HashSet<string>();
        }
    }
}