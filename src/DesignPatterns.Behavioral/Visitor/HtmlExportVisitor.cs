using System.Text;

namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Visitor for exporting documents to HTML format
/// </summary>
public class HtmlExportVisitor(HtmlExportOptions? options = null) : IDocumentVisitor<string>, ICombiningVisitor<string>
{
    private readonly HtmlExportOptions options = options ?? new HtmlExportOptions();

    public string Visit(Paragraph paragraph)
    {
        var style = options.IncludeInlineStyles
            ? $" style=\"font-family: {paragraph.FontFamily}; font-size: {paragraph.FontSize}px;{(paragraph.IsJustified ? " text-align: justify;" : "")}\""
            : "";

        return $"<p{style}>{EscapeHtml(paragraph.Content)}</p>";
    }

    public string Visit(Header header)
    {
        var anchor = !string.IsNullOrEmpty(header.Anchor) ? $" id=\"{header.Anchor}\"" : "";
        return $"<h{header.Level}{anchor}>{EscapeHtml(header.Content)}</h{header.Level}>";
    }

    public string Visit(Image image)
    {
        var alt = !string.IsNullOrEmpty(image.AltText) ? $" alt=\"{EscapeHtml(image.AltText)}\"" : "";
        var dimensions = image.Width > 0 && image.Height > 0
            ? $" width=\"{image.Width}\" height=\"{image.Height}\""
            : "";

        return $"<img src=\"{EscapeHtml(image.Source)}\"{alt}{dimensions} />";
    }

    public string Visit(Table table)
    {
        var html = new StringBuilder("<table>");

        if (table.Headers.Any())
        {
            html.Append("<thead><tr>");
            foreach (var header in table.Headers)
            {
                html.Append($"<th>{EscapeHtml(header)}</th>");
            }

            html.Append("</tr></thead>");
        }

        html.Append("<tbody>");
        foreach (var row in table.Data)
        {
            html.Append("<tr>");
            foreach (var cell in row)
            {
                html.Append($"<td>{EscapeHtml(cell)}</td>");
            }

            html.Append("</tr>");
        }

        html.Append("</tbody></table>");

        return html.ToString();
    }

    public string Visit(DocumentList list)
    {
        var tag = list.Type == ListType.Ordered ? "ol" : "ul";
        var indent = new string(' ', list.IndentLevel * 4);

        var html = new StringBuilder($"{indent}<{tag}>\n");
        foreach (var item in list.Items)
        {
            html.AppendLine($"{indent}  <li>{EscapeHtml(item)}</li>");
        }

        html.Append($"{indent}</{tag}>");

        return html.ToString();
    }

    public string Visit(ElementA element) => throw new NotSupportedException();
    public string Visit(ElementB element) => throw new NotSupportedException();
    public string Visit(ElementC element) => throw new NotSupportedException();

    public string CombineResults(List<string> results)
    {
        var html = new StringBuilder();

        if (options.IncludeDoctype)
        {
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("  <meta charset=\"utf-8\">");
            html.AppendLine("  <title>Document</title>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
        }

        foreach (var result in results)
        {
            html.AppendLine(options.PrettyPrint ? "  " + result : result);
        }

        if (options.IncludeDoctype)
        {
            html.AppendLine("</body>");
            html.Append("</html>");
        }

        return html.ToString();
    }

    private string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}