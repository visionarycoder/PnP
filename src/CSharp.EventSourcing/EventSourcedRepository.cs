using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CSharp.EventSourcing;

/// <summary>
/// Repository implementation for event sourced aggregates
/// </summary>
public class EventSourcedRepository<TAggregate> : IEventSourcedRepository<TAggregate> 
    where TAggregate : class, IAggregateRoot, new()
{
    private readonly IEventStore eventStore;
    private readonly ISnapshotStrategy snapshotStrategy;
    private readonly ILogger? logger;

    public EventSourcedRepository(
        IEventStore eventStore,
        ISnapshotStrategy? snapshotStrategy = null,
        ILogger<EventSourcedRepository<TAggregate>>? logger = null)
    {
        this.eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        this.snapshotStrategy = snapshotStrategy ?? new SimpleSnapshotStrategy();
        this.logger = logger;
    }

    public async Task<TAggregate?> GetByIdAsync(Guid id, CancellationToken token = default)
    {
        var aggregate = new TAggregate();
        aggregate.SetId(id);
        
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