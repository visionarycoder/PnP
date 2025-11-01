namespace Snippets.DesignPatterns.Behavioral.Strategy;

public record ValidationResult(
    bool IsValid,
    string Message,
    string Code,
    string[]? Warnings = null)
{
    public bool HasWarnings => Warnings?.Length > 0;
}