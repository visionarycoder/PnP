namespace CSharp.EventSourcing;

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