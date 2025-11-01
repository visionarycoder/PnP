namespace Snippets.DesignPatterns.Behavioral.Mediator;

public record ChatMessage(string Content, string SenderId, DateTime Timestamp, string? TargetId = null);