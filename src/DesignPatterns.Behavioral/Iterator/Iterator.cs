namespace Snippets.DesignPatterns.Behavioral.Iterator;

// Generic iterator interface

// Iterable interface

// Basic iterator implementation
public abstract class Iterator<T>(IList<T> items) : IIterator<T>
{
    protected int Position = -1;
    protected readonly IList<T> Items = items ?? throw new ArgumentNullException(nameof(items));

    public virtual bool HasNext()
    {
        return Position + 1 < Items.Count;
    }

    public virtual T Next()
    {
        if (!HasNext())
        {
            throw new InvalidOperationException("No more elements");
        }

        Position++;
        return Items[Position];
    }

    public virtual void Reset()
    {
        Position = -1;
    }

    public virtual T Current
    {
        get
        {
            if (Position < 0 || Position >= Items.Count)
            {
                throw new InvalidOperationException("Iterator is not positioned on a valid element");
            }

            return Items[Position];
        }
    }
}

// Forward iterator

// Reverse iterator

// Skip iterator (every nth element)

// Filter iterator

// Transform iterator

// Custom collection class

// Tree Iterator Example - Depth-First Traversal

// Matrix Iterator Example

// Pagination Iterator