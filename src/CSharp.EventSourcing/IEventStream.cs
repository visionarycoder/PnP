namespace CSharp.EventSourcing;

/// <summary>
/// Interface for event streams
/// </summary>
public interface IEventStream : IAsyncEnumerable<IEvent>
{
    /// <summary>
    /// ID of the stream (aggregate ID)
    /// </summary>
    Guid StreamId { get; }
    
    /// <summary>
    /// Current version of the stream
    /// </summary>
    int CurrentVersion { get; }
    
    /// <summary>
    /// Check if there are more events available
    /// </summary>
    Task<bool> HasMoreEventsAsync(CancellationToken token = default);
}