using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Snippets.CSharp.SpanOperations;

/// <summary>
/// Span-based string operations for zero-allocation string processing
/// </summary>
public static class SpanStringExtensions
{
    /// <summary>
    /// Split string using span without allocations
    /// </summary>
    public static SpanSplitEnumerator Split(this ReadOnlySpan<char> span, char separator)
    {
        return new SpanSplitEnumerator(span, separator);
    }

    /// <summary>
    /// Split with multiple separators
    /// </summary>
    public static SpanSplitEnumerator Split(this ReadOnlySpan<char> span, ReadOnlySpan<char> separators)
    {
        return new SpanSplitEnumerator(span, separators);
    }

    /// <summary>
    /// Trim whitespace using span
    /// </summary>
    public static ReadOnlySpan<char> TrimFast(this ReadOnlySpan<char> span)
    {
        int start = 0;
        int end = span.Length - 1;

        // Trim start
        while (start < span.Length && char.IsWhiteSpace(span[start]))
            start++;

        // Trim end
        while (end >= start && char.IsWhiteSpace(span[end]))
            end--;

        return start > end ? ReadOnlySpan<char>.Empty : span.Slice(start, end - start + 1);
    }

    /// <summary>
    /// Case-insensitive equals using span
    /// </summary>
    public static bool EqualsIgnoreCase(this ReadOnlySpan<char> span, ReadOnlySpan<char> other)
    {
        return span.Equals(other, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Contains check with case sensitivity options
    /// </summary>
    public static bool ContainsFast(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, 
        StringComparison comparison = StringComparison.Ordinal)
    {
        return span.IndexOf(value, comparison) >= 0;
    }

    /// <summary>
    /// Count occurrences without allocation
    /// </summary>
    public static int CountOccurrences(this ReadOnlySpan<char> span, char character)
    {
        int count = 0;
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] == character)
                count++;
        }
        return count;
    }

    /// <summary>
    /// Replace characters in-place (mutable span)
    /// </summary>
    public static void ReplaceInPlace(this Span<char> span, char oldChar, char newChar)
    {
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] == oldChar)
                span[i] = newChar;
        }
    }

    /// <summary>
    /// Reverse string in-place
    /// </summary>
    public static void ReverseInPlace(this Span<char> span)
    {
        span.Reverse();
    }

    /// <summary>
    /// Convert to uppercase in-place
    /// </summary>
    public static void ToUpperInPlace(this Span<char> span)
    {
        for (int i = 0; i < span.Length; i++)
        {
            span[i] = char.ToUpperInvariant(span[i]);
        }
    }

    /// <summary>
    /// Parse integer from span without allocation
    /// </summary>
    public static bool TryParseInt32(this ReadOnlySpan<char> span, out int result)
    {
        return int.TryParse(span, out result);
    }

    /// <summary>
    /// Parse double from span without allocation
    /// </summary>
    public static bool TryParseDouble(this ReadOnlySpan<char> span, out double result)
    {
        return double.TryParse(span, out result);
    }

    /// <summary>
    /// Join strings with separator using spans
    /// </summary>
    public static int Join(string[] strings, ReadOnlySpan<char> separator, Span<char> destination)
    {
        if (strings.Length == 0)
            return 0;

        int written = 0;

        // Copy first string
        var firstSpan = strings[0].AsSpan();
        if (firstSpan.Length <= destination.Length)
        {
            firstSpan.CopyTo(destination);
            written = firstSpan.Length;
        }
        else
        {
            return -1; // Not enough space
        }

        // Copy remaining strings with separators
        for (int i = 1; i < strings.Length; i++)
        {
            // Add separator
            if (written + separator.Length > destination.Length)
                return -1;

            separator.CopyTo(destination.Slice(written));
            written += separator.Length;

            // Add string
            var stringSpan = strings[i].AsSpan();
            if (written + stringSpan.Length > destination.Length)
                return -1;

            stringSpan.CopyTo(destination.Slice(written));
            written += stringSpan.Length;
        }

        return written;
    }
}