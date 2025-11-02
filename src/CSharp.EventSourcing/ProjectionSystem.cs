using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

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

/// <summary>
/// Implementation of projection manager for handling event projections
/// </summary>
public class ProjectionManager : IProjectionManager
{
    private readonly ConcurrentDictionary<string, IEventProjection> projections;
    private readonly IEventStore eventStore;
    private readonly ILogger? logger;

    public ProjectionManager(IEventStore eventStore, ILogger<ProjectionManager>? logger = null)
    {
        this.eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        this.logger = logger;
        projections = new ConcurrentDictionary<string, IEventProjection>();
    }

    public void RegisterProjection(IEventProjection projection)
    {
        projections[projection.ProjectionName] = projection;
        logger?.LogInformation("Registered projection: {ProjectionName}", projection.ProjectionName);
    }

    public async Task ProjectEventAsync(IEvent domainEvent, CancellationToken token = default)
    {
        var tasks = projections.Values.Select(async projection =>
        {
            try
            {
                await projection.ProjectAsync(domainEvent, token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Projection {ProjectionName} failed to process event {EventType}",
                    projection.ProjectionName, domainEvent.EventType);
            }
        });

        await Task.WhenAll(tasks).ConfigureAwait(false);
    }

    public async Task RebuildProjectionAsync(string projectionName, CancellationToken token = default)
    {
        if (!projections.TryGetValue(projectionName, out var projection))
        {
            throw new InvalidOperationException($"Projection '{projectionName}' not found");
        }

        logger?.LogInformation("Rebuilding projection: {ProjectionName}", projectionName);

        // Reset the projection
        await projection.ResetAsync(token).ConfigureAwait(false);

        // Replay all events
        var events = await eventStore.GetAllEventsAsync(0, int.MaxValue, token).ConfigureAwait(false);
        
        foreach (var domainEvent in events)
        {
            await projection.ProjectAsync(domainEvent, token).ConfigureAwait(false);
        }

        logger?.LogInformation("Completed rebuilding projection: {ProjectionName}", projectionName);
    }

    public async Task RebuildAllProjectionsAsync(CancellationToken token = default)
    {
        logger?.LogInformation("Rebuilding all projections...");

        var rebuildTasks = projections.Keys.Select(name => RebuildProjectionAsync(name, token));
        await Task.WhenAll(rebuildTasks).ConfigureAwait(false);

        logger?.LogInformation("Completed rebuilding all projections");
    }
}

/// <summary>
/// Interface for event replay functionality
/// </summary>
public interface IEventReplayService
{
    /// <summary>
    /// Replay events within a date range to projections
    /// </summary>
    Task ReplayEventsAsync(DateTime fromDate, DateTime toDate, CancellationToken token = default);
    
    /// <summary>
    /// Replay events from a specific position
    /// </summary>
    Task ReplayEventsFromPositionAsync(int fromPosition, int maxCount = 1000, CancellationToken token = default);
}

/// <summary>
/// Service for replaying events to rebuild projections or recover from failures
/// </summary>
public class EventReplayService : IEventReplayService
{
    private readonly IEventStore eventStore;
    private readonly IProjectionManager projectionManager;
    private readonly ILogger? logger;

    public EventReplayService(
        IEventStore eventStore,
        IProjectionManager projectionManager,
        ILogger<EventReplayService>? logger = null)
    {
        this.eventStore = eventStore ?? throw new ArgumentNullException(nameof(eventStore));
        this.projectionManager = projectionManager ?? throw new ArgumentNullException(nameof(projectionManager));
        this.logger = logger;
    }

    public async Task ReplayEventsAsync(DateTime fromDate, DateTime toDate, CancellationToken token = default)
    {
        logger?.LogInformation("Replaying events from {FromDate} to {ToDate}", fromDate, toDate);

        var events = await eventStore.GetAllEventsAsync(0, int.MaxValue, token).ConfigureAwait(false);
        var filteredEvents = events
            .Where(e => e.Timestamp >= fromDate && e.Timestamp <= toDate)
            .OrderBy(e => e.Timestamp);

        var eventCount = 0;
        foreach (var domainEvent in filteredEvents)
        {
            await projectionManager.ProjectEventAsync(domainEvent, token).ConfigureAwait(false);
            eventCount++;
        }

        logger?.LogInformation("Replayed {EventCount} events", eventCount);
    }

    public async Task ReplayEventsFromPositionAsync(int fromPosition, int maxCount = 1000, CancellationToken token = default)
    {
        logger?.LogInformation("Replaying events from position {FromPosition}, max count: {MaxCount}", fromPosition, maxCount);

        var events = await eventStore.GetAllEventsAsync(fromPosition, maxCount, token).ConfigureAwait(false);
        
        var eventCount = 0;
        foreach (var domainEvent in events)
        {
            await projectionManager.ProjectEventAsync(domainEvent, token).ConfigureAwait(false);
            eventCount++;
        }

        logger?.LogInformation("Replayed {EventCount} events", eventCount);
    }
}