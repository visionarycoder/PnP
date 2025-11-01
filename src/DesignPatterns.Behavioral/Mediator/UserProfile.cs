namespace Snippets.DesignPatterns.Behavioral.Mediator;

public record UserProfile(string DisplayName, string Status, Dictionary<string, object> Metadata);