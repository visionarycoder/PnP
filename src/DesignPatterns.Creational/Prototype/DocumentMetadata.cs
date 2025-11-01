namespace Snippets.DesignPatterns.Creational.Prototype;

public class DocumentMetadata
{
    public string Author { get; set; } = "Unknown";
    public string Department { get; set; } = "General";
    public string Version { get; set; } = "1.0";
    public Dictionary<string, string> CustomProperties { get; set; } = new();
}