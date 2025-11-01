namespace Snippets.DesignPatterns.Behavioral.Iterator;

public class FilterIterator<T>(IList<T> items, Func<T, bool> predicate) : IIterator<T>
{
    private readonly IList<T> items = items ?? throw new ArgumentNullException(nameof(items));
    private readonly Func<T, bool> predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
    private int position = -1;

    public bool HasNext()
    {
        for (int i = position + 1; i < items.Count; i++)
        {
            if (predicate(items[i]))
            {
                return true;
            }
        }

        return false;
    }

    public T Next()
    {
        for (int i = position + 1; i < items.Count; i++)
        {
            if (predicate(items[i]))
            {
                position = i;
                Current = items[i];
                return Current;
            }
        }

        throw new InvalidOperationException("No more elements matching the filter");
    }

    public void Reset()
    {
        position = -1;
        Current = default(T)!;
    }

    public T Current { get; private set; } = default(T)!;
}