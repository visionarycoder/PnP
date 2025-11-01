namespace Snippets.DesignPatterns.Behavioral.TemplateMethod;

public class ProcessingStatistics
{
    public TimeSpan ProcessingTime { get; set; }
    public int InputCount { get; set; }
    public int ProcessedCount { get; set; }
    public int OutputCount { get; set; }
    public double SuccessRate => InputCount > 0 ? (double)OutputCount / InputCount : 0;

    public void Reset()
    {
        ProcessingTime = TimeSpan.Zero;
        InputCount = ProcessedCount = OutputCount = 0;
    }
}