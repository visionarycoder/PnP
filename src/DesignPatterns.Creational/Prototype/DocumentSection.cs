namespace Snippets.DesignPatterns.Creational.Prototype;

public class DocumentSection
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
}