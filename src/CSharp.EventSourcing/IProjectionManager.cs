namespace CSharp.EventSourcing;

/// <summary>
/// Interface for managing event projections
/// </summary>
public interface IProjectionManager
{
    /// <summary>
    /// Project an event to all registered projections
    /// </summary>
    Task ProjectEventAsync(IEvent domainEvent, CancellationToken token = default);
    
    /// <summary>
    /// Rebuild a specific projection by replaying all events
    /// </summary>
    Task RebuildProjectionAsync(string projectionName, CancellationToken token = default);
    
    /// <summary>
    /// Rebuild all registered projections by replaying all events
    /// </summary>
    Task RebuildAllProjectionsAsync(CancellationToken token = default);
    
    /// <summary>
    /// Register a projection with the manager
    /// </summary>
    void RegisterProjection(IEventProjection projection);
}