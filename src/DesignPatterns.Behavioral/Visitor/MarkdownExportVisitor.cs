using System.Text;

namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Visitor for exporting documents to Markdown format
/// </summary>
public class MarkdownExportVisitor : IDocumentVisitor<string>, ICombiningVisitor<string>
{
    public string Visit(Paragraph paragraph)
    {
        return paragraph.Content + "\n";
    }

    public string Visit(Header header)
    {
        var prefix = new string('#', header.Level);
        return $"{prefix} {header.Content}\n";
    }

    public string Visit(Image image)
    {
        var alt = !string.IsNullOrEmpty(image.AltText) ? image.AltText : "Image";
        return $"![{alt}]({image.Source})\n";
    }

    public string Visit(Table table)
    {
        var md = new StringBuilder();

        if (table.Headers.Any())
        {
            md.AppendLine("| " + string.Join(" | ", table.Headers) + " |");
            md.AppendLine("| " + string.Join(" | ", table.Headers.Select(_ => "---")) + " |");
        }

        foreach (var row in table.Data)
        {
            md.AppendLine("| " + string.Join(" | ", row) + " |");
        }

        return md.ToString();
    }

    public string Visit(DocumentList list)
    {
        var md = new StringBuilder();
        var indent = new string(' ', list.IndentLevel * 2);
        var prefix = list.Type == ListType.Ordered ? "1." : "*";

        for (int i = 0; i < list.Items.Count; i++)
        {
            var itemPrefix = list.Type == ListType.Ordered ? $"{i + 1}." : "*";
            md.AppendLine($"{indent}{itemPrefix} {list.Items[i]}");
        }

        return md.ToString();
    }

    public string Visit(ElementA element) => throw new NotSupportedException();
    public string Visit(ElementB element) => throw new NotSupportedException();
    public string Visit(ElementC element) => throw new NotSupportedException();

    public string CombineResults(List<string> results)
    {
        return string.Join("\n", results);
    }
}