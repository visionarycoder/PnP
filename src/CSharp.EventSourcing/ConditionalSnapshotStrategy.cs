namespace CSharp.EventSourcing;

/// <summary>
/// Conditional snapshot strategy based on a predicate function
/// </summary>
public class ConditionalSnapshotStrategy : ISnapshotStrategy
{
    private readonly Func<IAggregateRoot, bool> condition;

    public ConditionalSnapshotStrategy(Func<IAggregateRoot, bool> condition)
    {
        this.condition = condition ?? throw new ArgumentNullException(nameof(condition));
    }

    public bool ShouldCreateSnapshot(IAggregateRoot aggregate)
    {
        return condition(aggregate);
    }
}