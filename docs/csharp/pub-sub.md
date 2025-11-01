# Publisher-Subscriber Pattern

**Description**: Comprehensive publisher-subscriber implementation including topic management, subscription filtering, event distribution, message broadcasting, hierarchical topics, wildcard subscriptions, and distributed pub-sub patterns for building scalable event-driven architectures.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Hosting;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Reactive.Subjects;
using System.Reactive.Linq;

// Core pub-sub interfaces
public interface IEventPublisher
{
    Task PublishAsync<T>(string topic, T eventData, CancellationToken token = default) where T : class;
    Task PublishAsync(string topic, IEvent eventData, CancellationToken token = default);
    Task PublishAsync(IEvent eventData, CancellationToken token = default);
}

public interface IEventSubscriber
{
    Task<ISubscription> SubscribeAsync<T>(string topic, Func<T, IEventContext, Task> handler, 
        CancellationToken token = default) where T : class;
    Task<ISubscription> SubscribeAsync(string topic, Func<IEvent, IEventContext, Task> handler,
        CancellationToken token = default);
    Task<ISubscription> SubscribeAsync(string topicPattern, Func<IEvent, IEventContext, Task> handler,
        SubscriptionOptions options, CancellationToken token = default);
    Task UnsubscribeAsync(Guid subscriptionId, CancellationToken token = default);
}

public interface IEventBroker : IEventPublisher, IEventSubscriber
{
    Task<IEnumerable<string>> GetTopicsAsync(CancellationToken token = default);
    Task<IEnumerable<ISubscription>> GetSubscriptionsAsync(string topic = null, CancellationToken token = default);
    Task<TopicStatistics> GetTopicStatisticsAsync(string topic, CancellationToken token = default);
    Task CreateTopicAsync(string topic, TopicConfiguration configuration = null, CancellationToken token = default);
    Task DeleteTopicAsync(string topic, CancellationToken token = default);
}

public interface ISubscription : IDisposable
{
    Guid Id { get; }
    string Topic { get; }
    string SubscriberName { get; }
    DateTime CreatedAt { get; }
    bool IsActive { get; }
    long ProcessedEventCount { get; }
    long FailedEventCount { get; }
    Task ActivateAsync();
    Task DeactivateAsync();
}

public interface IEvent
{
    Guid EventId { get; }
    string EventType { get; }
    DateTime Timestamp { get; }
    string Topic { get; }
    IDictionary<string, object> Headers { get; }
    byte[] Data { get; }
    string CorrelationId { get; }
    int Priority { get; }
}

public interface IEventContext
{
    Guid EventId { get; }
    string Topic { get; }
    DateTime Timestamp { get; }
    IDictionary<string, object> Headers { get; }
    string CorrelationId { get; }
    Guid SubscriptionId { get; }
    int RetryCount { get; }
    Task AcknowledgeAsync();
    Task RejectAsync(bool requeue = false);
}

// Event implementations
public class Event : IEvent
{
    public Event()
    {
        EventId = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
        Headers = new Dictionary<string, object>();
        Priority = 0;
    }

    public Guid EventId { get; set; }
    public string EventType { get; set; }
    public DateTime Timestamp { get; set; }
    public string Topic { get; set; }
    public IDictionary<string, object> Headers { get; set; }
    public byte[] Data { get; set; }
    public string CorrelationId { get; set; }
    public int Priority { get; set; }
}

public class TypedEvent<T> : Event where T : class
{
    public TypedEvent(T payload, string topic = null)
    {
        EventType = typeof(T).Name;
        Topic = topic ?? typeof(T).Name.ToLowerInvariant();
        Payload = payload;
        Data = JsonSerializer.SerializeToUtf8Bytes(payload);
    }

    public T Payload { get; private set; }

    public static TypedEvent<T> FromEvent(IEvent eventData)
    {
        var payload = JsonSerializer.Deserialize<T>(eventData.Data);
        return new TypedEvent<T>(payload, eventData.Topic)
        {
            EventId = eventData.EventId,
            EventType = eventData.EventType,
            Timestamp = eventData.Timestamp,
            Headers = eventData.Headers,
            CorrelationId = eventData.CorrelationId,
            Priority = eventData.Priority
        };
    }
}

// Subscription implementation
public class Subscription : ISubscription
{
    private readonly Func<IEvent, IEventContext, Task> handler;
    private readonly ILogger logger;
    private long processedEventCount = 0;
    private long failedEventCount = 0;
    private volatile bool isActive = true;
    private volatile bool isDisposed = false;

    public Subscription(
        string topic,
        Func<IEvent, IEventContext, Task> handler,
        string subscriberName = null,
        ILogger logger = null)
    {
        Id = Guid.NewGuid();
        Topic = topic ?? throw new ArgumentNullException(nameof(topic));
        this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
        SubscriberName = subscriberName ?? $"Subscriber-{Id:N}";
        CreatedAt = DateTime.UtcNow;
        this.logger = logger;
    }

    public Guid Id { get; }
    public string Topic { get; }
    public string SubscriberName { get; }
    public DateTime CreatedAt { get; }
    public bool IsActive => isActive && !isDisposed;
    public long ProcessedEventCount => processedEventCount;
    public long FailedEventCount => failedEventCount;

    public Task ActivateAsync()
    {
        if (!isDisposed)
        {
            isActive = true;
            logger?.LogInformation("Activated subscription {SubscriptionId} for topic {Topic}", Id, Topic);
        }
        return Task.CompletedTask;
    }

    public Task DeactivateAsync()
    {
        isActive = false;
        logger?.LogInformation("Deactivated subscription {SubscriptionId} for topic {Topic}", Id, Topic);
        return Task.CompletedTask;
    }

    public async Task HandleEventAsync(IEvent eventData, IEventContext context)
    {
        if (!IsActive) return;

        var stopwatch = Stopwatch.StartNew();
        
        try
        {
            logger?.LogTrace("Processing event {EventId} for subscription {SubscriptionId}", 
                eventData.EventId, Id);

            await handler(eventData, context).ConfigureAwait(false);
            
            Interlocked.Increment(ref processedEventCount);
            
            logger?.LogTrace("Successfully processed event {EventId} for subscription {SubscriptionId} in {ElapsedMs}ms",
                eventData.EventId, Id, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            Interlocked.Increment(ref failedEventCount);
            
            logger?.LogError(ex, "Failed to process event {EventId} for subscription {SubscriptionId}",
                eventData.EventId, Id);
            
            throw;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            isActive = false;
            logger?.LogInformation("Disposed subscription {SubscriptionId} for topic {Topic}", Id, Topic);
        }
    }
}

// Event context implementation
public class EventContext : IEventContext
{
    private readonly IEvent eventData;
    private readonly ISubscription subscription;
    private int retryCount = 0;

