namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Visitor for validating document elements
/// </summary>
public class ValidationVisitor(ValidationOptions? options = null)
    : IDocumentVisitor<DocumentValidationResult>, ICombiningVisitor<DocumentValidationResult>
{
    private readonly ValidationOptions options = options ?? new ValidationOptions();

    public DocumentValidationResult Visit(Paragraph paragraph)
    {
        var result = new DocumentValidationResult { ElementType = "Paragraph", ElementId = paragraph.Id };

        if (string.IsNullOrWhiteSpace(paragraph.Content))
        {
            result.AddError("Paragraph content cannot be empty");
        }

        if (paragraph.Content.Length > options.MaxParagraphLength)
        {
            result.AddWarning($"Paragraph exceeds maximum length of {options.MaxParagraphLength} characters");
        }

        if (paragraph.FontSize < 8 || paragraph.FontSize > 72)
        {
            result.AddWarning($"Font size {paragraph.FontSize} may not be suitable");
        }

        return result;
    }

    public DocumentValidationResult Visit(Header header)
    {
        var result = new DocumentValidationResult { ElementType = "Header", ElementId = header.Id };

        if (string.IsNullOrWhiteSpace(header.Content))
        {
            result.AddError("Header content cannot be empty");
        }

        if (header.Level < 1 || header.Level > 6)
        {
            result.AddError($"Header level must be between 1 and 6, got {header.Level}");
        }

        if (header.Content.Length > options.MaxHeaderLength)
        {
            result.AddWarning($"Header exceeds maximum length of {options.MaxHeaderLength} characters");
        }

        return result;
    }

    public DocumentValidationResult Visit(Image image)
    {
        var result = new DocumentValidationResult { ElementType = "Image", ElementId = image.Id };

        if (string.IsNullOrWhiteSpace(image.Source))
        {
            result.AddError("Image source cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(image.AltText) && options.RequireAltText)
        {
            result.AddWarning("Image should have alt text for accessibility");
        }

        if (image.Width <= 0 || image.Height <= 0)
        {
            result.AddWarning("Image dimensions should be specified");
        }

        return result;
    }

    public DocumentValidationResult Visit(Table table)
    {
        var result = new DocumentValidationResult { ElementType = "Table", ElementId = table.Id };

        if (table.Rows <= 0 || table.Columns <= 0)
        {
            result.AddError("Table must have positive number of rows and columns");
        }

        if (table.Columns > options.MaxTableColumns)
        {
            result.AddWarning($"Table has {table.Columns} columns, which may be too many for readability");
        }

        if (table.Data.Any(row => row.Count != table.Columns))
        {
            result.AddError("All table rows must have the same number of columns");
        }

        if (table.Headers.Any() && table.Headers.Count != table.Columns)
        {
            result.AddError("Number of headers must match number of columns");
        }

        return result;
    }

    public DocumentValidationResult Visit(DocumentList list)
    {
        var result = new DocumentValidationResult { ElementType = "List", ElementId = list.Id };

        if (!list.Items.Any())
        {
            result.AddWarning("List has no items");
        }

        if (list.Items.Count > options.MaxListItems)
        {
            result.AddWarning($"List has {list.Items.Count} items, consider breaking into smaller lists");
        }

        if (list.Items.Any(string.IsNullOrWhiteSpace))
        {
            result.AddError("List items cannot be empty");
        }

        if (list.IndentLevel < 0)
        {
            result.AddError("List indent level cannot be negative");
        }

        return result;
    }

    public DocumentValidationResult Visit(ElementA element) => throw new NotSupportedException();
    public DocumentValidationResult Visit(ElementB element) => throw new NotSupportedException();
    public DocumentValidationResult Visit(ElementC element) => throw new NotSupportedException();

    public DocumentValidationResult CombineResults(List<DocumentValidationResult> results)
    {
        var combined = new DocumentValidationResult { ElementType = "Document", ElementId = "document" };

        foreach (var result in results)
        {
            combined.Errors.AddRange(result.Errors);
            combined.Warnings.AddRange(result.Warnings);
        }

        return combined;
    }
}