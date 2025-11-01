namespace Snippets.DesignPatterns.Behavioral.Iterator;

public class PaginationIterator<T>(IList<T> items, int pageSize) : IIterator<IEnumerable<T>>
{
    private readonly IList<T> items = items ?? throw new ArgumentNullException(nameof(items));
    private readonly int pageSize = pageSize > 0
        ? pageSize
        : throw new ArgumentException("Page size must be positive", nameof(pageSize));
    private int currentPage = -1;

    public bool HasNext()
    {
        return (currentPage + 1) * pageSize < items.Count;
    }

    public IEnumerable<T> Next()
    {
        if (!HasNext())
        {
            throw new InvalidOperationException("No more pages");
        }

        currentPage++;
        int startIndex = currentPage * pageSize;
        int count = Math.Min(pageSize, items.Count - startIndex);

        Current = items.Skip(startIndex).Take(count);
        return Current;
    }

    public void Reset()
    {
        currentPage = -1;
        Current = [];
    }

    public IEnumerable<T> Current { get; private set; } = [];

    public int TotalPages => (int)Math.Ceiling((double)items.Count / pageSize);
    public int CurrentPageNumber => currentPage + 1;
}