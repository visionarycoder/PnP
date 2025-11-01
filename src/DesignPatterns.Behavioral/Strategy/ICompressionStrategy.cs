using System.IO.Compression;

namespace Snippets.DesignPatterns.Behavioral.Strategy;

/// <summary>
/// Data compression strategy interface
/// </summary>
public interface ICompressionStrategy
{
    string Name { get; }
    CompressionLevel Level { get; }
    byte[] Compress(byte[] data);
    byte[] Decompress(byte[] compressedData);
    CompressionStats GetCompressionStats(byte[] original, byte[] compressed);
}