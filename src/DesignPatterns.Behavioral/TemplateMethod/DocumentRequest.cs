namespace Snippets.DesignPatterns.Behavioral.TemplateMethod;

public class DocumentRequest
{
    public string? Title { get; set; }
    public string? Author { get; set; }
    public string? Subject { get; set; }
    public List<string> Keywords { get; set; } = [];
    public Dictionary<string, object> Data { get; set; } = new();
    public DocumentOptions Options { get; set; } = new();
}