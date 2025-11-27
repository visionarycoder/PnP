namespace CSharp.EventSourcing;

/// <summary>
/// Internal class for storing snapshots
/// </summary>
internal class StoredSnapshot : ISnapshot
{
    public Guid AggregateId { get; init; }
    public int Version { get; init; }
    public DateTime CreatedAt { get; init; }
    public string Data { get; init; } = string.Empty;
    public string AggregateType { get; init; } = string.Empty;
}