namespace CSharp.EventSourcing;

/// <summary>
/// Simple snapshot strategy that creates snapshots every N events
/// </summary>
public class SimpleSnapshotStrategy : ISnapshotStrategy
{
    private readonly int snapshotFrequency;

    public SimpleSnapshotStrategy(int snapshotFrequency = 10)
    {
        this.snapshotFrequency = snapshotFrequency;
    }

    public bool ShouldCreateSnapshot(IAggregateRoot aggregate)
    {
        return aggregate.Version > 0 && aggregate.Version % snapshotFrequency == 0;
    }
}