namespace Snippets.DesignPatterns.Behavioral.ChainOfResponsibility;

public class AuthRequest
{
    public string Username { get; init; } = "";
    public string Password { get; init; } = "";
    public string Token { get; init; } = "";
    public string IpAddress { get; init; } = "";
    public string UserAgent { get; init; } = "";
    public List<string> RequiredRoles { get; init; } = [];
    public string Resource { get; init; } = "";
    public Dictionary<string, object> Context { get; init; } = new();
}