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