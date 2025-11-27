namespace CSharp.EventSourcing;

/// <summary>
/// Default implementation of ISnapshot
/// </summary>
public class AggregateSnapshot : ISnapshot
{
    public Guid AggregateId { get; init; }
    public int Version { get; init; }
    public DateTime CreatedAt { get; init; }
    public string Data { get; init; } = string.Empty;
    public string AggregateType { get; init; } = string.Empty;
}