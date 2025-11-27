namespace CSharp.CancellationPatterns;

/// <summary>
/// Represents work progress information
/// </summary>
public record WorkProgress(int CompletedSteps, int TotalSteps, string Message)
{
    public double ProgressPercentage => TotalSteps > 0 ? (double)CompletedSteps / TotalSteps * 100 : 0;
}