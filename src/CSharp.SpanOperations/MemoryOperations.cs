using System.Runtime.InteropServices;

namespace CSharp.SpanOperations;

/// <summary>
/// Memory operations for async scenarios and parallel processing
/// </summary>
public static class MemoryOperations
{
    /// <summary>
    /// Asynchronous memory read operations
    /// </summary>
    public static async Task<int> ReadAsync(Stream stream, Memory<byte> buffer)
    {
        return await stream.ReadAsync(buffer);
    }

    /// <summary>
    /// Asynchronous memory write operations
    /// </summary>
    public static async Task WriteAsync(Stream stream, ReadOnlyMemory<byte> buffer)
    {
        await stream.WriteAsync(buffer);
    }

    /// <summary>
    /// Split memory into chunks for parallel processing
    /// </summary>
    public static Memory<T>[] SplitIntoChunks<T>(Memory<T> memory, int chunkCount)
    {
        if (chunkCount <= 0)
            throw new ArgumentException("Chunk count must be positive");

        var chunks = new Memory<T>[chunkCount];
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

    /// <summary>
    /// Parallel processing of memory chunks
    /// </summary>
    public static async Task ProcessInParallelAsync<T>(
        Memory<T> memory, 
        Func<Memory<T>, Task> processor, 
        int degreeOfParallelism = -1)
    {
        if (degreeOfParallelism <= 0)
            degreeOfParallelism = Environment.ProcessorCount;

        var chunks = SplitIntoChunks(memory, degreeOfParallelism);
        var tasks = chunks.Select(processor);
        
        await Task.WhenAll(tasks);
    }

    /// <summary>
    /// Copy memory with overlap detection
    /// </summary>
    public static void SafeCopy<T>(ReadOnlyMemory<T> source, Memory<T> destination)
    {
        if (source.Length > destination.Length)
            throw new ArgumentException("Destination is too small");

        // Check for overlap (when both memories point to the same underlying array)
        if (MemoryMarshal.TryGetArray<T>(source, out var sourceArray) &&
            MemoryMarshal.TryGetArray<T>(destination, out var destArray) &&
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