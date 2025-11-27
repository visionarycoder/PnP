namespace CSharp.EventSourcing;

/// <summary>
/// Internal class for storing events with metadata
/// </summary>
internal class StoredEvent
{
    public Guid EventId { get; init; }
    public Guid AggregateId { get; init; }
    public string AggregateType { get; init; } = string.Empty;
    public string EventType { get; init; } = string.Empty;
    public string EventData { get; init; } = string.Empty;
    public string Metadata { get; init; } = string.Empty;
    public int Version { get; init; }
    public DateTime Timestamp { get; init; }
    public long GlobalPosition { get; init; }
}