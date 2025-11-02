using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CSharp.EventSourcing;

/// <summary>
/// Base interface for all events in the system
/// </summary>
public interface IEvent
{
    /// <summary>
    /// Unique identifier for the event
    /// </summary>
    Guid EventId { get; }
    
    /// <summary>
    /// Timestamp when the event was created
    /// </summary>
    DateTime Timestamp { get; }
    
    /// <summary>
    /// Version of the event within its aggregate
    /// </summary>
    int Version { get; set; }
    
    /// <summary>
    /// Type name of the event
    /// </summary>
    string EventType { get; }
    
    /// <summary>
    /// Additional metadata associated with the event
    /// </summary>
    IDictionary<string, object> Metadata { get; }
}

/// <summary>
/// Domain event interface with aggregate information
/// </summary>
public interface IDomainEvent : IEvent
{
    /// <summary>
    /// ID of the aggregate that generated this event
    /// </summary>
    Guid AggregateId { get; set; }
    
    /// <summary>
    /// Type name of the aggregate
    /// </summary>
    string AggregateType { get; set; }
}

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

/// <summary>
/// Interface for aggregate root objects
/// </summary>
public interface IAggregateRoot
{
    /// <summary>
    /// Unique identifier for the aggregate
    /// </summary>
    Guid Id { get; }
    
    /// <summary>
    /// Current version of the aggregate
    /// </summary>
    int Version { get; }
    
    /// <summary>
    /// Events that have been raised but not yet committed
    /// </summary>
    IEnumerable<IEvent> UncommittedEvents { get; }
    
    /// <summary>
    /// Mark all uncommitted events as committed
    /// </summary>
    void MarkEventsAsCommitted();
    
    /// <summary>
    /// Load the aggregate state from historical events
    /// </summary>
    void LoadFromHistory(IEnumerable<IEvent> events);
    
    /// <summary>
    /// Set the aggregate ID (used during reconstruction)
    /// </summary>
    void SetId(Guid id);
}

/// <summary>
/// Interface for aggregate snapshots
/// </summary>
public interface ISnapshot
{
    /// <summary>
    /// ID of the aggregate this snapshot represents
    /// </summary>
    Guid AggregateId { get; }
    
    /// <summary>
    /// Version of the aggregate at snapshot time
    /// </summary>
    int Version { get; }
    
    /// <summary>
    /// Timestamp when the snapshot was created
    /// </summary>
    DateTime CreatedAt { get; }
    
    /// <summary>
    /// Serialized data of the aggregate state
    /// </summary>
    string Data { get; }
    
    /// <summary>
    /// Type name of the aggregate
    /// </summary>
    string AggregateType { get; }
}

/// <summary>
/// Interface for event streams
/// </summary>
public interface IEventStream : IAsyncEnumerable<IEvent>
{
    /// <summary>
    /// ID of the stream (aggregate ID)
    /// </summary>
    Guid StreamId { get; }
    
    /// <summary>
    /// Current version of the stream
    /// </summary>
    int CurrentVersion { get; }
    
    /// <summary>
    /// Check if there are more events available
    /// </summary>
    Task<bool> HasMoreEventsAsync(CancellationToken token = default);
}

/// <summary>
/// Repository interface for event sourced aggregates
/// </summary>
public interface IEventSourcedRepository<TAggregate> where TAggregate : class, IAggregateRoot
{
    /// <summary>
    /// Get an aggregate by its ID
    /// </summary>
    Task<TAggregate?> GetByIdAsync(Guid id, CancellationToken token = default);
    
    /// <summary>
    /// Save an aggregate with its uncommitted events
    /// </summary>
    Task SaveAsync(TAggregate aggregate, CancellationToken token = default);
    
    /// <summary>
    /// Check if an aggregate exists
    /// </summary>
    Task<bool> ExistsAsync(Guid id, CancellationToken token = default);
}

/// <summary>
/// Interface for snapshot creation strategies
/// </summary>
public interface ISnapshotStrategy
{
    /// <summary>
    /// Determine if a snapshot should be created for the given aggregate
    /// </summary>
    bool ShouldCreateSnapshot(IAggregateRoot aggregate);
}

/// <summary>
/// Interface for event serialization
/// </summary>
public interface IEventSerializer
{
    /// <summary>
    /// Serialize an object to string
    /// </summary>
    string Serialize(object obj);
    
    /// <summary>
    /// Deserialize an event from string data
    /// </summary>
    IEvent Deserialize(string data, string eventType);
    
    /// <summary>
    /// Deserialize a typed object from string data
    /// </summary>
    T Deserialize<T>(string data);
}

/// <summary>
/// Exception thrown when optimistic concurrency check fails
/// </summary>
public class OptimisticConcurrencyException : Exception
{
    public OptimisticConcurrencyException(string message) : base(message) { }
    public OptimisticConcurrencyException(string message, Exception innerException) : base(message, innerException) { }
}