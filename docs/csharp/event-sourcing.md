# Event Sourcing Pattern

**Description**: Comprehensive event sourcing implementation including event store management, aggregate root patterns, event serialization, snapshots, projections, command/query separation (CQRS), event replay, and distributed event sourcing patterns for building scalable and auditable systems.

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
using System.Reflection;
using System.ComponentModel.DataAnnotations;

// Core event sourcing interfaces
public interface IEvent
{
    Guid EventId { get; }
    DateTime Timestamp { get; }
    int Version { get; }
    string EventType { get; }
    IDictionary<string, object> Metadata { get; }
}

public interface IDomainEvent : IEvent
{
    Guid AggregateId { get; }
    string AggregateType { get; }
}

public interface IEventStore
{
    Task SaveEventsAsync(Guid aggregateId, IEnumerable<IEvent> events, 
        int expectedVersion, CancellationToken token = default);
    Task<IEnumerable<IEvent>> GetEventsAsync(Guid aggregateId, 
        int fromVersion = 0, CancellationToken token = default);
    Task<IEnumerable<IEvent>> GetAllEventsAsync(int fromPosition = 0, 
        int maxCount = 1000, CancellationToken token = default);
    Task<IEventStream> GetEventStreamAsync(Guid aggregateId, 
        CancellationToken token = default);
    Task<int> GetCurrentVersionAsync(Guid aggregateId, CancellationToken token = default);
    Task CreateSnapshotAsync(Guid aggregateId, ISnapshot snapshot, 
        CancellationToken token = default);
    Task<ISnapshot> GetLatestSnapshotAsync(Guid aggregateId, 
        CancellationToken token = default);
}

public interface IAggregateRoot
{
    Guid Id { get; }
    int Version { get; }
    IEnumerable<IEvent> UncommittedEvents { get; }
    void MarkEventsAsCommitted();
    void LoadFromHistory(IEnumerable<IEvent> events);
}

public interface ISnapshot
{
    Guid AggregateId { get; }
    int Version { get; }
    DateTime CreatedAt { get; }
    string Data { get; }
    string AggregateType { get; }
}

public interface IEventStream : IAsyncEnumerable<IEvent>
{
    Guid StreamId { get; }
    int CurrentVersion { get; }
    Task<bool> HasMoreEventsAsync(CancellationToken token = default);
}

// Base domain event implementation
public abstract class DomainEvent : IDomainEvent
{
    protected DomainEvent()
    {
        EventId = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
        EventType = GetType().Name;
        Metadata = new Dictionary<string, object>();
    }

    public Guid EventId { get; private set; }
    public DateTime Timestamp { get; private set; }
    public int Version { get; set; }
    public string EventType { get; private set; }
    public IDictionary<string, object> Metadata { get; private set; }
    public Guid AggregateId { get; set; }
    public string AggregateType { get; set; }
}

// In-memory event store implementation
public class InMemoryEventStore : IEventStore
{
    private readonly ConcurrentDictionary<Guid, List<StoredEvent>> eventStreams;
    private readonly ConcurrentDictionary<Guid, StoredSnapshot> snapshots;
    private readonly List<StoredEvent> globalEventLog;
    private readonly IEventSerializer eventSerializer;
    private readonly ILogger logger;
    private readonly object lockObject = new object();
    private long globalPosition = 0;

    public InMemoryEventStore(
        IEventSerializer eventSerializer = null,
        ILogger<InMemoryEventStore> logger = null)
    {
        eventStreams = new ConcurrentDictionary<Guid, List<StoredEvent>>();
        snapshots = new ConcurrentDictionary<Guid, StoredSnapshot>();
        globalEventLog = new List<StoredEvent>();
        this.eventSerializer = eventSerializer ?? new JsonEventSerializer();
        this.logger = logger;
    }

    public Task SaveEventsAsync(Guid aggregateId, IEnumerable<IEvent> events, 
        int expectedVersion, CancellationToken token = default)
    {
        var eventList = events.ToList();
        if (eventList.Count == 0) return Task.CompletedTask;

        lock (lockObject)
        {
            var stream = eventStreams.GetOrAdd(aggregateId, _ => new List<StoredEvent>());
            
            // Check optimistic concurrency
            var currentVersion = stream.Count;
            if (currentVersion != expectedVersion)
            {
                throw new OptimisticConcurrencyException(
                    $"Expected version {expectedVersion}, but current version is {currentVersion}");
            }

            // Add events to stream
            foreach (var domainEvent in eventList)
            {
                var storedEvent = new StoredEvent
                {
                    EventId = domainEvent.EventId,
                    AggregateId = aggregateId,
                    AggregateType = domainEvent is IDomainEvent de ? de.AggregateType : "Unknown",
                    EventType = domainEvent.EventType,
                    EventData = eventSerializer.Serialize(domainEvent),
                    Metadata = eventSerializer.Serialize(domainEvent.Metadata),
                    Version = ++currentVersion,
                    Timestamp = domainEvent.Timestamp,
                    GlobalPosition = ++globalPosition
                };

                stream.Add(storedEvent);
                globalEventLog.Add(storedEvent);

                logger?.LogTrace("Saved event {EventType} for aggregate {AggregateId} at version {Version}",
                    storedEvent.EventType, aggregateId, storedEvent.Version);
            }
        }

        return Task.CompletedTask;
    }

