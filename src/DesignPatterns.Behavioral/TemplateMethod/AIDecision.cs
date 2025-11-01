namespace Snippets.DesignPatterns.Behavioral.TemplateMethod;

public class AiDecision
{
    public GameAction Action { get; set; } = new();
    public double Confidence { get; set; }
    public TimeSpan ProcessingTime { get; set; }
    public string Reasoning { get; set; } = "";
    public AiStatistics Statistics { get; set; } = new();
}