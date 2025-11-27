namespace CSharp.EventSourcing;

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