    public Task<IEnumerable<IEvent>> GetEventsAsync(Guid aggregateId, 
        int fromVersion = 0, CancellationToken token = default)
    {
        if (!eventStreams.TryGetValue(aggregateId, out var stream))
        {
            return Task.FromResult(Enumerable.Empty<IEvent>());
        }

        var events = stream
            .Where(e => e.Version > fromVersion)
            .Select(DeserializeEvent)
            .Where(e => e != null)
            .ToList();

        logger?.LogTrace("Retrieved {Count} events for aggregate {AggregateId} from version {FromVersion}",
            events.Count, aggregateId, fromVersion);

        return Task.FromResult<IEnumerable<IEvent>>(events);
    }

    public Task<IEnumerable<IEvent>> GetAllEventsAsync(int fromPosition = 0, 
        int maxCount = 1000, CancellationToken token = default)
    {
        lock (lockObject)
        {
            var events = globalEventLog
                .Where(e => e.GlobalPosition > fromPosition)
                .Take(maxCount)
                .Select(DeserializeEvent)
                .Where(e => e != null)
                .ToList();

            logger?.LogTrace("Retrieved {Count} global events from position {FromPosition}",
                events.Count, fromPosition);

            return Task.FromResult<IEnumerable<IEvent>>(events);
        }
    }

    public Task<IEventStream> GetEventStreamAsync(Guid aggregateId, CancellationToken token = default)
    {
        var stream = new InMemoryEventStream(aggregateId, this, eventSerializer, logger);
        return Task.FromResult<IEventStream>(stream);
    }

    public Task<int> GetCurrentVersionAsync(Guid aggregateId, CancellationToken token = default)
    {
        if (!eventStreams.TryGetValue(aggregateId, out var stream))
        {
            return Task.FromResult(0);
        }

        return Task.FromResult(stream.Count);
    }

    public Task CreateSnapshotAsync(Guid aggregateId, ISnapshot snapshot, CancellationToken token = default)
    {
        var storedSnapshot = new StoredSnapshot
        {
            AggregateId = aggregateId,
            AggregateType = snapshot.AggregateType,
            Version = snapshot.Version,
            Data = snapshot.Data,
            CreatedAt = snapshot.CreatedAt
        };

        snapshots.AddOrUpdate(aggregateId, storedSnapshot, (key, existing) =>
        {
            return storedSnapshot.Version > existing.Version ? storedSnapshot : existing;
        });

        logger?.LogTrace("Created snapshot for aggregate {AggregateId} at version {Version}",
            aggregateId, snapshot.Version);

        return Task.CompletedTask;
    }

    public Task<ISnapshot> GetLatestSnapshotAsync(Guid aggregateId, CancellationToken token = default)
    {
        snapshots.TryGetValue(aggregateId, out var snapshot);
        return Task.FromResult<ISnapshot>(snapshot);
    }

    private IEvent DeserializeEvent(StoredEvent storedEvent)
    {
        try
        {
            return eventSerializer.Deserialize(storedEvent.EventData, storedEvent.EventType);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to deserialize event {EventType} with ID {EventId}",
                storedEvent.EventType, storedEvent.EventId);
            return null;
        }
    }
}

// Event stream implementation
public class InMemoryEventStream : IEventStream
{
    private readonly Guid streamId;
    private readonly InMemoryEventStore eventStore;
    private readonly IEventSerializer eventSerializer;
    private readonly ILogger logger;
    private int currentPosition = 0;

    public InMemoryEventStream(
        Guid streamId,
        InMemoryEventStore eventStore,
        IEventSerializer eventSerializer,
        ILogger logger)
    {
        this.streamId = streamId;
        this.eventStore = eventStore;
        this.eventSerializer = eventSerializer;
        this.logger = logger;
    }

    public Guid StreamId => streamId;
    public int CurrentVersion => currentPosition;

    public async Task<bool> HasMoreEventsAsync(CancellationToken token = default)
    {
        var currentVersion = await eventStore.GetCurrentVersionAsync(streamId, token).ConfigureAwait(false);
        return currentPosition < currentVersion;
    }

    public async IAsyncEnumerator<IEvent> GetAsyncEnumerator(CancellationToken cancellationToken = default)
    {
        while (await HasMoreEventsAsync(cancellationToken).ConfigureAwait(false))
        {
            var events = await eventStore.GetEventsAsync(streamId, currentPosition, cancellationToken)
                .ConfigureAwait(false);
            
            foreach (var domainEvent in events)
            {
                currentPosition++;
                yield return domainEvent;
            }
        }
    }
}

// Abstract aggregate root base class
public abstract class AggregateRoot : IAggregateRoot
{
    private readonly List<IEvent> uncommittedEvents = new List<IEvent>();
    private readonly Dictionary<Type, Action<IEvent>> eventHandlers = new Dictionary<Type, Action<IEvent>>();

    protected AggregateRoot()
    {
        Id = Guid.NewGuid();
        RegisterEventHandlers();
    }

    protected AggregateRoot(Guid id)
    {
        Id = id;
        RegisterEventHandlers();
    }

