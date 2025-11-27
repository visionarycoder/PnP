namespace CSharp.EventSourcing;

/// <summary>
/// Interface for event projections that can handle events and build read models
/// </summary>
public interface IEventProjection
{
    /// <summary>
    /// Project an event to update the read model
    /// </summary>
    Task ProjectAsync(IEvent domainEvent, CancellationToken token = default);
    
    /// <summary>
    /// Reset the projection by clearing all data
    /// </summary>
    Task ResetAsync(CancellationToken token = default);
    
    /// <summary>
    /// Name of the projection for identification
    /// </summary>
    string ProjectionName { get; }
}