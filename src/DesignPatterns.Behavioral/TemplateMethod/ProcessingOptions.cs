namespace Snippets.DesignPatterns.Behavioral.TemplateMethod;

public class ProcessingOptions
{
    public bool EnablePreProcessing { get; set; } = true;
    public bool EnablePostProcessing { get; set; } = true;
    public int BatchSize { get; set; } = 100;
    public bool ParallelProcessing { get; set; } = false;
    public Dictionary<string, object> CustomOptions { get; set; } = new();
}