    public Guid Id { get; protected set; }
    public int Version { get; protected set; }
    public IEnumerable<IEvent> UncommittedEvents => uncommittedEvents.AsReadOnly();

    public void MarkEventsAsCommitted()
    {
        uncommittedEvents.Clear();
    }

    public void LoadFromHistory(IEnumerable<IEvent> events)
    {
        foreach (var domainEvent in events.OrderBy(e => e.Version))
        {
            ApplyEvent(domainEvent, isNew: false);
            Version = domainEvent.Version;
        }
    }

    protected void RaiseEvent(IEvent domainEvent)
    {
        if (domainEvent is IDomainEvent de)
        {
            de.AggregateId = Id;
            de.AggregateType = GetType().Name;
        }

        domainEvent.Version = Version + 1;
        ApplyEvent(domainEvent, isNew: true);
        
        if (domainEvent is IDomainEvent)
        {
            uncommittedEvents.Add(domainEvent);
        }
        
        Version = domainEvent.Version;
    }

    private void ApplyEvent(IEvent domainEvent, bool isNew)
    {
        var eventType = domainEvent.GetType();
        if (eventHandlers.TryGetValue(eventType, out var handler))
        {
            handler(domainEvent);
        }
        else
        {
            // Try to find handler method by convention (Apply + EventName)
            var methodName = $"Apply{eventType.Name}";
            var method = GetType().GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
            
            if (method != null)
            {
                method.Invoke(this, new object[] { domainEvent });
            }
        }
    }

    protected void RegisterEventHandler<TEvent>(Action<TEvent> handler) where TEvent : IEvent
    {
        eventHandlers[typeof(TEvent)] = evt => handler((TEvent)evt);
    }

    protected virtual void RegisterEventHandlers()
    {
        // Override in derived classes to register event handlers
    }

    protected virtual ISnapshot CreateSnapshot()
    {
        return new AggregateSnapshot
        {
            AggregateId = Id,
            Version = Version,
            CreatedAt = DateTime.UtcNow,
            Data = JsonSerializer.Serialize(this),
            AggregateType = GetType().Name
        };
    }

    protected virtual void RestoreFromSnapshot(ISnapshot snapshot)
    {
        Version = snapshot.Version;
        // Override in derived classes to restore state
    }
}

// Repository for event sourced aggregates
public class EventSourcedRepository<TAggregate> : IEventSourcedRepository<TAggregate> 
    where TAggregate : class, IAggregateRoot, new()
{
    private readonly IEventStore eventStore;
    private readonly ISnapshotStrategy snapshotStrategy;
    private readonly ILogger logger;

    public EventSourcedRepository(
        IEventStore eventStore,
        ISnapshotStrategy snapshotStrategy = null,
        ILogger<EventSourcedRepository<TAggregate>> logger = null)
    {
        this.eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        this.snapshotStrategy = snapshotStrategy ?? new SimpleSnapshotStrategy();
        this.logger = logger;
    }

    public async Task<TAggregate> GetByIdAsync(Guid id, CancellationToken token = default)
    {
        var aggregate = new TAggregate();
        
        // Try to load from snapshot first
        var snapshot = await eventStore.GetLatestSnapshotAsync(id, token).ConfigureAwait(false);
        var fromVersion = 0;
        
        if (snapshot != null)
        {
            if (aggregate is AggregateRoot baseAggregate)
            {
                baseAggregate.RestoreFromSnapshot(snapshot);
                fromVersion = snapshot.Version;
            }
        }

        // Load events after snapshot
        var events = await eventStore.GetEventsAsync(id, fromVersion, token).ConfigureAwait(false);
        var eventList = events.ToList();
        
        if (fromVersion == 0 && eventList.Count == 0)
        {
            return null; // Aggregate doesn't exist
        }

        aggregate.LoadFromHistory(eventList);
        
        logger?.LogTrace("Loaded aggregate {AggregateId} with {EventCount} events from version {FromVersion}",
            id, eventList.Count, fromVersion);

        return aggregate;
    }

    public async Task SaveAsync(TAggregate aggregate, CancellationToken token = default)
    {
        var uncommittedEvents = aggregate.UncommittedEvents.ToList();
        if (uncommittedEvents.Count == 0) return;

        var expectedVersion = aggregate.Version - uncommittedEvents.Count;
        
        await eventStore.SaveEventsAsync(aggregate.Id, uncommittedEvents, expectedVersion, token)
            .ConfigureAwait(false);
        
        aggregate.MarkEventsAsCommitted();

        // Check if we should create a snapshot
        if (snapshotStrategy.ShouldCreateSnapshot(aggregate))
        {
            if (aggregate is AggregateRoot baseAggregate)
            {
                var snapshot = baseAggregate.CreateSnapshot();
                await eventStore.CreateSnapshotAsync(aggregate.Id, snapshot, token).ConfigureAwait(false);
                
                logger?.LogTrace("Created snapshot for aggregate {AggregateId} at version {Version}",
                    aggregate.Id, aggregate.Version);
            }
        }

        logger?.LogTrace("Saved {EventCount} events for aggregate {AggregateId}",
            uncommittedEvents.Count, aggregate.Id);
    }

    public async Task<bool> ExistsAsync(Guid id, CancellationToken token = default)
    {
        var currentVersion = await eventStore.GetCurrentVersionAsync(id, token).ConfigureAwait(false);
        return currentVersion > 0;
    }
}

