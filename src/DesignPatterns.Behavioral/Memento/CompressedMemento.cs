using System.IO.Compression;
using System.Text;
using System.Text.Json;

namespace Snippets.DesignPatterns.Behavioral.Memento;

/// <summary>
/// Compressed memento for large states using GZIP compression
/// </summary>
public class CompressedMemento : IMemento
{
    public byte[] CompressedData { get; }
    public DateTime CreatedAt { get; }
    public string Description { get; }
    public long OriginalSize { get; }
    public long CompressedSize => CompressedData.Length;
    public double CompressionRatio => (double)CompressedSize / OriginalSize;

    public CompressedMemento(object state, string description)
    {
        CreatedAt = DateTime.UtcNow;
        Description = description;

        var json = JsonSerializer.Serialize(state);
        var originalBytes = Encoding.UTF8.GetBytes(json);
        OriginalSize = originalBytes.Length;

        using var inputStream = new MemoryStream(originalBytes);
        using var outputStream = new MemoryStream();
        using (var compressionStream = new GZipStream(outputStream, CompressionMode.Compress))
        {
            inputStream.CopyTo(compressionStream);
        }

        CompressedData = outputStream.ToArray();
        Console.WriteLine($"Compressed {OriginalSize} bytes to {CompressedSize} bytes ({CompressionRatio:P1})");
    }

    public T Decompress<T>()
    {
        using var inputStream = new MemoryStream(CompressedData);
        using var compressionStream = new GZipStream(inputStream, CompressionMode.Decompress);
        using var outputStream = new MemoryStream();

        compressionStream.CopyTo(outputStream);
        var decompressedBytes = outputStream.ToArray();
        var json = Encoding.UTF8.GetString(decompressedBytes);

        return JsonSerializer.Deserialize<T>(json) ??
               throw new InvalidOperationException("Failed to deserialize memento");
    }
}