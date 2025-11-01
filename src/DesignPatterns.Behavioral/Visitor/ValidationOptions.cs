namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Validation options for document elements
/// </summary>
public class ValidationOptions
{
    public int MaxParagraphLength { get; set; } = 5000;
    public int MaxHeaderLength { get; set; } = 200;
    public bool RequireAltText { get; set; } = true;
    public int MaxTableColumns { get; set; } = 20;
    public int MaxListItems { get; set; } = 100;
}