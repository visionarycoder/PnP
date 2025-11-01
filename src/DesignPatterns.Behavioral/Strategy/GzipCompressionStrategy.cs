using System.IO.Compression;

namespace Snippets.DesignPatterns.Behavioral.Strategy;

/// <summary>
/// GZIP compression strategy
/// </summary>
public class GzipCompressionStrategy(CompressionLevel level = CompressionLevel.Optimal) : ICompressionStrategy
{
    public string Name => "GZIP";
    public CompressionLevel Level { get; } = level;

    public byte[] Compress(byte[] data)
    {
        using var output = new MemoryStream();
        using (var gzipStream = new GZipStream(output, Level))
        {
            gzipStream.Write(data, 0, data.Length);
        }

        return output.ToArray();
    }

    public byte[] Decompress(byte[] compressedData)
    {
        using var input = new MemoryStream(compressedData);
        using var gzipStream = new GZipStream(input, CompressionMode.Decompress);
        using var output = new MemoryStream();
        gzipStream.CopyTo(output);
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