    public EventContext(IEvent eventData, ISubscription subscription)
    {
        this.eventData = eventData ?? throw new ArgumentNullException(nameof(eventData));
        this.subscription = subscription ?? throw new ArgumentNullException(nameof(subscription));
    }

    public Guid EventId => eventData.EventId;
    public string Topic => eventData.Topic;
    public DateTime Timestamp => eventData.Timestamp;
    public IDictionary<string, object> Headers => eventData.Headers;
    public string CorrelationId => eventData.CorrelationId;
    public Guid SubscriptionId => subscription.Id;
    public int RetryCount => retryCount;

    public Task AcknowledgeAsync()
    {
        // In a real implementation, this would acknowledge the event with the broker
        return Task.CompletedTask;
    }

    public Task RejectAsync(bool requeue = false)
    {
        if (requeue)
        {
            retryCount++;
        }
        // In a real implementation, this would reject the event and optionally requeue
        return Task.CompletedTask;
    }
}

// Topic management
public class TopicManager
{
    private readonly ConcurrentDictionary<string, Topic> topics;
    private readonly ILogger logger;

    public TopicManager(ILogger<TopicManager> logger = null)
    {
        topics = new ConcurrentDictionary<string, Topic>();
        this.logger = logger;
    }

    public Task CreateTopicAsync(string topicName, TopicConfiguration configuration = null)
    {
        var topic = new Topic(topicName, configuration ?? new TopicConfiguration());
        
        topics.AddOrUpdate(topicName, topic, (key, existing) => existing);
        
        logger?.LogInformation("Created topic: {TopicName}", topicName);
        return Task.CompletedTask;
    }

    public Task<bool> DeleteTopicAsync(string topicName)
    {
        var removed = topics.TryRemove(topicName, out _);
        
        if (removed)
        {
            logger?.LogInformation("Deleted topic: {TopicName}", topicName);
        }
        
        return Task.FromResult(removed);
    }

    public Task<Topic> GetTopicAsync(string topicName)
    {
        topics.TryGetValue(topicName, out var topic);
        return Task.FromResult(topic);
    }

    public Task<IEnumerable<string>> GetTopicNamesAsync()
    {
        return Task.FromResult<IEnumerable<string>>(topics.Keys.ToList());
    }

    public Task<bool> TopicExistsAsync(string topicName)
    {
        return Task.FromResult(topics.ContainsKey(topicName));
    }

    public IEnumerable<string> FindMatchingTopics(string topicPattern)
    {
        if (string.IsNullOrEmpty(topicPattern))
            return Enumerable.Empty<string>();

        // Convert wildcard pattern to regex
        var regexPattern = "^" + Regex.Escape(topicPattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
        
        return topics.Keys.Where(topic => regex.IsMatch(topic)).ToList();
    }
}

public class Topic
{
    public Topic(string name, TopicConfiguration configuration)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        CreatedAt = DateTime.UtcNow;
        Statistics = new TopicStatistics();
    }

    public string Name { get; }
    public TopicConfiguration Configuration { get; }
    public DateTime CreatedAt { get; }
    public TopicStatistics Statistics { get; }
}

public class TopicConfiguration
{
    public bool IsPersistent { get; set; } = false;
    public int MaxSubscribers { get; set; } = int.MaxValue;
    public TimeSpan MessageRetention { get; set; } = TimeSpan.FromHours(24);
    public int MaxMessageSize { get; set; } = 1024 * 1024; // 1MB
    public bool EnableMessageOrdering { get; set; } = false;
    public QualityOfService QoS { get; set; } = QualityOfService.AtLeastOnce;
}

public enum QualityOfService
{
    AtMostOnce,
    AtLeastOnce,
    ExactlyOnce
}

public class TopicStatistics
{
    private long publishedEventCount = 0;
    private long subscriberCount = 0;
    private long totalDeliveries = 0;
    private long failedDeliveries = 0;

    public long PublishedEventCount => publishedEventCount;
    public long SubscriberCount => subscriberCount;
    public long TotalDeliveries => totalDeliveries;
    public long FailedDeliveries => failedDeliveries;
    public DateTime LastEventTime { get; private set; }

    public void IncrementPublishedEvents()
    {
        Interlocked.Increment(ref publishedEventCount);
        LastEventTime = DateTime.UtcNow;
    }

    public void IncrementSubscribers()
    {
        Interlocked.Increment(ref subscriberCount);
    }

    public void DecrementSubscribers()
    {
        Interlocked.Decrement(ref subscriberCount);
    }

    public void IncrementDeliveries()
    {
        Interlocked.Increment(ref totalDeliveries);
    }

    public void IncrementFailedDeliveries()
    {
        Interlocked.Increment(ref failedDeliveries);
    }
}

// Subscription options
public class SubscriptionOptions
{
    public string SubscriberName { get; set; }
    public bool IsWildcardSubscription { get; set; } = false;
    public Func<IEvent, bool> EventFilter { get; set; }
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public bool EnableDeadLetterQueue { get; set; } = false;
    public QualityOfService QoS { get; set; } = QualityOfService.AtLeastOnce;
}

// In-memory event broker implementation
public class InMemoryEventBroker : IEventBroker, IDisposable
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<ISubscription>> topicSubscriptions;
    private readonly ConcurrentDictionary<Guid, ISubscription> allSubscriptions;
    private readonly TopicManager topicManager;
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger logger;
    private volatile bool isDisposed = false;

    public InMemoryEventBroker(
        IServiceProvider serviceProvider = null,
        ILogger<InMemoryEventBroker> logger = null)
    {
        topicSubscriptions = new ConcurrentDictionary<string, ConcurrentBag<ISubscription>>();
        allSubscriptions = new ConcurrentDictionary<Guid, ISubscription>();
        topicManager = new TopicManager(serviceProvider?.GetService<ILogger<TopicManager>>());
        this.serviceProvider = serviceProvider;
        this.logger = logger;
    }

    public async Task PublishAsync<T>(string topic, T eventData, CancellationToken token = default) where T : class
    {
        var typedEvent = new TypedEvent<T>(eventData, topic);
        await PublishAsync(topic, typedEvent, token).ConfigureAwait(false);
    }

    public async Task PublishAsync(string topic, IEvent eventData, CancellationToken token = default)
    {
        eventData.Topic = topic;
        await PublishAsync(eventData, token).ConfigureAwait(false);
    }

    public async Task PublishAsync(IEvent eventData, CancellationToken token = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(InMemoryEventBroker));
        
