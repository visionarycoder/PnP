using System.IO.Compression;

namespace Snippets.DesignPatterns.Behavioral.Strategy;

/// <summary>
/// Deflate compression strategy
/// </summary>
public class DeflateCompressionStrategy(CompressionLevel level = CompressionLevel.Optimal) : ICompressionStrategy
{
    public string Name => "Deflate";
    public CompressionLevel Level { get; } = level;

    public byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var deflateStream = new DeflateStream(output, Level))
        {
            deflateStream.Write(data, 0, data.Length);
        }

        return output.ToArray();
    }

    public byte[] Decompress(byte[] compressedData)
    {
        using var input = new MemoryStream(compressedData);
        using var deflateStream = new DeflateStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        deflateStream.CopyTo(output);
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