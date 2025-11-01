namespace Snippets.DesignPatterns.Behavioral.TemplateMethod;

public class DocumentMetadata
{
    public string Title { get; set; } = "";
    public string Author { get; set; } = "";
    public DateTime CreatedDate { get; set; }
    public string Version { get; set; } = "1.0";
    public string Subject { get; set; } = "";
    public List<string> Keywords { get; set; } = [];
}