namespace CSharp.SpanOperations;

/// <summary>
/// Enumerator for splitting spans without allocation
/// </summary>
public ref struct SpanSplitEnumerator
{
    private ReadOnlySpan<char> span;
    private readonly ReadOnlySpan<char> separators;
    private readonly char separator;
    private readonly bool useMultipleSeparators;

    public SpanSplitEnumerator(ReadOnlySpan<char> span, char separator)
    {
        this.span = span;
        this.separator = separator;
        separators = default;
        useMultipleSeparators = false;
        Current = default;
    }

    public SpanSplitEnumerator(ReadOnlySpan<char> span, ReadOnlySpan<char> separators)
    {
        this.span = span;
        separator = default;
        this.separators = separators;
        useMultipleSeparators = true;
        Current = default;
    }

    public ReadOnlySpan<char> Current { get; private set; }

    public SpanSplitEnumerator GetEnumerator() => this;

    public bool MoveNext()
    {
        if (span.IsEmpty)
        {
            Current = default;
            return false;
        }

        int index = useMultipleSeparators ? 
            span.IndexOfAny(separators) : 
            span.IndexOf(separator);

        if (index == -1)
        {
            Current = span;
            span = ReadOnlySpan<char>.Empty;
            return true;
        }

        Current = span.Slice(0, index);
        span = span.Slice(index + 1);
        return true;
    }
}