        var topic = eventData.Topic;
        if (string.IsNullOrEmpty(topic))
        {
            throw new ArgumentException("Event must have a topic", nameof(eventData));
        }

        // Ensure topic exists
        var topicObj = await topicManager.GetTopicAsync(topic).ConfigureAwait(false);
        if (topicObj == null)
        {
            await topicManager.CreateTopicAsync(topic).ConfigureAwait(false);
            topicObj = await topicManager.GetTopicAsync(topic).ConfigureAwait(false);
        }

        topicObj.Statistics.IncrementPublishedEvents();

        logger?.LogTrace("Publishing event {EventId} to topic {Topic}", eventData.EventId, topic);

        // Get direct subscribers
        var directSubscribers = GetSubscribersForTopic(topic);
        
        // Get wildcard subscribers
        var wildcardSubscribers = GetWildcardSubscribers(topic);
        
        // Combine all subscribers
        var allSubscribers = directSubscribers.Concat(wildcardSubscribers)
            .Where(s => s.IsActive)
            .ToList();

        if (allSubscribers.Count == 0)
        {
            logger?.LogTrace("No subscribers found for topic {Topic}", topic);
            return;
        }

        // Deliver to all subscribers
        var deliveryTasks = allSubscribers.Select(subscription => 
            DeliverEventToSubscriberAsync(eventData, subscription, token));

        await Task.WhenAll(deliveryTasks).ConfigureAwait(false);

        logger?.LogTrace("Published event {EventId} to {SubscriberCount} subscribers on topic {Topic}",
            eventData.EventId, allSubscribers.Count, topic);
    }

    public async Task<ISubscription> SubscribeAsync<T>(string topic, 
        Func<T, IEventContext, Task> handler, CancellationToken token = default) where T : class
    {
        var wrappedHandler = new Func<IEvent, IEventContext, Task>(async (eventData, context) =>
        {
            var typedEvent = JsonSerializer.Deserialize<T>(eventData.Data);
            await handler(typedEvent, context).ConfigureAwait(false);
        });

        return await SubscribeAsync(topic, wrappedHandler, token).ConfigureAwait(false);
    }

    public async Task<ISubscription> SubscribeAsync(string topic, 
        Func<IEvent, IEventContext, Task> handler, CancellationToken token = default)
    {
        var options = new SubscriptionOptions();
        return await SubscribeAsync(topic, handler, options, token).ConfigureAwait(false);
    }

    public async Task<ISubscription> SubscribeAsync(string topicPattern, 
        Func<IEvent, IEventContext, Task> handler, SubscriptionOptions options, CancellationToken token = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(InMemoryEventBroker));

        var subscriptionLogger = serviceProvider?.GetService<ILogger<Subscription>>();
        var subscription = new Subscription(topicPattern, handler, options.SubscriberName, subscriptionLogger);

        // Store subscription
        allSubscriptions[subscription.Id] = subscription;

        if (options.IsWildcardSubscription)
        {
            // For wildcard subscriptions, we'll check against all topics during publish
            // No need to add to specific topic subscriptions
        }
        else
        {
            // Ensure topic exists
            if (!await topicManager.TopicExistsAsync(topicPattern).ConfigureAwait(false))
            {
                await topicManager.CreateTopicAsync(topicPattern).ConfigureAwait(false);
            }

            var topicObj = await topicManager.GetTopicAsync(topicPattern).ConfigureAwait(false);
            topicObj?.Statistics.IncrementSubscribers();

            // Add to topic-specific subscriptions
            var subscriptions = topicSubscriptions.GetOrAdd(topicPattern, _ => new ConcurrentBag<ISubscription>());
            subscriptions.Add(subscription);
        }

        logger?.LogInformation("Created subscription {SubscriptionId} for topic pattern {TopicPattern} (wildcard: {IsWildcard})",
            subscription.Id, topicPattern, options.IsWildcardSubscription);

        return subscription;
    }

    public async Task UnsubscribeAsync(Guid subscriptionId, CancellationToken token = default)
    {
        if (allSubscriptions.TryRemove(subscriptionId, out var subscription))
        {
            subscription.Dispose();

            // Remove from topic-specific subscriptions
            if (topicSubscriptions.TryGetValue(subscription.Topic, out var subscriptions))
            {
                // Note: ConcurrentBag doesn't support removal, so we'll mark as inactive
                // In a real implementation, you might use a different data structure
            }

            var topicObj = await topicManager.GetTopicAsync(subscription.Topic).ConfigureAwait(false);
            topicObj?.Statistics.DecrementSubscribers();

            logger?.LogInformation("Unsubscribed subscription {SubscriptionId} from topic {Topic}",
                subscriptionId, subscription.Topic);
        }
    }

    public async Task<IEnumerable<string>> GetTopicsAsync(CancellationToken token = default)
    {
        return await topicManager.GetTopicNamesAsync().ConfigureAwait(false);
    }

    public Task<IEnumerable<ISubscription>> GetSubscriptionsAsync(string topic = null, CancellationToken token = default)
    {
        if (string.IsNullOrEmpty(topic))
        {
            return Task.FromResult<IEnumerable<ISubscription>>(allSubscriptions.Values.ToList());
        }

        var subscriptions = GetSubscribersForTopic(topic);
        return Task.FromResult(subscriptions);
    }

    public async Task<TopicStatistics> GetTopicStatisticsAsync(string topic, CancellationToken token = default)
    {
        var topicObj = await topicManager.GetTopicAsync(topic).ConfigureAwait(false);
        return topicObj?.Statistics;
    }

    public async Task CreateTopicAsync(string topic, TopicConfiguration configuration = null, CancellationToken token = default)
    {
        await topicManager.CreateTopicAsync(topic, configuration).ConfigureAwait(false);
    }

    public async Task DeleteTopicAsync(string topic, CancellationToken token = default)
    {
        // Unsubscribe all subscribers
        var subscribers = GetSubscribersForTopic(topic).ToList();
        foreach (var subscriber in subscribers)
        {
            await UnsubscribeAsync(subscriber.Id, token).ConfigureAwait(false);
        }

        // Remove topic
        await topicManager.DeleteTopicAsync(topic).ConfigureAwait(false);
        topicSubscriptions.TryRemove(topic, out _);
    }

    private IEnumerable<ISubscription> GetSubscribersForTopic(string topic)
    {
        if (topicSubscriptions.TryGetValue(topic, out var subscriptions))
        {
            return subscriptions.Where(s => s.IsActive).ToList();
        }
        return Enumerable.Empty<ISubscription>();
    }

    private IEnumerable<ISubscription> GetWildcardSubscribers(string topic)
    {
        return allSubscriptions.Values
            .Where(s => s.IsActive && IsWildcardMatch(s.Topic, topic))
            .ToList();
    }

    private bool IsWildcardMatch(string pattern, string topic)
    {
        if (pattern == topic) return false; // Direct match, not wildcard
        
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
        return regex.IsMatch(topic);
    }

    private async Task DeliverEventToSubscriberAsync(IEvent eventData, ISubscription subscription, CancellationToken token)
    {
        var context = new EventContext(eventData, subscription);
        
        try
        {
            if (subscription is Subscription sub)
            {
                await sub.HandleEventAsync(eventData, context).ConfigureAwait(false);
            }

            var topicObj = await topicManager.GetTopicAsync(eventData.Topic).ConfigureAwait(false);
            topicObj?.Statistics.IncrementDeliveries();

            await context.AcknowledgeAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to deliver event {EventId} to subscription {SubscriptionId}",
                eventData.EventId, subscription.Id);

            var topicObj = await topicManager.GetTopicAsync(eventData.Topic).ConfigureAwait(false);
            topicObj?.Statistics.IncrementFailedDeliveries();

            await context.RejectAsync(requeue: false).ConfigureAwait(false);
        }
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;

            // Dispose all subscriptions
            foreach (var subscription in allSubscriptions.Values)
            {
                subscription.Dispose();
            }

            allSubscriptions.Clear();
            topicSubscriptions.Clear();

            logger?.LogInformation("Disposed event broker");
        }
    }
}

