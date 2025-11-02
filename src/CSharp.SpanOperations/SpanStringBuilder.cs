using System;

namespace Snippets.CSharp.SpanOperations;

/// <summary>
/// High-performance string builder using span
/// </summary>
public ref struct SpanStringBuilder
{
    private readonly Span<char> buffer;
    private int length;

    public SpanStringBuilder(Span<char> buffer)
    {
        this.buffer = buffer;
        length = 0;
    }

    public int Length => length;
    public int Capacity => buffer.Length;
    public ReadOnlySpan<char> AsSpan() => buffer.Slice(0, length);

    public bool TryAppend(ReadOnlySpan<char> value)
    {
        if (length + value.Length > buffer.Length)
            return false;
            
        value.CopyTo(buffer.Slice(length));
        length += value.Length;
        return true;
    }

    public bool TryAppend(char value)
    {
        if (length >= buffer.Length)
            return false;
            
        buffer[length++] = value;
        return true;
    }

    public bool TryAppend<T>(T value) where T : ISpanFormattable
    {
        return value.TryFormat(buffer.Slice(length), out int charsWritten, ReadOnlySpan<char>.Empty, null) &&
               (length += charsWritten) <= buffer.Length;
    }

    public bool TryAppendLine(ReadOnlySpan<char> value)
    {
        return TryAppend(value) && TryAppend('\n');
    }

    public void Clear()
    {
        length = 0;
    }

    public override string ToString()
    {
        return new string(buffer.Slice(0, length));
    }
}