using System.IO.Compression;

namespace Snippets.DesignPatterns.Behavioral.Strategy;

/// <summary>
/// Brotli compression strategy (modern, high-efficiency compression)
/// </summary>
public class BrotliCompressionStrategy(CompressionLevel level = CompressionLevel.Optimal) : ICompressionStrategy
{
    public string Name => "Brotli";
    public CompressionLevel Level { get; } = level;

    public byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var brotliStream = new BrotliStream(output, Level))
        {
            brotliStream.Write(data, 0, data.Length);
        }

        return output.ToArray();
    }

    public byte[] Decompress(byte[] compressedData)
    {
        using var input = new MemoryStream(compressedData);
        using var brotliStream = new BrotliStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        brotliStream.CopyTo(output);
        return output.ToArray();
    }

    public CompressionStats GetCompressionStats(byte[] original, byte[] compressed)
    {
        return new CompressionStats(
            Name,
            original.Length,
            compressed.Length,
            (double)compressed.Length / original.Length,
            Level.ToString());
    }
}