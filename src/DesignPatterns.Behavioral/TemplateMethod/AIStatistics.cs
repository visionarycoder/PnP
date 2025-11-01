namespace Snippets.DesignPatterns.Behavioral.TemplateMethod;

public class AiStatistics
{
    public int TotalDecisions { get; set; }
    public TimeSpan AverageDecisionTime { get; set; }
    public List<string> ActionHistory { get; set; } = [];
    public int ErrorCount { get; set; }
}