namespace Snippets.DesignPatterns.Behavioral.Mediator;

public record ValidationMessage(string FieldName, bool IsValid, string? ErrorMessage = null);