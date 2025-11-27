namespace CSharp.EventSourcing;

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