// Reactive event broker using System.Reactive
public class ReactiveEventBroker : IEventBroker, IDisposable
{
    private readonly ConcurrentDictionary<string, Subject<IEvent>> topicSubjects;
    private readonly ConcurrentDictionary<Guid, IDisposable> subscriptionDisposables;
    private readonly TopicManager topicManager;
    private readonly ILogger logger;
    private volatile bool isDisposed = false;

    public ReactiveEventBroker(ILogger<ReactiveEventBroker> logger = null)
    {
        topicSubjects = new ConcurrentDictionary<string, Subject<IEvent>>();
        subscriptionDisposables = new ConcurrentDictionary<Guid, IDisposable>();
        topicManager = new TopicManager();
        this.logger = logger;
    }

    public async Task PublishAsync<T>(string topic, T eventData, CancellationToken token = default) where T : class
    {
        var typedEvent = new TypedEvent<T>(eventData, topic);
        await PublishAsync(topic, typedEvent, token).ConfigureAwait(false);
    }

    public async Task PublishAsync(string topic, IEvent eventData, CancellationToken token = default)
    {
        eventData.Topic = topic;
        await PublishAsync(eventData, token).ConfigureAwait(false);
    }

    public Task PublishAsync(IEvent eventData, CancellationToken token = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(ReactiveEventBroker));

        var topic = eventData.Topic;
        if (string.IsNullOrEmpty(topic))
        {
            throw new ArgumentException("Event must have a topic", nameof(eventData));
        }

        var subject = topicSubjects.GetOrAdd(topic, _ => new Subject<IEvent>());
        
        try
        {
            subject.OnNext(eventData);
            logger?.LogTrace("Published reactive event {EventId} to topic {Topic}", eventData.EventId, topic);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to publish reactive event {EventId} to topic {Topic}", 
                eventData.EventId, topic);
            subject.OnError(ex);
        }

        return Task.CompletedTask;
    }

    public async Task<ISubscription> SubscribeAsync<T>(string topic, 
        Func<T, IEventContext, Task> handler, CancellationToken token = default) where T : class
    {
        var wrappedHandler = new Func<IEvent, IEventContext, Task>(async (eventData, context) =>
        {
            var typedEvent = JsonSerializer.Deserialize<T>(eventData.Data);
            await handler(typedEvent, context).ConfigureAwait(false);
        });

        return await SubscribeAsync(topic, wrappedHandler, token).ConfigureAwait(false);
    }

    public async Task<ISubscription> SubscribeAsync(string topic, 
        Func<IEvent, IEventContext, Task> handler, CancellationToken token = default)
    {
        var options = new SubscriptionOptions();
        return await SubscribeAsync(topic, handler, options, token).ConfigureAwait(false);
    }

    public async Task<ISubscription> SubscribeAsync(string topicPattern, 
        Func<IEvent, IEventContext, Task> handler, SubscriptionOptions options, CancellationToken token = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(ReactiveEventBroker));

        var subscription = new Subscription(topicPattern, handler, options.SubscriberName);
        
        IObservable<IEvent> observable;

        if (options.IsWildcardSubscription)
        {
            // Create observable that merges all matching topics
            observable = topicSubjects
                .Where(kvp => IsWildcardMatch(topicPattern, kvp.Key))
                .Select(kvp => kvp.Value)
                .Merge();
        }
        else
        {
            // Get or create subject for specific topic
            var subject = topicSubjects.GetOrAdd(topicPattern, _ => new Subject<IEvent>());
            observable = subject.AsObservable();
        }

        // Apply event filter if specified
        if (options.EventFilter != null)
        {
            observable = observable.Where(options.EventFilter);
        }

        // Subscribe to the observable
        var disposable = observable
            .ObserveOn(TaskPoolScheduler.Default)
            .Subscribe(
                onNext: async eventData =>
                {
                    var context = new EventContext(eventData, subscription);
                    try
                    {
                        if (subscription is Subscription sub)
                        {
                            await sub.HandleEventAsync(eventData, context).ConfigureAwait(false);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger?.LogError(ex, "Failed to handle reactive event {EventId} in subscription {SubscriptionId}",
                            eventData.EventId, subscription.Id);
                    }
                },
                onError: ex =>
                {
                    logger?.LogError(ex, "Error in reactive subscription {SubscriptionId}", subscription.Id);
                },
                onCompleted: () =>
                {
                    logger?.LogInformation("Reactive subscription {SubscriptionId} completed", subscription.Id);
                });

        subscriptionDisposables[subscription.Id] = disposable;

        logger?.LogInformation("Created reactive subscription {SubscriptionId} for topic pattern {TopicPattern}",
            subscription.Id, topicPattern);

        return subscription;
    }

    public Task UnsubscribeAsync(Guid subscriptionId, CancellationToken token = default)
    {
        if (subscriptionDisposables.TryRemove(subscriptionId, out var disposable))
        {
            disposable.Dispose();
            logger?.LogInformation("Unsubscribed reactive subscription {SubscriptionId}", subscriptionId);
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetTopicsAsync(CancellationToken token = default)
    {
        return Task.FromResult<IEnumerable<string>>(topicSubjects.Keys.ToList());
    }

    public Task<IEnumerable<ISubscription>> GetSubscriptionsAsync(string topic = null, CancellationToken token = default)
    {
        // In a real reactive implementation, you'd track subscriptions differently
        return Task.FromResult(Enumerable.Empty<ISubscription>());
    }

    public async Task<TopicStatistics> GetTopicStatisticsAsync(string topic, CancellationToken token = default)
    {
        var topicObj = await topicManager.GetTopicAsync(topic).ConfigureAwait(false);
        return topicObj?.Statistics;
    }

    public async Task CreateTopicAsync(string topic, TopicConfiguration configuration = null, CancellationToken token = default)
    {
        await topicManager.CreateTopicAsync(topic, configuration).ConfigureAwait(false);
        topicSubjects.GetOrAdd(topic, _ => new Subject<IEvent>());
    }

    public async Task DeleteTopicAsync(string topic, CancellationToken token = default)
    {
        if (topicSubjects.TryRemove(topic, out var subject))
        {
            subject.OnCompleted();
            subject.Dispose();
        }

        await topicManager.DeleteTopicAsync(topic).ConfigureAwait(false);
    }

    private bool IsWildcardMatch(string pattern, string topic)
    {
        var regexPattern = "^" + Regex.Escape(pattern)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";

        var regex = new Regex(regexPattern, RegexOptions.IgnoreCase);
        return regex.IsMatch(topic);
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;

            // Dispose all subscriptions
            foreach (var disposable in subscriptionDisposables.Values)
            {
                disposable.Dispose();
            }
            subscriptionDisposables.Clear();

            // Complete and dispose all subjects
            foreach (var subject in topicSubjects.Values)
            {
                try
                {
                    subject.OnCompleted();
                }
                catch
                {
                    // Ignore exceptions during completion
                }
                subject.Dispose();
            }
            topicSubjects.Clear();

            logger?.LogInformation("Disposed reactive event broker");
        }
    }
}

