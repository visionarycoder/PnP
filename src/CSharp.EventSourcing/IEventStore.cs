namespace CSharp.EventSourcing;

/// <summary>
/// Interface for event store implementations
/// </summary>
public interface IEventStore
{
    /// <summary>
    /// Save events to the store with optimistic concurrency control
    /// </summary>
    Task SaveEventsAsync(Guid aggregateId, IEnumerable<IEvent> events, int expectedVersion, CancellationToken token = default);
    
    /// <summary>
    /// Get events for a specific aggregate from a given version
    /// </summary>
    Task<IEnumerable<IEvent>> GetEventsAsync(Guid aggregateId, int fromVersion = 0, CancellationToken token = default);
    
    /// <summary>
    /// Get all events in the system from a given position
    /// </summary>
    Task<IEnumerable<IEvent>> GetAllEventsAsync(int fromPosition = 0, int maxCount = 1000, CancellationToken token = default);
    
    /// <summary>
    /// Get an event stream for a specific aggregate
    /// </summary>
    Task<IEventStream> GetEventStreamAsync(Guid aggregateId, CancellationToken token = default);
    
    /// <summary>
    /// Get the current version of an aggregate
    /// </summary>
    Task<int> GetCurrentVersionAsync(Guid aggregateId, CancellationToken token = default);
    
    /// <summary>
    /// Create a snapshot of an aggregate
    /// </summary>
    Task CreateSnapshotAsync(Guid aggregateId, ISnapshot snapshot, CancellationToken token = default);
    
    /// <summary>
    /// Get the latest snapshot for an aggregate
    /// </summary>
    Task<ISnapshot?> GetLatestSnapshotAsync(Guid aggregateId, CancellationToken token = default);
}