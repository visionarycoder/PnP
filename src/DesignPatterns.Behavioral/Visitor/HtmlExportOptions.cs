namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// HTML export options
/// </summary>
public class HtmlExportOptions
{
    public bool IncludeInlineStyles { get; set; } = true;
    public bool PrettyPrint { get; set; } = false;
    public bool IncludeDoctype { get; set; } = true;
}