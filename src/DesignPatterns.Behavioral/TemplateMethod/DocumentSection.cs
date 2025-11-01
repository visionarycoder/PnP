namespace Snippets.DesignPatterns.Behavioral.TemplateMethod;

public class DocumentSection
{
    public SectionType Type { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public int Order { get; set; }
}