// CQRS Command and Query interfaces
public interface ICommand
{
    Guid CommandId { get; }
    DateTime Timestamp { get; }
}

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{
    Task HandleAsync(TCommand command, CancellationToken token = default);
}

public interface IQuery<out TResult>
{
    Guid QueryId { get; }
    DateTime Timestamp { get; }
}

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken token = default);
}

// Command and Query dispatchers
public class CommandDispatcher : ICommandDispatcher
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger logger;

    public CommandDispatcher(IServiceProvider serviceProvider, ILogger<CommandDispatcher> logger = null)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        this.logger = logger;
    }

    public async Task DispatchAsync<TCommand>(TCommand command, CancellationToken token = default) 
        where TCommand : ICommand
    {
        var handler = serviceProvider.GetRequiredService<ICommandHandler<TCommand>>();
        
        logger?.LogTrace("Dispatching command {CommandType} with ID {CommandId}",
            typeof(TCommand).Name, command.CommandId);

        try
        {
            await handler.HandleAsync(command, token).ConfigureAwait(false);
            logger?.LogTrace("Successfully handled command {CommandId}", command.CommandId);
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to handle command {CommandId}", command.CommandId);
            throw;
        }
    }
}

public class QueryDispatcher : IQueryDispatcher
{
    private readonly IServiceProvider serviceProvider;
    private readonly ILogger logger;

    public QueryDispatcher(IServiceProvider serviceProvider, ILogger<QueryDispatcher> logger = null)
    {
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        this.logger = logger;
    }

    public async Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken token = default)
        where TQuery : IQuery<TResult>
    {
        var handler = serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResult>>();
        
        logger?.LogTrace("Dispatching query {QueryType} with ID {QueryId}",
            typeof(TQuery).Name, query.QueryId);

        try
        {
            var result = await handler.HandleAsync(query, token).ConfigureAwait(false);
            logger?.LogTrace("Successfully handled query {QueryId}", query.QueryId);
            return result;
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "Failed to handle query {QueryId}", query.QueryId);
            throw;
        }
    }
}

// Event projection system
public interface IEventProjection
{
    Task ProjectAsync(IEvent domainEvent, CancellationToken token = default);
    Task ResetAsync(CancellationToken token = default);
    string ProjectionName { get; }
}

public interface IProjectionManager
{
    Task ProjectEventAsync(IEvent domainEvent, CancellationToken token = default);
    Task RebuildProjectionAsync(string projectionName, CancellationToken token = default);
    Task RebuildAllProjectionsAsync(CancellationToken token = default);
    void RegisterProjection(IEventProjection projection);
}

public class ProjectionManager : IProjectionManager
{
    private readonly ConcurrentDictionary<string, IEventProjection> projections;
    private readonly IEventStore eventStore;
    private readonly ILogger logger;

    public ProjectionManager(IEventStore eventStore, ILogger<ProjectionManager> logger = null)
    {
        this.eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        this.logger = logger;
        projections = new ConcurrentDictionary<string, IEventProjection>();
    }

    public void RegisterProjection(IEventProjection projection)
    {
        projections[projection.ProjectionName] = projection;
        logger?.LogInformation("Registered projection: {ProjectionName}", projection.ProjectionName);
    }

