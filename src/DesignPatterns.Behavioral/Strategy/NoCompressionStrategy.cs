using System.IO.Compression;

namespace Snippets.DesignPatterns.Behavioral.Strategy;

/// <summary>
/// No compression strategy - returns data unchanged
/// </summary>
public class NoCompressionStrategy : ICompressionStrategy
{
    public string Name => "None";
    public CompressionLevel Level => CompressionLevel.NoCompression;

    public byte[] Compress(byte[] data) => data;

    public byte[] Decompress(byte[] compressedData) => compressedData;

    public CompressionStats GetCompressionStats(byte[] original, byte[] compressed)
    {
        return new CompressionStats(
            Name,
            original.Length,
            compressed.Length,
            1.0,
            "None");
    }
}