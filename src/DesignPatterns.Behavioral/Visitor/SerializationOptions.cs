namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Serialization options
/// </summary>
public class SerializationOptions
{
    public bool PrettyPrint { get; set; } = true;
    public bool IncludeTypeInfo { get; set; } = true;
    public bool IncludeMetadata { get; set; } = false;
}