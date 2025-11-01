namespace Snippets.DesignPatterns.Behavioral.ChainOfResponsibility;

public class AuthResponse
{
    public bool IsSuccessful { get; init; }
    public string Message { get; init; } = "";
    public string UserId { get; init; } = "";
    public List<string> UserRoles { get; init; } = [];
    public Dictionary<string, object> Claims { get; init; } = new();
    public string FailureReason { get; init; } = "";
    public string HandlerName { get; init; } = "";

    public static AuthResponse Success(string userId, List<string> roles, string handlerName,
        string message = "Authentication successful")
    {
        return new AuthResponse
        {
            IsSuccessful = true,
            Message = message,
            UserId = userId,
            UserRoles = roles,
            HandlerName = handlerName
        };
    }

    public static AuthResponse Failure(string reason, string handlerName)
    {
        return new AuthResponse
        {
            IsSuccessful = false,
            FailureReason = reason,
            HandlerName = handlerName,
            Message = $"Authentication failed: {reason}"
        };
    }
}