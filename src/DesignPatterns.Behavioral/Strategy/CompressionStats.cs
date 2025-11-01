namespace Snippets.DesignPatterns.Behavioral.Strategy;

public record CompressionStats(
    string Algorithm,
    int OriginalSize,
    int CompressedSize,
    double CompressionRatio,
    string Level)
{
    public double SpaceSavings => Math.Max(0, 1 - CompressionRatio);
    public string CompressionRatioPercent => $"{CompressionRatio:P1}";
    public string SpaceSavingsPercent => $"{SpaceSavings:P1}";
}