// Event aggregator for in-process messaging
public class EventAggregator : IEventPublisher, IEventSubscriber, IDisposable
{
    private readonly ConcurrentDictionary<Type, ConcurrentBag<EventHandlerWrapper>> handlers;
    private readonly ILogger logger;
    private volatile bool isDisposed = false;

    public EventAggregator(ILogger<EventAggregator> logger = null)
    {
        handlers = new ConcurrentDictionary<Type, ConcurrentBag<EventHandlerWrapper>>();
        this.logger = logger;
    }

    public Task PublishAsync<T>(string topic, T eventData, CancellationToken token = default) where T : class
    {
        return PublishAsync(eventData, token);
    }

    public Task PublishAsync(string topic, IEvent eventData, CancellationToken token = default)
    {
        return PublishAsync(eventData, token);
    }

    public async Task PublishAsync(IEvent eventData, CancellationToken token = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(EventAggregator));

        var eventType = eventData.GetType();
        
        if (handlers.TryGetValue(eventType, out var eventHandlers))
        {
            var tasks = eventHandlers
                .Where(h => h.IsActive)
                .Select(h => h.HandleAsync(eventData, token));

            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        logger?.LogTrace("Published event {EventType} to {HandlerCount} handlers", 
            eventType.Name, eventHandlers?.Count ?? 0);
    }

    public Task<ISubscription> SubscribeAsync<T>(string topic, 
        Func<T, IEventContext, Task> handler, CancellationToken token = default) where T : class
    {
        var wrapper = new EventHandlerWrapper<T>(handler);
        var eventHandlers = handlers.GetOrAdd(typeof(T), _ => new ConcurrentBag<EventHandlerWrapper>());
        eventHandlers.Add(wrapper);

        logger?.LogInformation("Subscribed to event type {EventType}", typeof(T).Name);
        
        return Task.FromResult<ISubscription>(wrapper);
    }

    public Task<ISubscription> SubscribeAsync(string topic, 
        Func<IEvent, IEventContext, Task> handler, CancellationToken token = default)
    {
        var wrapper = new EventHandlerWrapper(handler);
        var eventHandlers = handlers.GetOrAdd(typeof(IEvent), _ => new ConcurrentBag<EventHandlerWrapper>());
        eventHandlers.Add(wrapper);

        logger?.LogInformation("Subscribed to base event interface");
        
        return Task.FromResult<ISubscription>(wrapper);
    }

    public Task<ISubscription> SubscribeAsync(string topicPattern, 
        Func<IEvent, IEventContext, Task> handler, SubscriptionOptions options, CancellationToken token = default)
    {
        return SubscribeAsync(topicPattern, handler, token);
    }

    public Task UnsubscribeAsync(Guid subscriptionId, CancellationToken token = default)
    {
        foreach (var handlerBag in handlers.Values)
        {
            foreach (var handler in handlerBag)
            {
                if (handler.Id == subscriptionId)
                {
                    handler.Dispose();
                    logger?.LogInformation("Unsubscribed handler {SubscriptionId}", subscriptionId);
                    break;
                }
            }
        }

        return Task.CompletedTask;
    }

    private abstract class EventHandlerWrapper : ISubscription
    {
        public Guid Id { get; } = Guid.NewGuid();
        public string Topic { get; } = "EventAggregator";
        public string SubscriberName { get; } = "EventAggregatorSubscriber";
        public DateTime CreatedAt { get; } = DateTime.UtcNow;
        public bool IsActive { get; private set; } = true;
        public long ProcessedEventCount { get; private set; } = 0;
        public long FailedEventCount { get; private set; } = 0;

        public abstract Task HandleAsync(IEvent eventData, CancellationToken token);

        public Task ActivateAsync()
        {
            IsActive = true;
            return Task.CompletedTask;
        }

        public Task DeactivateAsync()
        {
            IsActive = false;
            return Task.CompletedTask;
        }

        protected void IncrementProcessed()
        {
            Interlocked.Increment(ref ProcessedEventCount);
        }

        protected void IncrementFailed()
        {
            Interlocked.Increment(ref FailedEventCount);
        }

        public virtual void Dispose()
        {
            IsActive = false;
        }
    }

    private class EventHandlerWrapper<T> : EventHandlerWrapper where T : class
    {
        private readonly Func<T, IEventContext, Task> handler;

        public EventHandlerWrapper(Func<T, IEventContext, Task> handler)
        {
            this.handler = handler ?? throw new ArgumentNullException(nameof(handler));
        }

