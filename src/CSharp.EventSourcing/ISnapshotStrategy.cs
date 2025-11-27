namespace CSharp.EventSourcing;

/// <summary>
/// Interface for snapshot creation strategies
/// </summary>
public interface ISnapshotStrategy
{
    /// <summary>
    /// Determine if a snapshot should be created for the given aggregate
    /// </summary>
    bool ShouldCreateSnapshot(IAggregateRoot aggregate);
}