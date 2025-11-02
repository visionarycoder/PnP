using System;
using System.Buffers;
using System.Linq;

namespace Snippets.CSharp.SpanOperations;

/// <summary>
/// Span-based formatting without allocations
/// </summary>
public static class SpanFormatters
{
    /// <summary>
    /// Format integer to span
    /// </summary>
    public static bool TryFormat(int value, Span<char> destination, out int charsWritten)
    {
        return value.TryFormat(destination, out charsWritten);
    }

    /// <summary>
    /// Format double with specific precision
    /// </summary>
    public static bool TryFormat(double value, Span<char> destination, out int charsWritten, int precision = 2)
    {
        return value.TryFormat(destination, out charsWritten, $"F{precision}".AsSpan());
    }

    /// <summary>
    /// Format DateTime
    /// </summary>
    public static bool TryFormat(DateTime value, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default)
    {
        if (format.IsEmpty)
            format = "yyyy-MM-dd HH:mm:ss".AsSpan();
            
        return value.TryFormat(destination, out charsWritten, format);
    }

    /// <summary>
    /// Join multiple formatted values
    /// </summary>
    public static bool TryJoinFormat<T>(ReadOnlySpan<T> values, ReadOnlySpan<char> separator, 
        Span<char> destination, out int charsWritten) where T : ISpanFormattable
    {
        charsWritten = 0;
        
        if (values.IsEmpty)
            return true;
            
        // Format first value
        if (!values[0].TryFormat(destination, out int written, ReadOnlySpan<char>.Empty, null))
            return false;
            
        charsWritten += written;
        
        // Format remaining values with separators
        for (int i = 1; i < values.Length; i++)
        {
            // Add separator
            if (charsWritten + separator.Length > destination.Length)
                return false;
                
            separator.CopyTo(destination.Slice(charsWritten));
            charsWritten += separator.Length;
            
            // Add value
            if (!values[i].TryFormat(destination.Slice(charsWritten), out written, ReadOnlySpan<char>.Empty, null))
                return false;
                
            charsWritten += written;
        }
        
        return true;
    }

    /// <summary>
    /// Format byte array as hex string
    /// </summary>
    public static bool TryFormatHex(ReadOnlySpan<byte> bytes, Span<char> destination, out int charsWritten, bool lowercase = false)
    {
        charsWritten = 0;
        
        if (bytes.Length * 2 > destination.Length)
            return false;
            
        var format = lowercase ? "x2" : "X2";
        
        for (int i = 0; i < bytes.Length; i++)
        {
            if (!bytes[i].TryFormat(destination.Slice(charsWritten, 2), out int written, format.AsSpan()))
                return false;
                
            charsWritten += written;
        }
        
        return true;
    }

    /// <summary>
    /// Build string using span operations
    /// </summary>
    public static string BuildString(int capacity, Action<SpanStringBuilder> buildAction)
    {
        var pool = ArrayPool<char>.Shared;
        var buffer = pool.Rent(capacity);
        
        try
        {
            var builder = new SpanStringBuilder(buffer.AsSpan());
            buildAction(builder);
            return new string(buffer, 0, builder.Length);
        }
        finally
        {
            pool.Return(buffer);
        }
    }
}