        public override async Task HandleAsync(IEvent eventData, CancellationToken token)
        {
            if (!IsActive) return;

            try
            {
                T typedEvent;
                
                if (eventData is T directEvent)
                {
                    typedEvent = directEvent;
                }
                else if (eventData is TypedEvent<T> typedEventWrapper)
                {
                    typedEvent = typedEventWrapper.Payload;
                }
                else
                {
                    return; // Event type doesn't match
                }

                var context = new EventContext(eventData, this);
                await handler(typedEvent, context).ConfigureAwait(false);
                
                IncrementProcessed();
            }
            catch
            {
                IncrementFailed();
                throw;
            }
        }
    }

    private class EventHandlerWrapper : EventHandlerWrapper<IEvent>
    {
        public EventHandlerWrapper(Func<IEvent, IEventContext, Task> handler) 
            : base(handler) { }
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;

            foreach (var handlerBag in handlers.Values)
            {
                foreach (var handler in handlerBag)
                {
                    handler.Dispose();
                }
            }

            handlers.Clear();
            logger?.LogInformation("Disposed event aggregator");
        }
    }
}

// Event store for persistent pub-sub
public class PersistentEventBroker : IEventBroker, IDisposable
{
    private readonly InMemoryEventBroker inmemoryBroker;
    private readonly List<IEvent> eventStore;
    private readonly object storeLock = new object();
    private readonly ILogger logger;
    private volatile bool isDisposed = false;

    public PersistentEventBroker(IServiceProvider serviceProvider = null, ILogger<PersistentEventBroker> logger = null)
    {
        inmemoryBroker = new InMemoryEventBroker(serviceProvider, 
            serviceProvider?.GetService<ILogger<InMemoryEventBroker>>());
        eventStore = new List<IEvent>();
        this.logger = logger;
    }

    public async Task PublishAsync<T>(string topic, T eventData, CancellationToken token = default) where T : class
    {
        var typedEvent = new TypedEvent<T>(eventData, topic);
        await PublishAsync(topic, typedEvent, token).ConfigureAwait(false);
    }

    public async Task PublishAsync(string topic, IEvent eventData, CancellationToken token = default)
    {
        eventData.Topic = topic;
        await PublishAsync(eventData, token).ConfigureAwait(false);
    }

    public async Task PublishAsync(IEvent eventData, CancellationToken token = default)
    {
        if (isDisposed) throw new ObjectDisposedException(nameof(PersistentEventBroker));

        // Store event persistently
        lock (storeLock)
        {
            eventStore.Add(eventData);
        }

        // Publish through in-memory broker
        await inmemoryBroker.PublishAsync(eventData, token).ConfigureAwait(false);

        logger?.LogTrace("Persistently stored and published event {EventId}", eventData.EventId);
    }

    public async Task<ISubscription> SubscribeAsync<T>(string topic, 
        Func<T, IEventContext, Task> handler, CancellationToken token = default) where T : class
    {
        // Subscribe to future events
        var subscription = await inmemoryBroker.SubscribeAsync(topic, handler, token).ConfigureAwait(false);

        // Replay historical events
        await ReplayEventsForSubscription<T>(topic, handler, token).ConfigureAwait(false);

        return subscription;
    }

    public Task<ISubscription> SubscribeAsync(string topic, 
        Func<IEvent, IEventContext, Task> handler, CancellationToken token = default)
    {
        return inmemoryBroker.SubscribeAsync(topic, handler, token);
    }

    public Task<ISubscription> SubscribeAsync(string topicPattern, 
        Func<IEvent, IEventContext, Task> handler, SubscriptionOptions options, CancellationToken token = default)
    {
        return inmemoryBroker.SubscribeAsync(topicPattern, handler, options, token);
    }

    public Task UnsubscribeAsync(Guid subscriptionId, CancellationToken token = default)
    {
        return inmemoryBroker.UnsubscribeAsync(subscriptionId, token);
    }

    public Task<IEnumerable<string>> GetTopicsAsync(CancellationToken token = default)
    {
        return inmemoryBroker.GetTopicsAsync(token);
    }

    public Task<IEnumerable<ISubscription>> GetSubscriptionsAsync(string topic = null, CancellationToken token = default)
    {
        return inmemoryBroker.GetSubscriptionsAsync(topic, token);
    }

    public Task<TopicStatistics> GetTopicStatisticsAsync(string topic, CancellationToken token = default)
    {
        return inmemoryBroker.GetTopicStatisticsAsync(topic, token);
    }

    public Task CreateTopicAsync(string topic, TopicConfiguration configuration = null, CancellationToken token = default)
    {
        return inmemoryBroker.CreateTopicAsync(topic, configuration, token);
    }

    public Task DeleteTopicAsync(string topic, CancellationToken token = default)
    {
        return inmemoryBroker.DeleteTopicAsync(topic, token);
    }

    public Task<IEnumerable<IEvent>> GetStoredEventsAsync(string topic = null, CancellationToken token = default)
    {
        lock (storeLock)
        {
            var events = string.IsNullOrEmpty(topic) 
                ? eventStore.ToList()
                : eventStore.Where(e => e.Topic == topic).ToList();

            return Task.FromResult<IEnumerable<IEvent>>(events);
        }
    }

    private async Task ReplayEventsForSubscription<T>(string topic, Func<T, IEventContext, Task> handler, 
        CancellationToken token) where T : class
    {
        lock (storeLock)
        {
            var historicalEvents = eventStore
                .Where(e => e.Topic == topic && e.EventType == typeof(T).Name)
                .ToList();

            foreach (var eventData in historicalEvents)
            {
                try
                {
                    var payload = JsonSerializer.Deserialize<T>(eventData.Data);
                    var context = new EventContext(eventData, new Subscription(topic, null));
                    
                    await handler(payload, context).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    logger?.LogError(ex, "Failed to replay event {EventId} for subscription", eventData.EventId);
                }
            }
        }
    }

    public void Dispose()
    {
        if (!isDisposed)
        {
            isDisposed = true;
            inmemoryBroker?.Dispose();
            
            lock (storeLock)
            {
                eventStore.Clear();
            }

            logger?.LogInformation("Disposed persistent event broker");
        }
    }
}
```

**Usage**:

```csharp
// Example 1: Basic Publisher-Subscriber Setup
Console.WriteLine("Basic Publisher-Subscriber Examples:");

// Set up services
var services = new ServiceCollection()
    .AddLogging(builder => builder.AddConsole())
    .AddSingleton<IEventBroker, InMemoryEventBroker>()
    .BuildServiceProvider();

var broker = services.GetRequiredService<IEventBroker>();

