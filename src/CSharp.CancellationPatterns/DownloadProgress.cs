namespace CSharp.CancellationPatterns;

/// <summary>
/// Represents download progress information
/// </summary>
public record DownloadProgress(long BytesDownloaded, long TotalBytes)
{
    public double ProgressPercentage => TotalBytes > 0 ? (double)BytesDownloaded / TotalBytes * 100 : 0;
}