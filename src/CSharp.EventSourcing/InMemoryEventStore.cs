using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CSharp.EventSourcing;

/// <summary>
/// In-memory implementation of event store for development and testing
/// </summary>
public class InMemoryEventStore : IEventStore
{
    private readonly ConcurrentDictionary<Guid, List<StoredEvent>> eventStreams;
    private readonly ConcurrentDictionary<Guid, StoredSnapshot> snapshots;
    private readonly List<StoredEvent> globalEventLog;
    private readonly IEventSerializer eventSerializer;
    private readonly ILogger? logger;
    private readonly object lockObject = new();
    private long globalPosition = 0;

    public InMemoryEventStore(
        IEventSerializer? eventSerializer = null,
        ILogger<InMemoryEventStore>? logger = null)
    {
        eventStreams = new ConcurrentDictionary<Guid, List<StoredEvent>>();
        snapshots = new();
        globalEventLog = new();
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
            .Cast<IEvent>()
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
                .Cast<IEvent>()
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

    public Task<ISnapshot?> GetLatestSnapshotAsync(Guid aggregateId, CancellationToken token = default)
    {
        snapshots.TryGetValue(aggregateId, out var snapshot);
        return Task.FromResult<ISnapshot?>(snapshot);
    }

    private IEvent? DeserializeEvent(StoredEvent storedEvent)
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