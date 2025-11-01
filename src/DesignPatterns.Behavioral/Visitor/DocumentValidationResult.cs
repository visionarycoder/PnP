namespace Snippets.DesignPatterns.Behavioral.Visitor;

/// <summary>
/// Validation result for document elements
/// </summary>
public class DocumentValidationResult
{
    public string ElementType { get; set; } = "";
    public string ElementId { get; set; } = "";
    public List<string> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];

    public bool IsValid => !Errors.Any();
    public bool HasWarnings => Warnings.Any();

    public void AddError(string error) => Errors.Add($"[{ElementType} {ElementId}] {error}");
    public void AddWarning(string warning) => Warnings.Add($"[{ElementType} {ElementId}] {warning}");

    public override string ToString()
    {
        var result = $"{ElementType} {ElementId}: ";
        if (IsValid && !HasWarnings)
        {
            result += "OK";
        }
        else
        {
            result += $"{Errors.Count} errors, {Warnings.Count} warnings";
        }

        return result;
    }
}