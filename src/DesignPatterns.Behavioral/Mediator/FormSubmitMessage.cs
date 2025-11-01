namespace Snippets.DesignPatterns.Behavioral.Mediator;

public record FormSubmitMessage(Dictionary<string, object?> FormData);