namespace CSharp.EventSourcing;

/// <summary>
/// Base interface for all queries in the system
/// </summary>
public interface IQuery<out TResult>
{
    /// <summary>
    /// Unique identifier for the query
    /// </summary>
    Guid QueryId { get; }
    
    /// <summary>
    /// Timestamp when the query was created
    /// </summary>
    DateTime Timestamp { get; }
}