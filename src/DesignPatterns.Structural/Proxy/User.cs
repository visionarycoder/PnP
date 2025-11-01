namespace Snippets.DesignPatterns.Structural.Proxy;

public class User
{
    public string Username { get; init; } = "";
    public UserRole Role { get; init; }
    public DateTime LastLogin { get; init; }
}