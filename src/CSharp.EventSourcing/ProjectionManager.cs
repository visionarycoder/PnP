using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CSharp.EventSourcing;

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