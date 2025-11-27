using System.Buffers;

namespace CSharp.SpanOperations;

/// <summary>
/// File I/O operations using Memory for async scenarios
/// </summary>
public static class SpanFileOperations
{
    /// <summary>
    /// Read file in chunks using Memory
    /// </summary>
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

    /// <summary>
    /// Process text file line by line without allocations
    /// </summary>
    public static async Task ProcessTextFileAsync(string filePath, Action<ReadOnlySpan<char>> lineProcessor)
    {
        using var reader = File.OpenText(filePath);
        
        string? line;
        while ((line = await reader.ReadLineAsync()) != null)
        {
            lineProcessor(line.AsSpan());
        }
    }

    /// <summary>
    /// Write data using Memory
    /// </summary>
    public static async Task WriteDataAsync(string filePath, IAsyncEnumerable<ReadOnlyMemory<byte>> dataChunks)
    {
        using var file = File.Create(filePath);
        
        await foreach (var chunk in dataChunks)
        {
            await file.WriteAsync(chunk);
        }
    }

    /// <summary>
    /// Copy file using spans for better performance
    /// </summary>
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