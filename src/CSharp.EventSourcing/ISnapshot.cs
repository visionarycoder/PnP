namespace CSharp.EventSourcing;

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