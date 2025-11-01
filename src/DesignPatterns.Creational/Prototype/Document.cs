namespace Snippets.DesignPatterns.Creational.Prototype;

/// <summary>
/// Example complex object that can be expensive to create
/// </summary>
public class Document() : DeepCloneableBase<Document>
{
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.Now;
    public DateTime ModifiedDate { get; set; } = DateTime.Now;
    public DocumentMetadata Metadata { get; set; } = new();
    public List<string> Tags { get; set; } = [];
    public List<DocumentSection> Sections { get; set; } = [];

    public Document(string title, string content) : this()
    {
        Title = title;
        Content = content;
    }

    /// <summary>
    /// Custom clone method with specific behavior
    /// </summary>
    public override Document Clone()
    {
        // Use base JSON serialization for deep copy
        var cloned = base.Clone();

        // Update specific properties for the clone
        cloned.CreatedDate = DateTime.Now;
        cloned.ModifiedDate = DateTime.Now;
        cloned.Title = $"Copy of {cloned.Title}";

        return cloned;
    }

    /// <summary>
    /// Shallow clone method (implements ICloneable)
    /// </summary>
    public Document ShallowClone()
    {
        return (Document)MemberwiseClone();
    }

    public void AddSection(string title, string content)
    {
        Sections.Add(new DocumentSection { Title = title, Content = content });
        ModifiedDate = DateTime.Now;
    }

    public override string ToString()
    {
        return $"Document: {Title} (Created: {CreatedDate:yyyy-MM-dd}, Sections: {Sections.Count}, Tags: {Tags.Count})";
    }
}