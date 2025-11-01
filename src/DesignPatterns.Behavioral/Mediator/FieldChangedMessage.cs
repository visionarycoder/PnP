namespace Snippets.DesignPatterns.Behavioral.Mediator;

public record FieldChangedMessage(string FieldName, object? Value);