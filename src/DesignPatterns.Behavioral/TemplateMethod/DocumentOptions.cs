namespace Snippets.DesignPatterns.Behavioral.TemplateMethod;

public class DocumentOptions
{
    public bool IncludeHeader { get; set; } = true;
    public bool IncludeFooter { get; set; } = true;
    public bool IncludeTableOfContents { get; set; } = false;
    public string Format { get; set; } = "PDF";
    public Dictionary<string, object> CustomOptions { get; set; } = new();
}