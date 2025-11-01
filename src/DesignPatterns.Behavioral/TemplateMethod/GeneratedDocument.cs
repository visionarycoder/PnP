namespace Snippets.DesignPatterns.Behavioral.TemplateMethod;

public class GeneratedDocument
{
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public DocumentMetadata Metadata { get; set; } = new();
    public DocumentFormatting Formatting { get; set; } = new();
    public int PageCount { get; set; }
    public bool Success { get; set; } = true;
    public string? Error { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
    public List<string> Logs { get; set; } = [];
}