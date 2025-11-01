namespace Snippets.DesignPatterns.Behavioral.ChainOfResponsibility;

public class TokenAuthHandler : BaseHandler<AuthRequest, AuthResponse>
{
    private readonly Dictionary<string, (string UserId, List<string> Roles, DateTime Expiry)> tokens = new()
    {
        ["token_admin_123"] = ("usr_001", ["admin", "user"], DateTime.UtcNow.AddHours(1)),
        ["token_user_456"] = ("usr_002", ["user"], DateTime.UtcNow.AddHours(1)),
        ["token_guest_789"] = ("usr_003", ["guest"], DateTime.UtcNow.AddMinutes(30))
    };

    protected override async Task<AuthResponse?> ProcessRequestAsync(AuthRequest request)
    {
        await Task.Delay(30);

        Console.WriteLine($"[Token Auth] Validating token: {request.Token}");

        if (string.IsNullOrEmpty(request.Token))
        {
            Console.WriteLine("[Token Auth] No token provided - passing to next handler");
            return null;
        }

        if (!tokens.TryGetValue(request.Token, out var tokenData))
        {
            Console.WriteLine($"[Token Auth] Invalid token");
            return AuthResponse.Failure("Invalid token", nameof(TokenAuthHandler));
        }

        if (DateTime.UtcNow > tokenData.Expiry)
        {
            Console.WriteLine($"[Token Auth] Token expired");
            return AuthResponse.Failure("Token expired", nameof(TokenAuthHandler));
        }

        Console.WriteLine($"[Token Auth] Token authentication successful");
        return AuthResponse.Success(tokenData.UserId, tokenData.Roles, nameof(TokenAuthHandler),
            "Token authentication successful");
    }
}