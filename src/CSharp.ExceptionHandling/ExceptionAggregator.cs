namespace CSharp.ExceptionHandling;

/// <summary>
/// Exception aggregator for collecting multiple exceptions during batch operations.
/// </summary>
public class ExceptionAggregator
{
    private readonly List<Exception> exceptions = new();
    private readonly object lockObject = new();

    public bool HasExceptions => exceptions.Count > 0;
    public int ExceptionCount => exceptions.Count;
    public IReadOnlyList<Exception> Exceptions => exceptions.AsReadOnly();

    public void Add(Exception exception)
    {
        if (exception == null) return;

        lock (lockObject)
        {
            exceptions.Add(exception);
        }
    }

    public void AddRange(IEnumerable<Exception> exceptions)
    {
        if (exceptions == null) return;

        lock (lockObject)
        {
            this.exceptions.AddRange(exceptions.Where(ex => ex != null));
        }
    }

    public void ThrowIfAny()
    {
        if (HasExceptions)
        {
            throw new AggregateException("One or more errors occurred during batch operation.", exceptions);
        }
    }

    public AggregateException? ToAggregateException()
    {
        return HasExceptions ? new AggregateException(exceptions) : null;
    }

    public void Clear()
    {
        lock (lockObject)
        {
            exceptions.Clear();
        }
    }
}