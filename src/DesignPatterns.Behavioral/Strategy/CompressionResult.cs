namespace Snippets.DesignPatterns.Behavioral.Strategy;

public record CompressionResult(
    string Algorithm,
    byte[] OriginalData,
    byte[] CompressedData,
    byte[] DecompressedData,
    CompressionStats Stats,
    long CompressionTimeMs,
    long DecompressionTimeMs,
    bool IsValid);