# Span and Memory Operations

**Description**: High-performance memory operations using Span&lt;T&gt;, Memory&lt;T&gt;, and ReadOnlySpan&lt;T&gt; for zero-allocation algorithms, efficient string processing, and memory-safe operations without garbage collection overhead.

**Language/Technology**: C# / .NET

**Code**:

```csharp
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

// Span-based string operations for zero-allocation string processing
public static class SpanStringExtensions
{
    // Split string using span without allocations
    public static SpanSplitEnumerator Split(this ReadOnlySpan<char> span, char separator)
    {
        return new SpanSplitEnumerator(span, separator);
    }

    // Split with multiple separators
    public static SpanSplitEnumerator Split(this ReadOnlySpan<char> span, ReadOnlySpan<char> separators)
    {
        return new SpanSplitEnumerator(span, separators);
    }

    // Trim whitespace using span
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

    // Case-insensitive equals using span
    public static bool EqualsIgnoreCase(this ReadOnlySpan<char> span, ReadOnlySpan<char> other)
    {
        return span.Equals(other, StringComparison.OrdinalIgnoreCase);
    }

    // Contains check with case sensitivity options
    public static bool ContainsFast(this ReadOnlySpan<char> span, ReadOnlySpan<char> value, 
        StringComparison comparison = StringComparison.Ordinal)
    {
        return span.IndexOf(value, comparison) >= 0;
    }

    // Count occurrences without allocation
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

    // Replace characters in-place (mutable span)
    public static void ReplaceInPlace(this Span<char> span, char oldChar, char newChar)
    {
        for (int i = 0; i < span.Length; i++)
        {
            if (span[i] == oldChar)
                span[i] = newChar;
        }
    }

    // Reverse string in-place
    public static void ReverseInPlace(this Span<char> span)
    {
        span.Reverse();
    }

    // Convert to uppercase in-place
    public static void ToUpperInPlace(this Span<char> span)
    {
        for (int i = 0; i < span.Length; i++)
        {
            span[i] = char.ToUpperInvariant(span[i]);
        }
    }

    // Parse integer from span without allocation
    public static bool TryParseInt32(this ReadOnlySpan<char> span, out int result)
    {
        return int.TryParse(span, out result);
    }

    // Parse double from span without allocation
    public static bool TryParseDouble(this ReadOnlySpan<char> span, out double result)
    {
        return double.TryParse(span, out result);
    }

    // Join spans with separator
    public static int Join(ReadOnlySpan<ReadOnlySpan<char>> spans, ReadOnlySpan<char> separator, Span<char> destination)
    {
        if (spans.IsEmpty)
            return 0;

        int totalLength = 0;
        int written = 0;

        // Copy first span
        if (spans[0].Length <= destination.Length)
        {
            spans[0].CopyTo(destination);
            written = spans[0].Length;
        }
        else
        {
            return -1; // Not enough space
        }

        // Copy remaining spans with separators
        for (int i = 1; i < spans.Length; i++)
        {
            // Add separator
            if (written + separator.Length > destination.Length)
                return -1;

            separator.CopyTo(destination.Slice(written));
            written += separator.Length;

            // Add span
            if (written + spans[i].Length > destination.Length)
                return -1;

            spans[i].CopyTo(destination.Slice(written));
            written += spans[i].Length;
        }

        return written;
    }
}

// Enumerator for splitting spans without allocation
public ref struct SpanSplitEnumerator
{
    private ReadOnlySpan<char> _span;
    private readonly ReadOnlySpan<char> _separators;
    private readonly char _separator;
    private readonly bool _useMultipleSeparators;

    public SpanSplitEnumerator(ReadOnlySpan<char> span, char separator)
    {
        _span = span;
        _separator = separator;
        _separators = default;
        _useMultipleSeparators = false;
        Current = default;
    }

    public SpanSplitEnumerator(ReadOnlySpan<char> span, ReadOnlySpan<char> separators)
    {
        _span = span;
        _separator = default;
        _separators = separators;
        _useMultipleSeparators = true;
        Current = default;
    }

    public ReadOnlySpan<char> Current { get; private set; }

    public SpanSplitEnumerator GetEnumerator() => this;

    public bool MoveNext()
    {
        if (_span.IsEmpty)
        {
            Current = default;
            return false;
        }

        int index = _useMultipleSeparators ? 
            _span.IndexOfAny(_separators) : 
            _span.IndexOf(_separator);

        if (index == -1)
        {
            Current = _span;
            _span = ReadOnlySpan<char>.Empty;
            return true;
        }

        Current = _span.Slice(0, index);
        _span = _span.Slice(index + 1);
        return true;
    }
}

// Span-based numerical operations
public static class SpanNumerics
{
    // Sum array using span (vectorized when possible)
    public static int Sum(ReadOnlySpan<int> span)
    {
        if (Vector.IsHardwareAccelerated && span.Length >= Vector<int>.Count)
        {
            return SumVectorized(span);
        }

        int sum = 0;
        for (int i = 0; i < span.Length; i++)
        {
            sum += span[i];
        }
        return sum;
    }

    private static int SumVectorized(ReadOnlySpan<int> span)
    {
        var vectors = MemoryMarshal.Cast<int, Vector<int>>(span);
        var vectorSum = Vector<int>.Zero;

        for (int i = 0; i < vectors.Length; i++)
        {
            vectorSum += vectors[i];
        }

        int result = Vector.Dot(vectorSum, Vector<int>.One);

        // Handle remaining elements
        int remaining = span.Length % Vector<int>.Count;
        if (remaining > 0)
        {
            var remainingSpan = span.Slice(span.Length - remaining);
            for (int i = 0; i < remainingSpan.Length; i++)
            {
                result += remainingSpan[i];
            }
        }

        return result;
    }

    // Min/Max operations
    public static T Min&lt;T&gt;(ReadOnlySpan&lt;T&gt; span) where T : IComparable&lt;T&gt;
    {
        if (span.IsEmpty)
            throw new ArgumentException("Span cannot be empty");

        T min = span[0];
        for (int i = 1; i < span.Length; i++)
        {
            if (span[i].CompareTo(min) < 0)
                min = span[i];
        }
        return min;
    }

    public static T Max&lt;T&gt;(ReadOnlySpan&lt;T&gt; span) where T : IComparable&lt;T&gt;
    {
        if (span.IsEmpty)
            throw new ArgumentException("Span cannot be empty");

        T max = span[0];
        for (int i = 1; i < span.Length; i++)
        {
            if (span[i].CompareTo(max) > 0)
                max = span[i];
        }
        return max;
    }

    // Average calculation
    public static double Average(ReadOnlySpan<int> span)
    {
        return span.IsEmpty ? 0.0 : (double)Sum(span) / span.Length;
    }

    public static double Average(ReadOnlySpan<double> span)
    {
        if (span.IsEmpty)
            return 0.0;

        double sum = 0.0;
        for (int i = 0; i < span.Length; i++)
        {
            sum += span[i];
        }
        return sum / span.Length;
    }

    // Find index of min/max element
    public static int IndexOfMin&lt;T&gt;(ReadOnlySpan&lt;T&gt; span) where T : IComparable&lt;T&gt;
    {
        if (span.IsEmpty)
            return -1;

        int minIndex = 0;
        T min = span[0];

        for (int i = 1; i < span.Length; i++)
        {
            if (span[i].CompareTo(min) < 0)
            {
                min = span[i];
                minIndex = i;
            }
        }

        return minIndex;
    }

    public static int IndexOfMax&lt;T&gt;(ReadOnlySpan&lt;T&gt; span) where T : IComparable&lt;T&gt;
    {
        if (span.IsEmpty)
            return -1;

        int maxIndex = 0;
        T max = span[0];

        for (int i = 1; i < span.Length; i++)
        {
            if (span[i].CompareTo(max) > 0)
            {
                max = span[i];
                maxIndex = i;
            }
        }

        return maxIndex;
    }

    // Variance and standard deviation
    public static double Variance(ReadOnlySpan<double> span)
    {
        if (span.Length < 2)
            return 0.0;

        double mean = Average(span);
        double sumSquaredDiffs = 0.0;

        for (int i = 0; i < span.Length; i++)
        {
            double diff = span[i] - mean;
            sumSquaredDiffs += diff * diff;
        }

        return sumSquaredDiffs / (span.Length - 1);
    }

    public static double StandardDeviation(ReadOnlySpan<double> span)
    {
        return Math.Sqrt(Variance(span));
    }
}

// Span-based searching and sorting algorithms
public static class SpanAlgorithms
{
    // Binary search on sorted span
    public static int BinarySearch&lt;T&gt;(ReadOnlySpan&lt;T&gt; span, T value) where T : IComparable&lt;T&gt;
    {
        int left = 0;
        int right = span.Length - 1;

        while (left <= right)
        {
            int mid = left + (right - left) / 2;
            int comparison = span[mid].CompareTo(value);

            if (comparison == 0)
                return mid;
            else if (comparison < 0)
                left = mid + 1;
            else
                right = mid - 1;
        }

        return ~left; // Return bitwise complement for insertion point
    }

    // Quick sort implementation for spans
    public static void QuickSort&lt;T&gt;(Span&lt;T&gt; span) where T : IComparable&lt;T&gt;
    {
        if (span.Length <= 1)
            return;

        QuickSortRecursive(span, 0, span.Length - 1);
    }

    private static void QuickSortRecursive&lt;T&gt;(Span&lt;T&gt; span, int low, int high) where T : IComparable&lt;T&gt;
    {
        if (low < high)
        {
            int pivotIndex = Partition(span, low, high);
            QuickSortRecursive(span, low, pivotIndex - 1);
            QuickSortRecursive(span, pivotIndex + 1, high);
        }
    }

    private static int Partition&lt;T&gt;(Span&lt;T&gt; span, int low, int high) where T : IComparable&lt;T&gt;
    {
        T pivot = span[high];
        int i = low - 1;

        for (int j = low; j < high; j++)
        {
            if (span[j].CompareTo(pivot) <= 0)
            {
                i++;
                (span[i], span[j]) = (span[j], span[i]);
            }
        }

        (span[i + 1], span[high]) = (span[high], span[i + 1]);
        return i + 1;
    }

    // Insertion sort for small spans (more efficient for small arrays)
    public static void InsertionSort&lt;T&gt;(Span&lt;T&gt; span) where T : IComparable&lt;T&gt;
    {
        for (int i = 1; i < span.Length; i++)
        {
            T key = span[i];
            int j = i - 1;

            while (j >= 0 && span[j].CompareTo(key) > 0)
            {
                span[j + 1] = span[j];
                j--;
            }

            span[j + 1] = key;
        }
    }

    // Hybrid sort that chooses algorithm based on size
    public static void HybridSort&lt;T&gt;(Span&lt;T&gt; span) where T : IComparable&lt;T&gt;
    {
        if (span.Length <= 16)
        {
            InsertionSort(span);
        }
        else
        {
            QuickSort(span);
        }
    }

    // Find all indices where predicate is true
    public static void FindIndices&lt;T&gt;(ReadOnlySpan&lt;T&gt; span, Predicate&lt;T&gt; predicate, Span<int> indices, out int count)
    {
        count = 0;
        for (int i = 0; i < span.Length && count < indices.Length; i++)
        {
            if (predicate(span[i]))
            {
                indices[count++] = i;
            }
        }
    }

    // Count elements matching predicate
    public static int Count&lt;T&gt;(ReadOnlySpan&lt;T&gt; span, Predicate&lt;T&gt; predicate)
    {
        int count = 0;
        for (int i = 0; i < span.Length; i++)
        {
            if (predicate(span[i]))
                count++;
        }
        return count;
    }

    // Check if any element matches predicate
    public static bool Any&lt;T&gt;(ReadOnlySpan&lt;T&gt; span, Predicate&lt;T&gt; predicate)
    {
        for (int i = 0; i < span.Length; i++)
        {
            if (predicate(span[i]))
                return true;
        }
        return false;
    }

    // Check if all elements match predicate
    public static bool All&lt;T&gt;(ReadOnlySpan&lt;T&gt; span, Predicate&lt;T&gt; predicate)
    {
        for (int i = 0; i < span.Length; i++)
        {
            if (!predicate(span[i]))
                return false;
        }
        return true;
    }

    // Remove duplicates from sorted span (in-place)
    public static int RemoveDuplicates&lt;T&gt;(Span&lt;T&gt; span) where T : IEquatable&lt;T&gt;
    {
        if (span.Length <= 1)
            return span.Length;

        int writeIndex = 1;
        
        for (int readIndex = 1; readIndex < span.Length; readIndex++)
        {
            if (!span[readIndex].Equals(span[readIndex - 1]))
            {
                if (writeIndex != readIndex)
                {
                    span[writeIndex] = span[readIndex];
                }
                writeIndex++;
            }
        }

        return writeIndex;
    }
}

// Memory&lt;T&gt; operations for async scenarios
public static class MemoryOperations
{
    // Asynchronous memory operations
    public static async Task<int> ReadAsync(Stream stream, Memory<byte> buffer)
    {
        return await stream.ReadAsync(buffer);
    }

    public static async Task WriteAsync(Stream stream, ReadOnlyMemory<byte> buffer)
    {
        await stream.WriteAsync(buffer);
    }

    // Split memory into chunks for parallel processing
    public static Memory&lt;T&gt;[] SplitIntoChunks&lt;T&gt;(Memory&lt;T&gt; memory, int chunkCount)
    {
        if (chunkCount <= 0)
            throw new ArgumentException("Chunk count must be positive");

        var chunks = new Memory&lt;T&gt;[chunkCount];
        int chunkSize = memory.Length / chunkCount;
        int remainder = memory.Length % chunkCount;

        int offset = 0;
        for (int i = 0; i < chunkCount; i++)
        {
            int currentChunkSize = chunkSize + (i < remainder ? 1 : 0);
            chunks[i] = memory.Slice(offset, currentChunkSize);
            offset += currentChunkSize;
        }

        return chunks;
    }

    // Parallel processing of memory chunks
    public static async Task ProcessInParallelAsync&lt;T&gt;(
        Memory&lt;T&gt; memory, 
        Func<Memory&lt;T&gt;, Task> processor, 
        int degreeOfParallelism = -1)
    {
        if (degreeOfParallelism <= 0)
            degreeOfParallelism = Environment.ProcessorCount;

        var chunks = SplitIntoChunks(memory, degreeOfParallelism);
        var tasks = chunks.Select(processor);
        
        await Task.WhenAll(tasks);
    }

    // Copy memory with overlap detection
    public static void SafeCopy&lt;T&gt;(ReadOnlyMemory&lt;T&gt; source, Memory&lt;T&gt; destination)
    {
        if (source.Length > destination.Length)
            throw new ArgumentException("Destination is too small");

        // Check for overlap (when both memories point to the same underlying array)
        if (MemoryMarshal.TryGetArray(source, out var sourceArray) &&
            MemoryMarshal.TryGetArray(destination, out var destArray) &&
            ReferenceEquals(sourceArray.Array, destArray.Array))
        {
            // Use memmove-like behavior for overlapping regions
            var sourceSpan = source.Span;
            var destSpan = destination.Span;
            
            if (sourceArray.Offset < destArray.Offset)
            {
                // Copy forward
                for (int i = 0; i < source.Length; i++)
                {
                    destSpan[i] = sourceSpan[i];
                }
            }
            else
            {
                // Copy backward
                for (int i = source.Length - 1; i >= 0; i--)
                {
                    destSpan[i] = sourceSpan[i];
                }
            }
        }
        else
        {
            // No overlap, safe to use normal copy
            source.Span.CopyTo(destination.Span);
        }
    }
}

// High-performance parsers using spans
public static class SpanParsers
{
    // Parse CSV line without allocations
    public static void ParseCsvLine(ReadOnlySpan<char> line, Span<ReadOnlySpan<char>> fields, out int fieldCount)
    {
        fieldCount = 0;
        var remaining = line;
        
        while (!remaining.IsEmpty && fieldCount < fields.Length)
        {
            int commaIndex = remaining.IndexOf(',');
            
            if (commaIndex == -1)
            {
                // Last field
                fields[fieldCount++] = remaining.TrimFast();
                break;
            }
            
            fields[fieldCount++] = remaining.Slice(0, commaIndex).TrimFast();
            remaining = remaining.Slice(commaIndex + 1);
        }
    }

    // Parse key-value pairs (key=value format)
    public static bool TryParseKeyValue(ReadOnlySpan<char> line, out ReadOnlySpan<char> key, out ReadOnlySpan<char> value)
    {
        int equalIndex = line.IndexOf('=');
        
        if (equalIndex == -1 || equalIndex == 0 || equalIndex == line.Length - 1)
        {
            key = default;
            value = default;
            return false;
        }
        
        key = line.Slice(0, equalIndex).TrimFast();
        value = line.Slice(equalIndex + 1).TrimFast();
        return true;
    }

    // Parse integers from delimited string
    public static void ParseIntegers(ReadOnlySpan<char> text, char delimiter, Span<int> results, out int count)
    {
        count = 0;
        
        foreach (var part in text.Split(delimiter))
        {
            if (count >= results.Length)
                break;
                
            if (int.TryParse(part, out int value))
            {
                results[count++] = value;
            }
        }
    }

    // Parse floating-point numbers
    public static void ParseDoubles(ReadOnlySpan<char> text, char delimiter, Span<double> results, out int count)
    {
        count = 0;
        
        foreach (var part in text.Split(delimiter))
        {
            if (count >= results.Length)
                break;
                
            if (double.TryParse(part, out double value))
            {
                results[count++] = value;
            }
        }
    }

    // Parse hex string to bytes
    public static bool TryParseHex(ReadOnlySpan<char> hexString, Span<byte> bytes, out int bytesWritten)
    {
        bytesWritten = 0;
        
        if (hexString.Length % 2 != 0)
            return false;
            
        for (int i = 0; i < hexString.Length; i += 2)
        {
            if (bytesWritten >= bytes.Length)
                return false;
                
            var hexByte = hexString.Slice(i, 2);
            if (!byte.TryParse(hexByte, System.Globalization.NumberStyles.HexNumber, null, out byte value))
                return false;
                
            bytes[bytesWritten++] = value;
        }
        
        return true;
    }
}

// Span-based formatting without allocations
public static class SpanFormatters
{
    // Format integer to span
    public static bool TryFormat(int value, Span<char> destination, out int charsWritten)
    {
        return value.TryFormat(destination, out charsWritten);
    }

    // Format double with specific precision
    public static bool TryFormat(double value, Span<char> destination, out int charsWritten, int precision = 2)
    {
        return value.TryFormat(destination, out charsWritten, $"F{precision}".AsSpan());
    }

    // Format DateTime
    public static bool TryFormat(DateTime value, Span<char> destination, out int charsWritten, ReadOnlySpan<char> format = default)
    {
        if (format.IsEmpty)
            format = "yyyy-MM-dd HH:mm:ss".AsSpan();
            
        return value.TryFormat(destination, out charsWritten, format);
    }

    // Join multiple formatted values
    public static bool TryJoinFormat&lt;T&gt;(ReadOnlySpan&lt;T&gt; values, ReadOnlySpan<char> separator, 
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

    // Format byte array as hex string
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

    // Build string using span operations
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

// High-performance string builder using span
public ref struct SpanStringBuilder
{
    private readonly Span<char> _buffer;
    private int _length;

    public SpanStringBuilder(Span<char> buffer)
    {
        _buffer = buffer;
        _length = 0;
    }

    public int Length => _length;
    public int Capacity => _buffer.Length;
    public ReadOnlySpan<char> AsSpan() => _buffer.Slice(0, _length);

    public bool TryAppend(ReadOnlySpan<char> value)
    {
        if (_length + value.Length > _buffer.Length)
            return false;
            
        value.CopyTo(_buffer.Slice(_length));
        _length += value.Length;
        return true;
    }

    public bool TryAppend(char value)
    {
        if (_length >= _buffer.Length)
            return false;
            
        _buffer[_length++] = value;
        return true;
    }

    public bool TryAppend&lt;T&gt;(T value) where T : ISpanFormattable
    {
        return value.TryFormat(_buffer.Slice(_length), out int charsWritten, ReadOnlySpan<char>.Empty, null) &&
               (_length += charsWritten) <= _buffer.Length;
    }

    public bool TryAppendLine(ReadOnlySpan<char> value)
    {
        return TryAppend(value) && TryAppend('\n');
    }

    public void Clear()
    {
        _length = 0;
    }

    public override string ToString()
    {
        return new string(_buffer.Slice(0, _length));
    }
}

// File I/O operations using Memory&lt;T&gt;
public static class SpanFileOperations
{
    // Read file in chunks using Memory&lt;T&gt;
    public static async IAsyncEnumerable<ReadOnlyMemory<byte>> ReadFileChunksAsync(
        string filePath, int chunkSize = 4096)
    {
        using var file = File.OpenRead(filePath);
        var pool = ArrayPool<byte>.Shared;
        var buffer = pool.Rent(chunkSize);
        
        try
        {
            int bytesRead;
            while ((bytesRead = await file.ReadAsync(buffer.AsMemory())) > 0)
            {
                yield return buffer.AsMemory(0, bytesRead);
            }
        }
        finally
        {
            pool.Return(buffer);
        }
    }

    // Process text file line by line without allocations
    public static async Task ProcessTextFileAsync(string filePath, Action<ReadOnlySpan<char>> lineProcessor)
    {
        using var reader = File.OpenText(filePath);
        var pool = ArrayPool<char>.Shared;
        var buffer = pool.Rent(1024);
        
        try
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                lineProcessor(line.AsSpan());
            }
        }
        finally
        {
            pool.Return(buffer);
        }
    }

    // Write data using Memory&lt;T&gt;
    public static async Task WriteDataAsync(string filePath, IAsyncEnumerable<ReadOnlyMemory<byte>> dataChunks)
    {
        using var file = File.Create(filePath);
        
        await foreach (var chunk in dataChunks)
        {
            await file.WriteAsync(chunk);
        }
    }

    // Copy file using spans for better performance
    public static async Task CopyFileAsync(string sourcePath, string destinationPath, int bufferSize = 81920)
    {
        using var source = File.OpenRead(sourcePath);
        using var destination = File.Create(destinationPath);
        
        var pool = ArrayPool<byte>.Shared;
        var buffer = pool.Rent(bufferSize);
        
        try
        {
            int bytesRead;
            while ((bytesRead = await source.ReadAsync(buffer.AsMemory())) > 0)
            {
                await destination.WriteAsync(buffer.AsMemory(0, bytesRead));
            }
        }
        finally
        {
            pool.Return(buffer);
        }
    }
}

// Performance comparison utilities
public static class SpanPerformanceUtils
{
    // Benchmark span operations vs traditional approaches
    public static void BenchmarkStringSplit(string testString, int iterations = 10000)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // Traditional string.Split
        stopwatch.Restart();
        for (int i = 0; i < iterations; i++)
        {
            var parts = testString.Split(',');
            // Consume results to prevent optimization
            _ = parts.Length;
        }
        stopwatch.Stop();
        Console.WriteLine($"String.Split: {stopwatch.ElapsedMilliseconds}ms");
        
        // Span-based split
        stopwatch.Restart();
        for (int i = 0; i < iterations; i++)
        {
            int count = 0;
            foreach (var part in testString.AsSpan().Split(','))
            {
                count++;
            }
            // Consume results
            _ = count;
        }
        stopwatch.Stop();
        Console.WriteLine($"Span.Split: {stopwatch.ElapsedMilliseconds}ms");
    }

    // Benchmark numeric operations
    public static void BenchmarkNumericOperations(int[] data, int iterations = 1000)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        
        // LINQ Sum
        stopwatch.Restart();
        for (int i = 0; i < iterations; i++)
        {
            _ = data.Sum();
        }
        stopwatch.Stop();
        Console.WriteLine($"LINQ Sum: {stopwatch.ElapsedMilliseconds}ms");
        
        // Span Sum
        stopwatch.Restart();
        for (int i = 0; i < iterations; i++)
        {
            _ = SpanNumerics.Sum(data.AsSpan());
        }
        stopwatch.Stop();
        Console.WriteLine($"Span Sum: {stopwatch.ElapsedMilliseconds}ms");
    }

    // Memory allocation comparison
    public static void CompareAllocations()
    {
        const int iterations = 10000;
        
        Console.WriteLine("Allocation Comparison:");
        
        // Measure before
        var before = GC.GetTotalMemory(true);
        
        // Traditional approach (allocates strings)
        for (int i = 0; i < iterations; i++)
        {
            var text = $"Item {i}";
            var parts = text.Split(' ');
            var trimmed = parts[1].Trim();
        }
        
        var afterTraditional = GC.GetTotalMemory(false);
        
        // Force GC
        GC.Collect();
        GC.WaitForPendingFinalizers();
        var afterGC = GC.GetTotalMemory(true);
        
        // Span approach (minimal allocations)
        for (int i = 0; i < iterations; i++)
        {
            var text = $"Item {i}".AsSpan();
            foreach (var part in text.Split(' '))
            {
                var trimmed = part.TrimFast();
                break; // Just process first part for comparison
            }
        }
        
        var afterSpan = GC.GetTotalMemory(false);
        
        Console.WriteLine($"Traditional allocated: {afterTraditional - before:N0} bytes");
        Console.WriteLine($"Span allocated: {afterSpan - afterGC:N0} bytes");
        Console.WriteLine($"Reduction: {((double)(afterTraditional - before) / (afterSpan - afterGC)):F1}x less allocation");
    }
}
```

