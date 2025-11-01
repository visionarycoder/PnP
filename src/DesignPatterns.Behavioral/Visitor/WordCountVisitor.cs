namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Visitor for counting words in document elements
/// </summary>
public class WordCountVisitor : IDocumentVisitor<int>, ICombiningVisitor<int>
{
    public int Visit(Paragraph paragraph)
    {
        return CountWords(paragraph.Content);
    }

    public int Visit(Header header)
    {
        return CountWords(header.Content);
    }

    public int Visit(Image image)
    {
        return CountWords(image.AltText);
    }

    public int Visit(Table table)
    {
        var count = table.Headers.Sum(h => CountWords(h));
        count += table.Data.SelectMany(row => row).Sum(cell => CountWords(cell));
        return count;
    }

    public int Visit(DocumentList list)
    {
        return list.Items.Sum(item => CountWords(item));
    }

    public int Visit(ElementA element) => throw new NotSupportedException();
    public int Visit(ElementB element) => throw new NotSupportedException();
    public int Visit(ElementC element) => throw new NotSupportedException();

    public int CombineResults(List<int> results)
    {
        return results.Sum();
    }

    private int CountWords(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return 0;

        return text.Split([' ', '\t', '\n', '\r'], StringSplitOptions.RemoveEmptyEntries).Length;
    }
}