    public async Task ProjectEventAsync(IEvent domainEvent, CancellationToken token = default)
    {
        var tasks = projections.Values.Select(async projection =>
        {
            try
            {
                await projection.ProjectAsync(domainEvent, token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Projection {ProjectionName} failed to process event {EventType}",
                    projection.ProjectionName, domainEvent.EventType);
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async Task RebuildProjectionAsync(string projectionName, CancellationToken token = default)
    {
        if (!projections.TryGetValue(projectionName, out var projection))
        {
            throw new InvalidOperationException($"Projection '{projectionName}' not found");
        }

        logger?.LogInformation("Rebuilding projection: {ProjectionName}", projectionName);

        // Reset the projection
        await projection.ResetAsync(token).ConfigureAwait(false);

        // Replay all events
        var events = await eventStore.GetAllEventsAsync(0, int.MaxValue, token).ConfigureAwait(false);
        
        foreach (var domainEvent in events)
        {
            await projection.ProjectAsync(domainEvent, token).ConfigureAwait(false);
        }

        logger?.LogInformation("Completed rebuilding projection: {ProjectionName}", projectionName);
    }

    public async Task RebuildAllProjectionsAsync(CancellationToken token = default)
    {
        logger?.LogInformation("Rebuilding all projections...");

        var rebuildTasks = projections.Keys.Select(name => RebuildProjectionAsync(name, token));
        await Task.WhenAll(rebuildTasks).ConfigureAwait(false);

        logger?.LogInformation("Completed rebuilding all projections");
    }
}

// Event serialization
public interface IEventSerializer
{
    string Serialize(object obj);
    IEvent Deserialize(string data, string eventType);
    T Deserialize&lt;T&gt;(string data);
}

public class JsonEventSerializer : IEventSerializer
{
    private readonly JsonSerializerOptions options;
    private readonly Dictionary<string, Type> eventTypes;

    public JsonEventSerializer()
    {
        options = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        eventTypes = new Dictionary<string, Type>();
        RegisterKnownEventTypes();
    }

    public string Serialize(object obj)
    {
        return JsonSerializer.Serialize(obj, options);
    }

    public IEvent Deserialize(string data, string eventType)
    {
        if (!eventTypes.TryGetValue(eventType, out var type))
        {
            throw new InvalidOperationException($"Unknown event type: {eventType}");
        }

        return (IEvent)JsonSerializer.Deserialize(data, type, options);
    }

    public T Deserialize&lt;T&gt;(string data)
    {
        return JsonSerializer.Deserialize&lt;T&gt;(data, options);
    }

    public void RegisterEventType&lt;T&gt;() where T : IEvent
    {
        eventTypes[typeof(T).Name] = typeof(T);
    }

    private void RegisterKnownEventTypes()
    {
        // Register common event types
        var eventTypes = Assembly.GetExecutingAssembly()
            .GetTypes()
            .Where(t => typeof(IEvent).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

        foreach (var type in eventTypes)
        {
            this.eventTypes[type.Name] = type;
        }
    }
}

// Snapshot strategy
public interface ISnapshotStrategy
{
    bool ShouldCreateSnapshot(IAggregateRoot aggregate);
}

public class SimpleSnapshotStrategy : ISnapshotStrategy
{
    private readonly int snapshotFrequency;

    public SimpleSnapshotStrategy(int snapshotFrequency = 10)
    {
        this.snapshotFrequency = snapshotFrequency;
    }

    public bool ShouldCreateSnapshot(IAggregateRoot aggregate)
    {
        return aggregate.Version > 0 && aggregate.Version % snapshotFrequency == 0;
    }
}

public class ConditionalSnapshotStrategy : ISnapshotStrategy
{
    private readonly Func<IAggregateRoot, bool> condition;

    public ConditionalSnapshotStrategy(Func<IAggregateRoot, bool> condition)
    {
        this.condition = condition ?? throw new ArgumentNullException(nameof(condition));
    }

    public bool ShouldCreateSnapshot(IAggregateRoot aggregate)
    {
        return condition(aggregate);
    }
}

// Event replay system
public class EventReplayService
{
    private readonly IEventStore eventStore;
    private readonly IProjectionManager projectionManager;
    private readonly ILogger logger;

    public EventReplayService(
        IEventStore eventStore,
        IProjectionManager projectionManager,
        ILogger<EventReplayService> logger = null)
    {
        this.eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        this.projectionManager = projectionManager ?? throw new ArgumentNullException(nameof(projectionManager));
        this.logger = logger;
    }

    public async Task ReplayEventsAsync(DateTime fromDate, DateTime toDate, 
        CancellationToken token = default)
    {
        logger?.LogInformation("Replaying events from {FromDate} to {ToDate}", fromDate, toDate);

        var events = await eventStore.GetAllEventsAsync(0, int.MaxValue, token).ConfigureAwait(false);
        
        var filteredEvents = events
            .Where(e => e.Timestamp >= fromDate && e.Timestamp <= toDate)
            .OrderBy(e => e.Timestamp);

        foreach (var domainEvent in filteredEvents)
        {
            await projectionManager.ProjectEventAsync(domainEvent, token).ConfigureAwait(false);
            logger?.LogTrace("Replayed event {EventType} at {Timestamp}", 
                domainEvent.EventType, domainEvent.Timestamp);
        }

        logger?.LogInformation("Completed event replay");
    }

    public async Task ReplayEventsForAggregateAsync(Guid aggregateId, 
        CancellationToken token = default)
    {
        logger?.LogInformation("Replaying events for aggregate {AggregateId}", aggregateId);

        var events = await eventStore.GetEventsAsync(aggregateId, 0, token).ConfigureAwait(false);
        
        foreach (var domainEvent in events.OrderBy(e => e.Version))
        {
            await projectionManager.ProjectEventAsync(domainEvent, token).ConfigureAwait(false);
            logger?.LogTrace("Replayed event {EventType} version {Version}", 
                domainEvent.EventType, domainEvent.Version);
        }

        logger?.LogInformation("Completed aggregate event replay");
    }
}

// Supporting classes and data structures
public class StoredEvent
{
    public Guid EventId { get; set; }
    public Guid AggregateId { get; set; }
    public string AggregateType { get; set; }
    public string EventType { get; set; }
    public string EventData { get; set; }
    public string Metadata { get; set; }
    public int Version { get; set; }
    public DateTime Timestamp { get; set; }
    public long GlobalPosition { get; set; }
}

public class StoredSnapshot : ISnapshot
{
    public Guid AggregateId { get; set; }
    public string AggregateType { get; set; }
    public int Version { get; set; }
    public string Data { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AggregateSnapshot : ISnapshot
{
    public Guid AggregateId { get; set; }
    public string AggregateType { get; set; }
    public int Version { get; set; }
    public string Data { get; set; }
    public DateTime CreatedAt { get; set; }
}

// Base command and query implementations
public abstract class Command : ICommand
{
    protected Command()
    {
        CommandId = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
    }

    public Guid CommandId { get; private set; }
    public DateTime Timestamp { get; private set; }
}

public abstract class Query<TResult> : IQuery<TResult>
{
    protected Query()
    {
        QueryId = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
    }

    public Guid QueryId { get; private set; }
    public DateTime Timestamp { get; private set; }
}

// Exception types
public class OptimisticConcurrencyException : Exception
{
    public OptimisticConcurrencyException(string message) : base(message) { }
    public OptimisticConcurrencyException(string message, Exception innerException) : base(message, innerException) { }
}

public class AggregateNotFoundException : Exception
{
    public AggregateNotFoundException(string message) : base(message) { }
    public AggregateNotFoundException(string message, Exception innerException) : base(message, innerException) { }
}

// Interface definitions for dependency injection
public interface IEventSourcedRepository<TAggregate> where TAggregate : IAggregateRoot
{
    Task<TAggregate> GetByIdAsync(Guid id, CancellationToken token = default);
    Task SaveAsync(TAggregate aggregate, CancellationToken token = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken token = default);
}

public interface ICommandDispatcher
{
    Task DispatchAsync<TCommand>(TCommand command, CancellationToken token = default) where TCommand : ICommand;
}

public interface IQueryDispatcher
{
    Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken token = default) where TQuery : IQuery<TResult>;
}
```

**Usage**:

```csharp
// Example 1: Basic Event Sourcing Setup
Console.WriteLine("Basic Event Sourcing Examples:");

// Set up services
var services = new ServiceCollection()
    .AddLogging(builder => builder.AddConsole())
    .AddSingleton<IEventSerializer, JsonEventSerializer>()
    .AddSingleton<IEventStore, InMemoryEventStore>()
    .AddSingleton<ISnapshotStrategy, SimpleSnapshotStrategy>()
    .AddSingleton<IProjectionManager, ProjectionManager>()
    .AddSingleton<ICommandDispatcher, CommandDispatcher>()
    .AddSingleton<IQueryDispatcher, QueryDispatcher>()
    .AddTransient<IEventSourcedRepository<BankAccount>, EventSourcedRepository<BankAccount>>()
    .AddTransient<ICommandHandler<CreateBankAccountCommand>, BankAccountCommandHandler>()
    .AddTransient<ICommandHandler<DepositMoneyCommand>, BankAccountCommandHandler>()
    .AddTransient<ICommandHandler<WithdrawMoneyCommand>, BankAccountCommandHandler>()
    .AddTransient<IQueryHandler<GetBankAccountQuery, BankAccountView>, BankAccountQueryHandler>()
    .BuildServiceProvider();

var eventStore = services.GetRequiredService<IEventStore>();
var repository = services.GetRequiredService<IEventSourcedRepository<BankAccount>>();
var commandDispatcher = services.GetRequiredService<ICommandDispatcher>();
var queryDispatcher = services.GetRequiredService<IQueryDispatcher>();

// Example 2: Domain Events and Aggregate
Console.WriteLine("\nDomain Events and Aggregate Examples:");

// Define domain events for bank account
public class BankAccountCreatedEvent : DomainEvent
{
    public string AccountNumber { get; set; }
    public string CustomerName { get; set; }
    public decimal InitialBalance { get; set; }
}

public class MoneyDepositedEvent : DomainEvent
{
    public decimal Amount { get; set; }
    public decimal NewBalance { get; set; }
    public string Description { get; set; }
}

public class MoneyWithdrawnEvent : DomainEvent
{
    public decimal Amount { get; set; }
    public decimal NewBalance { get; set; }
    public string Description { get; set; }
}

// Bank account aggregate
public class BankAccount : AggregateRoot
{
    public string AccountNumber { get; private set; }
    public string CustomerName { get; private set; }
    public decimal Balance { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public bool IsActive { get; private set; } = true;

    // Required parameterless constructor for repository
    public BankAccount() { }

    // Factory method to create new account
    public static BankAccount Create(string accountNumber, string customerName, decimal initialBalance)
    {
        var account = new BankAccount();
        
        var createdEvent = new BankAccountCreatedEvent
        {
            AccountNumber = accountNumber,
            CustomerName = customerName,
            InitialBalance = initialBalance
        };
        
        account.RaiseEvent(createdEvent);
        return account;
    }

    public void Deposit(decimal amount, string description = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Deposit amount must be positive");
        
        if (!IsActive)
            throw new InvalidOperationException("Cannot deposit to inactive account");

        var newBalance = Balance + amount;
        
        var depositEvent = new MoneyDepositedEvent
        {
            Amount = amount,
            NewBalance = newBalance,
            Description = description ?? $"Deposit of ${amount}"
        };
        
        RaiseEvent(depositEvent);
    }

    public void Withdraw(decimal amount, string description = null)
    {
        if (amount <= 0)
            throw new ArgumentException("Withdrawal amount must be positive");
        
        if (!IsActive)
            throw new InvalidOperationException("Cannot withdraw from inactive account");
            
        if (amount > Balance)
            throw new InvalidOperationException("Insufficient funds");

        var newBalance = Balance - amount;
        
        var withdrawEvent = new MoneyWithdrawnEvent
        {
            Amount = amount,
            NewBalance = newBalance,
            Description = description ?? $"Withdrawal of ${amount}"
        };
        
        RaiseEvent(withdrawEvent);
    }

    // Event handlers
    private void ApplyBankAccountCreatedEvent(BankAccountCreatedEvent evt)
    {
        Id = evt.AggregateId;
        AccountNumber = evt.AccountNumber;
        CustomerName = evt.CustomerName;
        Balance = evt.InitialBalance;
        CreatedAt = evt.Timestamp;
    }

    private void ApplyMoneyDepositedEvent(MoneyDepositedEvent evt)
    {
        Balance = evt.NewBalance;
    }

    private void ApplyMoneyWithdrawnEvent(MoneyWithdrawnEvent evt)
    {
        Balance = evt.NewBalance;
    }

    protected override void RegisterEventHandlers()
    {
        RegisterEventHandler<BankAccountCreatedEvent>(ApplyBankAccountCreatedEvent);
        RegisterEventHandler<MoneyDepositedEvent>(ApplyMoneyDepositedEvent);
        RegisterEventHandler<MoneyWithdrawnEvent>(ApplyMoneyWithdrawnEvent);
    }
}

// Example 3: CQRS Commands and Queries
Console.WriteLine("\nCQRS Commands and Queries Examples:");

// Commands
public class CreateBankAccountCommand : Command
{
    public string AccountNumber { get; set; }
    public string CustomerName { get; set; }
    public decimal InitialBalance { get; set; }
}

public class DepositMoneyCommand : Command
{
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; }
}

public class WithdrawMoneyCommand : Command
{
    public Guid AccountId { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; }
}

// Query
public class GetBankAccountQuery : Query<BankAccountView>
{
    public Guid AccountId { get; set; }
}

// View model for queries
public class BankAccountView
{
    public Guid Id { get; set; }
    public string AccountNumber { get; set; }
    public string CustomerName { get; set; }
    public decimal Balance { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
    public int TransactionCount { get; set; }
}

// Command handler
public class BankAccountCommandHandler : 
    ICommandHandler<CreateBankAccountCommand>,
    ICommandHandler<DepositMoneyCommand>,
    ICommandHandler<WithdrawMoneyCommand>
{
    private readonly IEventSourcedRepository<BankAccount> repository;

    public BankAccountCommandHandler(IEventSourcedRepository<BankAccount> repository)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task HandleAsync(CreateBankAccountCommand command, CancellationToken token = default)
    {
        var account = BankAccount.Create(command.AccountNumber, command.CustomerName, command.InitialBalance);
        await repository.SaveAsync(account, token);
    }

    public async Task HandleAsync(DepositMoneyCommand command, CancellationToken token = default)
    {
        var account = await repository.GetByIdAsync(command.AccountId, token);
        if (account == null)
            throw new AggregateNotFoundException($"Bank account {command.AccountId} not found");

        account.Deposit(command.Amount, command.Description);
        await repository.SaveAsync(account, token);
    }

    public async Task HandleAsync(WithdrawMoneyCommand command, CancellationToken token = default)
    {
        var account = await repository.GetByIdAsync(command.AccountId, token);
        if (account == null)
            throw new AggregateNotFoundException($"Bank account {command.AccountId} not found");

        account.Withdraw(command.Amount, command.Description);
        await repository.SaveAsync(account, token);
    }
}

// Query handler
public class BankAccountQueryHandler : IQueryHandler<GetBankAccountQuery, BankAccountView>
{
    private readonly IEventSourcedRepository<BankAccount> repository;

    public BankAccountQueryHandler(IEventSourcedRepository<BankAccount> repository)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    public async Task<BankAccountView> HandleAsync(GetBankAccountQuery query, CancellationToken token = default)
    {
        var account = await repository.GetByIdAsync(query.AccountId, token);
        if (account == null) return null;

        return new BankAccountView
        {
            Id = account.Id,
            AccountNumber = account.AccountNumber,
            CustomerName = account.CustomerName,
            Balance = account.Balance,
            CreatedAt = account.CreatedAt,
            IsActive = account.IsActive,
            TransactionCount = account.Version
        };
    }
}

// Example usage
var accountId = Guid.NewGuid();

// Create account
var createCommand = new CreateBankAccountCommand
{
    AccountNumber = "ACC-001",
    CustomerName = "John Doe",
    InitialBalance = 1000m
};

await commandDispatcher.DispatchAsync(createCommand);
Console.WriteLine("Created bank account");

// Deposit money
var depositCommand = new DepositMoneyCommand
{
    AccountId = accountId,
    Amount = 500m,
    Description = "Salary deposit"
};

await commandDispatcher.DispatchAsync(depositCommand);
Console.WriteLine("Deposited $500");

// Withdraw money
var withdrawCommand = new WithdrawMoneyCommand
{
    AccountId = accountId,
    Amount = 200m,
    Description = "ATM withdrawal"
};

await commandDispatcher.DispatchAsync(withdrawCommand);
Console.WriteLine("Withdrew $200");

// Query account
var query = new GetBankAccountQuery { AccountId = accountId };
var accountView = await queryDispatcher.DispatchAsync<GetBankAccountQuery, BankAccountView>(query);

Console.WriteLine($"Account: {accountView.AccountNumber}");
Console.WriteLine($"Customer: {accountView.CustomerName}");
Console.WriteLine($"Balance: ${accountView.Balance}");
Console.WriteLine($"Transactions: {accountView.TransactionCount}");

// Example 4: Event Projections
Console.WriteLine("\nEvent Projection Examples:");

// Account summary projection
public class AccountSummaryProjection : IEventProjection
{
    private readonly ConcurrentDictionary<Guid, AccountSummary> summaries;

    public AccountSummaryProjection()
    {
        summaries = new ConcurrentDictionary<Guid, AccountSummary>();
    }

    public string ProjectionName => "AccountSummary";

    public Task ProjectAsync(IEvent domainEvent, CancellationToken token = default)
    {
        switch (domainEvent)
        {
            case BankAccountCreatedEvent created:
                summaries[created.AggregateId] = new AccountSummary
                {
                    AccountId = created.AggregateId,
                    AccountNumber = created.AccountNumber,
                    CustomerName = created.CustomerName,
                    CurrentBalance = created.InitialBalance,
                    TotalDeposits = created.InitialBalance,
                    TotalWithdrawals = 0,
                    TransactionCount = 1,
                    LastTransactionDate = created.Timestamp
                };
                break;

            case MoneyDepositedEvent deposited:
                summaries.AddOrUpdate(deposited.AggregateId, 
                    new AccountSummary(), 
                    (id, summary) =>
                    {
                        summary.CurrentBalance = deposited.NewBalance;
                        summary.TotalDeposits += deposited.Amount;
                        summary.TransactionCount++;
                        summary.LastTransactionDate = deposited.Timestamp;
                        return summary;
                    });
                break;

            case MoneyWithdrawnEvent withdrawn:
                summaries.AddOrUpdate(withdrawn.AggregateId,
                    new AccountSummary(),
                    (id, summary) =>
                    {
                        summary.CurrentBalance = withdrawn.NewBalance;
                        summary.TotalWithdrawals += withdrawn.Amount;
                        summary.TransactionCount++;
                        summary.LastTransactionDate = withdrawn.Timestamp;
                        return summary;
                    });
                break;
        }

        return Task.CompletedTask;
    }

    public Task ResetAsync(CancellationToken token = default)
    {
        summaries.Clear();
        return Task.CompletedTask;
    }

    public AccountSummary GetSummary(Guid accountId)
    {
        summaries.TryGetValue(accountId, out var summary);
        return summary;
    }

    public IEnumerable<AccountSummary> GetAllSummaries()
    {
        return summaries.Values.ToList();
    }
}

public class AccountSummary
{
    public Guid AccountId { get; set; }
    public string AccountNumber { get; set; }
    public string CustomerName { get; set; }
    public decimal CurrentBalance { get; set; }
    public decimal TotalDeposits { get; set; }
    public decimal TotalWithdrawals { get; set; }
    public int TransactionCount { get; set; }
    public DateTime LastTransactionDate { get; set; }
}

// Register and use projection
var projectionManager = services.GetRequiredService<IProjectionManager>();
var accountSummaryProjection = new AccountSummaryProjection();
projectionManager.RegisterProjection(accountSummaryProjection);

// Example 5: Event Replay and Snapshots
Console.WriteLine("\nEvent Replay and Snapshot Examples:");

var replayService = new EventReplayService(eventStore, projectionManager);

// Replay events from the last week
await replayService.ReplayEventsAsync(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow);
Console.WriteLine("Replayed events from last week");

// Replay events for specific aggregate
await replayService.ReplayEventsForAggregateAsync(accountId);
Console.WriteLine($"Replayed events for account {accountId}");

// Rebuild all projections
await projectionManager.RebuildAllProjectionsAsync();
Console.WriteLine("Rebuilt all projections");

Console.WriteLine("\nEvent sourcing pattern examples completed!");
```

**Notes**:

- Implement comprehensive event sourcing patterns for building scalable and auditable systems
- Use aggregate root pattern to encapsulate business logic and maintain consistency boundaries
- Implement CQRS (Command Query Responsibility Segregation) for separating read and write operations
- Use event store for persistent event storage with optimistic concurrency control
- Implement snapshot strategies for performance optimization with large event streams
- Use projection system for building read models from event streams
- Implement event replay capabilities for rebuilding projections and recovering from failures
- Use proper event serialization with versioning support for schema evolution
- Implement command and query dispatchers for clean separation of concerns
- Use dependency injection for loose coupling and testability
- Handle concurrency conflicts with appropriate exception handling strategies
- Implement proper event metadata for tracking and auditing purposes
- Use background services for projection updates and maintenance operations

**Prerequisites**:

- Understanding of Domain-Driven Design (DDD) and aggregate patterns
- Knowledge of CQRS (Command Query Responsibility Segregation) architecture
- Familiarity with event-driven architectures and eventual consistency
- Experience with serialization and data persistence patterns
- Understanding of optimistic concurrency control and conflict resolution
- Knowledge of projection patterns and read model maintenance

**Related Snippets**:

- [Message Queue](message-queue.md) - Message queuing patterns for event distribution
- [Pub-Sub Pattern](pub-sub.md) - Publisher-subscriber patterns for event notification
- [Saga Patterns](saga-patterns.md) - Distributed transaction coordination patterns
