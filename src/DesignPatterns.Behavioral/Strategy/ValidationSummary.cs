namespace Snippets.DesignPatterns.Behavioral.Strategy;

public record ValidationSummary(
    ValidationResult[] Results,
    bool AllValid,
    string[] AllWarnings);