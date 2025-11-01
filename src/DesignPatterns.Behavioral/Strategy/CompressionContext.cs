using System.Diagnostics;

namespace Snippets.DesignPatterns.Behavioral.Strategy;

/// <summary>
/// Compression context for testing different compression strategies
/// </summary>
public class CompressionContext
{
    public CompressionResult CompressData(byte[] data, ICompressionStrategy strategy)
    {
        if (data == null)
            throw new ArgumentNullException(nameof(data));
        if (strategy == null)
            throw new ArgumentNullException(nameof(strategy));

        var stopwatch = Stopwatch.StartNew();
        var compressed = strategy.Compress(data);
        var compressionTime = stopwatch.ElapsedMilliseconds;

        stopwatch.Restart();
        var decompressed = strategy.Decompress(compressed);
        var decompressionTime = stopwatch.ElapsedMilliseconds;

        var stats = strategy.GetCompressionStats(data, compressed);
        var isValid = data.SequenceEqual(decompressed);

        return new CompressionResult(
            strategy.Name,
            data,
            compressed,
            decompressed,
            stats,
            compressionTime,
            decompressionTime,
            isValid);
    }

    public void CompareCompressionStrategies(byte[] data, params ICompressionStrategy[] strategies)
    {
        Console.WriteLine($"\n📦 Comparing Compression Algorithms (Data size: {data.Length:N0} bytes)");
        Console.WriteLine(new string('=', 95));
        Console.WriteLine(
            $"{"Algorithm",-12} {"Ratio",-8} {"Savings",-8} {"Comp(ms)",-9} {"Decomp(ms)",-11} {"Size",-12} {"Valid",-6}");
        Console.WriteLine(new string('-', 95));

        var results = new List<CompressionResult>();

        foreach (var strategy in strategies)
        {
            var result = CompressData(data, strategy);
            results.Add(result);

            Console.WriteLine($"{result.Algorithm,-12} " +
                              $"{result.Stats.CompressionRatioPercent,-8} " +
                              $"{result.Stats.SpaceSavingsPercent,-8} " +
                              $"{result.CompressionTimeMs,-9} " +
                              $"{result.DecompressionTimeMs,-11} " +
                              $"{result.CompressedData.Length:N0}".PadLeft(12) + " " +
                              $"{(result.IsValid ? "✅" : "❌"),-6}");
        }

        // Show best compression
        var bestRatio = results.OrderBy(r => r.Stats.CompressionRatio).First();
        Console.WriteLine(
            $"\n🏆 Best Compression: {bestRatio.Algorithm} ({bestRatio.Stats.SpaceSavingsPercent} space savings)");

        // Show fastest compression
        var fastest = results.OrderBy(r => r.CompressionTimeMs).First();
        Console.WriteLine($"⚡ Fastest Compression: {fastest.Algorithm} ({fastest.CompressionTimeMs}ms)");
    }
}