namespace Snippets.DesignPatterns.Behavioral.TemplateMethod;

public class SituationAnalysis
{
    public string Summary { get; set; } = "";
    public int ThreatLevel { get; set; }
    public int OpportunityLevel { get; set; }
    public string RecommendedStrategy { get; set; } = "";
}