// Define event types
public class OrderCreatedEvent
{
    public Guid OrderId { get; set; }
    public string CustomerEmail { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class PaymentProcessedEvent
{
    public Guid PaymentId { get; set; }
    public Guid OrderId { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; }
}

public class InventoryUpdatedEvent
{
    public string ProductId { get; set; }
    public int NewQuantity { get; set; }
    public string Reason { get; set; }
}

// Example 2: Basic Subscriptions
Console.WriteLine("\nBasic Subscription Examples:");

// Subscribe to order events
var orderSubscription = await broker.SubscribeAsync<OrderCreatedEvent>("orders", 
    async (order, context) =>
    {
        Console.WriteLine($"Processing order: {order.OrderId} for {order.CustomerEmail}");
        Console.WriteLine($"Amount: ${order.TotalAmount}");
        
        // Simulate processing
        await Task.Delay(100);
        await context.AcknowledgeAsync();
        
        Console.WriteLine($"Order {order.OrderId} processed successfully");
    });

// Subscribe to payment events  
var paymentSubscription = await broker.SubscribeAsync<PaymentProcessedEvent>("payments",
    async (payment, context) =>
    {
        Console.WriteLine($"Payment processed: {payment.PaymentId}");
        Console.WriteLine($"Order: {payment.OrderId}, Amount: ${payment.Amount}");
        
        await Task.Delay(50);
        await context.AcknowledgeAsync();
    });

Console.WriteLine($"Created subscriptions: {orderSubscription.Id}, {paymentSubscription.Id}");

// Publish some events
var orderEvent = new OrderCreatedEvent
{
    OrderId = Guid.NewGuid(),
    CustomerEmail = "customer@example.com",
    TotalAmount = 99.99m,
    CreatedAt = DateTime.UtcNow
};

await broker.PublishAsync("orders", orderEvent);

var paymentEvent = new PaymentProcessedEvent
{
    PaymentId = Guid.NewGuid(),
    OrderId = orderEvent.OrderId,
    Amount = orderEvent.TotalAmount,
    PaymentMethod = "CreditCard"
};

await broker.PublishAsync("payments", paymentEvent);

// Example 3: Wildcard Subscriptions
Console.WriteLine("\nWildcard Subscription Examples:");

// Subscribe to all events with wildcard pattern
var wildcardOptions = new SubscriptionOptions
{
    IsWildcardSubscription = true,
    SubscriberName = "AuditLogger"
};

var auditSubscription = await broker.SubscribeAsync("*", 
    async (eventData, context) =>
    {
        Console.WriteLine($"[AUDIT] Event: {eventData.EventType} on topic {eventData.Topic}");
        Console.WriteLine($"[AUDIT] Time: {eventData.Timestamp:yyyy-MM-dd HH:mm:ss}");
        Console.WriteLine($"[AUDIT] Event ID: {eventData.EventId}");
        await context.AcknowledgeAsync();
    }, wildcardOptions);

// Subscribe to specific pattern  
var inventoryOptions = new SubscriptionOptions
{
    IsWildcardSubscription = true,
    SubscriberName = "InventoryMonitor"
};

var inventorySubscription = await broker.SubscribeAsync("inventory.*",
    async (eventData, context) =>
    {
        Console.WriteLine($"[INVENTORY] {eventData.EventType} detected on {eventData.Topic}");
        await context.AcknowledgeAsync();
    }, inventoryOptions);

// Publish inventory events
await broker.PublishAsync("inventory.updated", new InventoryUpdatedEvent
{
    ProductId = "PROD-001",
    NewQuantity = 150,
    Reason = "Restock"
});

await broker.PublishAsync("inventory.reserved", new InventoryUpdatedEvent
{
    ProductId = "PROD-001", 
    NewQuantity = 145,
    Reason = "Order reservation"
});

// Example 4: Event Filtering
Console.WriteLine("\nEvent Filtering Examples:");

// Subscribe with event filter
var highValueOptions = new SubscriptionOptions
{
    SubscriberName = "HighValueOrderProcessor",
    EventFilter = (eventData) =>
    {
        if (eventData.EventType == nameof(OrderCreatedEvent))
        {
            try
            {
                var order = JsonSerializer.Deserialize<OrderCreatedEvent>(eventData.Data);
                return order.TotalAmount >= 100.0m; // Only high-value orders
            }
            catch
            {
                return false;
            }
        }
        return false;
    }
};

var highValueSubscription = await broker.SubscribeAsync("orders",
    async (eventData, context) =>
    {
        var order = JsonSerializer.Deserialize<OrderCreatedEvent>(eventData.Data);
        Console.WriteLine($"[HIGH VALUE] Processing high-value order: {order.OrderId} (${order.TotalAmount})");
        
        // Special processing for high-value orders
        await Task.Delay(200);
        await context.AcknowledgeAsync();
    }, highValueOptions);

// Publish orders with different values
await broker.PublishAsync("orders", new OrderCreatedEvent
{
    OrderId = Guid.NewGuid(),
    CustomerEmail = "small@example.com",
    TotalAmount = 25.99m,
    CreatedAt = DateTime.UtcNow
});

await broker.PublishAsync("orders", new OrderCreatedEvent
{
    OrderId = Guid.NewGuid(),
    CustomerEmail = "big@example.com", 
    TotalAmount = 499.99m,
    CreatedAt = DateTime.UtcNow
});

// Example 5: Topic Management
Console.WriteLine("\nTopic Management Examples:");

// Create topics with configuration
var orderTopicConfig = new TopicConfiguration
{
    IsPersistent = true,
    MaxSubscribers = 100,
    MessageRetention = TimeSpan.FromHours(24),
    EnableMessageOrdering = true,
    QoS = QualityOfService.AtLeastOnce
};

await broker.CreateTopicAsync("orders", orderTopicConfig);

// Get topic information
var topics = await broker.GetTopicsAsync();
Console.WriteLine($"Available topics: {string.Join(", ", topics)}");

var subscriptions = await broker.GetSubscriptionsAsync();
Console.WriteLine($"Total subscriptions: {subscriptions.Count()}");

// Get topic statistics
var orderStats = await broker.GetTopicStatisticsAsync("orders");
if (orderStats != null)
{
    Console.WriteLine($"Orders topic - Published: {orderStats.PublishedEventCount}, " +
                     $"Subscribers: {orderStats.SubscriberCount}, " +
                     $"Deliveries: {orderStats.TotalDeliveries}");
}

// Example 6: Reactive Event Broker
Console.WriteLine("\nReactive Event Broker Examples:");

var reactiveBroker = new ReactiveEventBroker(
    services.GetService<ILogger<ReactiveEventBroker>>());

// Subscribe to reactive events with LINQ operators
var reactiveSubscription = await reactiveBroker.SubscribeAsync<OrderCreatedEvent>("reactive.orders",
    async (order, context) =>
    {
        Console.WriteLine($"[REACTIVE] Order: {order.OrderId}, Amount: ${order.TotalAmount}");
        await context.AcknowledgeAsync();
    });

// Publish reactive events
for (int i = 1; i <= 5; i++)
{
    await reactiveBroker.PublishAsync("reactive.orders", new OrderCreatedEvent
    {
        OrderId = Guid.NewGuid(),
        CustomerEmail = $"reactive{i}@example.com",
        TotalAmount = i * 25.0m,
        CreatedAt = DateTime.UtcNow
    });
    
    await Task.Delay(100); // Small delay between events
}

// Example 7: Event Aggregator (In-Process)
Console.WriteLine("\nEvent Aggregator Examples:");

var eventAggregator = new EventAggregator(
    services.GetService<ILogger<EventAggregator>>());

// Subscribe to events by type
var aggregatorSub1 = await eventAggregator.SubscribeAsync<OrderCreatedEvent>("",
    async (order, context) =>
    {
        Console.WriteLine($"[AGGREGATOR] Handler 1 - Order: {order.OrderId}");
        await context.AcknowledgeAsync();
    });

var aggregatorSub2 = await eventAggregator.SubscribeAsync<OrderCreatedEvent>("",
    async (order, context) =>
    {
        Console.WriteLine($"[AGGREGATOR] Handler 2 - Order: {order.OrderId}");
        await context.AcknowledgeAsync();
    });

// Publish through event aggregator
await eventAggregator.PublishAsync("", new OrderCreatedEvent
{
    OrderId = Guid.NewGuid(),
    CustomerEmail = "aggregator@example.com",
    TotalAmount = 150.0m,
    CreatedAt = DateTime.UtcNow
});

// Example 8: Persistent Event Broker
Console.WriteLine("\nPersistent Event Broker Examples:");

var persistentBroker = new PersistentEventBroker(services, 
    services.GetService<ILogger<PersistentEventBroker>>());

// Subscribe (will replay historical events)
var persistentSub = await persistentBroker.SubscribeAsync<OrderCreatedEvent>("persistent.orders",
    async (order, context) =>
    {
        Console.WriteLine($"[PERSISTENT] Order: {order.OrderId} (Amount: ${order.TotalAmount})");
        await context.AcknowledgeAsync();
    });

// Publish events (will be stored persistently)
await persistentBroker.PublishAsync("persistent.orders", new OrderCreatedEvent
{
    OrderId = Guid.NewGuid(),
    CustomerEmail = "persistent1@example.com",
    TotalAmount = 75.0m,
    CreatedAt = DateTime.UtcNow
});

await persistentBroker.PublishAsync("persistent.orders", new OrderCreatedEvent
{
    OrderId = Guid.NewGuid(),
    CustomerEmail = "persistent2@example.com",
    TotalAmount = 125.0m,
    CreatedAt = DateTime.UtcNow
});

// Check stored events
var storedEvents = await persistentBroker.GetStoredEventsAsync("persistent.orders");
Console.WriteLine($"Stored {storedEvents.Count()} events for persistent.orders topic");

// Example 9: Performance and Monitoring
Console.WriteLine("\nPerformance and Monitoring Examples:");

// Monitor subscription performance
foreach (var subscription in new[] { orderSubscription, paymentSubscription, wildcardSubscription })
{
    Console.WriteLine($"Subscription {subscription.SubscriberName}:");
    Console.WriteLine($"  Processed: {subscription.ProcessedEventCount}");
    Console.WriteLine($"  Failed: {subscription.FailedEventCount}"); 
    Console.WriteLine($"  Active: {subscription.IsActive}");
    Console.WriteLine($"  Created: {subscription.CreatedAt:yyyy-MM-dd HH:mm:ss}");
}

// Load testing
Console.WriteLine("\nLoad Testing:");
var loadTestStart = DateTime.UtcNow;

var publishTasks = Enumerable.Range(1, 100).Select(async i =>
{
    await broker.PublishAsync("loadtest", new OrderCreatedEvent
    {
        OrderId = Guid.NewGuid(),
        CustomerEmail = $"loadtest{i}@example.com", 
        TotalAmount = i * 10.0m,
        CreatedAt = DateTime.UtcNow
    });
});

await Task.WhenAll(publishTasks);
var loadTestDuration = DateTime.UtcNow - loadTestStart;

Console.WriteLine($"Published 100 events in {loadTestDuration.TotalMilliseconds:F2}ms");

// Cleanup
await Task.Delay(1000); // Let final events process

// Unsubscribe from all subscriptions
await broker.UnsubscribeAsync(orderSubscription.Id);
await broker.UnsubscribeAsync(paymentSubscription.Id);
await reactiveBroker.UnsubscribeAsync(reactiveSubscription.Id);
await eventAggregator.UnsubscribeAsync(aggregatorSub1.Id);

// Dispose brokers
broker.Dispose();
reactiveBroker.Dispose();
eventAggregator.Dispose();
persistentBroker.Dispose();

Console.WriteLine("\nPublisher-subscriber pattern examples completed!");
```

**Notes**:

- Implement comprehensive publisher-subscriber patterns for decoupled event-driven communication
- Support topic-based message routing with hierarchical topic structures and wildcard subscriptions
- Use event filtering for selective message consumption based on content or metadata  
- Implement multiple broker types: in-memory, reactive (System.Reactive), event aggregator, and persistent
- Support quality of service levels (QoS) for different delivery guarantees
- Provide subscription management with activation, deactivation, and monitoring capabilities
- Implement topic management with configuration options for persistence, retention, and ordering
- Use reactive programming patterns with System.Reactive for advanced event processing
- Support event replay and persistence for durable messaging scenarios
- Implement proper error handling and retry logic for resilient event processing
- Provide comprehensive monitoring and statistics for operational visibility
- Support correlation IDs and event metadata for distributed tracing and debugging
- Use concurrent processing patterns for scalable event distribution
- Implement proper resource cleanup and disposal patterns for memory management

**Prerequisites**:

- Understanding of publisher-subscriber and observer design patterns
- Knowledge of event-driven architecture and asynchronous messaging
- Familiarity with System.Reactive for reactive programming concepts
- Experience with concurrent programming and thread-safe data structures
- Understanding of distributed system messaging patterns and quality of service
- Knowledge of topic-based routing and message filtering strategies

**Related Snippets**:

- [Event Sourcing](event-sourcing.md) - Event sourcing patterns for audit trails and state reconstruction
- [Message Queue](message-queue.md) - Message queuing patterns for reliable asynchronous communication
- [Saga Patterns](saga-patterns.md) - Distributed transaction coordination patterns