**Usage**:

```csharp
// Example 1: Zero-allocation string splitting
Console.WriteLine("Zero-allocation string splitting:");

var csvLine = "apple,banana,cherry,date,elderberry";
Console.WriteLine($"Original: {csvLine}");
Console.WriteLine("Split using span (no allocations):");

foreach (var part in csvLine.AsSpan().Split(','))
{
    Console.WriteLine($"  Part: '{part.ToString()}'");
}

// Example 2: String manipulation with spans
Console.WriteLine("\nString manipulation with spans:");

var text = "  Hello, World!  ";
var span = text.AsSpan();

var trimmed = span.TrimFast();
Console.WriteLine($"Trimmed: '{trimmed.ToString()}'");

Console.WriteLine($"Contains 'World': {trimmed.ContainsFast("World".AsSpan())}");
Console.WriteLine($"Comma count: {trimmed.CountOccurrences(',')}");

// In-place modifications using mutable span
var mutableText = "hello world".ToCharArray();
var mutableSpan = mutableText.AsSpan();

mutableSpan.ReplaceInPlace('l', 'L');
mutableSpan.ToUpperInPlace();
Console.WriteLine($"Modified: '{new string(mutableSpan)}'");

// Example 3: High-performance numerical operations
Console.WriteLine("\nHigh-performance numerical operations:");

var numbers = new int[] { 1, 5, 3, 9, 2, 8, 4, 7, 6 };
var numberSpan = numbers.AsSpan();

Console.WriteLine($"Numbers: [{string.Join(", ", numbers)}]");
Console.WriteLine($"Sum: {SpanNumerics.Sum(numberSpan)}");
Console.WriteLine($"Min: {SpanNumerics.Min(numberSpan)}");
Console.WriteLine($"Max: {SpanNumerics.Max(numberSpan)}");
Console.WriteLine($"Average: {SpanNumerics.Average(numberSpan):F2}");
Console.WriteLine($"Min index: {SpanNumerics.IndexOfMin(numberSpan)}");
Console.WriteLine($"Max index: {SpanNumerics.IndexOfMax(numberSpan)}");

// Example 4: Span-based algorithms
Console.WriteLine("\nSpan-based sorting algorithms:");

var unsorted = new int[] { 64, 34, 25, 12, 22, 11, 90 };
Console.WriteLine($"Original: [{string.Join(", ", unsorted)}]");

var forQuickSort = (int[])unsorted.Clone();
SpanAlgorithms.QuickSort(forQuickSort.AsSpan());
Console.WriteLine($"Quick sort: [{string.Join(", ", forQuickSort)}]");

var forInsertionSort = (int[])unsorted.Clone();
SpanAlgorithms.InsertionSort(forInsertionSort.AsSpan());
Console.WriteLine($"Insertion sort: [{string.Join(", ", forInsertionSort)}]");

var forHybridSort = (int[])unsorted.Clone();
SpanAlgorithms.HybridSort(forHybridSort.AsSpan());
Console.WriteLine($"Hybrid sort: [{string.Join(", ", forHybridSort)}]");

// Binary search
var sortedNumbers = new int[] { 1, 3, 5, 7, 9, 11, 13, 15 };
int searchValue = 7;
int index = SpanAlgorithms.BinarySearch(sortedNumbers.AsSpan(), searchValue);
Console.WriteLine($"Binary search for {searchValue}: index {index}");

// Example 5: Predicate-based operations
Console.WriteLine("\nPredicate operations:");

var testData = new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
var testSpan = testData.AsSpan();

var evenCount = SpanAlgorithms.Count(testSpan, x => x % 2 == 0);
Console.WriteLine($"Even numbers count: {evenCount}");

var hasLargeNumber = SpanAlgorithms.Any(testSpan, x => x > 8);
Console.WriteLine($"Has number > 8: {hasLargeNumber}");

var allPositive = SpanAlgorithms.All(testSpan, x => x > 0);
Console.WriteLine($"All positive: {allPositive}");

// Find indices of even numbers
var indices = new int[10];
SpanAlgorithms.FindIndices(testSpan, x => x % 2 == 0, indices.AsSpan(), out int evenIndicesCount);
Console.WriteLine($"Even number indices: [{string.Join(", ", indices.AsSpan(0, evenIndicesCount).ToArray())}]");

// Example 6: Memory operations for async scenarios
Console.WriteLine("\nMemory operations:");

var dataArray = Enumerable.Range(1, 12).ToArray();
var memory = dataArray.AsMemory();

// Split into chunks for parallel processing
var chunks = MemoryOperations.SplitIntoChunks(memory, 3);
Console.WriteLine($"Split into {chunks.Length} chunks:");

for (int i = 0; i < chunks.Length; i++)
{
    var chunkArray = chunks[i].ToArray();
    Console.WriteLine($"  Chunk {i + 1}: [{string.Join(", ", chunkArray)}]");
}

// Example 7: CSV parsing without allocations
Console.WriteLine("\nZero-allocation CSV parsing:");

var csvData = "John,25,Engineer,New York";
var fields = new ReadOnlySpan<char>[10]; // Pre-allocated span array

SpanParsers.ParseCsvLine(csvData.AsSpan(), fields.AsSpan(), out int fieldCount);

Console.WriteLine($"Parsed {fieldCount} fields from: {csvData}");
for (int i = 0; i < fieldCount; i++)
{
    Console.WriteLine($"  Field {i + 1}: '{fields[i].ToString()}'");
}

// Parse key-value pairs
var kvData = "name=Alice, age=30, city=Boston";
Console.WriteLine($"\nParsing key-value pairs from: {kvData}");

foreach (var pair in kvData.AsSpan().Split(','))
{
    if (SpanParsers.TryParseKeyValue(pair.TrimFast(), out var key, out var value))
    {
        Console.WriteLine($"  {key.ToString()} = {value.ToString()}");
    }
}

// Parse numbers from delimited string
var numberString = "10,20,30,40,50";
var parsedNumbers = new int[10];
SpanParsers.ParseIntegers(numberString.AsSpan(), ',', parsedNumbers.AsSpan(), out int numberCount);

Console.WriteLine($"Parsed {numberCount} integers: [{string.Join(", ", parsedNumbers.AsSpan(0, numberCount).ToArray())}]");

// Example 8: High-performance formatting
Console.WriteLine("\nHigh-performance formatting:");

var buffer = new char[100];
var bufferSpan = buffer.AsSpan();

// Format integers
if (SpanFormatters.TryFormat(12345, bufferSpan, out int written1))
{
    Console.WriteLine($"Formatted integer: '{new string(bufferSpan.Slice(0, written1))}'");
}

// Format double with precision
if (SpanFormatters.TryFormat(3.14159, bufferSpan, out int written2, precision: 3))
{
    Console.WriteLine($"Formatted double: '{new string(bufferSpan.Slice(0, written2))}'");
}

// Format DateTime
if (SpanFormatters.TryFormat(DateTime.Now, bufferSpan, out int written3))
{
    Console.WriteLine($"Formatted DateTime: '{new string(bufferSpan.Slice(0, written3))}'");
}

// Format byte array as hex
var bytes = new byte[] { 0xDE, 0xAD, 0xBE, 0xEF };
if (SpanFormatters.TryFormatHex(bytes.AsSpan(), bufferSpan, out int written4, lowercase: true))
{
    Console.WriteLine($"Hex format: '{new string(bufferSpan.Slice(0, written4))}'");
}

// Example 9: SpanStringBuilder for efficient string construction
Console.WriteLine("\nSpanStringBuilder usage:");

var result = SpanFormatters.BuildString(200, builder =>
{
    builder.TryAppend("Building a string: ");
    builder.TryAppend(DateTime.Now.Year);
    builder.TryAppend(" - ");
    builder.TryAppend(3.14159);
    builder.TryAppendLine(" (π)");
    
    for (int i = 1; i <= 5; i++)
    {
        builder.TryAppend("Item ");
        builder.TryAppend(i);
        if (i < 5) builder.TryAppend(", ");
    }
});

Console.WriteLine($"Built string: {result}");

// Example 10: Statistical operations on spans
Console.WriteLine("\nStatistical operations:");

var samples = new double[] { 1.5, 2.3, 3.7, 2.8, 4.1, 3.2, 2.9, 3.5, 4.0, 2.1 };
var sampleSpan = samples.AsSpan();

Console.WriteLine($"Samples: [{string.Join(", ", samples.Select(x => x.ToString("F1")))}]");
Console.WriteLine($"Average: {SpanNumerics.Average(sampleSpan):F2}");
Console.WriteLine($"Variance: {SpanNumerics.Variance(sampleSpan):F3}");
Console.WriteLine($"Std Dev: {SpanNumerics.StandardDeviation(sampleSpan):F3}");

// Example 11: Remove duplicates in-place
Console.WriteLine("\nRemove duplicates in-place:");

var duplicateData = new int[] { 1, 1, 2, 2, 2, 3, 4, 4, 5 };
Console.WriteLine($"Original: [{string.Join(", ", duplicateData)}]");

var uniqueCount = SpanAlgorithms.RemoveDuplicates(duplicateData.AsSpan());
Console.WriteLine($"After removing duplicates: [{string.Join(", ", duplicateData.AsSpan(0, uniqueCount).ToArray())}]");
Console.WriteLine($"Unique count: {uniqueCount}");

// Example 12: Joining strings with spans
Console.WriteLine("\nJoining strings with spans:");

var words = new string[] { "apple", "banana", "cherry" };
var wordSpans = words.Select(w => w.AsSpan()).ToArray();

var joinBuffer = new char[100];
var joinResult = SpanStringExtensions.Join(wordSpans.AsSpan(), " | ".AsSpan(), joinBuffer.AsSpan());

if (joinResult > 0)
{
    Console.WriteLine($"Joined: '{new string(joinBuffer.AsSpan(0, joinResult))}'");
}

// Example 13: Performance benchmarking
Console.WriteLine("\nPerformance benchmarking:");

var testString = string.Join(",", Enumerable.Range(1, 100).Select(i => $"item{i}"));
SpanPerformanceUtils.BenchmarkStringSplit(testString, 1000);

var largeArray = Enumerable.Range(1, 10000).ToArray();
SpanPerformanceUtils.BenchmarkNumericOperations(largeArray, 100);

// Example 14: Memory allocation comparison
Console.WriteLine("\nMemory allocation comparison:");
SpanPerformanceUtils.CompareAllocations();

// Example 15: Async file operations with Memory&lt;T&gt;
Console.WriteLine("\nAsync file operations:");

// Create a temporary file for demonstration
var tempFile = Path.GetTempFileName();
var testData = "Line 1: Hello\nLine 2: World\nLine 3: Span operations\nLine 4: Memory&lt;T&gt;";
await File.WriteAllTextAsync(tempFile, testData);

Console.WriteLine("Processing file line by line:");
int lineNumber = 0;
await SpanFileOperations.ProcessTextFileAsync(tempFile, line =>
{
    lineNumber++;
    Console.WriteLine($"  Line {lineNumber}: '{line.ToString()}'");
});

// Read file in chunks
Console.WriteLine("\nReading file in chunks:");
int chunkNumber = 0;
await foreach (var chunk in SpanFileOperations.ReadFileChunksAsync(tempFile, 10))
{
    chunkNumber++;
    var chunkText = System.Text.Encoding.UTF8.GetString(chunk.Span);
    Console.WriteLine($"  Chunk {chunkNumber}: '{chunkText.Replace('\n', '\\')}'");
}

// Cleanup
File.Delete(tempFile);

// Example 16: Advanced span operations
Console.WriteLine("\nAdvanced span operations:");

// Safe memory copy with overlap detection
var sourceData = new int[] { 1, 2, 3, 4, 5 };
var destData = new int[7];

MemoryOperations.SafeCopy(sourceData.AsMemory(), destData.AsMemory(2, 5));
Console.WriteLine($"Safe copy result: [{string.Join(", ", destData)}]");

// Vectorized sum demonstration
var largeNumbers = Enumerable.Range(1, 1000).ToArray();
var vectorSum = SpanNumerics.Sum(largeNumbers.AsSpan());
Console.WriteLine($"Vectorized sum of 1-1000: {vectorSum} (expected: {1000 * 1001 / 2})");

Console.WriteLine("\nSpan operations completed!");
```

