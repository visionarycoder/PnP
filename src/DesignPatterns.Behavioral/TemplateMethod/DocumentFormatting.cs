namespace Snippets.DesignPatterns.Behavioral.TemplateMethod;

public class DocumentFormatting
{
    public string FontFamily { get; set; } = "Arial";
    public int FontSize { get; set; } = 12;
    public string PageSize { get; set; } = "A4";
    public Dictionary<string, object> Styles { get; set; } = new();
}