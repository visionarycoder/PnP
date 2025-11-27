namespace CSharp.CancellationPatterns;

/// <summary>
/// Represents processing progress information
/// </summary>
public record ProcessingProgress(int CompletedItems, int TotalItems)
{
    /// <summary>
    /// Gets the completion percentage (0-100)
    /// </summary>
    public double ProgressPercentage => TotalItems > 0 ? (double)CompletedItems / TotalItems * 100 : 0;

    /// <summary>
    /// Gets a formatted progress string
    /// </summary>
    public string ProgressText => $"{CompletedItems}/{TotalItems} ({ProgressPercentage:F1}%)";
}