**Notes**:

- Span&lt;T&gt; and Memory&lt;T&gt; provide zero-allocation, high-performance memory operations
- ReadOnlySpan&lt;T&gt; ensures memory safety while allowing efficient read operations
- Span-based string operations eliminate temporary string allocations during parsing
- Vectorized operations automatically use SIMD instructions when available
- Memory&lt;T&gt; is heap-allocatable and async-friendly, while Span&lt;T&gt; is stack-only
- SpanSplitEnumerator provides allocation-free string splitting with foreach support
- In-place operations modify data directly without creating copies
- Span algorithms often outperform LINQ for numerical and search operations
- Buffer pooling with spans minimizes garbage collection pressure
- Span formatters enable allocation-free string building for performance-critical scenarios
- File I/O with Memory&lt;T&gt; provides better async performance than byte arrays
- Performance monitoring shows significant allocation reduction compared to traditional approaches

**Prerequisites**:

- .NET Core 2.1+ or .NET Framework 4.7.1+ for Span&lt;T&gt; and Memory&lt;T&gt; support
- Understanding of memory management and reference semantics
- Knowledge of vectorization and SIMD for numerical operations
- Familiarity with async/await patterns for Memory&lt;T&gt; operations
- Performance profiling tools to measure allocation and throughput improvements

**Related Snippets**:

- [Memory Pools](memory-pools.md) - ArrayPool&lt;T&gt; and object pooling strategies
- [Vectorization](vectorization.md) - SIMD operations with Vector&lt;T&gt;
- [Performance LINQ](performance-linq.md) - High-performance LINQ operations
- [Micro Optimizations](micro-optimizations.md) - Low-level performance techniques
