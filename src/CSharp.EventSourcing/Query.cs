namespace CSharp.EventSourcing;

/// <summary>
/// Base abstract class for queries
/// </summary>
public abstract class Query<TResult> : IQuery<TResult>
{
    protected Query()
    {
        QueryId = Guid.NewGuid();
        Timestamp = DateTime.UtcNow;
    }

    public Guid QueryId { get; private set; }
    public DateTime Timestamp { get; private set; }
}