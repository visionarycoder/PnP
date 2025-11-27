using Microsoft.Extensions.Logging;

namespace CSharp.EventSourcing;

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