namespace CSharp.EventSourcing;

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